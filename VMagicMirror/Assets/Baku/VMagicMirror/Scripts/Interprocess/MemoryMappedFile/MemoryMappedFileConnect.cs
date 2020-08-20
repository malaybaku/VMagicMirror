using System;
using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

//https://github.com/malaybaku/UnityMemoryMappedFile
//Unityはサーバー役をする

//用語: 
// Message: Command, Query, Responseのいずれかのテキストメッセージの事。
// Command: 戻り値がない、投げっぱなしのメッセージ
// Query: 戻り値が欲しいメッセージで、受け取った側がじゅうぶん短時間で戻り値を構成できるようなもの。
// Response: Queryへの返信
// ※戻り値の計算に長時間かかる処理はCommandの往復で表現する

namespace Baku.VMagicMirror.Mmf
{
    /// <summary>
    /// MemoryMappedFileで通信するクライアント
    /// </summary>
    public class MemoryMappedFileConnectClient : MemoryMappedNamedConnectBase
    {
        public async Task StartAsync(string name) => await StartInternal(name, false);
    }

    /// <summary>
    /// MemoryMappedFileで通信するサーバー
    /// </summary>
    public class MemoryMappedFileConnectServer : MemoryMappedNamedConnectBase
    {
        public async Task Start(string name) => await StartInternal(name, true);
    }

    //メッセージのバイナリフォーマットについて
    //0-1: 書き込み状態フラグ。
    // - 0: 受信完了。このときはWriteする側だけが触ってよい
    // - 1: 送信完了。このときはReadする側だけが触ってよい

    //2-3: メッセージタイプ
    // - 0: 自発的に送信したメッセージ
    // - 1: レスポンスとして返すメッセージ

    //4-7: リクエストID
    // - 0: リクエストIDは無効で、投げっぱなしのメッセージを表す
    // - 1以上: リクエストIDは有効で、このとき受信した側は同じIDで文字列レスポンスを返す
    //          メッセージタイプが1(レスポンス)の場合、かならずこちらになる。

    //8-11: ボディのバイナリ長
    // - 12バイト以降に入ってるメッセージの長さ

    //12-: ボディ
    // - UTF8エンコードした文字列をバイナリ化したもの

    /// <summary>
    /// MMFを用いて、投げっぱなしコマンドとレスポンス必須コマンドの2種類が送受信できる凄いやつだよ
    /// </summary>
    public abstract class MemoryMappedNamedConnectBase
    {
        private const long MemoryMappedFileCapacity = 131072;
        private readonly byte[] _readBuffer = new byte[131072];

        //送りたいメッセージ(クエリとコマンド両方)の一覧
        private readonly ConcurrentQueue<Message> _writeMessageQueue = new ConcurrentQueue<Message>();

        //返信待ちクエリの一覧
        private readonly ConcurrentDictionary<int, TaskCompletionSource<string>> _sendedQueries
            = new ConcurrentDictionary<int, TaskCompletionSource<string>>();

        private readonly object _requestIdLock = new object();
        private int _requestId = 0;
        private int RequestId
        {
            get { lock (_requestIdLock) return _requestId; }
        }

        private void IncrementRequestId()
        {
            lock (_requestIdLock)
            {
                _requestId++;
                //クエリのIDは1 ~ (int.MaxValue - 1)の範囲で回るようにしておく
                if (_requestId == int.MaxValue)
                {
                    _requestId = 1;
                }
            }
        }

        private readonly object _receiverLock = new object();
        private MemoryMappedFile _receiver;
        private MemoryMappedViewAccessor _receiverAccessor;

        private readonly object _senderLock = new object();
        private MemoryMappedFile _sender;
        private MemoryMappedViewAccessor _senderAccessor;

        private Thread _sendThread = null;
        private Thread _receiveThread = null;
        private CancellationTokenSource _cts;

        public event EventHandler<ReceiveCommandEventArgs> ReceiveCommand;
        public event EventHandler<ReceiveQueryEventArgs> ReceiveQuery;

        public bool IsConnected { get; private set; } = false;

        public async Task StartInternal(string pipeName, bool isServer)
        {
            _cts = new CancellationTokenSource();
            if (isServer)
            {
                _receiver = MemoryMappedFile.CreateOrOpen(pipeName + "_receiver", MemoryMappedFileCapacity);
                _sender = MemoryMappedFile.CreateOrOpen(pipeName + "_sender", MemoryMappedFileCapacity);
            }
            else
            {
                while (true)
                {
                    try
                    {
                        //サーバーと逆方向
                        _receiver = MemoryMappedFile.OpenExisting(pipeName + "_sender");
                        _sender = MemoryMappedFile.OpenExisting(pipeName + "_receiver");
                        break;
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                    }

                    if (_cts.Token.IsCancellationRequested)
                    {
                        return;
                    }
                    await Task.Delay(100);
                }
            }
            _receiverAccessor = _receiver.CreateViewAccessor();
            _senderAccessor = _sender.CreateViewAccessor();

            _receiveThread = new Thread(() => ReadThread());
            _receiveThread.Start();
            _sendThread = new Thread(() => WriteThread());
            _sendThread.Start();
            IsConnected = true;
        }

        public void SendCommand(string command)
            => _writeMessageQueue.Enqueue(Message.Command(command));

        public Task<string> SendQueryAsync(string command)
        {
            IncrementRequestId();
            _writeMessageQueue.Enqueue(Message.Query(command, RequestId));

            //すぐには返せないので完了予約だけ投げておしまい
            var source = new TaskCompletionSource<string>();
            _sendedQueries.TryAdd(RequestId, source);

            return source.Task;
        }

