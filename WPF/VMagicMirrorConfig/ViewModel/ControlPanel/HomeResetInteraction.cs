using System.Threading.Tasks;
using System.Windows;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    internal sealed class HomeResetInteraction : IHomeResetInteraction
    {
        public async Task<bool> ConfirmResetToDefaultAsync()
        {
            var indication = MessageIndication.ResetSettingConfirmation();
            return await MessageBoxWrapper.Instance.ShowAsync(
                indication.Title,
                indication.Content,
                MessageBoxWrapper.MessageBoxStyle.OKCancel
            );
        }

        public void CloseMainWindow() => Application.Current.MainWindow?.Close();
    }
}
