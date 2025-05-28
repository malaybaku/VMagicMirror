using System;
using System.Linq;
using Baku.VMagicMirror.IK;
using Baku.VMagicMirror.MediaPipeTracker;
using UnityEngine;
using Zenject;
using UniRx;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 全身の位置をずらす処理をやるクラス
    /// </summary>
    public class BodyMotionManager : MonoBehaviour
    {
        [SerializeField] private BodyLeanIntegrator bodyLeanIntegrator = null;
        [SerializeField] private ImageBasedBodyMotion imageBasedBodyMotion = null;
        [SerializeField] private ExternalTrackerBodyOffset exTrackerBodyMotion = null;
        [SerializeField] private MediaPipeTrackerBodyOffset mediaPipeBodyMotion = null;
        [SerializeField] private VMCPBodyOffset vmcpBodyOffset = null;
        [SerializeField] private WaitingBodyMotion waitingBodyMotion = null;

        [Tooltip("カメラに写った状態で体が横に並進する量の最大値")]
        [SerializeField] private float yMaxLength = 0.2f;
        [Tooltip("カメラに写った状態で体がタテに並進する量の最大値")]
        [SerializeField] private float xMaxLength = 0.2f;
        [Tooltip("体が最大まで横に並進したときのyawへの寄与(deg)")]
        [SerializeField] private float xToYawFactor = 20f;
        [Tooltip("体が最大まで横に並進したときのrollへの寄与(deg)")]
        [SerializeField] private float xToRollFactor = 2f;
        [Tooltip("体が最大までタテに並進したときのpitchへの寄与(deg)、下げるときに効いて上がるときは効かない")]
        [SerializeField] private float yToPitchFactor = 10f;

        public WaitingBodyMotion WaitingBodyMotion => waitingBodyMotion;

        private FaceControlConfiguration _faceControlConfig;

        private Transform _bodyIk = null;

        private Transform _vrmRoot = null;
        private Vector3 _defaultBodyIkPosition;
        private bool _isVrmLoaded = false;

        private bool _isGameInputMode;
        private Transform[] _upperBodyBones = Array.Empty<Transform>();

        [Inject]
        public void Initialize(
            IVRMLoadable vrmLoadable, 
            IMessageReceiver receiver, 
            IKTargetTransforms ikTargets,
            FaceControlConfiguration faceControlConfig,
            BodyMotionModeController modeController
            )
        {
            _bodyIk = ikTargets.Body;
            _faceControlConfig = faceControlConfig;
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmDisposing;
            var _ = new BodyMotionManagerReceiver(receiver, this);

            modeController.MotionMode
                .Subscribe(mode =>
                {
                    SetNoHandTrackMode(mode != BodyMotionMode.Default);
                    _isGameInputMode = mode == BodyMotionMode.GameInputLocomotion;
                })
                .AddTo(this);
        }
        
        private void Update()
        {
            if (!_isVrmLoaded || _isGameInputMode)
            {
                return;
            }

            var imageRelatedOffset = _faceControlConfig.ControlMode switch
            {
                FaceControlModes.WebCamHighPower => mediaPipeBodyMotion.BodyOffset,
                FaceControlModes.VMCProtocol => vmcpBodyOffset.BodyOffset,
                FaceControlModes.ExternalTracker => exTrackerBodyMotion.BodyOffset,
                _ => imageBasedBodyMotion.BodyIkXyOffset,
            };

            _bodyIk.localPosition =
                _defaultBodyIkPosition + 
                imageRelatedOffset +
                bodyLeanIntegrator.BodyOffsetSuggest + 　
                waitingBodyMotion.Offset;

            //画像ベースの移動量はIKと体に利かす -> 体に移動量を足さないと腰だけ動いて見た目が怖くなります
            _vrmRoot.position = imageRelatedOffset;
            //スムージングはサブクラスの方でやっているのでコッチでは処理不要。
            //第1項は並進要素を腰回転にきかせて違和感をへらすためのやつです
            _vrmRoot.localRotation = BodyOffsetToBodyAngle(imageRelatedOffset) * bodyLeanIntegrator.BodyLeanSuggest;
        }

        private void LateUpdate()
        {
            if (!_isVrmLoaded || !_isGameInputMode)
            {
                return;
            }

            var imageRelatedOffset = _faceControlConfig.ControlMode switch
            {
                FaceControlModes.ExternalTracker => exTrackerBodyMotion.BodyOffset,
                FaceControlModes.WebCamHighPower => mediaPipeBodyMotion.BodyOffset,
                _ => imageBasedBodyMotion.BodyIkXyOffset,
            };

            //通常時と同じ角度を参照するが、適用方法が異なり、直接ホネに値が入る
            ApplyRotationsToUpperBody(
                BodyOffsetToBodyAngle(imageRelatedOffset) * bodyLeanIntegrator.BodyLeanSuggest
            );
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            //NOTE: VRMLoadControllerがロード時点でbodyIkの位置をキャラのHipsあたりに調整しているので、それを貰う
            _defaultBodyIkPosition = _bodyIk.position;
            imageBasedBodyMotion.OnVrmLoaded(info);
            _vrmRoot = info.vrmRoot;

            _upperBodyBones = new[]
                {
                    info.animator.GetBoneTransform(HumanBodyBones.Spine),
                    info.animator.GetBoneTransform(HumanBodyBones.Chest),
                    info.animator.GetBoneTransform(HumanBodyBones.UpperChest),
                }
                .Where(t => t != null)
                .ToArray();

            _isVrmLoaded = true;
        }

        private void OnVrmDisposing()
        {
            _isVrmLoaded = false;

            _vrmRoot = null;
            imageBasedBodyMotion.OnVrmDisposing();
            _upperBodyBones = Array.Empty<Transform>();
        }
        
        private Quaternion BodyOffsetToBodyAngle(Vector3 offset)
        {
            var yaw = xToYawFactor * Mathf.Clamp(offset.x / xMaxLength, -1f, 1f);
            var roll = xToRollFactor * Mathf.Clamp(-offset.x / xMaxLength, -1f, 1f);
            var pitch = 0f;
            //下がるときだけ角度がつくことに注意
            if (offset.y < 0)
            {
                var yFactor = Mathf.Clamp01(-offset.y / yMaxLength);
                pitch = yToPitchFactor * yFactor * yFactor;
            }
            
            return Quaternion.Euler(pitch, yaw, roll);
        }

        private void SetNoHandTrackMode(bool enable)
        {
            imageBasedBodyMotion.NoHandTrackMode = enable;
            exTrackerBodyMotion.NoHandTrackMode.Value = enable;
            mediaPipeBodyMotion.NoHandTrackMode = enable;
        }

        private void ApplyRotationsToUpperBody(Quaternion localRot)
        {
            //通常ありえない
            if (_upperBodyBones.Length == 0)
            {
                return;
            }

            if (_upperBodyBones.Length == 1)
            {
                _upperBodyBones[0].localRotation *= localRot;
                return;
            }

            var factor = 1f / _upperBodyBones.Length;
            
            localRot.ToAngleAxis(out var angle, out var axis);
            var dividedRot = Quaternion.AngleAxis(angle * factor, axis);

            foreach (var t in _upperBodyBones)
            {
                t.localRotation *= dividedRot;
            }
        }
    }
}
