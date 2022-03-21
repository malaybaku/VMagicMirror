using System.Collections.ObjectModel;
using System.Linq;

namespace Baku.VMagicMirrorConfig.StreamingTabViewModels
{
    //NOTE: 配信タブは機能が雑多なため1タブ = 1ViewModelではなく、中のサブグループ1つに対して1つのViewModelを当てていく
    public class WindowViewModel : ViewModelBase
    {
        public WindowViewModel() : this(ModelResolver.Instance.Resolve<WindowSettingModel>())
        {
        }

        internal WindowViewModel(WindowSettingModel model)
        {
            _model = model;
            BackgroundImageSetCommand = new ActionCommand(_model.SetBackgroundImage);
            BackgroundImageClearCommand = new ActionCommand(
                () => _model.BackgroundImagePath.Value = ""
                );
        }

        private readonly WindowSettingModel _model;

        public RProperty<bool> IsTransparent => _model.IsTransparent;
        public RProperty<bool> WindowDraggable => _model.WindowDraggable;

        public ActionCommand BackgroundImageSetCommand { get; }
        public ActionCommand BackgroundImageClearCommand { get; }
    }

    public class FaceViewModel : ViewModelBase
    {
        public FaceViewModel() : this(
            ModelResolver.Instance.Resolve<MotionSettingModel>(),
            ModelResolver.Instance.Resolve<InstallPathChecker>(),
            ModelResolver.Instance.Resolve<DeviceListSource>(),
            ModelResolver.Instance.Resolve<MicrophoneStatus>()
            )
        {
        }

        internal FaceViewModel(
            MotionSettingModel setting, 
            InstallPathChecker installPathChecker,
            DeviceListSource deviceList, 
            MicrophoneStatus microphoneStatus
            )
        {
            _setting = setting;
            _deviceList = deviceList;
            _microphoneStatus = microphoneStatus;
            ShowInstallPathWarning = installPathChecker.HasMultiByteCharInInstallPath;

            CalibrateFaceCommand = new ActionCommand(_setting.RequestCalibrateFace);
        }

        private readonly MotionSettingModel _setting;
        private readonly DeviceListSource _deviceList;
        private readonly MicrophoneStatus _microphoneStatus;

        public ReadOnlyObservableCollection<string> CameraNames => _deviceList.CameraNames;
        public ReadOnlyObservableCollection<string> MicrophoneNames => _deviceList.MicrophoneNames;

        public RProperty<bool> EnableLipSync => _setting.EnableLipSync;
        public RProperty<string> LipSyncMicrophoneDeviceName => _setting.LipSyncMicrophoneDeviceName;


        public RProperty<bool> EnableFaceTracking => _setting.EnableFaceTracking;
        public RProperty<string> CameraDeviceName => _setting.CameraDeviceName;

        public RProperty<bool> EnableWebCamHighPowerMode => _setting.EnableWebCamHighPowerMode;
        public RProperty<bool> EnableImageBasedHandTracking => _setting.EnableImageBasedHandTracking;

        public bool ShowInstallPathWarning { get; } 
        public RProperty<bool> ShowMicrophoneVolume => _microphoneStatus.ShowMicrophoneVolume;
        public RProperty<int> MicrophoneVolumeValue => _microphoneStatus.MicrophoneVolumeValue;

        public RProperty<bool> UseLookAtPointMousePointer => _setting.UseLookAtPointMousePointer;
        public RProperty<bool> UseLookAtPointMainCamera => _setting.UseLookAtPointMainCamera;
        public RProperty<bool> UseLookAtPointNone => _setting.UseLookAtPointNone;

        public ActionCommand CalibrateFaceCommand { get; }
    }

    public class MotionViewModel : ViewModelBase
    {
        public MotionViewModel() : this(ModelResolver.Instance.Resolve<MotionSettingModel>())
        {
        }

        internal MotionViewModel(MotionSettingModel model)
        {
            _model = model;
            //TODO: 必要ならweak event patternに書き換える。
            //ただし、配信タブは今のところアプリと同期間だけ生存するので、あまり気にしないでもOK
            _model.KeyboardAndMouseMotionMode.PropertyChanged +=
                (_, __) => UpdateKeyboardAndMouseMotionMode();
            UpdateKeyboardAndMouseMotionMode();
            _model.GamepadMotionMode.PropertyChanged +=
                (_, __) => UpdateGamepadMotionMode();
            UpdateGamepadMotionMode();
        }

        private readonly MotionSettingModel _model;

        public RProperty<bool> EnableNoHandTrackMode => _model.EnableNoHandTrackMode;
        public RProperty<bool> EnableTwistBodyMotion => _model.EnableTwistBodyMotion;

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
    }

    public class VisibilityViewModel : ViewModelBase
    {
        public VisibilityViewModel() : this(
            ModelResolver.Instance.Resolve<LoadedAvatarInfo>(),
            ModelResolver.Instance.Resolve<LayoutSettingModel>(),
            ModelResolver.Instance.Resolve<GamepadSettingModel>(),
            ModelResolver.Instance.Resolve<LightSettingModel>()
            )
        {
        }

