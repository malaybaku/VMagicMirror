using System;
using System.ComponentModel;
using System.Windows;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    //NOTE: (ちょっと変な挙動だがUI側が決め打ちなので) Modelがなんと言おうと3要素ぶんの編集しか認めていないことに注意
    public class VMCPControlPanelViewModel : SettingViewModelBase
    {
        public VMCPControlPanelViewModel() : this(
            ModelResolver.Instance.Resolve<VMCPSettingModel>())
        {
        }

        internal VMCPControlPanelViewModel(VMCPSettingModel model)
        {
            _model = model;
            IsDirty = new RProperty<bool>(false, _ => UpdateInputValidity());
            ApplyChangeCommand = new ActionCommand(ApplyChange);
            RevertChangeCommand = new ActionCommand(RevertChange);
            OpenDocUrlCommand = new ActionCommand(OpenDocUrl);

            if (!IsInDesignMode)
            {
                LoadCurrentSettings();
                WeakEventManager<VMCPSettingModel, EventArgs>.AddHandler(
                    _model, nameof(_model.ConnectedStatusChanged), OnConnectedStatusChanged
                    );
                _model.SerializedVMCPSourceSetting.AddWeakEventHandler(OnSerializedVMCPSourceSettingChanged);
                ApplyConnectionStatus();
            }
        }

        private readonly VMCPSettingModel _model;

        public RProperty<bool> IsDirty { get; }
        public RProperty<bool> CanApply { get; } = new(false);
        public RProperty<bool> HasInvalidPortNumber { get; } = new(false);

        public RProperty<bool> VMCPEnabled => _model.VMCPEnabled;
        public VMCPSourceItemViewModel Source1 { get; set; } = new();
        public VMCPSourceItemViewModel Source2 { get; set; } = new();
        public VMCPSourceItemViewModel Source3 { get; set; } = new();

        public RProperty<bool> DisableCameraDuringVMCPActive => _model.DisableCameraDuringVMCPActive;

        public void SetDirty()
        {
            IsDirty.Value = true;
            //NOTE: Dirtyがtrue -> trueのまま切り替わらない場合でもポート番号のvalidityが変わった可能性があるのでチェックしに行く
            UpdateInputValidity();
        }
        
        public ActionCommand ApplyChangeCommand { get; }
        public ActionCommand RevertChangeCommand { get; }
        public ActionCommand OpenDocUrlCommand { get; }

        private void UpdateInputValidity()
        {
            HasInvalidPortNumber.Value =
                Source1.PortNumberIsInvalid.Value ||
                Source2.PortNumberIsInvalid.Value ||
                Source3.PortNumberIsInvalid.Value;

            CanApply.Value = IsDirty.Value && !HasInvalidPortNumber.Value;
        }

        private void LoadCurrentSettings()
        {
            var sources = _model.GetCurrentSetting().Sources;

            Source1 = new VMCPSourceItemViewModel(
                sources.Count > 0 ? sources[0] : new VMCPSource(),
                SetDirty);
            Source2 = new VMCPSourceItemViewModel(
                sources.Count > 1 ? sources[1] : new VMCPSource(),
                SetDirty);
            Source3 = new VMCPSourceItemViewModel(
                sources.Count > 2 ? sources[2] : new VMCPSource(),
                SetDirty);


            Source1.Connected.Value = sources.Count > 0 && _model.Conneceted[0];
            Source2.Connected.Value = sources.Count > 1 && _model.Conneceted[1];
            Source3.Connected.Value = sources.Count > 2 && _model.Conneceted[2];

            RaisePropertyChanged(nameof(Source1));
            RaisePropertyChanged(nameof(Source2));
            RaisePropertyChanged(nameof(Source3));
            IsDirty.Value = false;
        }

        private void OnSerializedVMCPSourceSettingChanged(object? sender, PropertyChangedEventArgs e)
        {
            LoadCurrentSettings();
        }

        private void OnConnectedStatusChanged(object? sender, EventArgs e) => ApplyConnectionStatus();

        private void ApplyConnectionStatus()
        {
            var connected = _model.Conneceted;
            if (connected.Count < 3)
            {
                return;
            }
            Source1.Connected.Value = connected[0];
            Source2.Connected.Value = connected[1];
            Source3.Connected.Value = connected[2];
        }

        private void ApplyChange()
        {
            var setting = new VMCPSources(new[]
            {
                Source1.CreateSetting(),
                Source2.CreateSetting(),
                Source3.CreateSetting(),
            });

            _model.SetVMCPSourceSetting(setting);
            IsDirty.Value = false;
        }

        private void RevertChange() => LoadCurrentSettings();

        private void OpenDocUrl()
        {
            var url = LocalizedString.GetString("URL_docs_vmc_protocol");
            UrlNavigate.Open(url);
        }
    }
}
