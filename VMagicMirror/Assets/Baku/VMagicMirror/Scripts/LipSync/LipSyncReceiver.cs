using UnityEngine;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    public class LipSyncReceiver : MonoBehaviour
    {
        [Inject]
        private ReceivedMessageHandler handler = null;

        private DeviceSelectableLipSyncContext _lipSyncContext;
        private AnimMorphEasedTarget _animMorphTarget;

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
                }
            });
            handler.QueryRequested += OnQueryRequested;
        }

        private void OnQueryRequested(ReceivedQuery query)
        {
            switch (query.Command)
            {
                case MessageQueryNames.CurrentMicrophoneDeviceName:
                    query.Result = _lipSyncContext.DeviceName;
                    break;
                case MessageQueryNames.MicrophoneDeviceNames:
                    query.Result = string.Join("\t", Microphone.devices);
                    break;
            }
        }

        private void SetLipSyncEnable(bool isEnabled)
        {
            _animMorphTarget.ForceClosedMouth = !isEnabled;
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
