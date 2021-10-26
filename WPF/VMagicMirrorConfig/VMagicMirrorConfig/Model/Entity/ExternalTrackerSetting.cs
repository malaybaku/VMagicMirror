namespace Baku.VMagicMirrorConfig
{
    public class ExternalTrackerSetting : SettingEntityBase
    {
        public const int TrackSourceNone = 0;
        public const int TrackSourceIFacialMocap = 1;

        /// <summary>
        /// NOTE: 規約としてこの値は書き換えません。
        /// デフォルト値を参照したい人が、プロパティ読み込みのみの為だけに使います。
        /// </summary>
        public static ExternalTrackerSetting Default { get; } = new ExternalTrackerSetting();


        // 基本メニュー部分
        public bool EnableExternalTracking { get; set; } = false;
        public bool EnableExternalTrackerLipSync { get; set; } = true;
        public bool EnableExternalTrackerEmphasizeExpression { get; set; } = false;
        public bool EnableExternalTrackerPerfectSync { get; set; } = false;

        // アプリ別の設定 (※今んとこIPを一方的に表示するだけなのであんまり難しい事はないです)
        public int TrackSourceType { get; set; } = 0;
        public string IFacialMocapTargetIpAddress { get; set; } = "";
        public string CalibrateData { get; set; } = "";

        // FaceSwitchの設定

        //NOTE1: この値は単体でJSONシリアライズされます(Unityにもそのまんま渡すため)
        //NOTE2: setterはアプリ起動直後、およびそれ以降で表情スイッチ系の設定を変えるたびに呼ばれます。
        public string SerializedFaceSwitchSetting { get; set; } = "";
    }
}
