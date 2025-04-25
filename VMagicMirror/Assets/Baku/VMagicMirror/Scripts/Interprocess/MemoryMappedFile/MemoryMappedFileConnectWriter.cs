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
    // - MMFの先頭から順にデータを書き込んでファイル領域を先に進む。これがQueueのように作用する
    //   - このとき、チャンク終端の次のバイトには逐一 `0` を書く (readerが追い越して読み込むのを防ぐため)
    // - チャンクの書き込みに十分な領域が余ってないのを検知したら、チャンクの代わりに `2` だけを書き込んだあと、ファイル冒頭に戻る
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
            
            // NOTE: ファイルはOpenExistingとかでも開くことがあり、直ちに開けるとは保証できないのでnullable
            private MemoryMappedFile? _file;
            private MemoryMappedViewAccessor? _accessor;
            private readonly object _writeLock = new();
            private readonly byte[] _writeBuffer = new byte[MemoryMappedFileCapacity];
            
            // 送りたいMessageの一覧。コマンド、クエリ、クエリのレスポンスを区別せず一列に並べる
            private readonly ConcurrentQueue<Message> _messages = new();
            
            // 次のチャンクを書き込む冒頭のファイル上のインデックス値。
            // readerの読み込みを待っている場合、このindexが指している部分に書き込み済みフラグが既に立っている
            // (& readerがそのフラグを消すのを待つ)事がある
            private int _index;
            
            private readonly object _lastMessageLock = new();
            private bool _lastMessageEnqueued;
            private bool LastMessageEnqueued
            {
                get
                {
                    lock (_lastMessageLock) return _lastMessageEnqueued;
                }
                set
                {
                    lock (_lastMessageLock) _lastMessageEnqueued = value;
                }
            }

            public bool LastMessageSent { get; private set; }
            
            // TODO: ファイル冒頭に戻るときだけTaskを実行し、それ以外は即時書き込みしていくように直したい
            // NOTE: Start / Stopがライフサイクルで1回ずつしか呼ばれない前提でこういう書き方にしてる
            public async Task RunAsync(MemoryMappedFile file, CancellationToken token)
            {
                lock (_writeLock)
                {
                    _file = file;
                    _accessor = file.CreateViewAccessor();
                }

                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        // 送るものが無いうちは単に待つ
                        if (!_messages.TryDequeue(out var msg))
                        {
                            // NOTE: SendCommandで指定した最終メッセージが一瞬で消費されてしまうと後述のif文を通過しないので、ここでも判定する
                            // TODO: タイミング問題の捌きがややこしいのでもうちょっとわかりやすくしたい && 大部分の処理を同期化することで大枠としてケアできると嬉しい
                            if (LastMessageEnqueued)
                            {
                                return;
                            }
                            await Task.Delay(1, token);
                            continue;
                        }

                        await WriteSingleMessageAsync(msg, token);
                        if (LastMessageEnqueued && _messages.IsEmpty)
                        {
                            LastMessageSent = true;
                            return;
                        }
                    }
                }
                catch (NullReferenceException)
                {
                    //TODO: 歴史的経緯でNullRefを気にしているが、実は不要かも…？
                }
                finally
                {
                    lock (_writeLock)
                    {
                        _file?.Dispose();
                        _file = null;
                        _accessor?.Dispose();
                        _accessor = null;
                    }
                }
            }

            public void SendCommand(ReadOnlyMemory<byte> content, bool isLastMessage)
            {
                if (LastMessageEnqueued)
                {
                    return;
                }

                _messages.Enqueue(Message.Command(content));

                if (isLastMessage)
                {
                    LastMessageEnqueued = true;
                }
            }

            public void SendQuery(ushort id, ReadOnlyMemory<byte> content)
            {
                if (LastMessageEnqueued)
                {
                    return;
                }

                _messages.Enqueue(Message.Query(content, id));
            }

            public void SendQueryResponse(ushort id, ReadOnlyMemory<byte> content)
            {
                if (LastMessageEnqueued)
                {
                    return;
                }

                _messages.Enqueue(Message.Response(content, id));
            }

            // NOTE: メッセージが長い場合は分割して送るような実装が入ってる
            private async Task WriteSingleMessageAsync(Message msg, CancellationToken token)
            {
                var data = msg.Body;
                // 送信成功したバイナリサイズの合計値
                var writeSize = 0;
                while (!token.IsCancellationRequested && writeSize < data.Length)
                {
                    // 書き込みできない(ファイル末尾に到達した)場合だけ非同期処理に帰着して、ファイル冒頭に戻す。
                    if (ShouldRewind())
                    {
                        WriteRewindFlagAndResetIndex();
                        await WaitRewindAsync(token);
                    }

                    // 書き込んだあと、正常に書き込めていればサイズを累積して続行
                    var size = WriteMessage(msg, writeSize);
                    if (size < 0)
                    {
                        return;
                    }

                    writeSize += size;
                }
            }

            private bool ShouldRewind()
            {
                lock (_writeLock)
                {
                    return _index > RewindMinIndex;
                }
            }

            private void WriteRewindFlagAndResetIndex()
            {
                lock (_writeLock)
                {
                    if (_accessor == null)
                    {
                        return;
                    }
                    _accessor.Write(_index, MessageStateRewind);
                    // NOTE: この時点ではMMFの冒頭に `1` (書き込み済みフラグ) が書かれているのが期待値。
                    // で、それをreaderが `0` に書き換えるのを待つことになる
                    _index = 0;
                }
            }

            // MMFをファイルの末尾まで使い切ったときに呼び出すことで、readerがファイル冒頭への書き込みを許可するまで待機する
            private async Task WaitRewindAsync(CancellationToken token)
            {
                while (!token.IsCancellationRequested)
                {
                    byte state;
                    lock (_writeLock)
                    {
                        if (_accessor == null)
                        {
                            return;
                        }
                        // 意味的には ReadByte(_index) でも正しいが、このメソッドを呼ぶとき常に _index = 0 なので定数で書いてる
                        state = _accessor.ReadByte(0);
                    }

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
            private int WriteMessage(Message msg, int dataOffset)
            {
                var data = msg.Body;
                lock (_writeLock)
                {
                    if (_accessor == null)
                    {
                        return -1;
                    }

                    _accessor.Write(_index + 1, msg.IsReply ? MessageTypeResponse : MessageTypeCommandOrQuery);
                    _accessor.Write(_index + 2, msg.QueryId);
                    _accessor.Write(_index + 4, data.Length);

                    // NOTE:
                    // - このmaxDataLengthが1以上になることは事前に ShouldResetToStart() を判定することで保証されている
                    // - 必ず1バイト余らせるのはRewindフラグを常に明示的に立てるため (≒reader側はMMFのファイル全長に関知しないでもよい)
                    var maxDataLength = (int)MemoryMappedFileCapacity - MessageHeaderSize - _index - 1;
                    
                    var writeLength = Math.Min(
                        data.Length - dataOffset,
                        maxDataLength
                        );
                    _accessor.Write(_index + 8, writeLength);

                    // _accessor.WriteArrayはSpanを直接受けないため、byte[]に書いてから書き込む
                    // NOTE: 書き込み側のメッセージを単に byte[] で受けるようにしてバッファを挟むのをやめてもよい
                    data.Span[dataOffset..(dataOffset + writeLength)].CopyTo(_writeBuffer);
                    _accessor.WriteArray(_index + 12, _writeBuffer, 0, writeLength);

                    var chunkStartIndex = _index;
                    _index += MessageHeaderSize + writeLength;
                    // readerが読み込みを追い越さないように、チャンクの次をあらかじめ未書き込み扱いにしておく。
                    // この書き込みはMMFの生成直後には意味がなくて、MMFの末尾まで到達して二巡目をやっている(=汚い状態になったMMFを使っている)とき
                    _accessor.Write(_index, MessageStateEmpty);
                    _accessor.Write(chunkStartIndex, MessageStateDataExist);
                    return writeLength;
                }
            }
        }
    }
}