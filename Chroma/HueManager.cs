using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Q42.HueApi;
using Q42.HueApi.ColorConverters;
using Q42.HueApi.ColorConverters.HSB;
using Q42.HueApi.Streaming;
using Q42.HueApi.Streaming.Extensions;
using Q42.HueApi.Streaming.Models;
using CustomJSONData;
using static CustomJSONData.Trees;
using UnityEngine;
using static Chroma.Plugin;
using CustomJSONData.CustomBeatmap;
using SongCore;
using SongCore.Data;

namespace Chroma
{
    class HueManager
    {
        CustomBeatmapData customBeatmapData;
        public bool Ready => Settings.ChromaConfig.Instance.HueAppKey != "none" && Settings.ChromaConfig.Instance.HueClientKey != "none";
        private static async Task<string> FindBridge()
        {
            return (await new HttpBridgeLocator().LocateBridgesAsync(TimeSpan.FromSeconds(5))).FirstOrDefault()?.IpAddress;
        }
        public static async Task syncAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Debug.Log("Scanning network for bridge");
            string bridge;
            try
            {
                bridge = await FindBridge();
                if (bridge == null)
                {
                    Debug.Log("No bridge found!");
                    return;
                }
                token.ThrowIfCancellationRequested();
                Debug.Log(bridge);
                Debug.Log("Found bridge, pairing...");
                var keys = await LocalHueClient.RegisterAsync(bridge, "Chroma", "BeatSaber", true);
                Debug.Log("Bridge paired!");
                token.ThrowIfCancellationRequested();
                Settings.ChromaConfig.Instance.setAppKey(keys.Username);
                Settings.ChromaConfig.Instance.setClientKey(keys.StreamingClientKey);
                Debug.Log(keys.Username);
                Debug.Log(keys.StreamingClientKey);
                await connect(token, bridge);
            }
            catch (Exception)
            {
                Debug.Log("Something went wrong!");
                throw;
            }
        }
        public static async Task connect(CancellationToken token, string ip = null)
        {

            if (ip == null)
            {
                ip = await FindBridge();
            }
            token.ThrowIfCancellationRequested();
            var appKey = Settings.ChromaConfig.Instance.HueAppKey;
            var clientKey = Settings.ChromaConfig.Instance.HueClientKey;
            Debug.Log("Connecting to bridge...");
            var client = new StreamingHueClient(ip, appKey, clientKey);
            token.ThrowIfCancellationRequested();
            Debug.Log("Connected! Getting entertainment group...");
            var group = (await client.LocalHueClient.GetEntertainmentGroups()).ElementAtOrDefault(0);
            if (group == null)
            {
                Debug.Log("Group is missing!");
                return;
            }
            token.ThrowIfCancellationRequested();
            var entGroup = new StreamingGroup(group.Locations);
            Debug.Log("Found group! Connecting to lightbulbs...");
            await client.Connect(group.Id);
            token.ThrowIfCancellationRequested();
            Debug.Log("Connected to bulbs! Syncing...");
            _ = client.AutoUpdate(entGroup, token);
            var entLayer = entGroup.GetNewLayer(true);
            LightInfo.setInfo(client, entLayer, token);
            await Task.Delay(TimeSpan.FromMilliseconds(50), token);
            if (Settings.ChromaConfig.Instance.LowLight == true) { entLayer.SetState(token, new RGBColor(255, 255, 255), 0.5); }
        }
        public static async Task Disconnect(StreamingHueClient client)
        {
            if (LightInfo.client != null)
            {
                client.Close();
                LightInfo.disconnect();
                Debug.Log("Disconnected");
            }
        }
        private static Color? GetColorFromData(dynamic data, int col, string member = COLOR)
        {
            IEnumerable<float> color = ((List<object>)CustomJSONData.Trees.at(data, member))?.Select(n => Convert.ToSingle(n));
            if (color == null)
            {
                return null;
            }
            return new Color(color.ElementAt(col), color.ElementAt(1), color.ElementAt(2), color.Count() > 3 ? color.ElementAt(3) : 1);
            
        }


