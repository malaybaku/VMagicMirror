using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    internal interface IVMCPDialogInteraction
    {
        Task<bool> ConfirmEnableVMCPTabAsync();
        Task<bool> ConfirmDisableVMCPTabAsync();
    }
}
