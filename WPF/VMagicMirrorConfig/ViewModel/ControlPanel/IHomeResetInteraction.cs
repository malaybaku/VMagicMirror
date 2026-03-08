using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    internal interface IHomeResetInteraction
    {
        Task<bool> ConfirmResetToDefaultAsync();
        void CloseMainWindow();
    }
}
