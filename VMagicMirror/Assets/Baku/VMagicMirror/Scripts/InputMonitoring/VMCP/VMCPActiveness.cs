using R3;

namespace Baku.VMagicMirror.VMCP
{
    // ※今は「どれかひとつでもactiveかどうか」という値を返してるが、hand/head/blendshapeについてのfacadeを兼任してもよい

    /// <summary>
    /// VMCPの受信状態がアクティブかどうか監視するクラス。
    /// </summary>
    public class VMCPActiveness : PresenterBase
    {
        private readonly VMCPHeadPose _headPose;
        private readonly VMCPHandPose _handPose;
        private readonly VMCPBlendShape _blendShape;

        public VMCPActiveness(
            VMCPHeadPose headPose,
            VMCPHandPose handPose,
            VMCPBlendShape blendShape)
        {
            _handPose = handPose;
            _headPose = headPose;
            _blendShape = blendShape;
        }

        private readonly ReactiveProperty<bool> _isActive = new ReactiveProperty<bool>(false);
        public IReadOnlyReactiveProperty<bool> IsActive => _isActive;

        public override void Initialize()
        {
            _handPose.IsActive
                .CombineLatest(
                    _headPose.IsActive,
                    _blendShape.IsActive,
                    (x, y, z) => x || y || z)
                .Subscribe(v => _isActive.Value = v)
                .AddTo(this);
        }
    }
}