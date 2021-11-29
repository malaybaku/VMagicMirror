using Baku.VMagicMirrorConfig.Mmf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig
{
    class MmfClient : IMessageSender, IMessageReceiver
    {
        /// <summary>
        /// コマンドライン引数が指定されてない場合に用いるMemoryMappedFileの名称のベース.
        /// おもにUnity側がエディタ実行 + ConfigAppがダブルクリック直接起動、というケースで使います。
        /// </summary>
        private const string DefaultChannelName = "Baku.VMagicMirror";

        public MmfClient()
        {
            _client = new MemoryMappedFileConnectClient();
            _client.ReceiveCommand += OnReceivedCommand;
            _client.ReceiveQuery += OnReceivedQuery;
        }

        private readonly MemoryMappedFileConnectClient _client;

        private bool _isCompositeMode = false;
        //NOTE: 64というキャパシティはどんぶり勘定です
        private readonly List<Message> _compositeMessages = new List<Message>(64);

        #region IMessageSender

        public void SendMessage(Message message)
        {
            if (_isCompositeMode)
            {
                //同じコマンド名の古いメッセージは削除し、最新値だけ残す
                //設定更新のコマンドはsetterメソッド的なのでこういう事をしても大丈夫
                if (_compositeMessages.FirstOrDefault(m => m.Command == message.Command) is Message msg)
                {
                    _compositeMessages.Remove(msg);
                }
                _compositeMessages.Add(message);
                return;
            }

            try
            {
                //NOTE: 前バージョンが投げっぱなし通信だったため、ここでも戻り値はとらない
                _client.SendCommand(message.Command + ":" + message.Content);
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        public async Task<string> QueryMessageAsync(Message message)
        {
            try
            {
                var response = await _client.SendQueryAsync(message.Command + ":" + message.Content);
                return response;
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
                return "";
            }
        }

        public void StartCommandComposite()
        {
            _isCompositeMode = true;
        }

        public void EndCommandComposite()
        {
            _isCompositeMode = false;
            if (_compositeMessages.Count > 0)
            {
                SendMessage(MessageFactory.Instance.CommandArray(_compositeMessages));
                _compositeMessages.Clear();
            }
        }

        #endregion

        #region IMessageReceiver

        public void Start()
        {
            //NOTE: この実装だと受信開始するまで送信もできない(ややヘンテコな感じがする)が、気にしないことにする
            StartAsync();
        }

        private async void StartAsync()
        {
            string channelName = CommandLineArgParser.TryLoadMmfFileName(out var givenName)
                ? givenName
                : DefaultChannelName;
            await _client.StartAsync(channelName);
        }

        public void Stop()
        {
            _client.Stop();
        }

        /// <summary> コマンド受信時に、UIスレッド上で発火する。 </summary>
        public event EventHandler<CommandReceivedEventArgs>? ReceivedCommand;

        /// <summary> クエリ受信時に、UIスレッド上で発火する。 </summary>
        public event EventHandler<QueryReceivedEventArgs>? ReceivedQuery;

        private void OnReceivedCommand(object? sender, ReceiveCommandEventArgs e)
        {
            string content = e.Command;
            int i = FindColonCharIndex(content);
            string command = (i == -1) ? content : content.Substring(0, i);
            string args = (i == -1) ? "" : content.Substring(i + 1);

            App.Current.Dispatcher.BeginInvoke(new Action(
                () => ReceivedCommand?.Invoke(this, new CommandReceivedEventArgs(command, args))
                ));
        }

        private void OnReceivedQuery(object? sender, ReceiveQueryEventArgs e)
        {
            string content = e.Query.Query;
            int i = FindColonCharIndex(content);
            string command = (i == -1) ? content : content.Substring(0, i);
            string args = (i == -1) ? "" : content.Substring(i + 1);

            var ea = new QueryReceivedEventArgs(command, args);

            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                ReceivedQuery?.Invoke(this, ea);
                e.Query.Reply(string.IsNullOrWhiteSpace(ea.Result) ? "" : ea.Result);
            }));
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

        #endregion

    }
}
