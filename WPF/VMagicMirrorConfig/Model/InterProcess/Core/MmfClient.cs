using Baku.VMagicMirror.Mmf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            _client = new MemoryMappedFileConnector();
            _client.ReceiveCommand += OnReceivedCommand;
            _client.ReceiveQuery += OnReceivedQuery;
        }

        private readonly MemoryMappedFileConnector _client;

        private bool _isCompositeMode;
        //NOTE: 64というキャパシティはどんぶり勘定
        private readonly List<Message> _compositeMessages = new(64);

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
                _client.SendCommand(message.Data);
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
                var rawResponse = await _client.SendQueryAsync(message.Data);
                return Encoding.UTF8.GetString(rawResponse.Span);
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

        // TODO: Start/Stopいずれも、内部的にでもいいからキャンセルをちゃんと扱いたい…

        public async void Start()
        {
            //NOTE: この実装だと受信開始するまで送信もできない(ややヘンテコな感じがする)が、気にしないことにする
            string channelName = CommandLineArgParser.TryLoadMmfFileName(out var givenName)
                ? givenName
                : DefaultChannelName;
            await _client.StartAsClientAsync(channelName);
        }

        public async void Stop()
        {
            await _client.StopAsync();
        }

        /// <summary> コマンド受信時に、UIスレッド上で発火する。 </summary>
        public event Action<CommandReceivedData>? ReceivedCommand;

        /// <summary> クエリ受信時に、UIスレッド上で発火する。 </summary>
        public event EventHandler<QueryReceivedEventArgs>? ReceivedQuery;

        private void OnReceivedCommand(ReadOnlyMemory<byte> data)
        {
            var content = Encoding.UTF8.GetString(data.Span);
            var i = FindColonCharIndex(content);
            var command = (i == -1) ? content : content[..i];
            var args = (i == -1) ? "" : content[(i + 1)..];

            App.Current.Dispatcher.BeginInvoke(new Action(
                () => ReceivedCommand?.Invoke(new CommandReceivedData(command, args))
                ));
        }

        private void OnReceivedQuery((int id, ReadOnlyMemory<byte> data) value)
        {
            var content = Encoding.UTF8.GetString(value.data.Span);
            var i = FindColonCharIndex(content);
            var command = (i == -1) ? content : content[..i];
            var args = (i == -1) ? "" : content[(i + 1)..];

            var ea = new QueryReceivedEventArgs(command, args);

            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                ReceivedQuery?.Invoke(this, ea);
                var resultBody = Encoding.UTF8.GetBytes(ea.Result ?? "");
                _client.SendQueryResponse(value.id, resultBody);
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
