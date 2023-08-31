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

        private readonly ReactiveProperty<bool> _enableWebCamTracking = new ReactiveProperty<bool>(true);
        private readonly ReactiveProperty<bool> _enableExTracker = new ReactiveProperty<bool>(false);

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
            _receiver.AssignCommandHandler(
                VmmCommands.EnableFaceTracking,
                message =>
                {
                    _enableWebCamTracking.Value = message.ToBoolean();
                    SetFaceControlMode();
                });
            
            _receiver.AssignCommandHandler(
                VmmCommands.ExTrackerEnable,
                message =>
                {
                    _enableExTracker.Value = message.ToBoolean();
                    SetFaceControlMode();
                });

            Observable.Merge(
                _vmcpHeadPose.IsActive,
                _enableWebCamTracking,
                _enableExTracker
                )
                .Subscribe(_ => SetFaceControlMode())
                .AddTo(this);

            _vmcpBlendShape
                .IsActive
                .Subscribe(active => _config.UseVMCPFacial = active)
                .AddTo(this);
        }

        void SetFaceControlMode()
        {
            _config.ControlMode =
                _vmcpHeadPose.IsActive.Value ? FaceControlModes.VMCProtocol :
                _enableExTracker.Value ? FaceControlModes.ExternalTracker :
                _enableWebCamTracking.Value ? FaceControlModes.WebCam :
                FaceControlModes.None;
        }
    }
}
