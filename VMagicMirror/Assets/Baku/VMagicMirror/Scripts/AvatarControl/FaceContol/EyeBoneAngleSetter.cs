using System.Linq;
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
        [SerializeField] private Vector2[] rates = new Vector2[4];
        [SerializeField] private Vector2 lookAtResult;

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
            _nonImageBasedMotion = nonImageBasedMotion;
            _angleMapApplier = new EyeBoneAngleMapApplier(receiver, vrmLoadable);
            _eyeLookAt = new EyeLookAt(vrmLoadable, ikTargets.LookAt);

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
                eyeDownMotionController,
            };
        }
        
        private void LateUpdate()
        {
            if (!_hasModel || (!_hasLeftEye && !_hasRightEye))
            {
                return;
            }
            
            eyeDownMotionController.UpdateRotationRate();

            var leftRate = Vector2.zero;
            var rightRate = Vector2.zero;

            foreach (var (s, i) in _sources.Select((src, index) => (src, index)))
            {
                if (!s.IsActive)
                {
                    continue;
                }
                leftRate += s.LeftEyeRotationRate;
                rightRate += s.RightEyeRotationRate;
                rates[i] = 0.5f * (s.LeftEyeRotationRate + s.RightEyeRotationRate);
            }

            //TODO: この辺でrateを制限したほうが良さそう？それともlookAtの加算後か？

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
            lookAtResult = new Vector2(_eyeLookAt.Yaw, _eyeLookAt.Pitch);

            //TODO: この辺でrateの制限とスケーリングを何かやりたいんだけどどうですかね
            
            //NOTE: 場合によってはそのまんまの値が出てくる
            (leftYaw, leftPitch) = _angleMapApplier.GetLeftMappedValues(leftYaw, leftPitch);
            (rightYaw, rightPitch) = _angleMapApplier.GetRightMappedValues(rightYaw, rightPitch);

            if (_hasLeftEye)
            {
                _leftEye.localRotation = Quaternion.Euler(leftPitch, leftYaw, 0f);
            }

            if (_hasRightEye)
            {
                _rightEye.localRotation = Quaternion.Euler(rightPitch, rightYaw, 0f);
            }
        }
    }
}
