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
        private VmmLipSyncContextBase _lipSyncContext;
        private AnimMorphEasedTarget _animMorphEasedTarget;
        private LipSyncIntegrator _lipSyncIntegrator;
        
        private string _receivedDeviceName = "";
        private bool _isLipSyncActive = true;
        private bool _isExTrackerActive = false;
        private bool _isExTrackerLipSyncActive = true;

        [Inject]
        public void Initialize(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                VmmCommands.EnableLipSync,
                message => SetLipSyncEnable(message.ToBoolean())
            );
            receiver.AssignCommandHandler(
                VmmCommands.SetMicrophoneDeviceName,
                message => SetMicrophoneDeviceName(message.Content)
            );
            receiver.AssignCommandHandler(
                VmmCommands.SetMicrophoneSensitivity,
                message => SetMicrophoneSensitivity(message.ToInt())
                );
            receiver.AssignCommandHandler(
                VmmCommands.ExTrackerEnable,
                message => SetExTrackerEnable(message.ToBoolean())
            );
            receiver.AssignCommandHandler(
                VmmCommands.ExTrackerEnableLipSync,
                message => SetExTrackerLipSyncEnable(message.ToBoolean())
            );

            receiver.AssignQueryHandler(
                VmmQueries.CurrentMicrophoneDeviceName,
                query => query.Result = _lipSyncContext.DeviceName
            );
            receiver.AssignQueryHandler(
                VmmQueries.MicrophoneDeviceNames,
                query => query.Result = DeviceNames.CreateDeviceNamesJson(
                    _lipSyncContext.GetAvailableDeviceNames()
                    )
            );
        }
        
        private void Start()
        {
            _lipSyncContext = GetComponent<VmmLipSyncContextBase>();
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

        //[dB]単位であることに注意
        private void SetMicrophoneSensitivity(int sensitivity)
        {
            _lipSyncContext.Sensitivity = sensitivity;
        }

        private void SetExTrackerEnable(bool enable)
        {
            _isExTrackerActive = enable;
            RefreshMicrophoneLipSyncStatus();
        }

        private void SetExTrackerLipSyncEnable(bool enable)
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
