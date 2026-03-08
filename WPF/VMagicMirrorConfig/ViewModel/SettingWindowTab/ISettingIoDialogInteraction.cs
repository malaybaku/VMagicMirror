using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    internal interface ISettingIoDialogInteraction
    {
        Task<bool> ConfirmEnableAutomationAsync();
        Task<bool> ConfirmDisableAutomationAsync();
        Task<bool> ConfirmToggleSkipLocalVrmLicenseCheckAsync(bool currentValue);
    }
}
