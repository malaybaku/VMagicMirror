using System;

namespace Baku.VMagicMirror.IpcMessage
{
    // NOTE:
    // - Serializeのときに書き込んだ型情報を見てないが、見て弾くようなガードを足してもよい
    // - 高速化は特に頑張ってないが、インライン化とかMemoryMarshal.Readとか使う余地はある
    public static class MessageDeserializer
    {
        public static ushort GetCommandId(ReadOnlyMemory<byte> data)
        {
            if (data.Length < 2) throw new ArgumentException("Data is too short");
            return BitConverter.ToUInt16(data.Span);
        }
        
        public static bool ToBool(ReadOnlyMemory<byte> data)
        {
            if (data.Length < 5) throw new ArgumentException("Invalid data length for Bool");
            return data.Span[4] != 0;
        }
        
        public static int ToInt(ReadOnlyMemory<byte> data)
        {
            if (data.Length < 8) throw new ArgumentException("Invalid data length for Int");
            return BitConverter.ToInt32(data.Span[4..]);
        }
        
        public static float ToFloat(ReadOnlyMemory<byte> data)
        {
            if (data.Length < 8) throw new ArgumentException("Invalid data length for Float");
            return BitConverter.ToSingle(data.Span[4..]);
        }
        
        public static string ToString(ReadOnlyMemory<byte> data)
        {
            if (data.Length < 5) throw new ArgumentException("Invalid data length for String");
            return System.Text.Encoding.UTF8.GetString(data.Span[4..]);
        }
        
        public static byte[] ToByteArray(ReadOnlyMemory<byte> data)
        {
            if (data.Length < 5) throw new ArgumentException("Invalid data length for ByteArray");
            return data.Span[4..].ToArray();
        }
        
        public static int[] ToIntArray(ReadOnlyMemory<byte> data)
        {
            if (data.Length < 8) throw new ArgumentException("Invalid data length for IntArray");
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
            if (data.Length < 8) throw new ArgumentException("Invalid data length for IntArray");
            var count = (data.Length - 4) / 4;
            var result = new float[count];

            var span = data.Span;
            for (var i = 0; i < count; i++)
            {
                result[i] = BitConverter.ToSingle(span[(4 + i * 4)..]);
            }
            
            return result;
        }
    }
}
