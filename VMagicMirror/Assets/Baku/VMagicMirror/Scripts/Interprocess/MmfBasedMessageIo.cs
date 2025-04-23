using System;
using System.Threading.Tasks;
using Baku.VMagicMirror.Mmf;
using Zenject;

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
            _server.SendCommand(message.Command + ":" + message.Content, isLastMessage);
        }

        public async Task<string> SendQueryAsync(Message message) 
            => await _server.SendQueryAsync(message.Command + ":" + message.Content);
        
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

        private void OnReceiveCommand(string rawContent)
        {
            var i = FindColonCharIndex(rawContent);
            var command = (i == -1) ? rawContent : rawContent[..i];
            var content = (i == -1) ? "" : rawContent[(i + 1)..];

            _dispatcher.ReceiveCommand(new ReceivedCommand(command, content));
        }
        
        private async void OnReceiveQuery((int id, string content) value)
        {
            var rawContent = value.content;
            var i = FindColonCharIndex(rawContent);
            var command = (i == -1) ? rawContent : rawContent[..i];
            var content = (i == -1) ? "" : rawContent[(i + 1)..];

            var res = await _dispatcher.ReceiveQuery(new ReceivedQuery(command, content));
            _server.SendQueryResponse(res, value.id);
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
