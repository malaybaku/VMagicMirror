using Baku.VMagicMirror.VMCP;
using UniRx;
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
                _enableWebCamTracking,
                _enableWebCamHighPowerMode,
                _enableExTracker,
                _enableHandTracking,
                (a0, a1, a2, a3, a4) => Unit.Default
                )
                .Subscribe(_ => SetFaceControlMode())
                .AddTo(this);

            _vmcpBlendShape.IsActive
                .Subscribe(active => _config.UseVMCPFacial = active)
                .AddTo(this);
        }

        void SetFaceControlMode()
        {
            //TODO: 「ハンドトラッキングが有効だと顔トラは低負荷ではなく高負荷になる」という条件が複数箇所に定義されてるのを直したい
            var webCamHighPowerModeActive = _enableWebCamHighPowerMode.Value || _enableHandTracking.Value;
            
            _config.ControlMode =
                _vmcpHeadPose.IsActive.Value ? FaceControlModes.VMCProtocol :
                _enableExTracker.Value ? FaceControlModes.ExternalTracker :
                (_enableWebCamTracking.Value && webCamHighPowerModeActive) ? FaceControlModes.WebCamHighPower :
                _enableWebCamTracking.Value ? FaceControlModes.WebCamLowPower :
                FaceControlModes.None;
        }
    }
}
