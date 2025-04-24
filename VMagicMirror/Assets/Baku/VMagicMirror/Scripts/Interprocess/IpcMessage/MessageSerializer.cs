using System;
using System.Collections.Generic;

// ペイロードになってるデータのフォーマット
// 0-1: コマンドID, ただしクエリへのレスポンスの場合は0でよい
// 2-3: データ型
// 4-: データ本体。データ型がNoneの場合、そもそもペイロードの全長が4byteになり、ここに相当する部分は無い
namespace Baku.VMagicMirror.IpcMessage
{
    public enum MessageValueTypes : ushort
    {
        None = 0,
        Bool = 1,
        Int = 2,
        Float = 3,
        String = 4,
        ByteArray = 5,
        IntArray = 6,
        FloatArray = 7,
    }

    public static class MessageSerializer
    {
        public static byte[] None(ushort commandId)
        {
            var result = new byte[4];
            SetupHeader(result, commandId, (ushort) MessageValueTypes.None);
            return result;
        }

        public static byte[] Bool(ushort commandId, bool value)
        {
            var result = new byte[5];
            SetupHeader(result, commandId, (ushort) MessageValueTypes.Bool);
            result[4] = (byte)(value ? 1 : 0);
            return result;
        }
        
        public static byte[] Int(ushort commandId, int value)
        {
            var result = new byte[8];
            SetupHeader(result, commandId, (ushort) MessageValueTypes.Int);
            BitConverter.TryWriteBytes(result.AsSpan()[4..], value);
            return result;
        }
        
        public static byte[] Float(ushort commandId, float value)
        {
            var result = new byte[8];
            SetupHeader(result, commandId, (ushort) MessageValueTypes.Float);
            BitConverter.TryWriteBytes(result.AsSpan()[4..], value);
            return result;
        }

        // NOTE: stringや配列はnullはNGだがカラにするのはOKであり、空の場合はデータの全長が4byteになる
        public static byte[] String(ushort commandId, string value)
        {
            var strBytes = System.Text.Encoding.UTF8.GetBytes(value);
            var result = new byte[4 + strBytes.Length];
            SetupHeader(result, commandId, (ushort) MessageValueTypes.String);
            if (strBytes.Length > 0)
            {
                Array.Copy(strBytes, 0, result, 4, strBytes.Length);
            }
            return result;
        }
        
        public static byte[] ByteArray(ushort commandId, byte[] value)
        {
            var result = new byte[4 + value.Length];
            SetupHeader(result, commandId, (ushort) MessageValueTypes.ByteArray);
            if (value.Length > 0)
            {
                Array.Copy(value, 0, result, 4, value.Length);
            }
            return result;
        }

        public static byte[] IntArray(ushort commandId, IReadOnlyList<int> value)
        {
            var result = new byte[4 + value.Count * 4];
            SetupHeader(result, commandId, (ushort)MessageValueTypes.IntArray);
            for (var i = 0; i < value.Count; i++)
            {
                BitConverter.TryWriteBytes(result.AsSpan()[(i * 4 + 4)..], value[i]);
            }
            return result;
        }
        
        public static byte[] FloatArray(ushort commandId, IReadOnlyList<float> value)
        {
            var result = new byte[4 + value.Count * 4];
            SetupHeader(result, commandId, (ushort) MessageValueTypes.FloatArray);
            for (var i = 0; i < value.Count; i++)
            {
                BitConverter.TryWriteBytes(result.AsSpan()[(i * 4 + 4)..], value[i]);
            }
            return result;
        }

        private static void SetupHeader(Span<byte> dest, ushort commandId, ushort valueType)
        {
            BitConverter.TryWriteBytes(dest, commandId);
            BitConverter.TryWriteBytes(dest[2..], valueType);
        }
    }
}
