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
        private const string ChannelName = "Baku.VMagicMirror";

        private MmfBasedMessageIo()
        {
            _server = new MemoryMappedFileConnectServer();
            _server.ReceiveCommand += OnReceiveCommand;
            _server.ReceiveQuery += OnReceiveQuery;
            //NOTE: awaitする意味がないのでawaitをつけず、かつコレは警告が出るので止めてます。
            //コンストラクタでいきなりStartするのがマナー悪い、というのは無くもないです
#pragma warning disable CS4014
            _server.Start(ChannelName);
#pragma warning restore CS4014
        }

        private readonly IpcMessageDispatcher _dispatcher = new IpcMessageDispatcher();
        private readonly MemoryMappedFileConnectServer _server;
        
        public void SendCommand(Message message) 
            => _server.SendCommand(message.Command + ":" + message.Content);

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

        private void OnReceiveCommand(object sender, ReceiveCommandEventArgs e)
        {
            string rawContent = e.Command;
            int i = FindColonCharIndex(rawContent);
            string command = (i == -1) ? rawContent : rawContent.Substring(0, i);
            string content = (i == -1) ? "" : rawContent.Substring(i + 1);

            _dispatcher.ReceiveCommand(new ReceivedCommand(command, content));
        }
        
        private async void OnReceiveQuery(object sender, ReceiveQueryEventArgs e)
        {
            string rawContent = e.Query.Query;
            int i = FindColonCharIndex(rawContent);
            string command = (i == -1) ? rawContent : rawContent.Substring(0, i);
            string content = (i == -1) ? "" : rawContent.Substring(i + 1);

            string res = await _dispatcher.ReceiveQuery(new ReceivedQuery(command, content));
            e.Query.Reply(res);
        }
        
        //コマンド名と引数名の区切り文字のインデックスを探します。
        private static int FindColonCharIndex(string s)
        {
            for (int i = 0; i < s.Length; i++)
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
