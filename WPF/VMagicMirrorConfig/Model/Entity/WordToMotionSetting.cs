using System.Collections.Generic;

namespace Baku.VMagicMirrorConfig
{
    public class WordToMotionSetting : SettingEntityBase
    {
        public static class DeviceTypes
        {
            public const int None = -1;
            public const int KeyboardWord = 0;
            public const int Gamepad = 1;
            public const int KeyboardTenKey = 2;
            public const int MidiController = 3;
        }

        /// <summary>
        /// NOTE: 規約としてこの値は書き換えません。
        /// デフォルト値を参照したい人が、プロパティ読み込みのみの為だけに使います。
        /// </summary>
        public static WordToMotionSetting Default { get; } = new WordToMotionSetting();

        public int SelectedDeviceType { get; set; } = DeviceTypes.KeyboardWord;

        //NOTE: 「UIに出さないけど保存はしたい」系のデータで、アバターロード時にUnityから勝手に送られてくる
        public List<string> ExtraBlendShapeClipNames { get; set; } = new List<string>();

        /// <summary>
        /// 一覧要素をシリアライズした文字列
        /// </summary>
        public string ItemsContentString { get; set; } = "";

        /// <summary>
        /// MIDIのノートマッピングをシリアライズした文字列
        /// </summary>
        public string MidiNoteMapString { get; set; } = "";
    }
}
