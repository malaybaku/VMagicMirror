using Baku.VMagicMirror;
using Baku.VMagicMirror.IpcMessage;
using System;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// IPC用のメッセージを受信する方式を定義します。
    /// </summary>
    internal interface IMessageReceiver
    {
        void Start();
        void Stop();
        event Action<CommandReceivedData>? ReceivedCommand;

        // NOTE: こっちは本質的にWriteが発生するので、参照型のEventArgsであることを維持している (EAと別の型のActionでも別に良いのだが)。
        event EventHandler<QueryReceivedEventArgs>? ReceivedQuery;
    }

    public readonly struct CommandReceivedData
    {
        public CommandReceivedData(ReadOnlyMemory<byte> data)
        {
            RawData = data;
            Command = (VmmServerCommands)MessageDeserializer.GetCommandId(data);
            ValueType = MessageDeserializer.GetValueType(data);

            _refValue = ValueType switch
            {
                MessageValueTypes.String => MessageDeserializer.ToString(data),
                MessageValueTypes.ByteArray => MessageDeserializer.ToByteArray(data),
                MessageValueTypes.IntArray => MessageDeserializer.ToIntArray(data),
                MessageValueTypes.FloatArray => MessageDeserializer.ToFloatArray(data),
                _ => null,
            };
        }

        public ReadOnlyMemory<byte> RawData { get; }
        public VmmServerCommands Command { get; }
        public MessageValueTypes ValueType { get; }

        // NOTE: 値の場合は _refValue に放り込まず、都度byte[]から読み出す…という使い分けをしていることに注意
        private readonly object? _refValue;

        // NOTE: byte[] とかを取得するGetter Methodを提供してないが、これは単に「それを使うメッセージがまだない」というだけであり、実装するぶんにはOK
        public string GetStringValue() => (string)_refValue!;
    }

    // NOTE: 2025/04時点でWPFがQueryを受け取るケースは存在しないっぽい。初使用時には注意してデバッグすること
    public class QueryReceivedEventArgs : EventArgs
    {
        public QueryReceivedEventArgs(ReadOnlyMemory<byte> data)
        {
            RawData = data;
            Command = (VmmServerCommands)MessageDeserializer.GetCommandId(data);
            ValueType = MessageDeserializer.GetValueType(data);
        }

        public ReadOnlyMemory<byte> RawData { get; }
        public VmmServerCommands Command { get; }
        public MessageValueTypes ValueType { get; }

        // NOTE: Unity->WPFのQueryは未使用なのでValueTypeに応じたデシリアライズ処理を実装していない。必要になったら追加してよい

        // NOTE: Queryのレスポンスはstringじゃなくても良いのだが、
        // 現行実装ではstringでそれっぽい結果になるものしか扱ってなさそうなので、stringのみをサポートしている
        public string Result { get; set; } = "";
    }
}
