// NOTE: このファイルはUnityとWPF双方から参照されていることに注意
#nullable enable
using System;
using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;

namespace Baku.VMagicMirror.Mmf
{
    // 基本的なデータ書き込みの流れ:
    // - MMFの先頭から順にデータを書き込んでファイル領域を進んでいく
    // - チャンクの書き込みに十分な領域が残っていないのを検知したら、チャンクの代わりに `2` だけを書き込んだあと、ファイル冒頭に戻る
    //   - このフローを使って大きなメッセージを分割送信することもあるし、単にrewindしてメッセージを書くだけのこともある
    // - ファイルの冒頭が(reader側のフラグ書き換えによって)書き込み可になるまで待つ
    // - 最初に戻る
    
    public partial class MemoryMappedFileConnector
    {
        private sealed class InternalWriter
        {
            // NOTE: 呼び出し元の知識も混入しているが、
            // 「Header + 8byte(=intデータ1つからなるコマンドのpayload) + 次の余り1byte」
            // を分割せず書き込める領域もないのならRewindしたほうがよい…という考え方でこのしきい値にしている
            private const int RewindMinIndex = (int)MemoryMappedFileCapacity - (MessageHeaderSize + 9);

            private readonly object _writeLock = new();
            
            // NOTE: ファイルはOpenExistingとかでも開くことがあり、直ちに開けるとは保証できないのでnullable
            private MemoryMappedFile? _file;
            private MemoryMappedViewAccessor? _accessor;
            private readonly byte[] _writeBuffer = new byte[MemoryMappedFileCapacity];
            
            // ポイント
            // - 送りたいMessageの一覧。コマンド、クエリ、クエリのレスポンスを区別せず一列に並べる
            // - 非同期タスク側ではキューが溜まっていると非同期処理が始まる
            private readonly ConcurrentQueue<Message> _messages = new();
            
            // 次のチャンクを書き込む冒頭のファイル上のインデックス値。常に下記どちらかの一つ以上を満たす
            // - ファイル冒頭を指している == 値が0 
            // - 書き込み可能なファイル上の位置を指している == _accessor.ReadByte(_index)の結果が0になるような値である
            private int _index;

            // 書き込みを非同期で行う場合に立つフラグ。同期スレッドからtrueに変更し、非同期スレッド側からfalseに戻す
            private bool _asyncWriteActive;
            private bool _lastMessageEnqueued;

            private bool RewindRequired => _index > RewindMinIndex;
   
            public bool LastMessageSent { get; private set; }
            
