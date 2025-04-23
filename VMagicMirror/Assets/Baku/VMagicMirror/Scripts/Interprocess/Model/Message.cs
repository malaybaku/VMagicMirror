using System;
using Baku.VMagicMirror.IpcMessage;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// UnityからWPF向けに送信するメッセージ
    /// </summary>
    public readonly struct Message
    {
        private Message(VmmServerCommands command, ReadOnlyMemory<byte> data)
        {
            Command = command;
            Data = data;
        }
        
        public VmmServerCommands Command { get; }
        public ReadOnlyMemory<byte> Data { get; }

        public static Message None(VmmServerCommands command)
            => new(command, MessageSerializer.None((ushort)command));

        public static Message String(VmmServerCommands command, string value)
            => new(command, MessageSerializer.String((ushort)command, value));

        // public Message(string command, string content)
        // {
        //     Command = command?.Replace(":", "") ?? "";
        //     Content = content ?? "";
        // }
        //
        // public Message(string command) : this(command, "")
        // {
        // }
        //
        // public string Command { get; }
        // public string Content { get; }
    }
}
