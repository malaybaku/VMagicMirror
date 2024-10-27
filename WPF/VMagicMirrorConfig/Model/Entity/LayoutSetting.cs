namespace Baku.VMagicMirrorConfig
{
    public class LayoutSetting : SettingEntityBase
    {
        //NOTE: Unity側に指定するIDのことで、コンボボックスとか配列のインデックスとは本質的には関係しない事に注意
        public const int TypingEffectIndexNone = -1;
        public const int TypingEffectIndexText = 0;
        public const int TypingEffectIndexLight = 1;
        //private const int TypingEffectIndexLaser = 2;
        public const int TypingEffectIndexButtefly = 3;

        /// <summary>
        /// NOTE: 規約としてこの値は書き換えません。
        /// デフォルト値を参照したい人が、プロパティ読み込みのみの為だけに使います。
        /// </summary>
        public static LayoutSetting Default { get; } = new LayoutSetting();

        // NOTE: ViewModelだった時代の経緯でこういうプロパティ上のツリー構造を取ってます
        // Setterがpublicなのはシリアライザのご機嫌取ってるだけなので普通のコードでは触らない事！
        public GamepadSetting Gamepad { get; set; } = new GamepadSetting();

        public int CameraFov { get; set; } = 40;

        public bool EnableMidiRead { get; set; } = false;
        public bool MidiControllerVisibility { get; set; } = false;

        //NOTE: カメラ位置、デバイスレイアウト、クイックセーブした視点については、ユーザーが直接いじる想定ではない
        public string CameraPosition { get; set; } = "";

        public string DeviceLayout { get; set; } = "";

        //視点のクイックセーブ/ロード

        //NOTE: 数が少ないので、ラクな方法ということでハードコーディングにしてます
        //以下3つの文字列は"CameraPosition+視野角"というデータで構成されます
        public string QuickSave1 { get; set; } = "";
        public string QuickSave2 { get; set; } = "";
        public string QuickSave3 { get; set; } = "";

        public bool HidVisibility { get; set; } = true;

        public bool PenVisibility { get; set; } = true;

        //NOTE: v1.4.0まではboolを選択肢の数だけ保存していたが、スケールしないのでインデックス方式に切り替える。
        //後方互換性については意図的に捨ててます(配信タブで復帰でき、気を使う意義が薄そうなため)
        public int SelectedTypingEffectId { get; set; } = TypingEffectIndexNone;

        public bool HideUnusedDevices { get; set; } = false;
    }
}
