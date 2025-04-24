using System;
using Baku.VMagicMirror.MediaPipeTracker;
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

        private readonly ReactiveProperty<string> _microphoneDeviceName = new("");
        private readonly ReactiveProperty<bool> _isMicrophoneLipSyncActive = new(true);
        private readonly ReactiveProperty<bool> _isExTrackerLipSyncActive = new(true);

        // このフラグがオンの場合、マイクを止める方向に処理を寄せたい
        private readonly ReactiveProperty<bool> _isImageBaseLipSyncActive = new(false);
        
        // NOTE: FaceControlConfig使ったほうがいい？
        private FaceControlConfiguration _faceControlConfig;
        private MediaPipeTrackerRuntimeSettingsRepository _mediaPipeTrackerRuntimeSettings;
        
        [Inject]
        public void Initialize(
            IMessageReceiver receiver,
            VMCPBlendShape vmcpBlendShape,
            FaceControlConfiguration faceControlConfig,
            MediaPipeTrackerRuntimeSettingsRepository mediaPipeTrackerRuntimeSettings
            )
        {
            _vmcpBlendShape = vmcpBlendShape;
            _faceControlConfig = faceControlConfig;
            _mediaPipeTrackerRuntimeSettings = mediaPipeTrackerRuntimeSettings;

            receiver.BindBoolProperty(VmmCommands.EnableLipSync, _isMicrophoneLipSyncActive);
            receiver.BindStringProperty(VmmCommands.SetMicrophoneDeviceName, _microphoneDeviceName);
            // NOTE: [dB]単位であることに注意
            receiver.AssignCommandHandler(
                VmmCommands.SetMicrophoneSensitivity,
                message => _lipSyncContext.Sensitivity = message.ToInt()
                );

            receiver.AssignCommandHandler(
                VmmCommands.ExTrackerEnableLipSync,
                m =>
                {
                    var value = m.ToBoolean();
                    _isExTrackerLipSyncActive.Value = value;
                    // TODO: フラグの渡し方がハンパすぎるので、 _lipSyncIntegrator側で勝手にフラグを見に行ってほしい気もする…
                    _lipSyncIntegrator.PreferExternalTrackerLipSync = value;
                });

            receiver.AssignQueryHandler(
                VmmCommands.CurrentMicrophoneDeviceName,
                query => query.Result = _lipSyncContext.DeviceName
            );
            receiver.AssignQueryHandler(
                VmmCommands.MicrophoneDeviceNames,
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

            _faceControlConfig.FaceControlMode
                .CombineLatest(
                    _isExTrackerLipSyncActive,
                    _mediaPipeTrackerRuntimeSettings.ShouldUseLipSyncResult,
                    (mode, exTrackerLipSync, webCamHighPowerLipSync) => (mode, exTrackerLipSync, webCamHighPowerLipSync)
                )
                .Subscribe(value =>
                {
                    var (mode, exTrackerLipSync, webCamHighPowerLipSync) = value;
                    _isImageBaseLipSyncActive.Value = 
                        (mode is FaceControlModes.ExternalTracker && exTrackerLipSync) ||
                        (mode is FaceControlModes.WebCamHighPower && webCamHighPowerLipSync);
                })
                .AddTo(this);
            
            _microphoneDeviceName.CombineLatest(
                _isMicrophoneLipSyncActive,
                _isImageBaseLipSyncActive,
                _vmcpBlendShape.IsActive,
                (a, b, c, d) => Unit.Default
                )
                .Skip(1)
                .Subscribe(_ => RefreshMicrophoneLipSyncStatus())
                .AddTo(this);
        }

        private void RefreshMicrophoneLipSyncStatus()
        {
            //NOTE: 毎回いったんストップするのはちょっとダサいけど、設定が切り替わる回数はたかが知れてるからいいかな…という判断です
            _lipSyncContext.StopRecording();

            var shouldStartReceive =
                !_vmcpBlendShape.IsActive.Value &&
                _isMicrophoneLipSyncActive.Value &&
                !_isImageBaseLipSyncActive.Value;

            _animMorphEasedTarget.ShouldReceiveData = shouldStartReceive;
            
            if (shouldStartReceive)
            {
                try
                {
                    _lipSyncContext.StartRecording(_microphoneDeviceName.Value);
                }
                catch (Exception ex)
                {
                    LogOutput.Instance.Write(ex);
                }
            }
        }
    }
}
