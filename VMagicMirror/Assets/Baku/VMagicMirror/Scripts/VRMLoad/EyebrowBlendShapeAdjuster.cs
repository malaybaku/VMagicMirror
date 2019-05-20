using System.Linq;

namespace Baku.VMagicMirror
{
    public class EyebrowBlendShapeAdjuster
    {
        private const string VRoid_Ver0_6_3_Up = "Face.M_F00_000_00_Fcl_BRW_Surprised";
        private const string VRoid_Ver0_6_3_Down = "Face.M_F00_000_00_Fcl_BRW_Angry";

        private const string VRoid_Ver0_6_2_Or_Older_Up = "Face.M_F00_000_Fcl_BRW_Surprised";
        private const string VRoid_Ver0_6_2_Or_Older_Down = "Face.M_F00_000_Fcl_BRW_Angry";

        private const string Alicia_Up = "eyeblow_up";
        private const string Alicia_Down = "eyeblow_down";

        public class EyebrowPreferredSettings
        {
            public bool IsValidPreset { get; set; } = false;
            public string EyebrowLeftUpKey { get; set; } = "";
            public string EyebrowLeftDownKey { get; set; } = "";
            public bool UseSeparatedKeyForEyebrow { get; set; } = false;
            public string EyebrowRightUpKey { get; set; } = "";
            public string EyebrowRightDownKey { get; set; } = "";
            public int EyebrowUpScale { get; set; } = 100;
            public int EyebrowDownScale { get; set; } = 100;
        }

        public EyebrowBlendShapeAdjuster(string[] allBlendShapeNames)
        {
            _allBlendShapeNames = allBlendShapeNames ?? new string[0];
        }
        private readonly string[] _allBlendShapeNames;

        public EyebrowPreferredSettings CreatePreferredSettings()
        {
            switch (CheckModelTypes())
            {
                case EstimatedModelTypes.VRoidVer0_6_3:
                    return new EyebrowPreferredSettings()
                    {
                        IsValidPreset = true,
                        EyebrowLeftUpKey = VRoid_Ver0_6_3_Up,
                        EyebrowLeftDownKey = VRoid_Ver0_6_3_Down,
                        EyebrowUpScale = 150,
                    };
                case EstimatedModelTypes.VRoidVer0_6_2_Or_Older:
                    return new EyebrowPreferredSettings()
                    {
                        IsValidPreset = true,
                        EyebrowLeftUpKey = VRoid_Ver0_6_2_Or_Older_Up,
                        EyebrowLeftDownKey = VRoid_Ver0_6_2_Or_Older_Down,
                    };
                case EstimatedModelTypes.Alicia:
                    return new EyebrowPreferredSettings()
                    {
                        IsValidPreset = true,
                        EyebrowLeftUpKey = Alicia_Up,
                        EyebrowLeftDownKey = Alicia_Down,
                    };
                case EstimatedModelTypes.Other:
                    return new EyebrowPreferredSettings();
            }

            //どれにも当てはまらない場合: 諦める
            return new EyebrowPreferredSettings();
        }

        private EstimatedModelTypes CheckModelTypes()
        {
            if (_allBlendShapeNames.Length == 0)
            {
                return EstimatedModelTypes.Other;
            }
            else if (_allBlendShapeNames.Contains(VRoid_Ver0_6_3_Up) &&
                _allBlendShapeNames.Contains(VRoid_Ver0_6_3_Down))
            {
                return EstimatedModelTypes.VRoidVer0_6_3;
            }
            else if (_allBlendShapeNames.Contains(VRoid_Ver0_6_2_Or_Older_Up) && 
                _allBlendShapeNames.Contains(VRoid_Ver0_6_2_Or_Older_Down))
            {
                return EstimatedModelTypes.VRoidVer0_6_2_Or_Older;
            }
            else if (_allBlendShapeNames.Contains(Alicia_Up) && 
                _allBlendShapeNames.Contains(Alicia_Down))
            {
                return EstimatedModelTypes.Alicia;
            }
            else
            {
                return EstimatedModelTypes.Other;
            }
        }

        enum EstimatedModelTypes
        {
            VRoidVer0_6_3,
            VRoidVer0_6_2_Or_Older,
            Alicia,
            Other,
        }
    }
}

