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
        event EventHandler<CommandReceivedEventArgs>? ReceivedCommand;
        event EventHandler<QueryReceivedEventArgs>? ReceivedQuery;
    }

    public class CommandReceivedEventArgs : EventArgs
    {
        public CommandReceivedEventArgs(string command, string args)
        {
            Command = command;
            Args = args;
        }
        public string Command { get; }
        public string Args { get; }
    }

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
