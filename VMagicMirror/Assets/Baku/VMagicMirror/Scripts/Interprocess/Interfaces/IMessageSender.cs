using System;
using System.Threading.Tasks;

namespace Baku.VMagicMirror
{
    public interface IMessageSender
    {
        void SendCommand(Message message);
        Task<string> SendQueryAsync(Message message);

        event Action<Message> SendingMessage;
    }
}
