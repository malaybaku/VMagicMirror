// NOTE: このファイルはUnityとWPF双方から参照されていることに注意
#nullable enable
using System;
using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;

// NOTE: このクラス及びInternalWriter, InternalReaderはUnityとWPFで同じ実装を使っている。

//用語: 
// Message: Command, Query, Responseのいずれかのテキストメッセージ
//   Content: Messageの実態であるようなstring
// Command: 戻り値がない、投げっぱなしのメッセージ
// Query: 戻り値が欲しいメッセージで、受け取った側がじゅうぶん短時間で戻り値を構成できるようなもの。
// Response: Queryへの返信
// ※戻り値の計算に長時間かかる処理はCommandの往復で表現する

// 読み書きするデータ単位 (単一メッセージ or メッセージの一部) のバイナリフォーマットについて
//0: 状態フラグ (byte)
// - MessageStateEmpty, MessageStateDataExist, MessageStateRewindのいずれかの値を取る

//1: メッセージの種類 (byte)
// - 0: CommandまたはQuery
// - 1: Response

//2-3: リクエストID (ushort)
// - 0: Commandの場合この値を指定して、投げっぱなしのメッセージを表す
// - 1以上: Queryの場合は返信に使ってほしいID値を表す。Responseの場合、どのQueryに対してのレスポンスであるかをIDで指定する。
//   - Query用のIDは送信側が一意性を保つように生成する

//4-7: メッセージ全体のバイナリ長 (int)
// - メッセージが短い場合、1つのデータに全データが収まるため、ここの値は12-15バイトに入る値と等しい
// - メッセージが長い場合は分けて送るため、12-15の値とココの値が一致しなくなる

//8-11: このチャンクのバイナリ長 (int)
// - 16バイト以降に入ってるメッセージの長さ
//   - NOTE: データがメッセージの一部である場合にoffsetを指定することが考えられるが、
// 　        メッセージには順序保証があってoffsetは不要なはずなので、offset情報はない。

//12-: ボディ (byte[])
// - 呼び出し元が指定したバイナリ、またはそのバイナリの一部

namespace Baku.VMagicMirror.Mmf
{
    /// <summary>
    /// <see cref="MemoryMappedFile"/> を内部的に使ってで送りっぱなしのコマンドとレスポンスが必要なクエリを送受信できるやつだよ
    /// </summary>
    public sealed partial class MemoryMappedFileConnector
    {
        private const long MemoryMappedFileCapacity = 262114;
        private const int MessageHeaderSize = 12;

        // データが書き込まれてない場合のチャンクの冒頭バイトの値
        private const byte MessageStateEmpty = 0;
        // 読み込み対象になるようなデータが書き込まれている場合のチャンクの冒頭バイトの値
        private const byte MessageStateDataExist = 1;
        // ファイルの末尾に到達しており、読み/書き双方が先頭に戻るべきであることを示すチャンク(※チャンクといっても1byteだが)の冒頭バイトの値。
        private const byte MessageStateRewind = 2;

        // NOTE: Command, Query, Responseで0,1,2を割り当てても別によい(必要ないから区別してないだけ)
        private const byte MessageTypeCommandOrQuery = 0;
        private const byte MessageTypeResponse = 1;
        
        private readonly InternalWriter _writer = new();
        private readonly InternalReader _reader = new();
        private readonly CancellationTokenSource _cts = new();

        // writerが送信して返信待ちのクエリ一覧
        private readonly ConcurrentDictionary<int, TaskCompletionSource<ReadOnlyMemory<byte>>> _queries = new();
        
        private Task? _readerTask;
        private Task? _writerTask;
        
        private readonly object _requestIdLock = new();
        private ushort _requestId;
        
        public event Action<ReadOnlyMemory<byte>>? ReceiveCommand
        {
            add => _reader.ReceiveCommand += value;
            remove => _reader.ReceiveCommand -= value;
        }
        
        public event Action<(ushort queryId, ReadOnlyMemory<byte> content)>? ReceiveQuery
        {
            add => _reader.ReceiveQuery += value;
            remove => _reader.ReceiveQuery -= value;
        }

        public bool LastMessageSent => _writer.LastMessageSent;

        public MemoryMappedFileConnector()
        {
            // NOTE: 完了待ちクエリの一覧を(writerではなく)本クラスが直接持っている方が自然かもしれない…
            _reader.ReceiveQueryResponse += value =>
            {
                var (id, content) = value;
                if (_queries.TryRemove(id, out var source))
                {
                    source.SetResult(content);
                }
            };
        }

