using System;
using Baku.VMagicMirror.IpcMessage;

namespace Baku.VMagicMirror
{
    /// <summary> UnityからWPF向けに送信するメッセージ全般 </summary>
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
        
        public static Message Bool(VmmServerCommands command, bool value)
            => new(command, MessageSerializer.Bool((ushort)command, value));

        public static Message Int(VmmServerCommands command, int value)
            => new(command, MessageSerializer.Int((ushort)command, value));
        
        // NOTE: BoolやIntがまだないが、追加してよい。
    }
}
