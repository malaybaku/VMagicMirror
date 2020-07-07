using Baku.VMagicMirror.InterProcess;
using UnityEngine;
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
        private DeviceSelectableLipSyncContext _lipSyncContext;
        private AnimMorphEasedTarget _animMorphEasedTarget;
        private LipSyncIntegrator _lipSyncIntegrator;
        
        private string _receivedDeviceName = "";
        private bool _isLipSyncActive = true;
        private bool _isExTrackerActive = false;
        private bool _isExTrackerLipSyncActive = false;

        [Inject]
        public void Initialize(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                MessageCommandNames.EnableLipSync,
                message => SetLipSyncEnable(message.ToBoolean())
            );
            receiver.AssignCommandHandler(
                MessageCommandNames.SetMicrophoneDeviceName,
                message => SetMicrophoneDeviceName(message.Content)
            );
            receiver.AssignCommandHandler(
                MessageCommandNames.ExTrackerEnable,
                message => SetExTrackerEnable(message.ToBoolean())
            );
            receiver.AssignCommandHandler(
                MessageCommandNames.ExTrackerEnableLipSync,
                message => SetExTrackerLipSyncEnable(message.ToBoolean())
            );

            receiver.AssignQueryHandler(
                MessageQueryNames.CurrentMicrophoneDeviceName,
                query => query.Result = _lipSyncContext.DeviceName
            );
            receiver.AssignQueryHandler(
                MessageQueryNames.MicrophoneDeviceNames,
                query => query.Result = string.Join("\t", Microphone.devices)
            );
        }
        
        private void Start()
        {
            _lipSyncContext = GetComponent<DeviceSelectableLipSyncContext>();
            _animMorphEasedTarget = GetComponent<AnimMorphEasedTarget>();
            _lipSyncIntegrator = GetComponent<LipSyncIntegrator>();
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
