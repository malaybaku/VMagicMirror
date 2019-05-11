using System;

namespace Baku.VMagicMirror
{
    //NOTE: このクラスはUnityとWPFで同じメンバ構成にすること
    [Serializable]
    public class AutoAdjustParameters
    {
        public bool EyebrowIsValidPreset { get; set; } = false;
        public string EyebrowLeftUpKey { get; set; } = "";
        public string EyebrowLeftDownKey { get; set; } = "";
        public bool UseSeparatedKeyForEyebrow { get; set; } = false;
        public string EyebrowRightUpKey { get; set; } = "";
        public string EyebrowRightDownKey { get; set; } = "";
        public int EyebrowUpScale { get; set; } = 100;
        public int EyebrowDownScale { get; set; } = 100;

        public int LengthFromWristToTip { get; set; } = 12;
        public int LengthFromWristToPalm { get; set; } = 6;

        //カメラ位置は不要: ポーリングで取得してるから勝手に反映される

        public int HidHeight { get; set; } = 90;
        public int HidHorizontalScale { get; set; } = 70;
        public int GamepadHeight { get; set; } = 90;
        public int GamepadHorizontalScale { get; set; } = 100;

    }
}