        internal VisibilityViewModel(
            LoadedAvatarInfo loadedAvatar,
            LayoutSettingModel layout, 
            GamepadSettingModel gamepad, 
            LightSettingModel effects)
        {
            _loadedAvatar = loadedAvatar;
            _layout = layout;
            _gamepad = gamepad;
            _effects = effects;
            ShowPenUnavaiableWarningCommand = new ActionCommand(async () =>
            {
                var indication = MessageIndication.WarnInfoAboutPenUnavaiable();
                await MessageBoxWrapper.Instance.ShowAsync(
                    indication.Title, indication.Content, MessageBoxWrapper.MessageBoxStyle.OK
                    );
            });
        }

        private readonly LoadedAvatarInfo _loadedAvatar;
        private readonly LayoutSettingModel _layout;
        private readonly GamepadSettingModel _gamepad;
        private readonly LightSettingModel _effects;

        public RProperty<bool> HidVisibility => _layout.HidVisibility;
        public RProperty<bool> PenVisibility => _layout.PenVisibility;
        public RProperty<bool> PenUnavailable => _loadedAvatar.ModelDoesNotSupportPen;
        public RProperty<bool> GamepadVisibility => _gamepad.GamepadVisibility;
        public RProperty<bool> EnableShadow => _effects.EnableShadow;
        public RProperty<bool> EnableWind => _effects.EnableWind;
        public RProperty<bool> UseDesktopLightAdjust => _effects.UseDesktopLightAdjust;

        public ActionCommand ShowPenUnavaiableWarningCommand { get; }
    }

    public class CameraViewModel : ViewModelBase
    {
        public CameraViewModel() : this(ModelResolver.Instance.Resolve<LayoutSettingModel>())
        {
        }

        internal CameraViewModel(LayoutSettingModel model)
        {
            _model = model;
            QuickSaveViewPointCommand = new ActionCommand<string>(async s => await _model.QuickSaveViewPoint(s));
            QuickLoadViewPointCommand = new ActionCommand<string>(_model.QuickLoadViewPoint);
            ResetCameraPositionCommand = new ActionCommand(_model.RequestResetCameraPosition);
        }

        private readonly LayoutSettingModel _model;

        public RProperty<bool> EnableFreeCameraMode => _model.EnableFreeCameraMode;

        public ActionCommand<string> QuickSaveViewPointCommand { get; }
        public ActionCommand<string> QuickLoadViewPointCommand { get; }

        public ActionCommand ResetCameraPositionCommand { get; }
    }

    public class DeviceLayoutViewModel : ViewModelBase
    {
        public DeviceLayoutViewModel() : this(ModelResolver.Instance.Resolve<LayoutSettingModel>())
        {
        }

        internal DeviceLayoutViewModel(LayoutSettingModel model)
        {
            _model = model;
            ResetDeviceLayoutCommand = new ActionCommand(_model.ResetDeviceLayout);
        }

        private readonly LayoutSettingModel _model;

        public RProperty<bool> EnableDeviceFreeLayout => _model.EnableDeviceFreeLayout;

        public ActionCommand ResetDeviceLayoutCommand { get; }
    }

    public class WordToMotionViewModel : ViewModelBase
    {
        public WordToMotionViewModel() : this(ModelResolver.Instance.Resolve<WordToMotionSettingModel>())
        {
        }

        internal WordToMotionViewModel(WordToMotionSettingModel model)
        {
            _model = model;
            Devices = WordToMotionDeviceItem.LoadAvailableItems();

            SelectedDevice = Devices.FirstOrDefault(d => d.Index == _model.SelectedDeviceType.Value);
            EnableWordToMotion.Value = _model.SelectedDeviceType.Value != WordToMotionSetting.DeviceTypes.None;

            //TODO: ViewModelの生存期間によってはWeak Event Patternが必須
            _model.SelectedDeviceType.PropertyChanged += (_, __) =>
            {
                SelectedDevice = Devices.FirstOrDefault(d => d.Index == _model.SelectedDeviceType.Value);
                EnableWordToMotion.Value = _model.SelectedDeviceType.Value != WordToMotionSetting.DeviceTypes.None;
            };
        }

        private readonly WordToMotionSettingModel _model;

        public RProperty<bool> EnableWordToMotion { get; } = new RProperty<bool>(true);

        public WordToMotionDeviceItem[] Devices { get; }

        private WordToMotionDeviceItem? _selectedDevice = null;
        public WordToMotionDeviceItem? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (_selectedDevice == value)
                {
                    return;
                }
                _selectedDevice = value;
                RaisePropertyChanged();
                _model.SelectedDeviceType.Value = _selectedDevice?.Index ?? WordToMotionSetting.DeviceTypes.None;
            }
        }
        public RProperty<int> SelectedDeviceType => _model.SelectedDeviceType;
    }
}
