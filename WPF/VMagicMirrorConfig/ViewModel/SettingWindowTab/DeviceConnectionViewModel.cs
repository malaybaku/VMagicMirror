namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class DeviceConnectionViewModel : ViewModelBase
    {
        public DeviceConnectionViewModel() : this(
            ModelResolver.Instance.Resolve<GamepadSettingModel>(),
            ModelResolver.Instance.Resolve<LayoutSettingModel>()
            )
        {
        }

        internal DeviceConnectionViewModel(
            GamepadSettingModel gamepadSetting,
            LayoutSettingModel layoutSetting
            )
        {
            _gamepadSetting = gamepadSetting;
            _layoutSetting = layoutSetting;

            ResetGamepadSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_gamepadSetting.ResetToDefault)
                );

            ResetMidiSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_layoutSetting.ResetMidiSetting)
                );
        }
        private readonly GamepadSettingModel _gamepadSetting;
        private readonly LayoutSettingModel _layoutSetting;


        public RProperty<bool> GamepadEnabled => _gamepadSetting.GamepadEnabled;
        public RProperty<bool> PreferDirectInput => _gamepadSetting.PreferDirectInputGamepad;

        //NOTE: 以下、本来ならEnum値1つで管理する方がよいが、TwoWayバインディングが簡便になるのでbool4つで代用。
        //というのをViewModel層でやってたのがModelに波及してしまった悪い例です…
        public RProperty<bool> GamepadLeanNone => _gamepadSetting.GamepadLeanNone;
        public RProperty<bool> GamepadLeanLeftStick => _gamepadSetting.GamepadLeanLeftStick;
        public RProperty<bool> GamepadLeanRightStick => _gamepadSetting.GamepadLeanRightStick;
        public RProperty<bool> GamepadLeanLeftButtons => _gamepadSetting.GamepadLeanLeftButtons;

        public RProperty<bool> GamepadLeanReverseHorizontal => _gamepadSetting.GamepadLeanReverseHorizontal;
        public RProperty<bool> GamepadLeanReverseVertical => _gamepadSetting.GamepadLeanReverseVertical;

        public RProperty<bool> EnableMidiRead => _layoutSetting.EnableMidiRead;

        public ActionCommand ResetGamepadSettingCommand { get; }
        public ActionCommand ResetMidiSettingCommand { get; }

    }
}
