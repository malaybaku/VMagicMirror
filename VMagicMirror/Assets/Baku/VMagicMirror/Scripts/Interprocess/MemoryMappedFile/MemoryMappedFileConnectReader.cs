// NOTE: このファイルはUnityとWPF双方から参照されていることに注意
#nullable enable
using System;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;

namespace Baku.VMagicMirror.Mmf
{
    public partial class MemoryMappedFileConnector
    {
        // 基本的なreaderのデータ読み込みの流れ:
        // - MMFの冒頭から順にデータを読み込んでいく
        //   - チャンクの先頭1バイトが `0` 以外になるまで待つ → チャンクを読む → 読み込みindexを加算…を繰り返す
        // - 特に、チャンクの先頭バイトが `2` のチャンクを検出したら、indexを0に戻しつつ、MMFの冒頭1バイトを `0` にリセット
        public sealed class InternalReader
        {
            private readonly object _readLock = new();
            private readonly byte[] _readBuffer = new byte[MemoryMappedFileCapacity];
            private MemoryMappedFile? _file;
            private MemoryMappedViewAccessor? _accessor;

            private int _index = 0;

            public event Action<ReadOnlyMemory<byte>>? ReceiveCommand;
            public event Action<(ushort queryId, ReadOnlyMemory<byte> content)>? ReceiveQuery;
            public event Action<(ushort queryId, ReadOnlyMemory<byte> content)>? ReceiveQueryResponse;
            
            
            public async Task RunAsync(MemoryMappedFile file, CancellationToken token)
            {
                lock (_readLock)
                {
                    _file = file;
                    _accessor = file.CreateViewAccessor();
                }

                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        await ReadSingleMessageAsync(token);
                    }
                }
                catch (NullReferenceException)
                {
                    //TODO: 歴史的経緯でNullRefを気にしているが、実は不要かも…？
                }
                finally
                {
                    lock (_readLock)
                    {
                        _file?.Dispose();
                        _file = null;
                        _accessor?.Dispose();
                        _accessor = null;
                    }
                }
            }
            
            // NOTE: 1メッセージが複数データに分かれて送られる場合のデータ結合はこの関数の中でやる
            private async Task ReadSingleMessageAsync(CancellationToken token)
            {
                await WaitReadReadyAsync(token);

                var isReply = false;
                ushort queryId = 0;
                var totalDataLength = 0;

                // メッセージのBody以外の情報はここで確定
                lock (_readLock)
                {
                    if (_accessor == null)
                    {
                        return;
                    }

                    var accessorOffset = _index;
                    isReply = _accessor.ReadByte(accessorOffset + 1) == MessageTypeResponse;
                    queryId = _accessor.ReadUInt16(accessorOffset + 2);
                    totalDataLength = _accessor.ReadInt32(accessorOffset + 4);
                }

                // 使いまわし用のBufferより大きいデータが来そうな場合だけ別でallocする。このallocは滅多に起きない想定
                var messageBytes = _readBuffer;
                if (totalDataLength > messageBytes.Length)
                {
                    messageBytes = new byte[totalDataLength];
                }

                // メッセージの末尾まで読んでいく
                var offset = 0;
                while (true)
                {
                    int dataLength;
                    lock (_readLock)
                    {
                        // _accessor == nullの場合、メッセージがハンパであっても中断する(アプリ終了のはずなので)
                        if (_accessor == null)
                        {
                            return;
                        }

                        var accessorOffset = _index;
                        
                        // NOTE:
                        // - チャンクの1~7byteは分割されたメッセージの2つ目以降でも同じ値が入っているので、コレ以降のループでは再読み込みしない
                        // - dataLengthがMMFの末尾からはみ出さないことはwriterが保証してるはずなので、わざわざ検証しない
                        dataLength = _accessor.ReadInt32(accessorOffset + 8);
                        _accessor.ReadArray(accessorOffset + 12, messageBytes, offset, dataLength);
                        // _accessor.Write(0, (byte)0);

                        _index += dataLength + MessageHeaderSize;
                    }

                    offset += dataLength;
                    if (offset >= totalDataLength)
                    {
                        break;
                    }

                    // まだ続きがある: 次のデータチャンクを待つ
                    await WaitReadReadyAsync(token);
                }

                // メッセージの末尾まで読むとココを通過して終了
                // NOTE: allocがもったいなく見えるかもしれないが、messageが消費されるのはメインスレッドであり、
                // messageの読み込みより前に次のデータ読み込みを行ってbufferが上書きされうるので、別で用意したほうが安全である
                var message = new byte[totalDataLength];
                Array.Copy(messageBytes, message, totalDataLength);
                HandleReceivedMessage(message, queryId, isReply);
            }

            private async Task WaitReadReadyAsync(CancellationToken token)
            {
                while (!token.IsCancellationRequested)
                {
                    byte state = 0;
                    lock (_readLock)
                    {
                        if (_accessor == null)
                        {
                            return;
                        }
                        
                        state = _accessor.ReadByte(_index);
                    }

                    switch (state)
                    {
                        case MessageStateEmpty:
                            // 何もないので待つ: アプリ起動中の大体の時間はここを通過し続ける
                            await Task.Delay(1, token);
                            break;
                        case MessageStateDataExist:
                            // 単にデータを読めばいい状態になった
                            return;
                        case MessageStateRewind:
                            // readerがrewindフラグ(≒MMFのファイル末尾)まで到達した = readerもwriterもファイル冒頭に戻る
                            lock (_readLock)
                            {
                                if (_accessor == null)
                                {
                                    return;
                                }
                                _accessor.Write(0, MessageStateEmpty);
                                _index = 0;
                            }
                            await Task.Delay(1, token);
                            break;
                        default:
                            throw new InvalidOperationException("unsupported message state");
                    }
                }
            }
            
            private void HandleReceivedMessage(ReadOnlyMemory<byte> message, ushort queryId, bool isReply)
            {
                //3パターンある
                // - こちらが投げたQueryの返答が戻ってきた
                // - Commandを受け取った
                // - Queryを受け取った
                if (isReply)
                {
                    ReceiveQueryResponse?.Invoke((queryId, message));
                }
                else if (queryId == 0)
                {
                    ReceiveCommand?.Invoke(message);
                }
                else
                {
                    ReceiveQuery?.Invoke((queryId, message));
                }
            }
        }
    }
}