        public static async Task ProcessEvent(BeatmapEventData data, Color? raw = null)
        {
            CancellationToken token = LightInfo.token;
            EntertainmentLayer entLayer = LightInfo.layer;
            double r = 0;
            double g = 0;
            double b = 0;
            double brightness;

            if (raw.HasValue)
            {
                Color color = (Color)raw;
                r = color.r;
                g = color.g;
                b = color.b;
            }
            else if (data.value == 5 || data.value == 6 || data.value == 7)
            {
                r = 1;
            }
            else
            {
                b = 1;
            }

            var rgbColor = new RGBColor(r, g, b);
            var hsbColor = rgbColor.GetHSB();
            if (Settings.ChromaConfig.Instance.LowLight == true)
            {
                brightness = 0.5f;

                if (hsbColor.Brightness < 127) { hsbColor.Brightness = 127; }
            }
            else
            {
                brightness = 0;
            }

            if (data.type == BeatmapEventType.Event0)
            {
                if (Settings.ChromaConfig.Instance.BackLaserGroup == BackLaserGroup.NONE)
                { }
                if (Settings.ChromaConfig.Instance.BackLaserGroup == BackLaserGroup.LEFT)
                {
                    switch(data.value)
                    {
                        case 0: entLayer.GetLeft().SetState(token, null, brightness); break;
                        case 1: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.BackLaserGroup == BackLaserGroup.RIGHT)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.GetRight().SetState(token, null, brightness); break;
                        case 1: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetRight().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetRight().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetRight().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetRight().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.BackLaserGroup == BackLaserGroup.FRONT)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.GetFront().SetState(token, null, brightness); break;
                        case 1: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetFront().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetFront().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetFront().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetFront().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.BackLaserGroup == BackLaserGroup.BACK)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.GetBack().SetState(token, null, brightness); break;
                        case 1: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetBack().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetBack().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetBack().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetBack().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.BackLaserGroup == BackLaserGroup.CENTER)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.GetCenter().SetState(token, null, brightness); break;
                        case 1: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.BackLaserGroup == BackLaserGroup.ALL)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.SetState(token, null, brightness); break;
                        case 1: entLayer.SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
            }
            
            if (data.type == BeatmapEventType.Event1)
            {
                if (Settings.ChromaConfig.Instance.BigRingsGroup == BigRingsGroup.NONE)
                { }
                if (Settings.ChromaConfig.Instance.BigRingsGroup == BigRingsGroup.LEFT)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.GetLeft().SetState(token, null, brightness); break;
                        case 1: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.BigRingsGroup == BigRingsGroup.RIGHT)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.GetRight().SetState(token, null, brightness); break;
                        case 1: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetRight().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetRight().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetRight().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetRight().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.BigRingsGroup == BigRingsGroup.FRONT)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.GetFront().SetState(token, null, brightness); break;
                        case 1: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetFront().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetFront().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetFront().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetFront().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.BigRingsGroup == BigRingsGroup.BACK)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.GetBack().SetState(token, null, brightness); break;
                        case 1: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetBack().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetBack().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetBack().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetBack().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.BigRingsGroup == BigRingsGroup.CENTER)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.GetCenter().SetState(token, null, brightness); break;
                        case 1: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.BigRingsGroup == BigRingsGroup.ALL)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.SetState(token, null, brightness); break;
                        case 1: entLayer.SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
            }
            if (data.type == BeatmapEventType.Event2)
            {
                if (Settings.ChromaConfig.Instance.LeftRotatingLaserGroup == LeftRotatingLaserGroup.NONE)
                { }
                if (Settings.ChromaConfig.Instance.LeftRotatingLaserGroup == LeftRotatingLaserGroup.LEFT)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.GetLeft().SetState(token, null, brightness); break;
                        case 1: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.LeftRotatingLaserGroup == LeftRotatingLaserGroup.RIGHT)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.GetRight().SetState(token, null, brightness); break;
                        case 1: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetRight().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetRight().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetRight().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetRight().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.LeftRotatingLaserGroup == LeftRotatingLaserGroup.FRONT)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.GetFront().SetState(token, null, brightness); break;
                        case 1: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetFront().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetFront().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetFront().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetFront().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.LeftRotatingLaserGroup == LeftRotatingLaserGroup.BACK)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.GetBack().SetState(token, null, brightness); break;
                        case 1: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetBack().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetBack().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetBack().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetBack().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.LeftRotatingLaserGroup == LeftRotatingLaserGroup.CENTER)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.GetCenter().SetState(token, null, brightness); break;
                        case 1: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.LeftRotatingLaserGroup == LeftRotatingLaserGroup.ALL)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.SetState(token, null, brightness); break;
                        case 1: entLayer.SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
            }
            if (data.type == BeatmapEventType.Event3)
            {
                if (Settings.ChromaConfig.Instance.RightRotatingLaserGroup == RightRotatingLaserGroup.NONE)
                { }
                if (Settings.ChromaConfig.Instance.RightRotatingLaserGroup == RightRotatingLaserGroup.LEFT)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.GetLeft().SetState(token, null, brightness); break;
                        case 1: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.RightRotatingLaserGroup == RightRotatingLaserGroup.RIGHT)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.GetRight().SetState(token, null, brightness); break;
                        case 1: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetRight().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetRight().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetRight().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetRight().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.RightRotatingLaserGroup == RightRotatingLaserGroup.FRONT)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.GetFront().SetState(token, null, brightness); break;
                        case 1: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetFront().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetFront().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetFront().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetFront().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.RightRotatingLaserGroup == RightRotatingLaserGroup.BACK)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.GetBack().SetState(token, null, brightness); break;
                        case 1: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetBack().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetBack().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetBack().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetBack().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.RightRotatingLaserGroup == RightRotatingLaserGroup.CENTER)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.GetCenter().SetState(token, null, brightness); break;
                        case 1: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.RightRotatingLaserGroup == RightRotatingLaserGroup.ALL)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.SetState(token, null, brightness); break;
                        case 1: entLayer.SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
            }
            if (data.type == BeatmapEventType.Event4)
            {
                if (Settings.ChromaConfig.Instance.CenterLightGroup == CenterLightGroup.NONE)
                { }
                if (Settings.ChromaConfig.Instance.CenterLightGroup == CenterLightGroup.LEFT)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.GetLeft().SetState(token, null, brightness); break;
                        case 1: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetLeft().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.CenterLightGroup == CenterLightGroup.RIGHT)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.GetRight().SetState(token, null, brightness); break;
                        case 1: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetRight().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetRight().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetRight().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetRight().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetRight().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.CenterLightGroup == CenterLightGroup.FRONT)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.GetFront().SetState(token, null, brightness); break;
                        case 1: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetFront().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetFront().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetFront().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetFront().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetFront().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.CenterLightGroup == CenterLightGroup.BACK)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.GetBack().SetState(token, null, brightness); break;
                        case 1: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetBack().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetBack().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetBack().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetBack().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetBack().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.CenterLightGroup == CenterLightGroup.CENTER)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.GetCenter().SetState(token, null, brightness); break;
                        case 1: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.GetCenter().SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
                if (Settings.ChromaConfig.Instance.CenterLightGroup == CenterLightGroup.ALL)
                {
                    switch (data.value)
                    {
                        case 0: entLayer.SetState(token, null, brightness); break;
                        case 1: entLayer.SetState(token, hsbColor.GetRGB(), 1); break;
                        case 2: entLayer.SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 3: entLayer.SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 5: entLayer.SetState(token, hsbColor.GetRGB(), 1); break;
                        case 6: entLayer.SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                        case 7: entLayer.SetState(token, hsbColor.GetRGB(), 1); await Task.Delay(TimeSpan.FromMilliseconds(20), token); entLayer.SetState(token, hsbColor.GetRGB(), brightness, TimeSpan.FromSeconds(1)); break;
                    }
                }
            }
        }
    }
}
