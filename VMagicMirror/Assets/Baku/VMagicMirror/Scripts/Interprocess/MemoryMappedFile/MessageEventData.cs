using System;

namespace Baku.VMagicMirror.Mmf
{
    public class ReceiveCommandEventArgs : EventArgs
    {
        public ReceiveCommandEventArgs(string content)
        {
            Command = content;
        }
        public string Command { get; }
    }

    public class ReceiveQueryEventArgs : EventArgs
    {
        public ReceiveQueryEventArgs(MemoryMappedNamedConnectBase.ReceivedQuery query)
        {
            Query = query;
        }
        public MemoryMappedNamedConnectBase.ReceivedQuery Query { get; }
    }
}
