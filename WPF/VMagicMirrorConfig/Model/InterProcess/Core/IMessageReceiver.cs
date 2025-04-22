using System;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// IPC用のメッセージを受信する方式を定義します。
    /// </summary>
    internal interface IMessageReceiver
    {
        void Start();
        void Stop();
        event Action<CommandReceivedData>? ReceivedCommand;

        // NOTE: こっちは本質的にWriteが発生するので、参照型のEventArgsであることを維持している (EAと別の型のActionでも別に良いのだが)。
        event EventHandler<QueryReceivedEventArgs>? ReceivedQuery;
    }

    public readonly struct CommandReceivedData
    {
        public CommandReceivedData(string command, string args)
        {
            Command = command;
            Args = args;
        }
        public string Command { get; }
        public string Args { get; }
    }

    // NOTE: 2025/04時点でWPFがQueryを受け取るケースは存在しないっぽい。初使用時には注意してデバッグすること
    public class QueryReceivedEventArgs : EventArgs
    {
        public QueryReceivedEventArgs(string command, string args)
        {
            Command = command;
            Args = args;
        }
        public string Command { get; }
        public string Args { get; }
        public string Result { get; set; } = "";
    }
}
