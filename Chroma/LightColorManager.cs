﻿namespace Chroma
{
    using System.Collections.Generic;
    using System.Linq;
    using Chroma.Colorizer;
    using Chroma.Utils;
    using CustomJSONData;
    using CustomJSONData.CustomBeatmap;
    using UnityEngine;
    using static Plugin;

    internal static class LightColorManager
    {
        internal static List<int> LightIDOverride { get; set; }

        internal static void ColorLightSwitch(MonoBehaviour monobehaviour, BeatmapEventData beatmapEventData)
        {
            monobehaviour.SetLastValue(beatmapEventData.value);

            Color? color = null;
            bool gradi = false;

            // legacy was a mistake
            color = LegacyLightHelper.GetLegacyColor(beatmapEventData) ?? color;

            if (beatmapEventData is CustomBeatmapEventData customData)
            {
                dynamic dynData = customData.customData;
                if (monobehaviour is LightSwitchEventEffect lightSwitchEventEffect)
                {
                    object lightID = Trees.at(dynData, LIGHTID);
                    if (lightID != null)
                    {
                        switch (lightID)
                        {
                            case List<object> lightIDobjects:
                                LightIDOverride = lightIDobjects.Select(n => System.Convert.ToInt32(n)).ToList();

                                break;

                            case long lightIDint:
                                LightIDOverride = new List<int> { (int)lightIDint };

                                break;
                        }
                    }

                    // propID is now DEPRECATED!!!!!!!!
                    object propID = Trees.at(dynData, PROPAGATIONID);
                    if (propID != null)
                    {
                        ILightWithId[][] lights = lightSwitchEventEffect.GetLightsPropagationGrouped();
                        int lightCount = lights.Length;
                        switch (propID)
                        {
                            case List<object> propIDobjects:
                                int[] propIDArray = propIDobjects.Select(n => System.Convert.ToInt32(n)).ToArray();
                                List<ILightWithId> overrideLights = new List<ILightWithId>();
                                for (int i = 0; i < propIDArray.Length; i++)
                                {
                                    if (lightCount > propIDArray[i])
                                    {
                                        overrideLights.AddRange(lights[propIDArray[i]]);
                                    }
                                }

                                SetLegacyPropIdOverride(overrideLights.ToArray());

                                break;

                            case long propIDlong:
                                if (lightCount > propIDlong)
                                {
                                    SetLegacyPropIdOverride(lights[propIDlong]);
                                }

                                break;
                        }
                    }

                    dynamic gradientObject = Trees.at(dynData, LIGHTGRADIENT);
                    if (gradientObject != null)
                    {
                        color = ChromaGradientController.AddGradient(gradientObject, beatmapEventData.type, beatmapEventData.time);
                        if (Settings.ChromaConfig.Instance.HueEnabled == true)
                        {
                            HueManager.ProcessEvent(beatmapEventData, null, gradientObject);
                        }
                        gradi = true;
                    }
                }

                Color? colorData = ChromaUtils.GetColorFromData(dynData);
                if (colorData.HasValue)
                {
                    color = colorData;
                    ChromaGradientController.CancelGradient(beatmapEventData.type);
                    gradi = false;
                }
            }

            if (color.HasValue)
            {
                monobehaviour.SetLightingColors(color.Value, color.Value, color.Value, color.Value);
            }
            else if (!ChromaGradientController.IsGradientActive(beatmapEventData.type))
            {
                monobehaviour.Reset();
            }
            if (Settings.ChromaConfig.Instance.HueEnabled == true && gradi == false)
            {
                HueManager.ProcessEvent(beatmapEventData, color);
            }
        }

        private static void SetLegacyPropIdOverride(params ILightWithId[] lights)
        {
            HarmonyPatches.LightSwitchEventEffectHandleBeatmapObjectCallbackControllerBeatmapEventDidTrigger.LegacyLightOverride = lights;
        }
    }
}
