using System;
using Baku.VMagicMirror.IK;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// VMagicMirror実装の中で唯一アバターの目ボーンのrotationを制御する権限のあるクラス(になってほしい…)
    /// </summary>
    /// <remarks>
    /// LateUpdateの中でも非常に遅いタイミングで実行される
    /// NOTE: うまくいくとBlendShape方式の目も動かせるようになるかもしれないが、ストレッチゴール扱い
    /// </remarks>
    public class EyeBoneAngleSetter : MonoBehaviour
    {
        private const float HorizontalRateToAngle = 35f;
        private const float VerticalRateToAngle = 35f;
        
        [SerializeField] private EyeDownMotionController eyeDownMotionController;
        [SerializeField] private EyeJitter eyeJitter;
        [SerializeField] private ExternalTrackerEyeJitter externalTrackerEyeJitter;

        private NonImageBasedMotion _nonImageBasedMotion;
        private EyeBoneAngleMapApplier _angleMapApplier;
        private EyeLookAt _eyeLookAt;

        private Transform _leftEye;
        private Transform _rightEye;
        private bool _hasModel;
        private bool _hasLeftEye;
        private bool _hasRightEye;

        private IEyeRotationRequestSource[] _sources;

        [Inject]
        public void Initialize(IMessageReceiver receiver, IVRMLoadable vrmLoadable, IKTargetTransforms ikTargets, NonImageBasedMotion nonImageBasedMotion)
        {
            _angleMapApplier = new EyeBoneAngleMapApplier(receiver, vrmLoadable);
            _eyeLookAt = new EyeLookAt(receiver, vrmLoadable, ikTargets.LookAt);

            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmDisposed;
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _leftEye = info.animator.GetBoneTransform(HumanBodyBones.LeftEye);
            _rightEye = info.animator.GetBoneTransform(HumanBodyBones.RightEye);

            //NOTE: せっかく整頓しているので、片目だけボーンがあるモデルでちゃんと動くことを検討する
            _hasLeftEye = _leftEye != null;
            _hasRightEye = _rightEye != null;
            _hasModel = true;
        }

        private void OnVrmDisposed()
        {
            _hasModel = false;
            _hasLeftEye = false;
            _hasRightEye = false;

            _leftEye = null;
            _rightEye = null;
        }

        private void Start()
        {
            //NOTE: 今の計算方式だと考慮不要なはずだが、順番も少しは意識してもいいかも
            _sources = new IEyeRotationRequestSource[]
            {
                _nonImageBasedMotion,
                eyeJitter,
                externalTrackerEyeJitter,
            };
        }
        
        private void LateUpdate()
        {
            if (!_hasModel || (!_hasLeftEye && !_hasRightEye))
            {
                return;
            }
            //TODO: 各コンポーネントの主張と設定を総合して目ボーンの角度を適用する

            var leftRate = Vector2.zero;
            var rightRate = Vector2.zero;

            foreach (var s in _sources)
            {
                if (!s.IsActive)
                {
                    continue;
                }
                leftRate += s.LeftEyeRotationRate;
                rightRate += s.RightEyeRotationRate;
            }

            // 符号に注意、Unityの普通の座標系ではピッチは下が正
            var leftYaw = leftRate.x * HorizontalRateToAngle;
            var leftPitch = -leftRate.y * VerticalRateToAngle;

            var rightYaw = rightRate.x * HorizontalRateToAngle;
            var rightPitch = -rightRate.y * VerticalRateToAngle;

            _eyeLookAt.Calculate();
            leftYaw += _eyeLookAt.Yaw;
            rightYaw += _eyeLookAt.Yaw;
            leftPitch += _eyeLookAt.Pitch;
            rightPitch += _eyeLookAt.Pitch;
            
            //NOTE: 場合によってはそのまんまの値が出てくる
            var mappedLeftYawPitch = _angleMapApplier.GetLeftMappedValues(leftYaw, leftPitch);
            var mappedRightYawPitch = _angleMapApplier.GetRightMappedValues(rightYaw, rightPitch);

        }
    }
}
