namespace Baku.VMagicMirrorConfig.ViewModel
{
    //NOTE: (ちょっと変な挙動だがUI側が決め打ちなので) Modelがなんと言おうと3要素ぶんの編集しか認めていないことに注意
    public class VMCPSettingViewModel : SettingViewModelBase
    {
        public VMCPSettingViewModel() : this(
            ModelResolver.Instance.Resolve<VMCPSettingModel>(),
            ModelResolver.Instance.Resolve<PreferenceSettingModel>())
        {
        }

        internal VMCPSettingViewModel(
            VMCPSettingModel settingModel,
            PreferenceSettingModel preferenceModel)
        {
            _settingModel = settingModel;
            _preferenceModel = preferenceModel;
            EnableVMCPTabOnControlPanelCommand = new ActionCommand(EnableVMCPTab);
            DisableVMCPTabOnControlPanelCommand = new ActionCommand(DisableVMCPTab);
            OpenDocUrlCommand = new ActionCommand(OpenDocUrl);
        }

        private readonly VMCPSettingModel _settingModel;
        private readonly PreferenceSettingModel _preferenceModel;

        public RProperty<bool> ShowVMCPTabOnControlPanel => _preferenceModel.ShowVMCPTabOnControlPanel;

        public ActionCommand EnableVMCPTabOnControlPanelCommand { get; }
        public ActionCommand DisableVMCPTabOnControlPanelCommand { get; }
        public ActionCommand OpenDocUrlCommand { get; }

        public async void EnableVMCPTab()
        {
            var dialog = MessageIndication.EnableVMCPTab();
            var result = await MessageBoxWrapper.Instance.ShowAsync(dialog.Title, dialog.Content, MessageBoxWrapper.MessageBoxStyle.OKCancel);
            if (result)
            {
                _preferenceModel.ShowVMCPTabOnControlPanel.Value = true;
            }
        }

        public async void DisableVMCPTab()
        {
            var dialog = MessageIndication.DisableVMCPTab();
            var result = await MessageBoxWrapper.Instance.ShowAsync(dialog.Title, dialog.Content, MessageBoxWrapper.MessageBoxStyle.OKCancel);
            if (result)
            {
                _preferenceModel.ShowVMCPTabOnControlPanel.Value = false;
                _settingModel.VMCPEnabled.Value = false;
            }
        }

        private void OpenDocUrl()
        {
            var url = LocalizedString.GetString("URL_docs_vmc_protocol");
            UrlNavigate.Open(url);
        }
    }
}
