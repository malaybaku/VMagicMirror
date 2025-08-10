using UnityEngine;
using R3;
using Zenject;

namespace Baku.VMagicMirror
{
    // TODO: もうちょっと堅牢に動くようになったら復活してもよい。現在は回転値のジャンプが目につくのでBindしてない
    public class MediaPipeHandLocalRotLimiter : PresenterBase
    {
        // bye byeとかで手をふる方向の制限
        private const float ClampYaw = 30f;
        // come onみたいな動きのときにひねる方向
        private const float ClampRoll = 60f;
        
        private readonly IVRMLoadable _vrmLoadable;
        private readonly LateUpdateSourceAfterFinalIK _lateUpdateSource;
        private readonly HandIKIntegrator _handIKIntegrator;

        private bool _hasModel;
        private Transform _leftHandBone;
        private Transform _rightHandBone;
        
        [Inject]
        public MediaPipeHandLocalRotLimiter(
            IVRMLoadable vrmLoadable,
            LateUpdateSourceAfterFinalIK lateUpdateSource,
            HandIKIntegrator handIKIntegrator
            )
        {
            _vrmLoadable = vrmLoadable;
            _handIKIntegrator = handIKIntegrator;
            _lateUpdateSource = lateUpdateSource;
        }

        public override void Initialize()
        {
            _vrmLoadable.VrmLoaded += info =>
            {
                _leftHandBone = info.animator.GetBoneTransform(HumanBodyBones.LeftHand);
                _rightHandBone = info.animator.GetBoneTransform(HumanBodyBones.RightHand);
                _hasModel = true;
            };

            _vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _leftHandBone = null;
                _rightHandBone = null;
            };
            
            // NOTE: 発火タイミングの調整がしたい(TwistRelaxerのちょっと後くらいにLateUpateが走ってほしい)のでMonoBehaviourでやる
            _lateUpdateSource.OnPreLateUpdate
                .Subscribe(_ => LateUpdate())
                .AddTo(this);
        }

        private void LateUpdate()
        {
            if (!_hasModel) return;

            if (_handIKIntegrator.LeftTargetType.Value is HandTargetType.ImageBaseHand)
            {
                LimitLeftHandBoneLocalRotation();
            }
        
            if (_handIKIntegrator.RightTargetType.Value is HandTargetType.ImageBaseHand)
            {
                LimitRightHandBoneLocalRotation();
            }
        }

        private void LimitLeftHandBoneLocalRotation()
        {
            // やりたいこと
            // - 手首がロコツに骨折するのを防ぐ
            // - その際、ロール(x軸)だけは骨折判定をかなり緩くしたい(=いわゆる手のひらクルクルするのは骨折ではないことにしたい)
            
            // 厳格かというとかなり怪しいが、オイラー角表現の分解に基づいて制限するくらいにしとく (ないよりはだいぶよい)
            var euler = _leftHandBone.localRotation.eulerAngles;
            var y = Mathf.Clamp(MathUtil.ClampAngle(euler.y), -ClampYaw, ClampYaw);
            var z = Mathf.Clamp(MathUtil.ClampAngle(euler.z), -ClampRoll, ClampRoll);
            _leftHandBone.localRotation = Quaternion.Euler(euler.x, y, z);
        }

        private void LimitRightHandBoneLocalRotation()
        {
            var euler = _rightHandBone.localRotation.eulerAngles;
            var y = Mathf.Clamp(MathUtil.ClampAngle(euler.y), -ClampYaw, ClampYaw);
            var z = Mathf.Clamp(MathUtil.ClampAngle(euler.z), -ClampRoll, ClampRoll);
            _rightHandBone.localRotation = Quaternion.Euler(euler.x, y, z);
        } 
    }
}
