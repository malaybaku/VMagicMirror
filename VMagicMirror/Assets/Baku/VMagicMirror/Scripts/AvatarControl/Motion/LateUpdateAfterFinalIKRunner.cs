using Baku.VMagicMirror.FK;
using R3;
using RootMotion.FinalIK;
using Zenject;

namespace Baku.VMagicMirror
{
    public sealed class LateUpdateAfterFinalIKRunner : PresenterBase
    {
        private readonly LateUpdateSourceAfterFinalIK _source;
        private readonly IVRMLoadable _vrmLoadable;

        private readonly MediaPipeHandLocalRotLimiter _mediaPipeHandLocalRotLimiter;
        private readonly VrmaMotionSetter _vrmaMotionSetter;
        private readonly ArmMuscleInterpolator _armMuscleInterpolator;
        private readonly SpineBoneModifier _spineBoneModifier;
        private LimbIK _leftLegIk;
        private LimbIK _rightLegIk;
        private bool _hasModel;

        [Inject]
        public LateUpdateAfterFinalIKRunner(
            LateUpdateSourceAfterFinalIK source,
            IVRMLoadable vrmLoadable,
            MediaPipeHandLocalRotLimiter mediaPipeHandLocalRotLimiter,
            VrmaMotionSetter vrmaMotionSetter,
            ArmMuscleInterpolator armMuscleInterpolator,
            SpineBoneModifier spineBoneModifier
            )
        {
            _source = source;
            _vrmLoadable = vrmLoadable;
            _mediaPipeHandLocalRotLimiter = mediaPipeHandLocalRotLimiter;
            _vrmaMotionSetter = vrmaMotionSetter;
            _armMuscleInterpolator = armMuscleInterpolator;
            _spineBoneModifier = spineBoneModifier;
        }
        
        public override void Initialize()
        {
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmDisposing;

            _source.OnLateUpdate
                .Subscribe(_ => OnLateUpdate())
                .AddTo(this);
        }

        private void OnLateUpdate()
        {
            // FBBIK -> LimbIK をこの順で呼ぶのをスクリプト上で担保したいのでここに書いてる
            if (_hasModel)
            {
                _leftLegIk.solver.Update();
                _rightLegIk.solver.Update();
            }

            _mediaPipeHandLocalRotLimiter.LateUpdate();
            _vrmaMotionSetter.ApplyUpdate();
            _armMuscleInterpolator.Update();
            _spineBoneModifier.Apply();
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _leftLegIk = info.LeftLegIk;
            _rightLegIk = info.RightLegIk;
            _hasModel = true;
        }

        private void OnVrmDisposing()
        {
            _hasModel = false;
            _leftLegIk = null;
            _rightLegIk = null;
        }
    }
}
