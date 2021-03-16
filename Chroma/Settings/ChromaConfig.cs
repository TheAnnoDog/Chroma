namespace Chroma.Settings
{
    public class ChromaConfig
    {
        private static bool _customColorEventsEnabled = true;

        public static ChromaConfig Instance { get; set; }

        public bool CustomColorEventsEnabled
        {
            get => _customColorEventsEnabled;
            set
            {
                SongCore.Loader.Instance?.RefreshSongs();
                Utils.ChromaUtils.SetSongCoreCapability(Plugin.REQUIREMENTNAME, value);
                _customColorEventsEnabled = value;
            }
        }

        //hue settings
        public bool HueEnabled { get; set; } = false;

        public BackLaserGroup BackLaserGroup { get; set; } = BackLaserGroup.NONE;

        public string HueAppKey { get; set; } = "none";

        public string HueClientKey { get; set; } = "none";

        public bool LowLight { get; set; } = false;

        public BigRingsGroup BigRingsGroup { get; set; } = BigRingsGroup.NONE;

        public LeftRotatingLaserGroup LeftRotatingLaserGroup { get; set; } = LeftRotatingLaserGroup.NONE;

        public RightRotatingLaserGroup RightRotatingLaserGroup { get; set; } = RightRotatingLaserGroup.NONE;

        public CenterLightGroup CenterLightGroup { get; set; } = CenterLightGroup.NONE;

        public bool EnvironmentEnhancementsEnabled { get; set; } = true;

        public void setAppKey(string key)
        {
            HueAppKey = key;
        }

        public void setClientKey(string key)
        {
            HueClientKey = key;
        }
    }
}
