using System;
using System.Threading.Tasks;
using System.Windows;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    internal sealed class SaveLoadDataInteraction : ISaveLoadDataInteraction
    {
        public async Task<bool> ConfirmLoadAsync(int index)
        {
            var indication = MessageIndication.ConfirmSettingFileLoad();
            return await MessageBoxWrapper.Instance.ShowAsync(
                indication.Title,
                string.Format(indication.Content, index),
                MessageBoxWrapper.MessageBoxStyle.OKCancel
            );
        }

        public async Task<bool> ConfirmSaveAsync(int index)
        {
            var indication = MessageIndication.ConfirmSettingFileSave();
            return await MessageBoxWrapper.Instance.ShowAsync(
                indication.Title,
                string.Format(indication.Content, index),
                MessageBoxWrapper.MessageBoxStyle.OKCancel
            );
        }

        public void NotifyLoadCompleted(int index)
        {
            SnackbarWrapper.Enqueue(string.Format(
                LocalizedString.GetString("SettingFile_LoadCompleted"), index
            ));
        }

        public void NotifySaveCompleted(int index)
        {
            SnackbarWrapper.Enqueue(string.Format(
                LocalizedString.GetString("SettingFile_SaveCompleted"), index
            ));
        }

        public void BeginRefresh(Action refreshAction)
        {
            _ = Application.Current.Dispatcher.BeginInvoke(refreshAction);
        }
    }
}