        public void StartAsServer(string name)
        {
            var readerFile = MemoryMappedFile.CreateOrOpen(name + "_receiver", MemoryMappedFileCapacity);
            _readerTask = _reader.RunAsync(readerFile, _cts.Token);

            var writerFile = MemoryMappedFile.CreateOrOpen(name + "_sender", MemoryMappedFileCapacity);
            _writerTask = _writer.RunAsync(writerFile, _cts.Token);
        }

        // NOTE: キャンセルが必要な場合は内部的なCancellationTokenSourceでキャンセルされる。
        // 外部からのtokenを受け付けてもいいが、今のライフサイクル設計だと不要そうなので頑張ってない
        public async Task StartAsClientAsync(string name)
        {
            MemoryMappedFile readerFile;
            MemoryMappedFile writerFile;
            
            while (true)
            {
                try
                {
                    //NOTE: サーバーと逆方向の呼称になることに注意
                    readerFile = MemoryMappedFile.OpenExisting(name + "_sender");
                    writerFile = MemoryMappedFile.OpenExisting(name + "_receiver");
                    break;
                }
                catch (System.IO.FileNotFoundException)
                {
                    // server側がファイルオープンしてないときに通過する。この場合、ファイルが開くまで待つ
                }

                if (_cts.Token.IsCancellationRequested)
                {
                    return;
                }
                await Task.Delay(100);
            }
            
            _readerTask = _reader.RunAsync(readerFile, _cts.Token);
            _writerTask = _writer.RunAsync(writerFile, _cts.Token);
        }

        // NOTE: MemoryMappedFileが閉じるまで待ちたい…という意図があることに注意
        public async Task StopAsync()
        {
            _cts.Cancel();
            _cts.Dispose();
            
            try
            {
                if (_readerTask != null)
                {
                    await _readerTask;
                }
            }
            catch (OperationCanceledException)
            {
            }

            try
            {
                if (_writerTask != null)
                {
                    await _writerTask;
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void SendCommand(ReadOnlyMemory<byte> content, bool isLastMessage = false) => _writer.SendCommand(content, isLastMessage);
       
        /// <summary>
        /// NOTE: この関数は内部的にawaitしないので、呼び出し元は明示的にawaitすること
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task<ReadOnlyMemory<byte>> SendQueryAsync(ReadOnlyMemory<byte> content)
        {
            var source = new TaskCompletionSource<ReadOnlyMemory<byte>>();
            var id = GenerateQueryId();
            _queries.TryAdd(id, source);
            _writer.SendQuery(id, content);
            return await source.Task;
        }

        public void SendQueryResponse(ushort id, ReadOnlyMemory<byte> content) => _writer.SendQueryResponse(id, content);
        
        private ushort GenerateQueryId()
        {
            lock (_requestIdLock)
            {
                _requestId++;
                // クエリのIDは1 ~ (ushort.MaxValue - 1) の範囲で連番で回す。
                // NOTE: クエリはすぐに消費される前提でしか使ってないので、ushortが一巡してIDが被ったからといって実害はない
                if (_requestId == ushort.MaxValue)
                {
                    _requestId = 1;
                }
                return _requestId;
            }
        }
        
        private readonly struct Message
        {
            private Message(ReadOnlyMemory<byte> body, bool isReply, ushort queryId)
            {
                Body = body;
                IsReply = isReply;
                QueryId = queryId;
            }

            /// <summary>
            /// メッセージのボディ部
            /// </summary>
            public ReadOnlyMemory<byte> Body { get; }

            /// <summary>
            /// このメッセージが相手からのクエリの返信かどうか
            /// </summary>
            public bool IsReply { get; }

            /// <summary>
            /// こちらからのコマンド送信の場合は0、
            /// こちらからのクエリ送信の場合は送るクエリのID、
            /// 相手へのクエリ返信の場合は相手が送ってきたクエリのID
            /// </summary>
            public ushort QueryId { get; }

            public static Message Command(ReadOnlyMemory<byte> content) => new(content, false, 0);
            public static Message Query(ReadOnlyMemory<byte> content, ushort id) => new(content, false, id);
            public static Message Response(ReadOnlyMemory<byte> content, ushort id) => new(content, true, id);
        }
    }   
}
