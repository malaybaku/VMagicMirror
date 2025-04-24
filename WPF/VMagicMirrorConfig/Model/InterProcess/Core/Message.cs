using Baku.VMagicMirror;
using Baku.VMagicMirror.IpcMessage;
using System;

namespace Baku.VMagicMirrorConfig
{
    public readonly struct Message
    {
        private Message(VmmCommands command, ReadOnlyMemory<byte> data)
        {
            Command = command;
            Data = data;
        }

        public VmmCommands Command { get; }
        public ReadOnlyMemory<byte> Data { get; }

        public static Message None(VmmCommands command) => new(command, MessageSerializer.None((ushort)command));
        public static Message Bool(VmmCommands command, bool value) => new(command, MessageSerializer.Bool((ushort)command, value));
        public static Message Int(VmmCommands command, int value) => new(command, MessageSerializer.Int((ushort)command, value));
        public static Message Float(VmmCommands command, float value) => new(command, MessageSerializer.Float((ushort)command, value));
        public static Message String(VmmCommands command, string value) => new(command, MessageSerializer.String((ushort)command, value));
        public static Message ByteArray(VmmCommands command, byte[] value) => new(command, MessageSerializer.ByteArray((ushort)command, value));
        public static Message IntArray(VmmCommands command, int[] value) => new(command, MessageSerializer.IntArray((ushort)command, value));
        public static Message FloatArray(VmmCommands command, float[] value) => new(command, MessageSerializer.FloatArray((ushort)command, value));
    }
}
