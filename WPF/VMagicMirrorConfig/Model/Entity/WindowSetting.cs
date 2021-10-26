namespace Baku.VMagicMirrorConfig
{
    public class WindowSetting : SettingEntityBase
    {
        /// <summary>
        /// NOTE: 規約としてこの値は書き換えません。
        /// デフォルト値を参照したい人が、プロパティ読み込みのみの為だけに使います。
        /// </summary>
        public static WindowSetting Default { get; } = new WindowSetting();

        public int R { get; set; } = 0;
        public int G { get; set; } = 255;
        public int B { get; set; } = 0;

        public bool IsTransparent { get; set; } = false;
        public bool WindowDraggable { get; set; } = true;
        public bool TopMost { get; set; } = true;

        /// <summary> ここVRMのパスと同じくローカルPCの情報を思いっきり掴むので注意。 </summary>
        public string BackgroundImagePath { get; set; } = "";

        public int WholeWindowTransparencyLevel { get; set; } = 2;
        public int AlphaValueOnTransparent { get; set; } = 128;

    }
}
