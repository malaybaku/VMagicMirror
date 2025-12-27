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

        public bool EnableSpoutOutput { get; set; } = false;
        public int SpoutResolutionType { get; set; } = 0;

        public bool EnableCircleCrop { get; set; } = false;
        // 両方とも [%] 指定なのでこういう数値
        public float CircleCropSize { get; set; } = 98f;
        public float CircleCropBorderWidth { get; set; } = 2f;

        public int CropBorderR { get; set; } = 255;
        public int CropBorderG { get; set; } = 255;
        public int CropBorderB { get; set; } = 255;
    }

    public enum SpoutResolutionType
    {
        SameAsWindow = 0,
        Fixed1280 = 1,
        Fixed1920 = 2,
        Fixed2560 = 3,
        Fixed3840 = 4,
        // NOTE: ここから下は縦長解像度で、v3.9.1まででは存在しなかったオプション。
        // `1280` 等の数値は縦幅であって横幅ではないので注意
        Fixed1280Vertical = 5,
        Fixed1920Vertical = 6,
        Fixed2560Vertical = 7,
        Fixed3840Vertical = 8,
    }
}
