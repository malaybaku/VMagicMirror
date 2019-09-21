using UnityEngine;

namespace Baku.VMagicMirror
{
    public class MicrophoneQueryHandler : MonoBehaviour
    {
        [SerializeField]
        private ReceivedMessageHandler handler = null;

        [SerializeField]
        private DeviceSelectableLipSyncContext lipSyncContext = null;

        private void Start()
        {
            handler.QueryRequested += OnQueryRequested;
        }

        private void OnDestroy()
        {
            handler.QueryRequested -= OnQueryRequested;
        }

        private void OnQueryRequested(object sender, ReceivedMessageHandler.QueryEventArgs e)
        {
            switch (e.Query.Command)
            {
                case MessageQueryNames.CurrentMicrophoneDeviceName:
                    e.Query.Result = lipSyncContext.DeviceName;
                    break;
                case MessageQueryNames.MicrophoneDeviceNames:
                    e.Query.Result = string.Join("\t", Microphone.devices);
                    break;
                default:
                    break;
            }
        }
    }
}
