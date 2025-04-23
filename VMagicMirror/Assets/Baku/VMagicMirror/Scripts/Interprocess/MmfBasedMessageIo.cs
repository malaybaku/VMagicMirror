using System;
using System.Threading.Tasks;
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

            var body = Encoding.UTF8.GetBytes(
                message.Command + ":" + message.Content
            );
            _server.SendCommand(body, isLastMessage);
        }

        public async Task<string> SendQueryAsync(Message message)
        {
            var body = Encoding.UTF8.GetBytes(message.Command + ":" + message.Content);
            var response = await _server.SendQueryAsync(body);
            return Encoding.UTF8.GetString(response.Span);
        }
        
        public void AssignCommandHandler(string command, Action<ReceivedCommand> handler)
            => _dispatcher.AssignCommandHandler(command, handler);

        public void AssignQueryHandler(string query, Action<ReceivedQuery> handler)
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

        public void Tick() => _dispatcher.Tick();

        private void OnReceiveCommand(ReadOnlyMemory<byte> data)
        {
            var rawContent = Encoding.UTF8.GetString(data.Span);

            var i = FindColonCharIndex(rawContent);
            var command = (i == -1) ? rawContent : rawContent[..i];
            var content = (i == -1) ? "" : rawContent[(i + 1)..];

            _dispatcher.ReceiveCommand(new ReceivedCommand(command, content));
        }
        
        private async void OnReceiveQuery((int id, ReadOnlyMemory<byte> data) value)
        {
            var rawContent = Encoding.UTF8.GetString(value.data.Span);
            var i = FindColonCharIndex(rawContent);
            var command = (i == -1) ? rawContent : rawContent[..i];
            var content = (i == -1) ? "" : rawContent[(i + 1)..];

            var res = await _dispatcher.ReceiveQuery(new ReceivedQuery(command, content));
            var body = Encoding.UTF8.GetBytes(res);
            _server.SendQueryResponse(value.id, body);
        }
        
        //コマンド名と引数名の区切り文字のインデックスを探します。
        private static int FindColonCharIndex(string s)
        {
            for (var i = 0; i < s.Length; i++)
            {
                if (s[i] == ':')
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
