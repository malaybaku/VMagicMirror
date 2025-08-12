using Baku.VMagicMirror.VMCP;
using R3;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// <see cref="FaceControlConfiguration"/>のうち、制御モードについてプロセス間通信をもとに書き込むクラスです。
    /// </summary>
    public class FaceControlConfigurationReceiver : PresenterBase
    {
        private readonly IMessageReceiver _receiver;
        private readonly FaceControlConfiguration _config;

        //NOTE: VMCP自体のon/offだけだとモードが確定しないので別途持ってきてる
        private readonly VMCPBlendShape _vmcpBlendShape;
        private readonly VMCPHeadPose _vmcpHeadPose;

        private readonly ReactiveProperty<bool> _enableWebCamTracking = new(true);
        private readonly ReactiveProperty<bool> _enableWebCamHighPowerMode = new(false);
        private readonly ReactiveProperty<bool> _enableExTracker = new(false);
        private readonly ReactiveProperty<bool> _enableHandTracking = new(false);

        [Inject]
        public FaceControlConfigurationReceiver(
            IMessageReceiver receiver, 
            FaceControlConfiguration config,
            VMCPBlendShape vmcpBlendShape,
            VMCPHeadPose vmcpHeadPose)
        {
            _receiver = receiver;
            _config = config;
            _vmcpBlendShape = vmcpBlendShape;
            _vmcpHeadPose = vmcpHeadPose;
        }

        public override void Initialize()
        {
            _receiver.BindBoolProperty(VmmCommands.EnableFaceTracking, _enableWebCamTracking);
            _receiver.BindBoolProperty(VmmCommands.EnableWebCamHighPowerMode, _enableWebCamHighPowerMode);
            _receiver.BindBoolProperty(VmmCommands.ExTrackerEnable, _enableExTracker);
            _receiver.BindBoolProperty(VmmCommands.EnableImageBasedHandTracking, _enableHandTracking);
            
            
            _vmcpHeadPose.IsActive.CombineLatest(
                _vmcpBlendShape.IsActive,
                _enableWebCamTracking,
                _enableWebCamHighPowerMode,
                _enableExTracker,
                _enableHandTracking,
                (a0, a1, a2, a3, a4, a5) => Unit.Default
                )
                .Subscribe(_ => SetFaceControlMode())
                .AddTo(this);
        }

        void SetFaceControlMode()
        {
            //TODO: 「ハンドトラッキングが有効だと顔トラは低負荷ではなく高負荷になる」という条件が複数箇所に定義されてるのを直したい
            var webCamHighPowerModeActive = _enableWebCamHighPowerMode.Value || _enableHandTracking.Value;

            // NOTE: VMCProtocolの受信ではポーズと表情が分離してあるため、
            // 「モーションは受信するけど表情は使わない」や「表情は受信するけどモーションは使わない」というケースが発生することに注意
            var motionMode = 
                _vmcpHeadPose.IsActive.CurrentValue ? FaceControlModes.VMCProtocol :
                _enableExTracker.Value ? FaceControlModes.ExternalTracker :
                (_enableWebCamTracking.Value && webCamHighPowerModeActive) ? FaceControlModes.WebCamHighPower :
                _enableWebCamTracking.Value ? FaceControlModes.WebCamLowPower :
                FaceControlModes.None;

            var blendShapeMode = 
                _vmcpBlendShape.IsActive.CurrentValue ? FaceControlModes.VMCProtocol :
                _enableExTracker.Value ? FaceControlModes.ExternalTracker :
                (_enableWebCamTracking.Value && webCamHighPowerModeActive) ? FaceControlModes.WebCamHighPower :
                _enableWebCamTracking.Value ? FaceControlModes.WebCamLowPower :
                FaceControlModes.None;
            
            _config.SetFaceControlMode(motionMode, blendShapeMode);
        }
    }
}
