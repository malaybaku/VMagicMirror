using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    internal sealed class SettingIoDialogInteraction : ISettingIoDialogInteraction
    {
        public async Task<bool> ConfirmEnableAutomationAsync()
        {
            var indication = MessageIndication.EnableAutomation();
            return await MessageBoxWrapper.Instance.ShowAsync(
                indication.Title, indication.Content, MessageBoxWrapper.MessageBoxStyle.OKCancel
            );
        }

        public async Task<bool> ConfirmDisableAutomationAsync()
        {
            var indication = MessageIndication.DisableAutomation();
            return await MessageBoxWrapper.Instance.ShowAsync(
                indication.Title, indication.Content, MessageBoxWrapper.MessageBoxStyle.OKCancel
            );
        }

        public async Task<bool> ConfirmToggleSkipLocalVrmLicenseCheckAsync(bool currentValue)
        {
            var indication = currentValue
                ? MessageIndication.DisableSkipLocalVrmLicenseCheck()
                : MessageIndication.SkipLocalVrmLicenseCheck();

            return await MessageBoxWrapper.Instance.ShowAsync(
                indication.Title, indication.Content, MessageBoxWrapper.MessageBoxStyle.OKCancel
            );
        }
    }
}