        public async Task StopAsync()
        {
            IsConnected = false;
            _cts?.Cancel();
            
            lock (_receiverLock)
            {
                _receiverAccessor?.Dispose();
                _receiver?.Dispose();
                _receiverAccessor = null;
                _receiver = null;
            }

            lock (_senderLock)
            {
                _senderAccessor?.Dispose();
                _sender?.Dispose();
                _senderAccessor = null;
                _sender = null;
            }

            await Task.Run(() =>
            {
                _receiveThread.Join();
                _sendThread.Join();
            });
        }

        private void SendQueryResponse(string command, int id)
            => _writeMessageQueue.Enqueue(Message.Response(command, id));

        private void ReadThread()
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    while (!CheckReceivedMessageExists())
                    {
                        if (_cts.Token.IsCancellationRequested)
                        {
                            return;
                        }
                        Thread.Sleep(1);
                    }
                    ReadMessage();
                }
            }
            catch (NullReferenceException)
            {
            }
        }

        private bool CheckReceivedMessageExists()
        {
            lock (_receiverLock)
            {
                return 
                    _receiverAccessor != null && 
                    _receiverAccessor.ReadByte(0) == 1;
            }
        }
  
        private void ReadMessage()
        {
            string message = "";
            bool isReply = false;
            int id = 0;

            lock (_receiverLock)
            {
                if (_receiverAccessor == null)
                {
                    return;
                }
                
                short messageType = _receiverAccessor.ReadInt16(2);
                isReply = messageType > 0;
                id = _receiverAccessor.ReadInt32(4);
                int bodyLength = _receiverAccessor.ReadInt32(8);

                _receiverAccessor.ReadArray(12, _readBuffer, 0, bodyLength);
                message = Encoding.UTF8.GetString(_readBuffer, 0, bodyLength);
                _receiverAccessor.Write(0, (byte)0);
            }


            //3パターンある
            // - こちらが投げたQueryの返答が戻ってきた
            // - Commandを受け取った
            // - Queryを受け取った
            if (isReply)
            {
                if (_sendedQueries.TryRemove(id, out var src))
                {
                    //戻ってきた結果によってTaskが完了となる
                    src.SetResult(message);
                }
            }
            else if (id == 0)
            {
                ReceiveCommand?.Invoke(this, new ReceiveCommandEventArgs(message));
            }
            else
            {
                var query = new ReceivedQuery(message, id, this);
                ReceiveQuery?.Invoke(this, new ReceiveQueryEventArgs(query));
            }
        }

        private void WriteThread()
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    Message msg = null;
                    //送るものが無いうちは待ち
                    while (!_writeMessageQueue.TryDequeue(out msg))
                    {
                        if (_cts.Token.IsCancellationRequested)
                        {
                            return;
                        }
                        Thread.Sleep(1);
                    }

                    if (msg == null)
                    {
                        //来ないはずだが念のため
                        continue;
                    }

                    //書き込みOKになるまで待ち
                    while (!CheckCanWriteMessage())
                    {
                        if (_cts.Token.IsCancellationRequested)
                        {
                            return;
                        }
                        Thread.Sleep(1);
                    }

                    WriteMessage(msg);
                }
            }
            catch (NullReferenceException)
            {
            }
        }

        private bool CheckCanWriteMessage()
        {
            lock (_senderLock)
            {
                return
                    _senderAccessor != null &&
                    _senderAccessor.ReadByte(0) == 0;
            }
        }

        private void WriteMessage(Message msg)
        {
            if (!IsConnected)
            {
                return;
            }

            lock (_senderLock)
            {
                if (_senderAccessor == null)
                {
                    return;
                }
                
                _senderAccessor.Write(2, (short)(msg.IsReply ? 1 : 0));
                _senderAccessor.Write(4, msg.Id);

                byte[] data = Encoding.UTF8.GetBytes(msg.Text);
                _senderAccessor.Write(8, data.Length);
                _senderAccessor.WriteArray(12, data, 0, data.Length);

                _senderAccessor.Write(0, (byte)1);
            }
        }
        
        public class ReceivedQuery
        {
            public ReceivedQuery(string content, int id, MemoryMappedNamedConnectBase pipe)
            {
                Query = content;
                _id = id;
                _pipe = pipe;
            }

            private readonly MemoryMappedNamedConnectBase _pipe;
            private readonly int _id;

            public bool HasReplyCompleted { get; private set; }
            public string Query { get; }

            public void Reply(string content)
            {
                //2回やらない
                if (HasReplyCompleted)
                {
                    return;
                }

                _pipe.SendQueryResponse(content, _id);
                HasReplyCompleted = true;
            }
        }

        class Message
        {
            private Message(string text, bool isReply, int id)
            {
                Text = text;
                IsReply = isReply;
                Id = id;
            }

            /// <summary>
            /// メッセージのボディ部
            /// </summary>
            public string Text { get; }

            /// <summary>
            /// このメッセージが相手からのクエリの返信かどうか
            /// </summary>
            public bool IsReply { get; }

            /// <summary>
            /// こちらからのコマンド送信の場合は0、
            /// こちらからのクエリ送信の場合は送るクエリのID、
            /// 相手へのクエリ返信の場合は相手が送ってきたクエリのID
            /// </summary>
            public int Id { get; }

            public static Message Command(string text) => new Message(text, false, 0);
            public static Message Query(string text, int id) => new Message(text, false, id);
            public static Message Response(string text, int id) => new Message(text, true, id);
        }
    }   
}
