using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    internal sealed class VMCPDialogInteraction : IVMCPDialogInteraction
    {
        public async Task<bool> ConfirmEnableVMCPTabAsync()
        {
            var dialog = MessageIndication.EnableVMCPTab();
            return await MessageBoxWrapper.Instance.ShowAsync(
                dialog.Title, dialog.Content, MessageBoxWrapper.MessageBoxStyle.OKCancel
            );
        }

        public async Task<bool> ConfirmDisableVMCPTabAsync()
        {
            var dialog = MessageIndication.DisableVMCPTab();
            return await MessageBoxWrapper.Instance.ShowAsync(
                dialog.Title, dialog.Content, MessageBoxWrapper.MessageBoxStyle.OKCancel
            );
        }
    }
}
