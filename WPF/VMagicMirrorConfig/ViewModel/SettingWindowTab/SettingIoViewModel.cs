using System.ComponentModel;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class SettingIoViewModel : SettingViewModelBase
    {
        public SettingIoViewModel() : this(
            ModelResolver.Instance.Resolve<AutomationSettingModel>(),
            ModelResolver.Instance.Resolve<PreferenceSettingModel>(),
            ModelResolver.Instance.Resolve<ISettingIoDialogInteraction>()
            )
        {
        }

        internal SettingIoViewModel(
            AutomationSettingModel model,
            PreferenceSettingModel preferenceSettingModel,
            ISettingIoDialogInteraction dialogInteraction)
        {
            _model = model;
            _preferenceSettingModel = preferenceSettingModel;
            _dialogInteraction = dialogInteraction;

            OpenInstructionUrlCommand = new ActionCommand(OpenInstructionUrl);
            RequestEnableAutomationCommand = new ActionCommand(OnEnableAutomationRequested);
            RequestDisableAutomationCommand = new ActionCommand(OnDisableAutomationRequested);
            ApplyPortNumberCommand = new ActionCommand(ApplyPortNumber);
            ToggleSkipLocalVrmLicenseCheckCommand = new ActionCommand(ToggleSkipLocalVrmLicenseCheck);

            if (IsInDesignMode)
            {
                AutomationPortNumberText = new RProperty<string>("");
                return;
            }

            AutomationPortNumberText = new RProperty<string>(
                _model.AutomationPortNumber.Value.ToString(), v =>
                {
                    //フォーマット違反になってないかチェック
                    PortNumberIsInvalid.Value = !(int.TryParse(v, out int i) && i >= 0 && i < 65536);
                });

            _model.AutomationPortNumber.AddWeakEventHandler(OnAutomationPortNumberChanged);
        }

        private readonly AutomationSettingModel _model;
        private readonly PreferenceSettingModel _preferenceSettingModel;
        private readonly ISettingIoDialogInteraction _dialogInteraction;

        public RProperty<bool> IsAutomationEnabled => _model.IsAutomationEnabled;

        public RProperty<string> AutomationPortNumberText { get; }
        //NOTE: Converter使うのも違う気がするのでViewModel層でやってしまう
        public RProperty<bool> PortNumberIsInvalid { get; } = new RProperty<bool>(false);
        public RProperty<bool> SkipLocalVrmLicenseCheck => _preferenceSettingModel.SkipLocalVrmLicenseCheck;

        public ActionCommand OpenInstructionUrlCommand { get; }
        public ActionCommand RequestEnableAutomationCommand { get; }
        public ActionCommand RequestDisableAutomationCommand { get; }
        public ActionCommand ApplyPortNumberCommand { get; }
        public ActionCommand ToggleSkipLocalVrmLicenseCheckCommand { get; }

        private async void OnEnableAutomationRequested()
        {
            var result = await _dialogInteraction.ConfirmEnableAutomationAsync();

            if (result)
            {
                _model.IsAutomationEnabled.Value = true;
            }
        }

        private async void OnDisableAutomationRequested()
        {
            var result = await _dialogInteraction.ConfirmDisableAutomationAsync();

            if (result)
            {
                _model.IsAutomationEnabled.Value = false;
            }
        }

        private void ApplyPortNumber()
        {
            if (int.TryParse(AutomationPortNumberText.Value, out int i) && i >= 0 && i < 65536)
            {
                _model.AutomationPortNumber.Value = i;
            }
        }

        //NOTE: オートメーションの説明ではあるけど設定ファイルタブ全体の設定に飛ばす。
        //どのみちファイルI/Oがどうなってるか説明する必要あるので
        private void OpenInstructionUrl()
            => UrlNavigate.Open(LocalizedString.GetString("URL_docs_setting_files"));

        private void OnAutomationPortNumberChanged(object? sender, PropertyChangedEventArgs e)
        {
            AutomationPortNumberText.Value = _model.AutomationPortNumber.Value.ToString();
        }

        private async void ToggleSkipLocalVrmLicenseCheck()
        {
            var result = await _dialogInteraction
                .ConfirmToggleSkipLocalVrmLicenseCheckAsync(SkipLocalVrmLicenseCheck.Value);

            if (result)
            {
                _preferenceSettingModel.SkipLocalVrmLicenseCheck.Value = 
                    !_preferenceSettingModel.SkipLocalVrmLicenseCheck.Value;
            }
        }
    }
}
