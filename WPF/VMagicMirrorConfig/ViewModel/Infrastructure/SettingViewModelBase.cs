using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    /// <summary> 
    /// ViewModelから直接メッセージI/Oがしたい場合に使える基底クラス
    /// </summary>
    public abstract class SettingViewModelBase : ViewModelBase
    {
        //private protected SettingViewModelBase(IMessageSender sender)
        //{
        //    Sender = sender;
        //}

        //private protected readonly IMessageSender Sender;

        //private protected virtual void SendMessage(Message message)
        //    => Sender.SendMessage(message);

        //private protected async Task<string> SendQueryAsync(Message message)
        //    => await Sender.QueryMessageAsync(message);
    }
}
