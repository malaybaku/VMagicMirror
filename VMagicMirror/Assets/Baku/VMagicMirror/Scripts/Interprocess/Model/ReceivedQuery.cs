using System;
using Baku.VMagicMirror.IpcMessage;

namespace Baku.VMagicMirror
{
    public class ReceivedQuery
    {
        public ReceivedQuery(ReadOnlyMemory<byte> data)
        {
            RawData = data;
            Command = (VmmCommands) MessageDeserializer.GetCommandId(data);
            ValueType = MessageDeserializer.GetValueType(data);
            Result = "";
        }

        public ReadOnlyMemory<byte> RawData { get; }
        public VmmCommands Command { get; }
        public MessageValueTypes ValueType { get; }

        // NOTE: Bool以外へのデシリアライズがないのは単に使ってる場所がないからで、追加はしてよい
        public bool ToBoolean() => MessageDeserializer.ToBool(RawData);
        
        // NOTE: Queryのレスポンスはstringじゃなくても良いのだが、
        // 現行実装ではstringでそれっぽい結果になるものしか扱ってなさそうなので、stringのみをサポートしている
        public string Result { get; set; }
    }
}
