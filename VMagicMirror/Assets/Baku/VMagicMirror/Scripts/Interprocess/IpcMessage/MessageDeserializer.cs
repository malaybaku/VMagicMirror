using System;
using System.Diagnostics;

namespace Baku.VMagicMirror.IpcMessage
{
    // NOTE:
    // - Serializeのときに書き込んだ型情報を見てないが、見て弾くようなガードを足してもよい
    // - 高速化は特に頑張ってないが、インライン化とかMemoryMarshal.Readとか使う余地はある
    public static class MessageDeserializer
    {
        // NOTE: 「WPF/Unity双方でデバッグビルドではこのシンボルを使っている」ということに依存している
        const string DevEnvSymbol = "DEV_ENV";
        
        public static ushort GetCommandId(ReadOnlyMemory<byte> data)
        {
            ValidateDataLength(data, 2);
            return BitConverter.ToUInt16(data.Span);
        }

        public static MessageValueTypes GetValueType(ReadOnlyMemory<byte> data)
        {
            // NOTE: None扱いの(CommandIdのみの)データを2byteで送るのは将来の仕様変更としてはアリ。
            // そういうメッセージが大量に増えたら2byteで送るのを検討してもよい
            ValidateDataLength(data, 4);
            return (MessageValueTypes)BitConverter.ToUInt16(data.Span[2..]);
        }
        
        public static bool ToBool(ReadOnlyMemory<byte> data)
        {
            ValidateDataLength(data, 5);
            ValidateType(data, MessageValueTypes.Bool);

            return data.Span[4] != 0;
        }
        
        public static int ToInt(ReadOnlyMemory<byte> data)
        {
            ValidateDataLength(data, 8);
            ValidateType(data, MessageValueTypes.Int);

            return BitConverter.ToInt32(data.Span[4..]);
        }
        
        public static float ToFloat(ReadOnlyMemory<byte> data)
        {
            ValidateDataLength(data, 8);
            ValidateType(data, MessageValueTypes.Float);

            return BitConverter.ToSingle(data.Span[4..]);
        }

        // NOTE: バイナリ長が4byteの場合も不正なわけではない(空である)ことに注意

        public static string ToString(ReadOnlyMemory<byte> data)
        {
            ValidateType(data, MessageValueTypes.String);

            if (data.Length < 5) return "";
            return System.Text.Encoding.UTF8.GetString(data.Span[4..]);
        }
        
        public static byte[] ToByteArray(ReadOnlyMemory<byte> data)
        {
            ValidateType(data, MessageValueTypes.ByteArray);

            if (data.Length < 5) return Array.Empty<byte>();
            return data.Span[4..].ToArray();
        }
        
        public static int[] ToIntArray(ReadOnlyMemory<byte> data)
        {
            ValidateType(data, MessageValueTypes.IntArray);

            if (data.Length < 8) return Array.Empty<int>();
            var count = (data.Length - 4) / 4;
            var result = new int[count];

            var span = data.Span;
            for (var i = 0; i < count; i++)
            {
                result[i] = BitConverter.ToInt32(span[(4 + i * 4)..]);
            }

            return result;
        }

        public static float[] ToFloatArray(ReadOnlyMemory<byte> data)
        {
            ValidateType(data, MessageValueTypes.FloatArray);
            
            if (data.Length < 8) return Array.Empty<float>();
            var count = (data.Length - 4) / 4;
            var result = new float[count];

            var span = data.Span;
            for (var i = 0; i < count; i++)
            {
                result[i] = BitConverter.ToSingle(span[(4 + i * 4)..]);
            }
            
            return result;
        }

        // 実装ミスでのみ送受信で型がマッチしなかったり、長さが不適切だったりするケースが発生しうる
        [Conditional(DevEnvSymbol)]
        private static void ValidateDataLength(ReadOnlyMemory<byte> data, int minLength)
        {
            if (data.Length < minLength)
            {
                throw new ArgumentException($"Data length is less than {minLength}, actual={data.Length}");;
            }
        }
        
        [Conditional(DevEnvSymbol)]
        private static void ValidateType(ReadOnlyMemory<byte> data, MessageValueTypes expected)
        {
            var valueType = GetValueType(data);
            if (valueType != expected)
            {
                throw new ArgumentException($"Data is not {expected}, actual={valueType}");
            }
        }
    }
}
