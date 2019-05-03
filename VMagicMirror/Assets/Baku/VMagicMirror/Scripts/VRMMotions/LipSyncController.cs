using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    public class LipSyncController : MonoBehaviour
    {
        [SerializeField]
        private ReceivedMessageHandler handler = null;

        private DeviceSelectableLipSyncContext _lipSyncContext = null;
        private AnimMorphEasedTarget _animMorphTarget = null;

        private string _receivedDeviceName = "";
        private bool _isLipSyncActive = true;

        private void Start()
        {
            _lipSyncContext = GetComponent<DeviceSelectableLipSyncContext>();
            _animMorphTarget = GetComponent<AnimMorphEasedTarget>();
            handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.EnableLipSync:
                        SetLipSyncEnable(message.ToBoolean());
                        break;
                    case MessageCommandNames.SetMicrophoneDeviceName:
                        SetMicrophoneDeviceName(message.Content);
                        break;
                    default:
                        break;
                }
            });
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
                    e.Query.Result = _lipSyncContext.DeviceName;
                    break;
                case MessageQueryNames.MicrophoneDeviceNames:
                    e.Query.Result = string.Join("\t", Microphone.devices);
                    break;
                default:
                    break;
            }
        }

        private void SetLipSyncEnable(bool isEnabled)
        {
            _animMorphTarget.enabled = isEnabled;
            _isLipSyncActive = isEnabled;
            if (isEnabled)
            {
                //変な名前を受け取ってたら実際には起動しない点に注意
                _lipSyncContext.StartRecording(_receivedDeviceName);
            }
            else
            {
                _lipSyncContext.StopRecording();
            }
        }

        private void SetMicrophoneDeviceName(string deviceName)
        {
            _receivedDeviceName = deviceName;
            if (_isLipSyncActive)
            {
                _lipSyncContext.StopRecording();
                _lipSyncContext.StartRecording(deviceName);
            }
        }
    }
}
