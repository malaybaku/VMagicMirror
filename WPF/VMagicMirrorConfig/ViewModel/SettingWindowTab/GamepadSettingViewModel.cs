namespace Baku.VMagicMirror.ViewModelsConfig
{
    /// <summary>ゲームパッド関係の設定のViewModel。素通し設定ばかりなのでとても単純。</summary>
    public class GamepadSettingViewModel : SettingViewModelBase
    {
        public GamepadSettingViewModel() : this(ModelResolver.Instance.Resolve<GamepadSettingModel>()) 
        {
        }

        internal GamepadSettingViewModel(GamepadSettingModel model)
        {
            _model = model;
            ResetSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetToDefault)
                );
        }

        private readonly GamepadSettingModel _model;

        public RProperty<bool> GamepadEnabled => _model.GamepadEnabled;
        public RProperty<bool> PreferDirectInput => _model.PreferDirectInputGamepad;

        //NOTE: 以下、本来ならEnum値1つで管理する方がよいが、TwoWayバインディングが簡便になるのでbool4つで代用。
        //というのをViewModel層でやってたのがModelに波及してしまった悪い例です…
        public RProperty<bool> GamepadLeanNone => _model.GamepadLeanNone;
        public RProperty<bool> GamepadLeanLeftStick => _model.GamepadLeanLeftStick;
        public RProperty<bool> GamepadLeanRightStick => _model.GamepadLeanRightStick;
        public RProperty<bool> GamepadLeanLeftButtons => _model.GamepadLeanLeftButtons;

        public RProperty<bool> GamepadLeanReverseHorizontal => _model.GamepadLeanReverseHorizontal;
        public RProperty<bool> GamepadLeanReverseVertical => _model.GamepadLeanReverseVertical;

        public ActionCommand ResetSettingCommand { get; }
    }
}
