namespace Baku.VMagicMirrorConfig
{
    public class AutomationSetting : SettingEntityBase
    {
        /// <summary>
        /// NOTE: 規約としてこの値は書き換えません。
        /// デフォルト値を参照したい人が、プロパティ読み込みのみの為だけに使います。
        /// </summary>
        public static AutomationSetting Default { get; } = new AutomationSetting();

        public bool IsAutomationEnabled { get; set; } = false;

        //NOTE: デフォルトは適当。iFacialMocapと被ってないなら割となんでもよい。
        public int AutomationPortNumber { get; set; } = 56131;


    }
}
