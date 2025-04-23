using System;
using System.Linq;
using Baku.VMagicMirror.IpcMessage;

namespace Baku.VMagicMirror
{
    public readonly struct ReceivedCommand
    {
        // NOTE: 厳密にはcommandのみのデータのバイナリはEmptyではない(4byteあるはず)が、実質無害なので捨てる。
        // このとき、無の配列からValueTypeを読み込もうとするとエラーになるので、
        public ReceivedCommand(ReadOnlyMemory<byte> data)
        {
            RawData = data;
            Command = (VmmCommands) MessageDeserializer.GetCommandId(data);
            ValueType = MessageDeserializer.GetValueType(data);

            StringValue = "";
            ByteArrayValue = Array.Empty<byte>();
            IntArrayValue = Array.Empty<int>();
            FloatArrayValue = Array.Empty<float>();
            switch (ValueType)
            {
                case MessageValueTypes.String: StringValue = MessageDeserializer.ToString(data); break;
                case MessageValueTypes.ByteArray: ByteArrayValue = MessageDeserializer.ToByteArray(data); break;
                case MessageValueTypes.IntArray: IntArrayValue = MessageDeserializer.ToIntArray(data); break;
                case MessageValueTypes.FloatArray: FloatArrayValue = MessageDeserializer.ToFloatArray(data); break;
            }
        }

        public ReadOnlyMemory<byte> RawData { get; }
        public VmmCommands Command { get; }
        public MessageValueTypes ValueType { get; }

        // NOTE: 配列とstringは複数回デシリアライズするとダルいので保持してしまう
        public string StringValue { get; }
        public byte[] ByteArrayValue { get; }
        public int[] IntArrayValue { get; }
        public float[] FloatArrayValue { get; }
        
        //public string Content { get; }

        // NOTE: 逐一読み込むと効率が悪いのだが、string以外のデータは逐次読み込み
        public bool ToBoolean() => MessageDeserializer.ToBool(RawData);

        public int ToInt() => MessageDeserializer.ToInt(RawData);
        // NOTE: Percentage / Centimeterの内部表現はfloatじゃなくてintなことに注意
        public float ParseAsPercentage() => MessageDeserializer.ToInt(RawData) * 0.01f;
        public float ParseAsCentimeter() => MessageDeserializer.ToInt(RawData) * 0.01f;

        // TODO: 送信側がそもそも "0,1" みたいなstringを投げつけてるのをちゃんとしたIntArrayに直す。Colorのほうも同様
        public int[] ToIntArray()
            => StringValue.Split(',')
                .Select(e => int.TryParse(e, out int result) ? result : 0)
                .ToArray();
        
        /// <summary>
        /// コマンドによってLength==3(RGB)の場合とLength==4(ARGB)の場合があるので注意
        /// </summary>
        /// <returns></returns>
        public float[] ToColorFloats()
            => ToIntArray()
            .Select(v => v / 255.0f)
            .ToArray();
    }
}
