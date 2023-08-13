using System;
using Baku.VMagicMirror.VMCP;
using UniRx;
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
        private VMCPBlendShape _vmcpBlendShape;

        private readonly ReactiveProperty<string> _deviceName = new ReactiveProperty<string>("");
        private readonly ReactiveProperty<bool> _isLipSyncActive = new ReactiveProperty<bool>(true);
        private readonly ReactiveProperty<bool> _isExTrackerActive = new ReactiveProperty<bool>(false);
        private readonly ReactiveProperty<bool> _isExTrackerLipSyncActive = new ReactiveProperty<bool>(true);

        [Inject]
        public void Initialize(
            IMessageReceiver receiver,
            VMCPBlendShape vmcpBlendShape)
        {
            _vmcpBlendShape = vmcpBlendShape;

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

            _deviceName.CombineLatest(
                _isLipSyncActive,
                _isExTrackerActive,
                _isExTrackerLipSyncActive,
                _vmcpBlendShape.IsActive,
                (a, b, c, d, e) => Unit.Default
                )
                .Skip(1)
                .Subscribe(_ => RefreshMicrophoneLipSyncStatus())
                .AddTo(this);
        }

        //[dB]単位であることに注意
        private void SetMicrophoneSensitivity(int sensitivity) => _lipSyncContext.Sensitivity = sensitivity;

        private void SetLipSyncEnable(bool isEnabled) => _isLipSyncActive.Value = isEnabled;
        private void SetMicrophoneDeviceName(string deviceName) => _deviceName.Value = deviceName;
        private void SetExTrackerEnable(bool enable) => _isExTrackerActive.Value = enable;
        private void SetExTrackerLipSyncEnable(bool enable) => _isExTrackerLipSyncActive.Value = enable;

        private void RefreshMicrophoneLipSyncStatus()
        {
            //NOTE: 毎回いったんストップするのはちょっとダサいけど、設定が切り替わる回数はたかが知れてるからいいかな…という判断です
            _lipSyncContext.StopRecording();

            var shouldStartReceive =
                !_vmcpBlendShape.IsActive.Value && 
                _isLipSyncActive.Value &&
                !(_isExTrackerActive.Value && _isExTrackerLipSyncActive.Value);

            _animMorphEasedTarget.ShouldReceiveData = shouldStartReceive;
            _lipSyncIntegrator.PreferExternalTrackerLipSync = _isExTrackerActive.Value && _isExTrackerLipSyncActive.Value;
            
            if (shouldStartReceive)
            {
                try
                {
                    _lipSyncContext.StartRecording(_deviceName.Value);
                }
                catch (Exception ex)
                {
                    LogOutput.Instance.Write(ex);
                }
            }
        }
    }
}
