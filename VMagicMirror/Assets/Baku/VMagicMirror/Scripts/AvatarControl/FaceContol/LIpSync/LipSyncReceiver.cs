using System;
using Baku.VMagicMirror.Buddy;
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
        private AvatarFacialApiImplement _buddyAvatarFacialApi;

        private readonly ReactiveProperty<string> _deviceName = new("");
        private readonly ReactiveProperty<bool> _isLipSyncActive = new(true);
        private readonly ReactiveProperty<bool> _isExTrackerActive = new(false);
        private readonly ReactiveProperty<bool> _isExTrackerLipSyncActive = new(true);

        [Inject]
        public void Initialize(
            IMessageReceiver receiver,
            VMCPBlendShape vmcpBlendShape,
            AvatarFacialApiImplement buddyAvatarFacialApi)
        {
            _vmcpBlendShape = vmcpBlendShape;
            _buddyAvatarFacialApi = buddyAvatarFacialApi;

            receiver.BindBoolProperty(VmmCommands.EnableLipSync, _isLipSyncActive);
            receiver.AssignCommandHandler(
                VmmCommands.SetMicrophoneDeviceName,
                message => SetMicrophoneDeviceName(message.Content)
            );
            receiver.AssignCommandHandler(
                VmmCommands.SetMicrophoneSensitivity,
                message => SetMicrophoneSensitivity(message.ToInt())
                );
            receiver.BindBoolProperty(VmmCommands.ExTrackerEnable, _isExTrackerActive);
            receiver.BindBoolProperty(VmmCommands.ExTrackerEnableLipSync, _isExTrackerLipSyncActive);

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
                _buddyAvatarFacialApi.RequireMicrophoneRecording,
                (a, b, c, d, e, f) => Unit.Default
                )
                .Skip(1)
                .Subscribe(_ => RefreshMicrophoneLipSyncStatus())
                .AddTo(this);
        }

        //[dB]単位であることに注意
        private void SetMicrophoneSensitivity(int sensitivity) => _lipSyncContext.Sensitivity = sensitivity;

        private void SetMicrophoneDeviceName(string deviceName) => _deviceName.Value = deviceName;

        private void RefreshMicrophoneLipSyncStatus()
        {
            //NOTE: 毎回いったんストップするのはちょっとダサいけど、設定が切り替わる回数はたかが知れてるからいいかな…という判断です
            _lipSyncContext.StopRecording();

            var shouldStartReceive =
                _buddyAvatarFacialApi.RequireMicrophoneRecording.Value || (
                    !_vmcpBlendShape.IsActive.Value && 
                    _isLipSyncActive.Value &&
                    !(_isExTrackerActive.Value && _isExTrackerLipSyncActive.Value)
                );

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
