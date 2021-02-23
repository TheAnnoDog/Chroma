namespace Chroma.Settings
{
    using System.Collections.Generic;
    using BeatSaberMarkupLanguage.Attributes;

    internal class ChromaSettingsUI : PersistentSingleton<ChromaSettingsUI>
    {
        //Hue Settings

        [UIValue("backlasergroupchoices")]
        private readonly List<object> _backlasergroupchoices = new List<object> { BackLaserGroup.NONE, BackLaserGroup.LEFT, BackLaserGroup.RIGHT, BackLaserGroup.FRONT, BackLaserGroup.BACK, BackLaserGroup.CENTER, BackLaserGroup.ALL };

        [UIValue("bigringsgroupchoices")]
        private readonly List<object> _bigringsgroupchoices = new List<object> { BigRingsGroup.NONE, BigRingsGroup.LEFT, BigRingsGroup.RIGHT, BigRingsGroup.FRONT, BigRingsGroup.BACK, BigRingsGroup.CENTER, BigRingsGroup.ALL };

        [UIValue("leftrotatinglasergroupchoices")]
        private readonly List<object> _leftrotatinglasergroupchoices = new List<object> { LeftRotatingLaserGroup.NONE, LeftRotatingLaserGroup.LEFT, LeftRotatingLaserGroup.RIGHT, LeftRotatingLaserGroup.FRONT, LeftRotatingLaserGroup.BACK, LeftRotatingLaserGroup.CENTER, LeftRotatingLaserGroup.ALL };

        [UIValue("rightrotatinglasergroupchoices")]
        private readonly List<object> _rightrotatinglasergroupchoices = new List<object> { RightRotatingLaserGroup.NONE, RightRotatingLaserGroup.LEFT, RightRotatingLaserGroup.RIGHT, RightRotatingLaserGroup.FRONT, RightRotatingLaserGroup.BACK, RightRotatingLaserGroup.CENTER, RightRotatingLaserGroup.ALL };

        [UIValue("centerlightgroupchoices")]
        private readonly List<object> _enterlightgroupchoices = new List<object> { CenterLightGroup.NONE, CenterLightGroup.LEFT, CenterLightGroup.RIGHT, CenterLightGroup.FRONT, CenterLightGroup.BACK, CenterLightGroup.CENTER, CenterLightGroup.ALL };

        [UIValue("hueenabled")]
        public bool HueEnabled
        {
            get => ChromaConfig.Instance.HueEnabled;
            set => ChromaConfig.Instance.HueEnabled = value;
        }

        [UIValue("lowlight")]
        public bool LowLight
        {
            get => ChromaConfig.Instance.LowLight;
            set => ChromaConfig.Instance.LowLight = value;
        }

        [UIValue("blg")]
        public BackLaserGroup BackLaserGroup
        {
            get => ChromaConfig.Instance.BackLaserGroup;
            set => ChromaConfig.Instance.BackLaserGroup = value;
        }

        [UIValue("brg")]
        public BigRingsGroup brgv
        {
            get => ChromaConfig.Instance.BigRingsGroup;
            set => ChromaConfig.Instance.BigRingsGroup = value;
        }

        [UIValue("lrlg")]
        public LeftRotatingLaserGroup lrlgv
        {
            get => ChromaConfig.Instance.LeftRotatingLaserGroup;
            set => ChromaConfig.Instance.LeftRotatingLaserGroup = value;
        }

        [UIValue("rrlg")]
        public RightRotatingLaserGroup rrlgv
        {
            get => ChromaConfig.Instance.RightRotatingLaserGroup;
            set => ChromaConfig.Instance.RightRotatingLaserGroup = value;
        }

        [UIValue("clg")]
        public CenterLightGroup clgv
        {
            get => ChromaConfig.Instance.CenterLightGroup;
            set => ChromaConfig.Instance.CenterLightGroup = value;
        }

        //Chroma Settings

        [UIValue("lightshowonly")]
        public bool LightshowModifier
        {
            get => ChromaConfig.Instance.LightshowModifier;
            set => ChromaConfig.Instance.LightshowModifier = value;
        }

        [UIValue("rgbevents")]
        public bool CustomColorEventsEnabled
        {
            get => !ChromaConfig.Instance.CustomColorEventsEnabled;
            set => ChromaConfig.Instance.CustomColorEventsEnabled = !value;
        }

        [UIValue("platform")]
        public bool EnvironmentEnhancementsEnabled
        {
            get => !ChromaConfig.Instance.EnvironmentEnhancementsEnabled;
            set => ChromaConfig.Instance.EnvironmentEnhancementsEnabled = !value;
        }

        // Lightshow
        [UIValue("playersplace")]
        public bool PlayersPlace
        {
            get => ChromaConfig.Instance.PlayersPlace;
            set => ChromaConfig.Instance.PlayersPlace = value;
        }

        [UIValue("spectrograms")]
        public bool Spectrograms
        {
            get => ChromaConfig.Instance.Spectrograms;
            set => ChromaConfig.Instance.Spectrograms = value;
        }

        [UIValue("backcolumns")]
        public bool BackColumns
        {
            get => ChromaConfig.Instance.BackColumns;
            set => ChromaConfig.Instance.BackColumns = value;
        }

        [UIValue("buildings")]
        public bool Buildings
        {
            get => ChromaConfig.Instance.Buildings;
            set => ChromaConfig.Instance.Buildings = value;
        }

        //Hue Formatters

#pragma warning disable IDE0051 // Remove unused private members
        [UIAction("hueblf")]
        private string hueblf(BackLaserGroup t)
        {
            switch (t)
            {
                case BackLaserGroup.NONE:
                    return "None";

                case BackLaserGroup.LEFT:
                    return "Left";

                case BackLaserGroup.RIGHT:
                    return "Right";

                case BackLaserGroup.FRONT:
                    return "Front";

                case BackLaserGroup.BACK:
                    return "Back";

                case BackLaserGroup.CENTER:
                    return "Center";
                case BackLaserGroup.ALL:
                    return "All";
                default:
                    return "All";
            }
        }
        [UIAction("huebrf")]
        private string huebrf(BigRingsGroup t)
        {
            switch (t)
            {
                case BigRingsGroup.NONE:
                    return "None";

                case BigRingsGroup.LEFT:
                    return "Left";

                case BigRingsGroup.RIGHT:
                    return "Right";

                case BigRingsGroup.FRONT:
                    return "Front";

                case BigRingsGroup.BACK:
                    return "Back";

                case BigRingsGroup.CENTER:
                    return "Center";
                case BigRingsGroup.ALL:
                    return "All";
                default:
                    return "All";
            }
        }
        [UIAction("huelrlf")]
        private string huelrlf(LeftRotatingLaserGroup t)
        {
            switch (t)
            {
                case LeftRotatingLaserGroup.NONE:
                    return "None";

                case LeftRotatingLaserGroup.LEFT:
                    return "Left";

                case LeftRotatingLaserGroup.RIGHT:
                    return "Right";

                case LeftRotatingLaserGroup.FRONT:
                    return "Front";

                case LeftRotatingLaserGroup.BACK:
                    return "Back";

                case LeftRotatingLaserGroup.CENTER:
                    return "Center";
                case LeftRotatingLaserGroup.ALL:
                    return "All";
                default:
                    return "All";
            }
        }
        [UIAction("huerrlf")]
        private string huerrlf(RightRotatingLaserGroup t)
        {
            switch (t)
            {
                case RightRotatingLaserGroup.NONE:
                    return "None";

                case RightRotatingLaserGroup.LEFT:
                    return "Left";

                case RightRotatingLaserGroup.RIGHT:
                    return "Right";

                case RightRotatingLaserGroup.FRONT:
                    return "Front";

                case RightRotatingLaserGroup.BACK:
                    return "Back";

                case RightRotatingLaserGroup.CENTER:
                    return "Center";
                case RightRotatingLaserGroup.ALL:
                    return "All";
                default:
                    return "All";
            }
        }
        [UIAction("huecf")]
        private string huecf(CenterLightGroup t)
        {
            switch (t)
            {
                case CenterLightGroup.NONE:
                    return "None";

                case CenterLightGroup.LEFT:
                    return "Left";

                case CenterLightGroup.RIGHT:
                    return "Right";

                case CenterLightGroup.FRONT:
                    return "Front";

                case CenterLightGroup.BACK:
                    return "Back";

                case CenterLightGroup.CENTER:
                    return "Center";
                case CenterLightGroup.ALL:
                    return "All";
                default:
                    return "All";
            }
        }
    }
}
