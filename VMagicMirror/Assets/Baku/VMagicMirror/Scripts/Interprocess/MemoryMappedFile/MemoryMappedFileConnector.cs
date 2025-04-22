#nullable enable
using System;
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
//0-1: 書き込み状態フラグ。
// - 0: 受信完了。このときはWriteする側だけが触ってよい
// - 1: 送信完了(メッセージの末尾)。このときはReadする側だけが触ってよい

//2-3: メッセージの種類
// - 0: CommandまたはQuery
// - 1: Response

//4-7: リクエストID
// - 0: Commandの場合この値を指定して、投げっぱなしのメッセージを表す
// - 1以上: Queryの場合は返信に使ってほしいID値を表す。Responseの場合、どのQueryに対してのレスポンスであるかをIDで指定する。
//   - Query用のIDは送信側が一意性を保つように生成する

//8-11: メッセージ全体のバイナリ長
// - メッセージが短い場合、1つのデータに全データが収まるため、ここの値は12-15バイトに入る値と等しい
// - メッセージが長い場合は分けて送るため、12-15の値とココの値が一致しなくなる

//12-15: このチャンクのバイナリ長
// - 16バイト以降に入ってるメッセージの長さ
//   - NOTE: データがメッセージの一部である場合にoffsetを指定することが考えられるが、
// 　        メッセージには順序保証があってoffsetは不要なはずなので、offset情報はない。

//16-: ボディ
// - 文字列をUTF8扱いでバイナリ化したもの

namespace Baku.VMagicMirror.Mmf
{
    /// <summary>
    /// <see cref="MemoryMappedFile"/> を内部的に使ってで送りっぱなしのコマンドとレスポンスが必要なクエリを送受信できるやつだよ
    /// </summary>
    public sealed partial class MemoryMappedFileConnector
    {
        private const long MemoryMappedFileCapacity = 262114;
        private const int MessageHeaderSize = 16;
        private const int MaxMessageBodySize = (int)MemoryMappedFileCapacity - MessageHeaderSize;

        private const int MessageStateWriteReady = 0;
        private const int MessageStateReadReady = 1;

        private readonly InternalWriter _writer = new();
        private readonly InternalReader _reader = new();
        private readonly CancellationTokenSource _cts = new();

        private Task? _readerTask;
        private Task? _writerTask;
        
        public event Action<string>? ReceiveCommand
        {
            add => _reader.ReceiveCommand += value;
            remove => _reader.ReceiveCommand -= value;
        }
        
        public event Action<(int id, string content)>? ReceiveQuery
        {
            add => _reader.ReceiveQuery += value;
            remove => _reader.ReceiveQuery -= value;
        }

        public MemoryMappedFileConnector()
        {
            // NOTE: 完了待ちクエリの一覧を(writerではなく)本クラスが直接持っている方が自然かもしれない…
            _reader.ReceiveQueryResponse += value =>
            {
                var (id, command) = value;
                _writer.TrySetQueryResult(id, command);
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

        public void SendCommand(string command) => _writer.SendCommand(command);
        public async Task<string> SendQueryAsync(string command) => await _writer.SendQueryAsync(command);
        public void SendQueryResponse(string command, int id) => _writer.SendQueryResponse(command, id);

        private readonly struct Message
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

            public static Message Command(string text) => new(text, false, 0);
            public static Message Query(string text, int id) => new(text, false, id);
            public static Message Response(string text, int id) => new(text, true, id);
        }
    }   
}
