using System;
using System.ComponentModel;
using System.Net;
using System.Windows;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    //NOTE: (ちょっと変な挙動だがUI側が決め打ちなので) Modelがなんと言おうと3要素ぶんの編集しか認めていないことに注意
    public class VMCPControlPanelViewModel : SettingViewModelBase
    {
        public VMCPControlPanelViewModel() : this(
            ModelResolver.Instance.Resolve<VMCPSettingModel>(),
            ModelResolver.Instance.Resolve<MotionSettingModel>())
        {
        }

        internal VMCPControlPanelViewModel(
            VMCPSettingModel model,
            MotionSettingModel motionSettingModel)
        {
            _model = model;
            _motionSettingModel = motionSettingModel;

            IsDirty = new RProperty<bool>(false, _ => UpdateReceiveSettingInputValidity());
            ApplyChangeCommand = new ActionCommand(ApplyReceiveSettings);
            RevertChangeCommand = new ActionCommand(RevertReceiveSettingsChange);
            OpenDocUrlCommand = new ActionCommand(OpenDocUrl);
            FixBodyMotionStyleCommand = new ActionCommand(FixBodyMotionStyle);


            ApplySendSettingsCommand = new ActionCommand(ApplySendSettings);
            RevertSendSettingsCommand = new ActionCommand(RevertSendSettings);
            OpenFullEditionDownloadUrlCommand = new ActionCommand(OpenFullEditionUrl);

            IsSendSettingsDirty = new RProperty<bool>(false, _ => UpdateSendSettingsValidity());
            ShowEffectWhenSendEnabled = FeatureLocker.FeatureLocked || IsInDesignMode
                ? _alwaysTrue
                : _model.ShowEffectDuringVMCPSendEnabled;

            var sendSetting = IsInDesignMode 
                ? VMCPSendSetting.Default() 
                : _model.GetCurrentSendSetting();
            SendAddress = new RProperty<string>(sendSetting.SendAddress, _ => SetSendSettingsDirty());
            SendPort = new RProperty<int>(sendSetting.SendPort, _ => SetSendSettingsDirty());

            SendBonePose = new RProperty<bool>(sendSetting.SendBonePose, v => _model.SetSendBonePose(v));
            SendFingerBonePose = new RProperty<bool>(sendSetting.SendFingerBonePose, v => _model.SetSendFingerBonePose(v));
            SendFacial = new RProperty<bool>(sendSetting.SendFacial, v => _model.SetSendFacial(v));
            SendNonStandardFacial = new RProperty<bool>(sendSetting.SendNonStandardFacial, v => _model.SetSendNonStandardFacial(v));
            SendUseVrm0Facial = new RProperty<bool>(sendSetting.UseVrm0Facial, v => _model.SetUseVrm0Facial(v));
            SendPrefer30Fps = new RProperty<bool>(sendSetting.Prefer30Fps, v => _model.SetPrefer30Fps(v));

            ForceToShowVisualEffectWhenSendEnabled = FeatureLocker.FeatureLocked;
            

            if (!IsInDesignMode)
            {
                LoadCurrentReceiveSettings();
                WeakEventManager<VMCPSettingModel, EventArgs>.AddHandler(
                    _model, nameof(_model.ConnectedStatusChanged), OnReceiveConnectStatusChanged
                    );
                _model.SerializedVMCPSourceSetting.AddWeakEventHandler(OnSerializedVMCPSourceSettingChanged);
                ApplyReceiveConnectionStatus();

                _model.VMCPEnabled.AddWeakEventHandler(OnBodyMotionStyleCorrectnessMaybeChanged);
                _motionSettingModel.EnableNoHandTrackMode.AddWeakEventHandler(OnBodyMotionStyleCorrectnessMaybeChanged);

                _model.SerializedVMCPSendSetting.AddWeakEventHandler(OnSerializedVMCPSendSettingChanged);
                UpdateBodyMotionStyleCorrectness();

                UpdateSendSettingsValidity();
            }
        }

        private readonly VMCPSettingModel _model;
        private readonly MotionSettingModel _motionSettingModel;

        // Receive
        public RProperty<bool> IsDirty { get; }
        public RProperty<bool> CanApply { get; } = new(false);
        public RProperty<bool> HasInvalidPortNumber { get; } = new(false);

        public RProperty<bool> VMCPEnabled => _model.VMCPEnabled;
        public VMCPSourceItemViewModel Source1 { get; set; } = new();
        public VMCPSourceItemViewModel Source2 { get; set; } = new();
        public VMCPSourceItemViewModel Source3 { get; set; } = new();

        public RProperty<bool> EnableVMCPReceiveLerp => _model.EnableVMCPReceiveLerp;
        public RProperty<bool> BodyMotionStyleIncorrectForHandTracking { get; } = new(false);

        // Send

        // NOTE: Dirty性が発生するのはAddress/Portまでで、BoneとかFacialとかの粒度の変更は即時適用
        public RProperty<bool> IsSendSettingsDirty { get; }
        public RProperty<bool> CanApplySendSettings { get; } = new(false);
        public RProperty<bool> HasInvalidSendPortNumber { get; } = new(false);
        public RProperty<bool> HasInvalidSendAddress { get; } = new(false);

        // Sendのうち機能制限に関する部分
        public bool ForceToShowVisualEffectWhenSendEnabled { get; }
        public RProperty<bool> ShowEffectWhenSendEnabled { get; }
        private readonly RProperty<bool> _alwaysTrue = new(true);

        // Sendのうち機能制限とは関係ない部分
        public RProperty<bool> VMCPSendEnabled => _model.VMCPSendEnabled;
        public RProperty<string> SendAddress { get; }
        public RProperty<int> SendPort { get; }
        public RProperty<bool> SendBonePose { get; }
        public RProperty<bool> SendFingerBonePose { get; }
        public RProperty<bool> SendFacial { get; }
        public RProperty<bool> SendNonStandardFacial { get; }
        public RProperty<bool> SendUseVrm0Facial { get; }
        public RProperty<bool> SendPrefer30Fps { get; }

        private void SetReceiveSettingsDirty()
        {
            IsDirty.Value = true;
            //NOTE: Dirtyがtrue -> trueのまま切り替わらない場合でもポート番号のvalidityが変わった可能性があるのでチェックしに行く
            UpdateReceiveSettingInputValidity();
        }
        
        public ActionCommand ApplyChangeCommand { get; }
        public ActionCommand RevertChangeCommand { get; }
        public ActionCommand OpenDocUrlCommand { get; }
        public ActionCommand FixBodyMotionStyleCommand { get; }

        public ActionCommand ApplySendSettingsCommand { get; }
        public ActionCommand RevertSendSettingsCommand { get; }
        public ActionCommand OpenFullEditionDownloadUrlCommand { get; }

        private void UpdateReceiveSettingInputValidity()
        {
            HasInvalidPortNumber.Value =
                Source1.PortNumberIsInvalid.Value ||
                Source2.PortNumberIsInvalid.Value ||
                Source3.PortNumberIsInvalid.Value;

            CanApply.Value = IsDirty.Value && !HasInvalidPortNumber.Value;
        }

        private void LoadCurrentReceiveSettings()
        {
            var sources = _model.GetCurrentReceiveSetting().Sources;

            Source1 = new VMCPSourceItemViewModel(
                sources.Count > 0 ? sources[0] : new VMCPSource(),
                SetReceiveSettingsDirty);
            Source2 = new VMCPSourceItemViewModel(
                sources.Count > 1 ? sources[1] : new VMCPSource(),
                SetReceiveSettingsDirty);
            Source3 = new VMCPSourceItemViewModel(
                sources.Count > 2 ? sources[2] : new VMCPSource(),
                SetReceiveSettingsDirty);


            Source1.Connected.Value = sources.Count > 0 && _model.Connected[0];
            Source2.Connected.Value = sources.Count > 1 && _model.Connected[1];
            Source3.Connected.Value = sources.Count > 2 && _model.Connected[2];

            RaisePropertyChanged(nameof(Source1));
            RaisePropertyChanged(nameof(Source2));
            RaisePropertyChanged(nameof(Source3));
            UpdateBodyMotionStyleCorrectness();
            IsDirty.Value = false;
        }

        private void OnSerializedVMCPSourceSettingChanged(object? sender, PropertyChangedEventArgs e) 
            => LoadCurrentReceiveSettings();

        private void OnBodyMotionStyleCorrectnessMaybeChanged(object? sender, PropertyChangedEventArgs e)
            => UpdateBodyMotionStyleCorrectness();

        private void OnReceiveConnectStatusChanged(object? sender, EventArgs e) => ApplyReceiveConnectionStatus();

        private void OnSerializedVMCPSendSettingChanged(object? sender, PropertyChangedEventArgs e)
            => LoadCurrentSendSettings();

        private void ApplyReceiveConnectionStatus()
        {
            var connected = _model.Connected;
            if (connected.Count < 3)
            {
                return;
            }
            Source1.Connected.Value = connected[0];
            Source2.Connected.Value = connected[1];
            Source3.Connected.Value = connected[2];
        }

        private void ApplyReceiveSettings()
        {
            var setting = new VMCPSources(
            [
                Source1.CreateSetting(),
                Source2.CreateSetting(),
                Source3.CreateSetting(),
            ]);

            _model.SetVMCPSourceSetting(setting);
            IsDirty.Value = false;
        }

        private void RevertReceiveSettingsChange() => LoadCurrentReceiveSettings();

        private void UpdateBodyMotionStyleCorrectness()
        {
            var sourceHasHandTrackingOption =
                (!Source1.PortNumberIsInvalid.Value && Source1.ReceiveHandPose.Value) ||
                (!Source2.PortNumberIsInvalid.Value && Source2.ReceiveHandPose.Value) ||
                (!Source3.PortNumberIsInvalid.Value && Source3.ReceiveHandPose.Value);

            BodyMotionStyleIncorrectForHandTracking.Value =
                sourceHasHandTrackingOption &&
                _model.VMCPEnabled.Value && 
                _motionSettingModel.EnableNoHandTrackMode.Value;
        }

        private void SetSendSettingsDirty()
        {
            IsSendSettingsDirty.Value = true;
            //NOTE: Dirtyがtrue -> trueのままでもポート番号のvalidityが変わった可能性があるのでチェックする
            // (Receiveと同じ考え方)
            UpdateSendSettingsValidity();
        }

        private void UpdateSendSettingsValidity()
        {
            var port = SendPort.Value;

            // NOTE: Addressについては、Unity側で uOscClient.StartClient が内部的に IPAddress.Parse を使うのと揃えている
            HasInvalidSendPortNumber.Value = port < 0 || port > 65535;
            HasInvalidSendAddress.Value = !IPAddress.TryParse(SendAddress.Value, out _);

            CanApplySendSettings.Value = 
                IsSendSettingsDirty.Value && 
                !HasInvalidSendPortNumber.Value && 
                !HasInvalidSendAddress.Value;
        }


        private void RevertSendSettings() => LoadCurrentSendSettings();

        private void LoadCurrentSendSettings()
        {
            var sendSetting = _model.GetCurrentSendSetting();
            SendAddress.Value = sendSetting.SendAddress;
            SendPort.Value = sendSetting.SendPort;
            SendBonePose.Value = sendSetting.SendBonePose;
            SendFingerBonePose.Value = sendSetting.SendFingerBonePose;
            SendFacial.Value = sendSetting.SendFacial;
            SendNonStandardFacial.Value = sendSetting.SendNonStandardFacial;
            SendUseVrm0Facial.Value = sendSetting.UseVrm0Facial;
            SendPrefer30Fps.Value = sendSetting.Prefer30Fps;

            // NOTE:
            // - Applyしても変化がない可能性もあるが、それはOK (Model側でSendがガードされるはず)
            // - Applyの中でDirtyフラグがリセットされる前提でApplyだけ呼んでいる
            ApplySendSettings();
        }

        private void ApplySendSettings()
        {
            _model.SetVMCPSendSetting(new VMCPSendSetting()
            {
                SendAddress = SendAddress.Value,
                SendPort = SendPort.Value,
                SendBonePose = SendBonePose.Value,
                SendFingerBonePose = SendFingerBonePose.Value,
                SendFacial = SendFacial.Value,
                SendNonStandardFacial = SendNonStandardFacial.Value,
                UseVrm0Facial = SendUseVrm0Facial.Value,
                Prefer30Fps = SendPrefer30Fps.Value,
            });

            IsSendSettingsDirty.Value = false;
            CanApplySendSettings.Value = false;
        }

        private void OpenDocUrl()
        {
            var url = LocalizedString.GetString("URL_docs_vmc_protocol");
            UrlNavigate.Open(url);
        }

        private void OpenFullEditionUrl() 
            => UrlNavigate.Open("https://baku-dreameater.booth.pm/items/3064040");

        private void FixBodyMotionStyle()
        {
            _motionSettingModel.EnableNoHandTrackMode.Value = false;
            _motionSettingModel.EnableGameInputLocomotionMode.Value = false;
            SnackbarWrapper.Enqueue(LocalizedString.GetString("Snackbar_BodyMotionStyle_Set_Default"));
        }
    }
}