            public async Task RunAsync(MemoryMappedFile file, CancellationToken token)
            {
                _file = file;
                _accessor = file.CreateViewAccessor();

                try
                {
                    while (!token.IsCancellationRequested && !LastMessageSent)
                    {
                        var shouldRun = false;
                        lock (_writeLock)
                        {
                            shouldRun = _asyncWriteActive || _messages.Count > 0 || RewindRequired;
                        }

                        // 非同期処理が必要ないうちはここで待機: 待ってる間にアプリケーション終了する場合はDelayのところで抜ける
                        if (!shouldRun)
                        {
                            await Task.Delay(10, token);
                            continue;
                        }
                        
                        // 非同期処理: キューがなくなるまで非同期でメッセージを捌き続ける。
                        // このwhile句を抜けるまでの間、_messages 以外のフィールドに競合アクセスしないことは SendMessage の実装で保証する
                        while (_messages.TryDequeue(out var msg))
                        {
                            await WriteSingleMessageAsync(msg, token);
                        }

                        // 非同期処理が終わったあと: 同期処理に戻ってもOKにする
                        lock (_writeLock)
                        {
                            if (_lastMessageEnqueued && _messages.IsEmpty)
                            {
                                LastMessageSent = true;
                            }
                            _asyncWriteActive = false;
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (NullReferenceException)
                {
                    //TODO: 歴史的経緯でNullRefを気にしているが、実は不要かも…？
                }
                finally
                {
                    lock (_writeLock)
                    {
                        LastMessageSent = true;
                        _file?.Dispose();
                        _file = null;
                        _accessor?.Dispose();
                        _accessor = null;
                    }
                }
            }
            
            public void SendCommand(ReadOnlyMemory<byte> content, bool isLastMessage) 
                => SendMessage(Message.Command(content), isLastMessage);

            // NOTE: Queryを最終メッセージ扱いすることはないので第2引数は常にfalseでよい。QueryResponseも同様
            public void SendQuery(ushort id, ReadOnlyMemory<byte> content)
                => SendMessage(Message.Query(content, id), false);

            public void SendQueryResponse(ushort id, ReadOnlyMemory<byte> content)
                => SendMessage(Message.Response(content, id), false);

            private void SendMessage(Message msg, bool isLastMessage)
            {
                // NOTE: 同期/非同期の書き込み処理が混ざると危ないので広めにlockするしかない
                lock (_writeLock)
                {
                    if (_accessor == null || _lastMessageEnqueued)
                    {
                        return;
                    }

                    if (CanWriteSynchronously(msg.Body))
                    {
                        // 同期で1回だけメッセージを書き込めばよい場合、実際にそうする。ここに帰着するケースが大多数になる想定
                        _ = Write(msg, 0);
                        if (isLastMessage)
                        {
                            _lastMessageEnqueued = true;
                            LastMessageSent = true;
                        }
                        return;
                    }

                    // 非同期じゃないとダメな場合はこっち
                    _messages.Enqueue(msg);
                    if (isLastMessage)
                    {
                        _lastMessageEnqueued = true;
                    }
                    _asyncWriteActive = true;
                }
            }
            
            // NOTE: スレッド競合を考慮するため、シングルスレッドだったら見ないでいいような条件も入っていることに注意
            // - 非同期処理が実行中ではない
            // - Queueに処理待ちメッセージがない
            // - ファイル末尾まで余裕がある
            // - コンテンツの長さが長すぎない(分割して送る必要がない)
            //   - この条件については、末尾に1バイト余らせないといけないことにも注意
            private bool CanWriteSynchronously(ReadOnlyMemory<byte> content)
            {
                return !_asyncWriteActive &&
                    _messages.Count == 0 &&
                    !RewindRequired &&
                    _index + MessageHeaderSize + content.Length < (int)MemoryMappedFileCapacity - 1;
            }

            // NOTE: メッセージが長い場合は分割して送るような実装が入ってる
            private async Task WriteSingleMessageAsync(Message msg, CancellationToken token)
            {
                var data = msg.Body;
                // 送信成功したバイナリサイズの合計値
                var writeSize = 0;
                while (!token.IsCancellationRequested && writeSize < data.Length)
                {
                    // ファイル末尾に到達した場合は非同期処理に帰着して、ファイル冒頭に戻す。
                    if (RewindRequired)
                    {
                        await RewindAsync(token);
                    }

                    // 書き込んだあと、正常に書き込めていればサイズを累積して続行
                    var size = Write(msg, writeSize);
                    if (size < 0)
                    {
                        return;
                    }

                    writeSize += size;
                }
            }

            // MMFをファイルの末尾まで使い切ったときに呼び出すことで、readerがファイル冒頭への書き込みを許可するまで待機する
            private async Task RewindAsync(CancellationToken token)
            {
                if (_accessor == null) return;
                
                _accessor.Write(_index, MessageStateRewind);
                // NOTE: この時点ではMMFの冒頭に `1` (書き込み済みフラグ) が書かれているのが期待値。
                // これをreaderが `0` に書き換えるのを待つことになる
                _index = 0;

                while (!token.IsCancellationRequested)
                {
                    // 意味的には ReadByte(_index) でも正しいが、このメソッドを呼ぶとき常に _index = 0 なので定数で書いてる
                    var state = _accessor.ReadByte(0);
                    if (state == MessageStateEmpty)
                    {
                        return;
                    }
                    
                    await Task.Delay(1, token);
                }
            }

            // NOTE:
            //  - 戻り値はデータを書き込んだバイト数。
            //  - ただし、書き込みが失敗した場合には -1 を返す。-1 が戻った場合、それ以降は書き込みを試みないのが期待値
            private int Write(Message msg, int dataOffset)
            {
                if (_accessor == null) return -1;

                var data = msg.Body;
                _accessor.Write(_index + 1, msg.IsReply ? MessageTypeResponse : MessageTypeCommandOrQuery);
                _accessor.Write(_index + 2, msg.QueryId);
                _accessor.Write(_index + 4, data.Length);

                // NOTE:
                // - このmaxDataLengthが1以上になることは事前チェック (RewindRequiredのチェック) で保証してある想定
                // - 1バイト余らせないと「メッセージの次のバイト」が担保できないので、それもチェックしている
                var maxDataLength = (int)MemoryMappedFileCapacity - MessageHeaderSize - _index - 1;
                var writeLength = Math.Min(data.Length - dataOffset, maxDataLength);
                _accessor.Write(_index + 8, writeLength);

                // _accessor.WriteArrayはSpanを直接受けないため、byte[]に書いてから書き込む
                // NOTE: 書き込み側のメッセージを単に byte[] で受けるようにしてバッファを挟むのをやめてもよい
                data.Span[dataOffset..(dataOffset + writeLength)].CopyTo(_writeBuffer);
                _accessor.WriteArray(_index + 12, _writeBuffer, 0, writeLength);

                // 末尾の次のバイトを先にリセットすることに注意: 逆だとreaderが読み込みを追い越してしまう可能性がある
                _accessor.Write(_index + MessageHeaderSize + writeLength, MessageStateEmpty);
                _accessor.Write(_index, MessageStateDataExist);
                _index += MessageHeaderSize + writeLength;

                return writeLength;
            }
        }
    }
}