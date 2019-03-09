using UnityEngine;

namespace Baku.VMagicMirror
{
    public class DummyMessageSender : MonoBehaviour
    {
        public string command = "";
        public string args = "";

        public bool trigger = false;

        [SerializeField]
        ReceivedMessageHandler handler;

        void Update()
        {
            if (trigger && !string.IsNullOrWhiteSpace(command))
            {
                handler?.Receive(command + ":" + args);
                command = "";
                args = "";
                trigger = false;
            }

        }
    }
}
