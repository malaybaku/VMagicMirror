using Baku.VMagicMirror;
using Baku.VMagicMirror.IpcMessage;
using System;

namespace Baku.VMagicMirrorConfig
{
    public readonly struct Message
    {
        private Message(ReadOnlyMemory<byte> data)
        {
            Data = data;
        }

        public ReadOnlyMemory<byte> Data { get; }

        
        public static Message None(VmmCommands command) => new(MessageSerializer.None((ushort)command));
        public static Message Bool(VmmCommands command, bool value) => new(MessageSerializer.Bool((ushort)command, value));
        public static Message Int(VmmCommands command, int value) => new(MessageSerializer.Int((ushort)command, value));
        public static Message Float(VmmCommands command, float value) => new(MessageSerializer.Float((ushort)command, value));
        public static Message String(VmmCommands command, string value) => new(MessageSerializer.String((ushort)command, value));
        public static Message ByteArray(VmmCommands command, byte[] value) => new(MessageSerializer.ByteArray((ushort)command, value));
        public static Message IntArray(VmmCommands command, int[] value) => new(MessageSerializer.IntArray((ushort)command, value));
        public static Message FloatArray(VmmCommands command, float[] value) => new(MessageSerializer.FloatArray((ushort)command, value));



        ////NOTE: コマンドにはコロン(":")を入れない事！(例外スローの方が健全かも)
        //public Message(string command, string content)
        //{
        //    Command = command?.Replace(":", "") ?? "";
        //    Content = content ?? "";
        //}

        ////パラメータが無いものはコレで十分
        //public Message(string command) : this(command, "")
        //{
        //}

        //public string Command { get; }
        //public string Content { get; }

    }
}
