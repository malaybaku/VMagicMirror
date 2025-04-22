#nullable enable
using System;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;

namespace Baku.VMagicMirror.Mmf
{
    public partial class MemoryMappedFileConnector
    {
        public sealed class InternalReader
        {
            private readonly object _readLock = new();
            private readonly byte[] _readBuffer = new byte[MemoryMappedFileCapacity];
            private MemoryMappedFile? _file;
            private MemoryMappedViewAccessor? _accessor;

            private bool CanReadMessage
            {
                get
                {
                    lock (_readLock) return _accessor?.ReadByte(0) == MessageStateReadReady;
                }
            }

            public event Action<string>? ReceiveCommand;
            public event Action<(int id, string message)>? ReceiveQuery;
            public event Action<(int id, string message)>? ReceiveQueryResponse;
            
            
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
                var id = 0;
                var totalDataLength = 0;

                // メッセージのBody以外の情報はここで確定
                lock (_readLock)
                {
                    if (_accessor == null)
                    {
                        return;
                    }
                    
                    var messageType = _accessor.ReadInt16(2);
                    isReply = messageType > 0;
                    id = _accessor.ReadInt32(4);
                    totalDataLength = _accessor.ReadInt32(8);
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
                        
                        dataLength = _accessor.ReadInt32(12);
                        _accessor.ReadArray(16, messageBytes, offset, dataLength);
                        _accessor.Write(0, (byte)0);
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
                var message = System.Text.Encoding.UTF8.GetString(messageBytes, 0, totalDataLength);
                HandleReceivedMessage(message, id, isReply);
            }

            private async Task WaitReadReadyAsync(CancellationToken token)
            {
                while (!CanReadMessage)
                {
                    await Task.Delay(1, token);
                }
            }
            
            private void HandleReceivedMessage(string message, int id, bool isReply)
            {
                //3パターンある
                // - こちらが投げたQueryの返答が戻ってきた
                // - Commandを受け取った
                // - Queryを受け取った
                if (isReply)
                {
                    ReceiveQueryResponse?.Invoke((id, message));
                }
                else if (id == 0)
                {
                    ReceiveCommand?.Invoke(message);
                }
                else
                {
                    ReceiveQuery?.Invoke((id, message));
                }
            }
        }
    }
}