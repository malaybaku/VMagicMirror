using UnityEngine;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// マイクによるリップシンク関連のメッセージを受け取るクラス。
    /// メッセージの状況に基づいてOVRLipSyncのオンオフを制御する。
    /// 外部トラッキングで上書きするときはオフ、みたいな所まで面倒を見てくれる。
    /// </summary>
    public class LipSyncReceiver : MonoBehaviour
    {
        [Inject]
        private ReceivedMessageHandler handler = null;

        private DeviceSelectableLipSyncContext _lipSyncContext;
        private AnimMorphEasedTarget _animMorphEasedTarget;
        private LipSyncIntegrator _lipSyncIntegrator;
        
        private string _receivedDeviceName = "";
        private bool _isLipSyncActive = true;
        private bool _isExTrackerActive = false;
        private bool _isExTrackerLipSyncActive = false;

        private void Start()
        {
            _lipSyncContext = GetComponent<DeviceSelectableLipSyncContext>();
            _animMorphEasedTarget = GetComponent<AnimMorphEasedTarget>();
            _lipSyncIntegrator = GetComponent<LipSyncIntegrator>();
            
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
                    case MessageCommandNames.ExTrackerEnable:
                        SetExTrackerEnable(message.ToBoolean());
                        break;
                    case MessageCommandNames.ExTrackerEnableLipSync:
                        SetExTrackerLipSyncEnable(message.ToBoolean());
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
            _isLipSyncActive = isEnabled;
            RefreshMicrophoneLipSyncStatus();
        }

        private void SetMicrophoneDeviceName(string deviceName)
        {
            _receivedDeviceName = deviceName;
            RefreshMicrophoneLipSyncStatus();
        }

        private void SetExTrackerLipSyncEnable(bool enable)
        {
            _isExTrackerActive = enable;
            RefreshMicrophoneLipSyncStatus();
        }

        private void SetExTrackerEnable(bool enable)
        {
            _isExTrackerLipSyncActive = enable;
            RefreshMicrophoneLipSyncStatus();
        }

        private void RefreshMicrophoneLipSyncStatus()
        {
            //NOTE: 毎回いったんストップするのはちょっとダサいけど、設定が切り替わる回数はたかが知れてるからいいかな…という判断です
            _lipSyncContext.StopRecording();

            bool shouldStartReceive =
                _isLipSyncActive &&
                !(_isExTrackerActive && _isExTrackerLipSyncActive);

            _animMorphEasedTarget.ShouldReceiveData = shouldStartReceive;
            _lipSyncIntegrator.PreferExternalTrackerLipSync = _isExTrackerActive && _isExTrackerLipSyncActive;
            
            if (shouldStartReceive)
            {
                _lipSyncContext.StartRecording(_receivedDeviceName);
            }
        }
    }
}
