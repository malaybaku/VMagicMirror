using System;

namespace Baku.VMagicMirror
{
    //NOTE: このクラスはUnityとWPFで同じメンバ構成にするが、シリアライザの都合でUnity: フィールド, WPF: プロパティとかの使い分けは適宜行う。
    [Serializable]
    public class AutoAdjustParameters
    {
        public bool EyebrowIsValidPreset = false;
        public string EyebrowLeftUpKey = "";
        public string EyebrowLeftDownKey = "";
        public bool UseSeparatedKeyForEyebrow = false;
        public string EyebrowRightUpKey = "";
        public string EyebrowRightDownKey = "";
        public int EyebrowUpScale = 100;
        public int EyebrowDownScale = 100;

        public int LengthFromWristToTip = 12;
        public int LengthFromWristToPalm = 6;

        //カメラ位置は不要: ポーリングで取得してるから勝手に反映される

        public int HidHeight = 90;
        public int HidHorizontalScale = 70;
        public int GamepadHeight = 90;
        public int GamepadHorizontalScale = 100;

    }
}
