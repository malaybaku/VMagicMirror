namespace Baku.VMagicMirrorConfig
{
    public class GamepadSetting : SettingEntityBase
    {
        /// <summary>
        /// NOTE: 規約としてこの値は書き換えません。
        /// デフォルト値を参照したい人が、プロパティ読み込みのみの為だけに使います。
        /// </summary>
        public static GamepadSetting Default { get; } = new GamepadSetting();

        public bool GamepadEnabled { get; set; } = true;
        public bool PreferDirectInputGamepad { get; set; } = false;
        public bool GamepadVisibility { get; set; } = false;

        //NOTE: 本来ならEnum値1つで管理する方がよいが、TwoWayバインディングが簡便になるのでbool4つで代用していた経緯があり、こういう持ち方。
        public bool GamepadLeanNone { get; set; } = false;
        public bool GamepadLeanLeftButtons { get; set; } = false;
        public bool GamepadLeanLeftStick { get; set; } = true;
        public bool GamepadLeanRightStick { get; set; } = false;

        public bool GamepadLeanReverseHorizontal { get; set; } = false;
        public bool GamepadLeanReverseVertical { get; set; } = false;

        public void ResetToDefault()
        {
            GamepadEnabled = true;
            PreferDirectInputGamepad = false;
            //NOTE: Visibilityは別のとこでいじるため、ここでは不要

            GamepadLeanNone = false;
            GamepadLeanLeftButtons = false;
            GamepadLeanLeftStick = true;
            GamepadLeanRightStick = false;

            GamepadLeanReverseHorizontal = false;
            GamepadLeanReverseVertical = false;
        }
    }
}
