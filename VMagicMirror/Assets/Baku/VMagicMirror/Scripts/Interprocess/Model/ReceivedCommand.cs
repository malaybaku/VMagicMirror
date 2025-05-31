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
            Command = (VmmCommands)MessageDeserializer.GetCommandId(data);
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
        public VmmCommands Command { get; }
        public MessageValueTypes ValueType { get; }

        // NOTE: 値の場合は _refValue に放り込まず、都度byte[]から読み出す…という使い分けをしていることに注意
        private readonly object _refValue;

        //　NOTE: ここだけToStringだとキモいので名前を避けてます
        public string GetStringValue() => (string)_refValue;

        public byte[] ToByteArray() => (byte[])_refValue;
        public int[] ToIntArray() => (int[])_refValue;
        public float[] ToFloatArray() => (float[])_refValue;
        
        //public string Content { get; }

        // NOTE: 逐一読み込むと効率が悪いのだが、string以外のデータは逐次読み込み
        public bool ToBoolean() => MessageDeserializer.ToBool(RawData);

        public int ToInt() => MessageDeserializer.ToInt(RawData);
        // NOTE: Percentage / Centimeterの内部表現はfloatじゃなくてintなことに注意
        public float ParseAsPercentage() => MessageDeserializer.ToInt(RawData) * 0.01f;
        public float ParseAsCentimeter() => MessageDeserializer.ToInt(RawData) * 0.01f;

        /// <summary>
        /// コマンドによってLength==3(RGB)の場合とLength==4(ARGB)の場合があるので注意
        /// </summary>
        /// <returns></returns>
        public float[] ToColorFloats() => ToIntArray()
            .Select(v => v / 255.0f)
            .ToArray();
    }
}
