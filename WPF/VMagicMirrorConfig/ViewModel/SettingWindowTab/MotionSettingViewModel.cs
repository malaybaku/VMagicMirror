using System.ComponentModel;
using System.Linq;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class MotionSettingViewModel : SettingViewModelBase
    {
        public MotionSettingViewModel() : this(ModelResolver.Instance.Resolve<MotionSettingModel>())
        {
        }

        internal MotionSettingViewModel(MotionSettingModel model)
        {
            _model = model;

            ResetHandDownCustomPoseCommand = new ActionCommand(
                () => _model.ResetCustomHandDownPose()
                );

            ResetArmMotionSettingCommand = new ActionCommand(
                 () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetArmSetting)
                 );
            ResetHandMotionSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetHandSetting)
                );
            ResetWaitMotionSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetWaitMotionSetting)
                );

            if (IsInDesignMode)
            {
                return;
            }

            _model.KeyboardAndMouseMotionMode.AddWeakEventHandler(OnKeyboardAndMouseMotionModeChanged);
            _model.GamepadMotionMode.AddWeakEventHandler(OnGamepadMotionModeChanged);
            UpdateKeyboardAndMouseMotionMode();
            UpdateGamepadMotionMode();
        }

        private readonly MotionSettingModel _model;

        private void OnKeyboardAndMouseMotionModeChanged(object? sender, PropertyChangedEventArgs e)
        {
            UpdateKeyboardAndMouseMotionMode();
        }
        private void OnGamepadMotionModeChanged(object? sender, PropertyChangedEventArgs e)
        {
            UpdateGamepadMotionMode();
        }

        #region モーションの種類の制御

        //モデル層から引っ張った方がよいかもしれないが、それは無理に頑張らないでもよいかも
        public MotionModeSelectionViewModel[] KeyboardAndMouseMotions 
            => MotionModeSelectionViewModel.KeyboardAndMouseMotions;

        public MotionModeSelectionViewModel[] GamepadMotions =>
            MotionModeSelectionViewModel.GamepadMotions;

        private MotionModeSelectionViewModel? _keyboardAndMouseMotionMode = null;
        public MotionModeSelectionViewModel? KeyboardAndMouseMotionMode
        {
            get => _keyboardAndMouseMotionMode;
            set
            {
                if (_keyboardAndMouseMotionMode != value)
                {
                    _keyboardAndMouseMotionMode = value;
                    if (value != null)
                    {
                        _model.KeyboardAndMouseMotionMode.Value = value.Index;
                    }
                    RaisePropertyChanged();
                }
            }
        }
   
        private MotionModeSelectionViewModel? _gamepadMotionMode = null;
        public MotionModeSelectionViewModel? GamepadMotionMode
        {
            get => _gamepadMotionMode;
            set
            {
                if (_gamepadMotionMode != value)
                {
                    _gamepadMotionMode = value;
                    if (value != null)
                    {
                        _model.GamepadMotionMode.Value = value.Index;
                    }
                    RaisePropertyChanged();
                }
            }
        }

        private void UpdateKeyboardAndMouseMotionMode() =>
            KeyboardAndMouseMotionMode = KeyboardAndMouseMotions
                .FirstOrDefault(m => m.Index == _model.KeyboardAndMouseMotionMode.Value);

        private void UpdateGamepadMotionMode() =>
             GamepadMotionMode = GamepadMotions
                .FirstOrDefault(m => m.Index == _model.GamepadMotionMode.Value);

        #endregion

        
        public RProperty<bool> EnableNoHandTrackMode => _model.EnableNoHandTrackMode;
        public RProperty<bool> EnableTwistBodyMotion => _model.EnableTwistBodyMotion;
        public RProperty<bool> EnableCustomHandDownPose => _model.EnableCustomHandDownPose;

        public RProperty<bool> EnableHidRandomTyping => _model.EnableHidRandomTyping;
        public RProperty<bool> EnableShoulderMotionModify => _model.EnableShoulderMotionModify;
        public RProperty<bool> EnableHandDownTimeout => _model.EnableHandDownTimeout;
        public RProperty<int> WaistWidth => _model.WaistWidth;
        public RProperty<int> ElbowCloseStrength => _model.ElbowCloseStrength;
        public RProperty<bool> EnableFpsAssumedRightHand => _model.EnableFpsAssumedRightHand;


        public RProperty<bool> ShowPresentationPointer => _model.ShowPresentationPointer;
        public RProperty<int> PresentationArmRadiusMin => _model.PresentationArmRadiusMin;

        public RProperty<int> LengthFromWristToTip => _model.LengthFromWristToTip;
        public RProperty<int> HandYOffsetBasic => _model.HandYOffsetBasic;
        public RProperty<int> HandYOffsetAfterKeyDown => _model.HandYOffsetAfterKeyDown;
       

        public RProperty<bool> EnableWaitMotion => _model.EnableWaitMotion;
        public RProperty<int> WaitMotionScale => _model.WaitMotionScale;
        public RProperty<int> WaitMotionPeriod => _model.WaitMotionPeriod;


        public ActionCommand ResetHandDownCustomPoseCommand { get; }
        public ActionCommand ResetArmMotionSettingCommand { get; }
        public ActionCommand ResetHandMotionSettingCommand { get; }
        public ActionCommand ResetWaitMotionSettingCommand { get; }
    }

    /// <summary>
    /// マウス/キーボードなりゲームパッドなりについて、操作したときのモーションの種類を指定するやつ
    /// </summary>
    public class MotionModeSelectionViewModel
    {
        public MotionModeSelectionViewModel(int index, string localizationKey)
        {
            Index = index;
            _localizationKey = localizationKey;
            Label.Value = LocalizedString.GetString(_localizationKey);
            LanguageSelector.Instance.LanguageChanged +=
                () => Label.Value = LocalizedString.GetString(_localizationKey);
        }

        private readonly string _localizationKey;
        public int Index { get; }

        public RProperty<string> Label { get; } = new RProperty<string>("");

        //NOTE: immutable arrayのほうが性質は良いのでそうしてもよい
        public static MotionModeSelectionViewModel[] KeyboardAndMouseMotions { get; } = new[]
        {
            new MotionModeSelectionViewModel(MotionSetting.KeyboardMouseMotionDefault, "Motion_Arm_MouseAndKeyMode_Default"),
            new MotionModeSelectionViewModel(MotionSetting.KeyboardMouseMotionPresentation, "Motion_Arm_MouseAndKeyMode_Presentation"),
            new MotionModeSelectionViewModel(MotionSetting.KeyboardMouseMotionPenTablet, "Motion_Arm_MouseAndKeyMode_PenTablet"),
            //NOTE: 順番と値が違うのはわざと。
            new MotionModeSelectionViewModel(MotionSetting.KeyboardMouseMotionNone, "Motion_Arm_MouseAndKeyMode_None"),
        };

        public static MotionModeSelectionViewModel[] GamepadMotions { get; } = new[]
        {
            new MotionModeSelectionViewModel(0, "Motion_Arm_GamepadMode_Default"),
            new MotionModeSelectionViewModel(1, "Motion_Arm_GamepadMode_ArcadeStick"),
            //NOTE: Gun ControllerとかCar Handleとかも想定していたが、Unity側の実装が間に合ってないので無し。
        };
    }

}
