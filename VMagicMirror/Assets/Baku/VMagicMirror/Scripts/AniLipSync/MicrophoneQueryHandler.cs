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

        private void OnQueryRequested(ReceivedQuery query)
        {
            switch (query.Command)
            {
                case MessageQueryNames.CurrentMicrophoneDeviceName:
                    query.Result = lipSyncContext.DeviceName;
                    break;
                case MessageQueryNames.MicrophoneDeviceNames:
                    query.Result = string.Join("\t", Microphone.devices);
                    break;
            }
        }
    }
}
