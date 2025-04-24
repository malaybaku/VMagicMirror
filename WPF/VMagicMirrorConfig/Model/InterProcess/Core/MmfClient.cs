using Baku.VMagicMirror;
using Baku.VMagicMirror.IpcMessage;
using Baku.VMagicMirror.Mmf;
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
            _client = new MemoryMappedFileConnector();
            _client.ReceiveCommand += OnReceivedCommand;
            _client.ReceiveQuery += OnReceivedQuery;
        }

        private readonly MemoryMappedFileConnector _client;

        private bool _isCompositeMode;
        //NOTE: 64というキャパシティはどんぶり勘定
        private readonly List<Message> _compositeMessages = new(64);

        public void SendMessage(Message message)
        {
            if (_isCompositeMode)
            {
                //同じコマンド名の古いメッセージは削除し、最新値だけ残す
                //設定更新のコマンドはsetterメソッド的なのでこういう事をしても大丈夫
                if (_compositeMessages.FindIndex(m => m.Command == message.Command) is int x && x >= 0)
                {
                    _compositeMessages.RemoveAt(x);
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

        async Task<string> IMessageSender.QueryMessageAsync(Message message)
        {
            try
            {
                var rawResponse = await _client.SendQueryAsync(message.Data);
                return new CommandReceivedData(rawResponse).GetStringValue();
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
                return "";
            }
        }

        void IMessageSender.StartCommandComposite()
        {
            _isCompositeMode = true;
        }

		void IMessageSender.EndCommandComposite()
        {
            _isCompositeMode = false;
            if (_compositeMessages.Count > 0)
            {
                SendMessage(MessageFactory.CommandArray(_compositeMessages));
                _compositeMessages.Clear();
            }
        }


        // TODO: Start/Stopいずれも、内部的にでもいいからキャンセルをちゃんと扱いたい…

        async void IMessageReceiver.Start()
        {
            //NOTE: この実装だと受信開始するまで送信もできない(ややヘンテコな感じがする)が、気にしないことにする
            string channelName = CommandLineArgParser.TryLoadMmfFileName(out var givenName)
                ? givenName
                : DefaultChannelName;
            await _client.StartAsClientAsync(channelName);
        }

        async void IMessageReceiver.Stop()
        {
            await _client.StopAsync();
        }

        /// <summary> コマンド受信時に、UIスレッド上で発火する。 </summary>
        public event Action<CommandReceivedData>? ReceivedCommand;

        /// <summary> クエリ受信時に、UIスレッド上で発火する。 </summary>
        public event EventHandler<QueryReceivedEventArgs>? ReceivedQuery;

        private void OnReceivedCommand(ReadOnlyMemory<byte> data)
        {
            App.Current.Dispatcher.BeginInvoke(new Action(
                () => ReceivedCommand?.Invoke(new CommandReceivedData(data))
                ));
        }

        private void OnReceivedQuery((int id, ReadOnlyMemory<byte> data) value)
        {
            var ea = new QueryReceivedEventArgs(value.data);

            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                ReceivedQuery?.Invoke(this, ea);

                var responseData = MessageSerializer.String((ushort)VmmCommands.Unknown, ea.Result ?? "");
                _client.SendQueryResponse(value.id, responseData);
            }));
        }
    }
}
