using Baku.VMagicMirrorConfig.View;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Baku.VMagicMirrorConfig.ViewModel.StreamingTabViewModels
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
            BackgroundImageSetCommand = new ActionCommand(() => _model.SetBackgroundImage());
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
            ModelResolver.Instance.Resolve<ExternalTrackerSettingModel>(),
            ModelResolver.Instance.Resolve<InstallPathChecker>(),
            ModelResolver.Instance.Resolve<DeviceListSource>(),
            ModelResolver.Instance.Resolve<MicrophoneStatus>()
            )
        {
        }

        internal FaceViewModel(
            MotionSettingModel setting,
            ExternalTrackerSettingModel externalTrackerSettingModel,
            InstallPathChecker installPathChecker,
            DeviceListSource deviceList,
            MicrophoneStatus microphoneStatus
            )
        {
            _setting = setting;
            _externalTrackerSetting = externalTrackerSettingModel;
            _deviceList = deviceList;
            _microphoneStatus = microphoneStatus;


            CalibrateFaceCommand = new ActionCommand(() => _setting.RequestCalibrateFace());
            EndExTrackerIfNeededCommand = new ActionCommand(
                async () => await _externalTrackerSetting.DisableExternalTrackerWithConfirmAsync()
                );

            if (!IsInDesignMode)
            {
                LipSyncMicrophoneDeviceName = new RProperty<string>(_setting.LipSyncMicrophoneDeviceName.Value, v =>
                {
                    if (!string.IsNullOrEmpty(v))
                    {
                        _setting.LipSyncMicrophoneDeviceName.Value = v;
                    }
                });

                CameraDeviceName = new RProperty<string>(_setting.CameraDeviceName.Value, v =>
                {
                    if (!string.IsNullOrEmpty(v))
                    {
                        _setting.CameraDeviceName.Value = v;
                    }
                });

                ShowInstallPathWarning = installPathChecker.HasMultiByteCharInInstallPath;
                _setting.LipSyncMicrophoneDeviceName.AddWeakEventHandler(OnMicrophoneDeviceNameChanged);
                _setting.CameraDeviceName.AddWeakEventHandler(OnCameraDeviceNameChanged);
            }
            else
            {
                LipSyncMicrophoneDeviceName = new RProperty<string>("");
                CameraDeviceName = new RProperty<string>("");
            }
        }

        private readonly MotionSettingModel _setting;
        private readonly ExternalTrackerSettingModel _externalTrackerSetting;
        private readonly DeviceListSource _deviceList;
        private readonly MicrophoneStatus _microphoneStatus;

        public ReadOnlyObservableCollection<string> CameraNames => _deviceList.CameraNames;
        public ReadOnlyObservableCollection<string> MicrophoneNames => _deviceList.MicrophoneNames;

        public RProperty<bool> EnableLipSync => _setting.EnableLipSync;
        //NOTE: 値が空のときはModelと同期したくないため、分けている。カメラも同様
        //(本当はプロパティの流用してるほうが変なので、こっちが正しい。はず)
        public RProperty<string> LipSyncMicrophoneDeviceName { get; }

        public RProperty<bool> EnableFaceTracking => _setting.EnableFaceTracking;
        public RProperty<string> CameraDeviceName { get; }

        public bool ShowInstallPathWarning { get; } 
        public RProperty<bool> ShowMicrophoneVolume => _microphoneStatus.ShowMicrophoneVolume;
        public RProperty<int> MicrophoneVolumeValue => _microphoneStatus.MicrophoneVolumeValue;
        public RProperty<int> MicrophoneSensitivity => _setting.MicrophoneSensitivity;

        public RProperty<bool> UseLookAtPointMousePointer => _setting.UseLookAtPointMousePointer;
        public RProperty<bool> UseLookAtPointMainCamera => _setting.UseLookAtPointMainCamera;
        public RProperty<bool> UseLookAtPointNone => _setting.UseLookAtPointNone;


        public RProperty<bool> EnableExternalTracking => _externalTrackerSetting.EnableExternalTracking;

        public ActionCommand CalibrateFaceCommand { get; }
        public ActionCommand EndExTrackerIfNeededCommand { get; }

        private void OnMicrophoneDeviceNameChanged(object? sender, PropertyChangedEventArgs e)
        {
            LipSyncMicrophoneDeviceName.Value = _setting.LipSyncMicrophoneDeviceName.Value;
        }

        private void OnCameraDeviceNameChanged(object? sender, PropertyChangedEventArgs e)
        {
            CameraDeviceName.Value = _setting.CameraDeviceName.Value;
        }
    }

    public class MotionViewModel : ViewModelBase
    {
        public MotionViewModel() : this(ModelResolver.Instance.Resolve<MotionSettingModel>())
        {
        }

        internal MotionViewModel(MotionSettingModel model)
        {
            _model = model;

            OpenGameInputSettingWindowCommand = new ActionCommand(() => GameInputKeyAssignWindow.OpenOrActivateExistingWindow());

            if (!IsInDesignMode)
            {
                _model.KeyboardAndMouseMotionMode.AddWeakEventHandler(UpdateKeyboardAndMouseMotionModeAsHandler);
                _model.GamepadMotionMode.AddWeakEventHandler(UpdateGamepadMotionModeAsHandler);

                _model.EnableNoHandTrackMode.AddWeakEventHandler(UpdateBodyMotionModeAsHandler);
                _model.EnableGameInputLocomotionMode.AddWeakEventHandler(UpdateBodyMotionModeAsHandler);

                UpdateKeyboardAndMouseMotionMode();
                UpdateGamepadMotionMode();
                UpdateBodyMotionMode();
            }
        }

        private readonly MotionSettingModel _model;
        private bool _silentSetMode = false;

        public ActionCommand OpenGameInputSettingWindowCommand { get; }
        public RProperty<bool> EnableTwistBodyMotion => _model.EnableTwistBodyMotion;

        public RProperty<bool> EnableNoHandTrackMode => _model.EnableNoHandTrackMode;

        //モデル層から引っ張った方がよいかもしれないが、それは無理に頑張らないでもよいかも
        public MotionModeSelectionViewModel[] KeyboardAndMouseMotions
            => MotionModeSelectionViewModel.KeyboardAndMouseMotions;

        public MotionModeSelectionViewModel[] GamepadMotions =>
            MotionModeSelectionViewModel.GamepadMotions;

        public BodyMotionBaseModeSelectionViewModel[] BodyMotionAvailableModes => 
            BodyMotionBaseModeSelectionViewModel.AvailableModes;

        private BodyMotionBaseModeSelectionViewModel? _bodyMotionMode = null;
        public BodyMotionBaseModeSelectionViewModel? BodyMotionMode
        {
            get => _bodyMotionMode;
            set
            {
                if (_bodyMotionMode == value)
                {
                    return;
                }

                _bodyMotionMode = value;
                if (value != null && !_silentSetMode)
                {
                    _model.EnableNoHandTrackMode.Value = value.Mode == BodyMotionBaseMode.NoHandTracking;
                    _model.EnableGameInputLocomotionMode.Value = value.Mode == BodyMotionBaseMode.GameInputLocomotion;
                }
                RaisePropertyChanged();
            }
        }

        public RProperty<bool> EnableGameInputLocomotion => _model.EnableGameInputLocomotionMode;

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

        private void UpdateKeyboardAndMouseMotionModeAsHandler(object? sender, PropertyChangedEventArgs e)
            => UpdateKeyboardAndMouseMotionMode();

        private void UpdateGamepadMotionModeAsHandler(object? sender, PropertyChangedEventArgs e)
            => UpdateGamepadMotionMode();

        private void UpdateBodyMotionModeAsHandler(object? sender, PropertyChangedEventArgs e)
            => UpdateBodyMotionMode();

        private void UpdateKeyboardAndMouseMotionMode() =>
            KeyboardAndMouseMotionMode = KeyboardAndMouseMotions
                .FirstOrDefault(m => m.Index == _model.KeyboardAndMouseMotionMode.Value);

        private void UpdateGamepadMotionMode() =>
             GamepadMotionMode = GamepadMotions
                .FirstOrDefault(m => m.Index == _model.GamepadMotionMode.Value);

        private void UpdateBodyMotionMode()
        {
            _silentSetMode = true;

            var mode =
                _model.EnableGameInputLocomotionMode.Value ? BodyMotionBaseMode.GameInputLocomotion :
                _model.EnableNoHandTrackMode.Value ? BodyMotionBaseMode.NoHandTracking :
                BodyMotionBaseMode.Default;

            //NOTE: いちおうFirstOrDefaultにしているが、nullは戻ってこないはず
            BodyMotionMode = BodyMotionAvailableModes.FirstOrDefault(m => m.Mode == mode);
            _silentSetMode = false;
        }
    }

    public class VisibilityViewModel : ViewModelBase
    {
        public VisibilityViewModel() : this(
            ModelResolver.Instance.Resolve<LoadedAvatarInfo>(),
            ModelResolver.Instance.Resolve<WindowSettingModel>(),
            ModelResolver.Instance.Resolve<LayoutSettingModel>(),
            ModelResolver.Instance.Resolve<GamepadSettingModel>(),
            ModelResolver.Instance.Resolve<LightSettingModel>()
            )
        {
        }

        internal VisibilityViewModel(
            LoadedAvatarInfo loadedAvatar,
            WindowSettingModel window,
            LayoutSettingModel layout, 
            GamepadSettingModel gamepad, 
            LightSettingModel effects)
        {
            _loadedAvatar = loadedAvatar;
            _window = window;
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

            if (!IsInDesignMode)
            {
                _layout.SelectedTypingEffectId.AddWeakEventHandler(OnTypingEffectIdChanged);
                _typingEffectItem = TypingEffectSelections
                    .FirstOrDefault(v => v.Id == _layout.SelectedTypingEffectId.Value);

                _window.IsTransparent.AddWeakEventHandler(OnOutlineEffectWarningMaybeChanged);
                _effects.EnableOutlineEffect.AddWeakEventHandler(OnOutlineEffectWarningMaybeChanged);
                ShowOutlineEffectWarning.Value = !_window.IsTransparent.Value && _effects.EnableOutlineEffect.Value;
            }
        }

        private readonly LoadedAvatarInfo _loadedAvatar;
        private readonly WindowSettingModel _window;
        private readonly LayoutSettingModel _layout;
        private readonly GamepadSettingModel _gamepad;
        private readonly LightSettingModel _effects;

        public RProperty<bool> HidVisibility => _layout.HidVisibility;
        public RProperty<bool> PenVisibility => _layout.PenVisibility;
        public RProperty<bool> PenUnavailable => _loadedAvatar.ModelDoesNotSupportPen;
        public RProperty<bool> GamepadVisibility => _gamepad.GamepadVisibility;
        public RProperty<bool> EnableShadow => _effects.EnableShadow;
        public RProperty<bool> EnableWind => _effects.EnableWind;

        //public RProperty<bool> UseDesktopLightAdjust => _effects.UseDesktopLightAdjust;
        public RProperty<bool> UseOutlineEffect => _effects.EnableOutlineEffect;

        /// <summary>
        /// 背景不透明なのに縁取りエフェクトをオンにしているあいだtrueになる
        /// </summary>
        public RProperty<bool> ShowOutlineEffectWarning { get; } = new(false);

        public ActionCommand ShowPenUnavaiableWarningCommand { get; }


        private void OnOutlineEffectWarningMaybeChanged(object? sender, PropertyChangedEventArgs e)
        {
            ShowOutlineEffectWarning.Value = !_window.IsTransparent.Value && _effects.EnableOutlineEffect.Value;
        }


        #region タイピングエフェクト

        private TypingEffectSelectionItem? _typingEffectItem = null;
        public TypingEffectSelectionItem? TypingEffectItem
        {
            get => _typingEffectItem;
            set
            {
                //ここのガード文はComboBoxを意識した書き方なことに注意
                if (value == null || _typingEffectItem == value || (_typingEffectItem != null && _typingEffectItem.Id == value.Id))
                {
                    return;
                }

                _typingEffectItem = value;
                _layout.SelectedTypingEffectId.Value = _typingEffectItem.Id;
                RaisePropertyChanged();
            }
        }

        public TypingEffectSelectionItem[] TypingEffectSelections { get; } = new TypingEffectSelectionItem[]
        {
            new TypingEffectSelectionItem(LayoutSetting.TypingEffectIndexNone, "None", PackIconKind.EyeOff),
            new TypingEffectSelectionItem(LayoutSetting.TypingEffectIndexText, "Text", PackIconKind.Abc),
            new TypingEffectSelectionItem(LayoutSetting.TypingEffectIndexLight, "Light", PackIconKind.FlashOn),
            //new TypingEffectSelectionItem(LayoutSetting.TypingEffectIndexLaser, "Laser", PackIconKind.Wand),
            new TypingEffectSelectionItem(LayoutSetting.TypingEffectIndexButtefly, "Butterfly", PackIconKind.DotsHorizontal),
            new TypingEffectSelectionItem(LayoutSetting.TypingEffectIndexManga, "Manga", PackIconKind.Comment),
        };

        private void OnTypingEffectIdChanged(object? sender, PropertyChangedEventArgs e)
        {
            TypingEffectItem = TypingEffectSelections
                .FirstOrDefault(s => s.Id == _layout.SelectedTypingEffectId.Value);
        }

        #endregion
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
            QuickLoadViewPointCommand = new ActionCommand<string>(s => _model.QuickLoadViewPoint(s));
            ResetCameraPositionCommand = new ActionCommand(() => _model.RequestResetCameraPosition());
        }

        private readonly LayoutSettingModel _model;

        public RProperty<bool> EnableFreeCameraMode => _model.EnableFreeCameraMode;

        public RProperty<string> QuickSave1 => _model.QuickSave1;
        public RProperty<string> QuickSave2 => _model.QuickSave2;
        public RProperty<string> QuickSave3 => _model.QuickSave3;

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
            ResetDeviceLayoutCommand = new ActionCommand(() => _model.ResetDeviceLayout());
        }

        private readonly LayoutSettingModel _model;

        public RProperty<bool> EnableDeviceFreeLayout => _model.EnableDeviceFreeLayout;

        public ActionCommand ResetDeviceLayoutCommand { get; }
    }

    public class WordToMotionViewModel : ViewModelBase
    {
        public WordToMotionViewModel() : this(
            ModelResolver.Instance.Resolve<WordToMotionSettingModel>(),
            ModelResolver.Instance.Resolve<MotionSettingModel>(),
            ModelResolver.Instance.Resolve<GameInputSettingModel>())
        {
        }

        internal WordToMotionViewModel(
            WordToMotionSettingModel model,
            MotionSettingModel motionSettingModel,
            GameInputSettingModel gameInputSettingModel)
        {
            _model = model;
            _motionSettingModel = motionSettingModel;
            _gameInputSettingModel = gameInputSettingModel;
            Devices = WordToMotionDeviceItemViewModel.LoadAvailableItems();

            if (IsInDesignMode)
            {
                return;
            }

            SelectedDevice = Devices.FirstOrDefault(d => d.Index == _model.SelectedDeviceType.Value);
            EnableWordToMotion.Value = _model.SelectedDeviceType.Value != WordToMotionSetting.DeviceTypes.None;

            //TODO: ViewModelの生存期間によってはWeak Event Patternが必須
            _model.SelectedDeviceType.PropertyChanged += (_, __) =>
            {
                SelectedDevice = Devices.FirstOrDefault(d => d.Index == _model.SelectedDeviceType.Value);
                EnableWordToMotion.Value = _model.SelectedDeviceType.Value != WordToMotionSetting.DeviceTypes.None;
                CheckGamepadGameInputActiveness(_, __);
            };

            GameInputGamepadActive.Value =
                _model.SelectedDeviceType.Value == WordToMotionSetting.DeviceTypes.Gamepad && 
                 _gameInputSettingModel.GamepadEnabled.Value &&
                 _motionSettingModel.EnableGameInputLocomotionMode.Value;
            _gameInputSettingModel.GamepadEnabled.AddWeakEventHandler(CheckGamepadGameInputActiveness);
            _motionSettingModel.EnableGameInputLocomotionMode.AddWeakEventHandler(CheckGamepadGameInputActiveness);
        }

        private readonly WordToMotionSettingModel _model;
        private readonly MotionSettingModel _motionSettingModel;
        private readonly GameInputSettingModel _gameInputSettingModel;

        public RProperty<bool> EnableWordToMotion { get; } = new RProperty<bool>(true);

        public WordToMotionDeviceItemViewModel[] Devices { get; }

        private WordToMotionDeviceItemViewModel? _selectedDevice = null;
        public WordToMotionDeviceItemViewModel? SelectedDevice
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

        public RProperty<bool> GameInputGamepadActive { get; } = new(false);

        private void CheckGamepadGameInputActiveness(object? sender, PropertyChangedEventArgs e)
        {
            GameInputGamepadActive.Value =
                _model.SelectedDeviceType.Value == WordToMotionSetting.DeviceTypes.Gamepad &&
                _gameInputSettingModel.GamepadEnabled.Value &&
                _motionSettingModel.EnableGameInputLocomotionMode.Value;
        }
    }
}
