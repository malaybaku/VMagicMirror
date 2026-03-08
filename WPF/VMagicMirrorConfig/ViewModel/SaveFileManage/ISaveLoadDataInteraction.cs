using System;
using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    internal interface ISaveLoadDataInteraction
    {
        Task<bool> ConfirmLoadAsync(int index);
        Task<bool> ConfirmSaveAsync(int index);
        void NotifyLoadCompleted(int index);
        void NotifySaveCompleted(int index);
        void BeginRefresh(Action refreshAction);
    }
}
