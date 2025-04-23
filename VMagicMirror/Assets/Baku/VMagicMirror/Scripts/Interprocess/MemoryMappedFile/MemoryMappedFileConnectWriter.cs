// NOTE: このファイルはUnityとWPF双方から参照されていることに注意
#nullable enable
using System;
using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Baku.VMagicMirror.Mmf
{
    public partial class MemoryMappedFileConnector
    {
        private sealed class InternalWriter
        {
            // NOTE: ファイルはOpenExistingとかでも開くことがあり、直ちに開けるとは保証できないのでnullableになる
            private MemoryMappedFile? _file;
            private MemoryMappedViewAccessor? _accessor;
            private readonly object _senderLock = new();
            
            // 送りたいMessageの一覧。コマンド、クエリ、クエリのレスポンスを区別せず一列に並べる
            private readonly ConcurrentQueue<Message> _messages = new();

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
            
            private bool CanWriteMessage
            {
                get
                {
                    lock (_senderLock)
                    {
                        return
                            _accessor != null &&
                            _accessor.ReadByte(0) == MessageStateWriteReady;
                    }
                }
            }

            public bool LastMessageSent { get; private set; }
            
            // NOTE: Start / Stopがライフサイクルで1回ずつしか呼ばれない前提でこういう書き方にしてる
            public async Task RunAsync(MemoryMappedFile file, CancellationToken token)
            {
                lock (_senderLock)
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
                    lock (_senderLock)
                    {
                        _file?.Dispose();
                        _file = null;
                        _accessor?.Dispose();
                        _accessor = null;
                    }
                }
            }

            public void SendCommand(string content, bool isLastMessage)
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

            public void SendQuery(int id, string content)
            {
                if (LastMessageEnqueued)
                {
                    return;
                }

                _messages.Enqueue(Message.Query(content, id));
            }

            public void SendQueryResponse(int id, string content)
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
                var data = Encoding.UTF8.GetBytes(msg.Text);
                // 送信成功したバイナリサイズの合計値
                var writeSize = 0;
                while (!token.IsCancellationRequested && writeSize < data.Length)
                {
                    //書き込みOKになるまで待ち
                    while (!CanWriteMessage)
                    {
                        await Task.Delay(1, token);
                    }

                    // 書き込んだあと、正常に書き込めていればサイズを累積して続行
                    var size = WriteMessage(msg, data, writeSize);
                    if (size < 0)
                    {
                        return;
                    }

                    writeSize += size;
                }
            }

            // NOTE:
            //  - 戻り値はデータを書き込んだバイト数。
            //  - ただし、書き込みが失敗した場合には -1 を返す。-1 が戻った場合、それ以降は書き込みを試みないのが期待値
            private int WriteMessage(Message msg, byte[] data, int offset)
            {
                lock (_senderLock)
                {
                    if (_accessor == null)
                    {
                        return -1;
                    }

                    _accessor.Write(2, (short)(msg.IsReply ? 1 : 0));
                    _accessor.Write(4, msg.Id);
                    _accessor.Write(8, data.Length);

                    // データが乗り切らない場合は書けるとこまでにする。大多数のケースでは一回でメッセージ全部が書き切れるはず
                    var writeLength = data.Length - offset > MaxMessageBodySize
                        ? MaxMessageBodySize
                        : data.Length - offset;
                    _accessor.Write(12, writeLength);
                    _accessor.WriteArray(16, data, offset, writeLength);
                    _accessor.Write(0, (byte)MessageStateReadReady);
                    return writeLength;
                }
            }
        }
    }
}