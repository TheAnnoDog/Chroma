namespace Chroma.Settings
{
    using BeatSaberMarkupLanguage.Attributes;
    using System;
    using System.Threading;
    using UnityEngine;

    internal class ChromaHueConnectUI : PersistentSingleton<ChromaHueConnectUI>
    {

        private static CancellationTokenSource hueCts = new CancellationTokenSource();
        [UIAction("connectbridge")]
    private async void connectBridge()
        {
            try
            {
                hueCts.Cancel();
                hueCts = new CancellationTokenSource();
            }
            catch (Exception) { } // can't cancel unused token
            Debug.Log("Received button press");
            await HueManager.syncAsync(hueCts.Token);
        }
    }
}