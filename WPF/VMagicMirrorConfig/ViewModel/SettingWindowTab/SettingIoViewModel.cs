using System.ComponentModel;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class SettingIoViewModel : SettingViewModelBase
    {
        public SettingIoViewModel() : this(
            ModelResolver.Instance.Resolve<AutomationSettingModel>()
            )
        {
        }

        internal SettingIoViewModel(AutomationSettingModel model)
        {
            _model = model;

            OpenInstructionUrlCommand = new ActionCommand(OpenInstructionUrl);
            RequestEnableAutomationCommand = new ActionCommand(OnEnableAutomationRequested);
            RequestDisableAutomationCommand = new ActionCommand(OnDisableAutomationRequested);
            ApplyPortNumberCommand = new ActionCommand(ApplyPortNumber);

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

        public RProperty<bool> IsAutomationEnabled => _model.IsAutomationEnabled;

        public RProperty<string> AutomationPortNumberText { get; }
        //NOTE: Converter使うのも違う気がするのでViewModel層でやってしまう
        public RProperty<bool> PortNumberIsInvalid { get; } = new RProperty<bool>(false);

        public ActionCommand OpenInstructionUrlCommand { get; }
        public ActionCommand RequestEnableAutomationCommand { get; }
        public ActionCommand RequestDisableAutomationCommand { get; }
        public ActionCommand ApplyPortNumberCommand { get; }

        private async void OnEnableAutomationRequested()
        {
            var indication = MessageIndication.EnableAutomation();
            var result = await MessageBoxWrapper.Instance.ShowAsync(
                indication.Title, indication.Content, MessageBoxWrapper.MessageBoxStyle.OKCancel
                );

            if (result)
            {
                _model.IsAutomationEnabled.Value = true;
            }
        }

        private async void OnDisableAutomationRequested()
        {
            var indication = MessageIndication.DisableAutomation();
            var result = await MessageBoxWrapper.Instance.ShowAsync(
                indication.Title, indication.Content, MessageBoxWrapper.MessageBoxStyle.OKCancel
                );

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
    }
}
