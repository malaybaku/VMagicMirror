using System;
using System.Threading.Tasks;
using Baku.VMagicMirror.IpcMessage;
using Baku.VMagicMirror.Mmf;
using Zenject;
using Encoding = System.Text.Encoding;

namespace Baku.VMagicMirror.InterProcess
{
    /// <summary> MemoryMappedFile越しでWPFと通信するクラス </summary>
    public class MmfBasedMessageIo : 
        IMessageReceiver, IMessageSender, IMessageDispatcher,
        IReleaseBeforeQuit, ITickable
    {
        [Inject]
        public MmfBasedMessageIo()
        {
            _server = new MemoryMappedFileConnector();
            _server.ReceiveCommand += OnReceiveCommand;
            _server.ReceiveQuery += OnReceiveQuery;
            _server.StartAsServer(MmfChannelIdSource.ChannelId);
        }

        private readonly IpcMessageDispatcher _dispatcher = new();
        private readonly MemoryMappedFileConnector _server;

        public event Action<Message> SendingMessage;
        public bool LastMessageSent => _server.LastMessageSent;
        
        public void SendCommand(Message message, bool isLastMessage = false)
        {
            try
            {
                SendingMessage?.Invoke(message);
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);                
            }

            _server.SendCommand(message.Data, isLastMessage);
        }

        public async Task<string> SendQueryAsync(Message message)
        {
            var response = await _server.SendQueryAsync(message.Data);
            return Encoding.UTF8.GetString(response.Span);
        }
        
        public void AssignCommandHandler(VmmCommands command, Action<ReceivedCommand> handler)
            => _dispatcher.AssignCommandHandler(command, handler);

        public void AssignQueryHandler(VmmCommands query, Action<ReceivedQuery> handler)
            => _dispatcher.AssignQueryHandler(query, handler);

        public void ReceiveCommand(ReceivedCommand command) => _dispatcher.ReceiveCommand(command);
        
        public void ReleaseBeforeCloseConfig()
        {
            //何もしない: この時点でメッセージI/Oは停止しないでもOK
        }

        public async Task ReleaseResources()
        {
            await _server.StopAsync();
        }

        void ITickable.Tick() => _dispatcher.Tick();

        private void OnReceiveCommand(ReadOnlyMemory<byte> data)
        {
            _dispatcher.ReceiveCommand(new ReceivedCommand(data));
        }
        
        private async void OnReceiveQuery((int id, ReadOnlyMemory<byte> data) value)
        {
            var res = await _dispatcher.ReceiveQuery(new ReceivedQuery(value.data));
            var body = Encoding.UTF8.GetBytes(res);
            _server.SendQueryResponse(value.id, body);
        }
    }
}
