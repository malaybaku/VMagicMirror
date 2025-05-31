using System;

namespace Baku.VMagicMirrorConfig
{
    class MessageIo : IDisposable
    {
        public MessageIo()
        {
            var mmfClient = new MmfClient();
            Sender = mmfClient;
            Receiver = mmfClient;
        }

        public IMessageSender Sender { get; }
        public IMessageReceiver Receiver { get; }

        public void Start() => Receiver.Start();

        public void Dispose() => Receiver.Stop();
    }
}
