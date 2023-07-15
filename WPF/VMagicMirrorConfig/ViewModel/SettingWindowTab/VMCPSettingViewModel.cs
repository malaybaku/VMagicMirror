namespace Baku.VMagicMirrorConfig.ViewModel
{
    //NOTE: (ちょっと変な挙動だがUI側が決め打ちなので) Modelがなんと言おうと3要素ぶんの編集しか認めていないことに注意
    public class VMCPSettingViewModel : SettingViewModelBase
    {
        public VMCPSettingViewModel() : this(
            ModelResolver.Instance.Resolve<VMCPSettingModel>())
        {
        }

        internal VMCPSettingViewModel(VMCPSettingModel model)
        {
            _model = model;
            _isDirty = new RProperty<bool>(false, _ => UpdateCanApply());
            ApplyChangeCommand = new ActionCommand(ApplyChange);
            RevertChangeCommand = new ActionCommand(RevertChange);
            OpenDocUrlCommand = new ActionCommand(OpenDocUrl);

            if (!IsInDesignMode)
            {
                LoadCurrentSettings();
            }
        }

        private readonly VMCPSettingModel _model;

        private readonly RProperty<bool> _isDirty;
        public RProperty<bool> CanApply = new(false);

        public RProperty<bool> VMCPEnabled => _model.VMCPEnabled;
        public VMCPSourceItemViewModel Source1 { get; private set; } = new();
        public VMCPSourceItemViewModel Source2 { get; private set; } = new();
        public VMCPSourceItemViewModel Source3 { get; private set; } = new();

        public void SetDirty() => _isDirty.Value = true;
        
        public ActionCommand ApplyChangeCommand { get; }
        public ActionCommand RevertChangeCommand { get; }
        public ActionCommand OpenDocUrlCommand { get; }

        private void UpdateCanApply()
        {
            CanApply.Value = _isDirty.Value &&
                !Source1.PortNumberIsInvalid.Value &&
                !Source2.PortNumberIsInvalid.Value &&
                !Source3.PortNumberIsInvalid.Value;
        }

        private void LoadCurrentSettings()
        {
            var sources = _model.GetCurrentSetting().Sources;

            Source1 = new VMCPSourceItemViewModel(
                sources.Count > 0 ? sources[0] : new VMCPSource(),
                this);
            Source2 = new VMCPSourceItemViewModel(
                sources.Count > 1 ? sources[1] : new VMCPSource(),
                this);
            Source3 = new VMCPSourceItemViewModel(
                sources.Count > 2 ? sources[2] : new VMCPSource(),
                this);

            RaisePropertyChanged(nameof(Source1));
            RaisePropertyChanged(nameof(Source2));
            RaisePropertyChanged(nameof(Source3));
            _isDirty.Value = false;
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
            _isDirty.Value = false;
        }

        private void RevertChange() => LoadCurrentSettings();

        private void OpenDocUrl()
        {
            var url = LocalizedString.GetString("URL_docs_vmc_protocol");
            UrlNavigate.Open(url);
        }
    }
}
