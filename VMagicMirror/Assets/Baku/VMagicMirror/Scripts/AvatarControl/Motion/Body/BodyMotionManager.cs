using Baku.VMagicMirror.IK;
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

            var imageRelatedOffset = _faceControlConfig.ControlMode == FaceControlModes.ExternalTracker
                ? exTrackerBodyMotion.BodyOffset
                : imageBasedBodyMotion.BodyIkXyOffset;
            
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
        
        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            //NOTE: VRMLoadControllerがロード時点でbodyIkの位置をキャラのHipsあたりに調整しているので、それを貰う
            _defaultBodyIkPosition = _bodyIk.position;
            imageBasedBodyMotion.OnVrmLoaded(info);
            _vrmRoot = info.vrmRoot;

            _isVrmLoaded = true;
        }

        private void OnVrmDisposing()
        {
            _isVrmLoaded = false;

            _vrmRoot = null;
            imageBasedBodyMotion.OnVrmDisposing();
        }
        
        private Quaternion BodyOffsetToBodyAngle(Vector3 offset)
        {
            float yaw = xToYawFactor * Mathf.Clamp(offset.x / xMaxLength, -1f, 1f);
            float roll = xToRollFactor * Mathf.Clamp(-offset.x / xMaxLength, -1f, 1f);
            float pitch = 0f;
            //下がるときだけ角度がつくことに注意
            if (offset.y < 0)
            {
                float yFactor = Mathf.Clamp01(-offset.y / yMaxLength);
                pitch = yToPitchFactor * yFactor * yFactor;
            }
            
            return Quaternion.Euler(pitch, yaw, roll);
        }

        public void EnableImageBaseBodyLeanZ(bool enable)
            => imageBasedBodyMotion.EnableBodyLeanZ = enable;

        public void SetNoHandTrackMode(bool enable)
        {
            imageBasedBodyMotion.NoHandTrackMode = enable;
            exTrackerBodyMotion.NoHandTrackMode = enable;
        }
    }
}
