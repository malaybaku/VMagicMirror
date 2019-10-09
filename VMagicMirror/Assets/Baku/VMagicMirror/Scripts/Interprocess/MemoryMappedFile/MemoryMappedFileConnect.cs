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
        private const long MemoryMappedFileCapacity = 65536;
        private readonly byte[] _readBuffer = new byte[65536];

        //送りたいメッセージ(クエリとコマンド両方)の一覧
        private readonly ConcurrentQueue<Message> writeMessageQueue = new ConcurrentQueue<Message>();

        //変身待ちクエリの一覧
        private readonly ConcurrentDictionary<int, TaskCompletionSource<string>> sendedQueries
            = new ConcurrentDictionary<int, TaskCompletionSource<string>>();

        private readonly object requestIdLock = new object();
        private int requestId = 0;
        private int RequestId
        {
            get { lock (requestIdLock) return requestId; }
        }

        private void IncrementRequestId()
        {
            lock (requestIdLock)
            {
                requestId++;
                //クエリのIDは1 ~ (int.MaxValue - 1)の範囲で回るようにしておく
                if (requestId == int.MaxValue)
                {
                    requestId = 1;
                }
            }
        }

        private MemoryMappedFile receiver;
        private MemoryMappedViewAccessor receiverAccessor;

        private MemoryMappedFile sender;
        private MemoryMappedViewAccessor senderAccessor;

        private CancellationTokenSource cts;

        public event EventHandler<ReceiveCommandEventArgs> ReceiveCommand;
        public event EventHandler<ReceiveQueryEventArgs> ReceiveQuery;

        public bool IsConnected { get; private set; } = false;

        public async Task StartInternal(string pipeName, bool isServer)
        {
            cts = new CancellationTokenSource();
            if (isServer)
            {
                receiver = MemoryMappedFile.CreateOrOpen(pipeName + "_receiver", MemoryMappedFileCapacity);
                sender = MemoryMappedFile.CreateOrOpen(pipeName + "_sender", MemoryMappedFileCapacity);
            }
            else
            {
                while (true)
                {
                    try
                    {
                        //サーバーと逆方向
                        receiver = MemoryMappedFile.OpenExisting(pipeName + "_sender");
                        sender = MemoryMappedFile.OpenExisting(pipeName + "_receiver");
                        break;
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                    }

                    if (cts.Token.IsCancellationRequested)
                    {
                        return;
                    }
                    await Task.Delay(100);
                }
            }
            receiverAccessor = receiver.CreateViewAccessor();
            senderAccessor = sender.CreateViewAccessor();
            if (isServer)
            {
                //前回実行時のデータが残る可能性があるので、明示的に未書き込み状態にする
                receiverAccessor.Write(0, (byte)0);
                senderAccessor.Write(0, (byte)0);
            }
            new Thread(() => ReadThread()).Start();
            new Thread(() => WriteThread()).Start();
            IsConnected = true;
        }

        public void SendCommand(string command)
            => writeMessageQueue.Enqueue(Message.Command(command));

        public Task<string> SendQueryAsync(string command)
        {
            IncrementRequestId();
            writeMessageQueue.Enqueue(Message.Query(command, RequestId));

            //すぐには返せないので完了予約だけ投げておしまい
            var source = new TaskCompletionSource<string>();
            sendedQueries.TryAdd(RequestId, source);

            return source.Task;
        }

        public void Stop()
        {
            IsConnected = false;
            cts?.Cancel();
            receiverAccessor?.Dispose();
            senderAccessor?.Dispose();
            receiver?.Dispose();
            sender?.Dispose();
            receiverAccessor = null;
            senderAccessor = null;
            receiver = null;
            sender = null;
        }

        private void SendQueryResponse(string command, int id)
            => writeMessageQueue.Enqueue(Message.Response(command, id));

        private void ReadThread()
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    while (receiverAccessor.ReadByte(0) != 1)
                    {
                        if (cts.Token.IsCancellationRequested)
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

        private void WriteThread()
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    Message msg = null;
                    //送るものが無いうちは待ち
                    while (!writeMessageQueue.TryDequeue(out msg))
                    {
                        if (cts.Token.IsCancellationRequested)
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
                    while (senderAccessor.ReadByte(0) != 0)
                    {
                        if (cts.Token.IsCancellationRequested)
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

        private void ReadMessage()
        {
            short messageType = receiverAccessor.ReadInt16(2);
            bool isReply = messageType > 0;
            int id = receiverAccessor.ReadInt32(4);
            int bodyLength = receiverAccessor.ReadInt32(8);

            receiverAccessor.ReadArray(12, _readBuffer, 0, bodyLength);
            string message = Encoding.UTF8.GetString(_readBuffer, 0, bodyLength);

            receiverAccessor.Write(0, (byte)0);

            //3パターンある
            // - こちらが投げたQueryの返答が戻ってきた
            // - Commandを受け取った
            // - Queryを受け取った
            if (isReply)
            {
                if (sendedQueries.TryRemove(id, out var src))
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

        private void WriteMessage(Message msg)
        {
            if (!IsConnected)
            {
                return;
            }

            senderAccessor.Write(2, (short)(msg.IsReply ? 1 : 0));
            senderAccessor.Write(4, msg.Id);

            byte[] data = Encoding.UTF8.GetBytes(msg.Text);
            senderAccessor.Write(8, data.Length);
            senderAccessor.WriteArray(12, data, 0, data.Length);

            senderAccessor.Write(0, (byte)1);
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
