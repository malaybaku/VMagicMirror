using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    /// <summary>
    /// 外部トラッキングベースで<see cref="FaceAttitudeController"/>と同等の処理を提供します。
    /// どっちか片方だけがEnableされているのを期待しています
    /// </summary>
    public class MediaPipeFaceAttitudeController : MonoBehaviour
    {
        private const float HeadTotalRotationLimitDeg = 40f;

        [Tooltip("胴体回転にあてる分とか、見栄えを考慮して回転の適用率を1より小さくするファクター")]
        [Range(0f, 1f)]
        [SerializeField] private float angleApplyFactor = 0.8f;

        [Range(0f, 1f)]
        [SerializeField] private float pitchFactor = 0.8f;
        
        [Tooltip("ピッチにのみ別途適用する角度制限")]
        [Range(0f, 40f)]
        [SerializeField] private float pitchLimitDeg = 25f;

        [Tooltip("ちょっとだけ直前フレームとの補間でならすファクター")]
        [SerializeField] private float lerpFactor = 18.0f;
        
        [Tooltip("トラッキングロスト時に姿勢を戻す速度のファクター。通常時よりかなり遅めに設定する")]
        [SerializeField] private float lerpFactorOnLost = 3f;

        public bool IsActive { get; set; } = true;

        private bool _hasModel = false;
        private bool _hasNeck = false;
        private Transform _neck = null;
        private Transform _head = null;
        private MediaPipeKinematicSetter _mediaPipeKinematicSetter;
        //private ExternalTrackerDataSource _externalTracker;
        private GameInputBodyMotionController _gameInputBodyMotionController;
        private CarHandleBasedFK _carHandleBasedFk;
        private Quaternion _prevRotation = Quaternion.identity;
        
        [Inject]
        public void Initialize(
            IVRMLoadable vrmLoadable, 
            GameInputBodyMotionController gameInputBodyMotionController,
            CarHandleBasedFK carHandleBasedFk,
            MediaPipeKinematicSetter mediaPipeKinematicSetter
            //ExternalTrackerDataSource externalTracker
            )
        {
            _mediaPipeKinematicSetter = mediaPipeKinematicSetter;
            //_externalTracker = externalTracker;
            _carHandleBasedFk = carHandleBasedFk;
            _gameInputBodyMotionController = gameInputBodyMotionController;

            vrmLoadable.VrmLoaded += info =>
            {
                var animator = info.controlRig;
                _neck = animator.GetBoneTransform(HumanBodyBones.Neck);
                _head = animator.GetBoneTransform(HumanBodyBones.Head);
                _hasNeck = _neck != null;
                _hasModel = true;
            };
            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _hasNeck = false;
                _neck = null;
                _head = null;
            };
        }

        private void LateUpdate()
        {
            if (!_hasModel || !IsActive)
            {
                _prevRotation = Quaternion.identity;
                return;
            }

            // NOTE: トラッキングしていない場合は正面向きに戻すのでこういう書き方
            var rawAngle = 0f;
            var rawAxis = Vector3.forward;

            // NOTE: この判定により、トラッキングロストしてから戻るまでに少しディレイがつく
            var isTracked =
                _mediaPipeKinematicSetter.TryGetHeadPose(out var headPose) ||
                !_mediaPipeKinematicSetter.IsHeadPoseLostEnoughLong();
            if (isTracked)
            {
                headPose.rotation.ToAngleAxis(out rawAngle, out rawAxis);
            }

            rawAngle = Mathf.Repeat(rawAngle + 180f, 360f) - 180f;
            
            // NOTE: ExTrackerの実装だとここでgameInputやcarHandleの回転を左右反転するが、MediaPipe版では幾何的に正しく計算してるので不要
            //角度を0側に寄せる: 動きが激しすぎるとアレなので
            var gameInputRot = _gameInputBodyMotionController.LookAroundRotation;
            var carHandleRot = _carHandleBasedFk.GetHeadYawRotation();
            
            var rot = 
                gameInputRot *
                carHandleRot * 
                Quaternion.AngleAxis(rawAngle * angleApplyFactor, rawAxis);
            
            //ピッチだけ追加で絞る
            var pitchCheck = rot * Vector3.forward;
            var pitchAngle = Mathf.Asin(-pitchCheck.y) * Mathf.Rad2Deg;

            var reducedPitch = Mathf.Clamp(pitchAngle * pitchFactor, -pitchLimitDeg, pitchLimitDeg);
            var pitchResetRot = Quaternion.AngleAxis(
                reducedPitch - pitchAngle,
                Vector3.right
                );
            rot *= pitchResetRot;

            //もう一度角度をチェックし、合計がデカすぎたら絞る
            rot.ToAngleAxis(out float totalDeg, out var totalAxis);
            totalDeg = Mathf.Repeat(totalDeg + 180f, 360f) - 180f;
            totalDeg = Mathf.Clamp(totalDeg, -HeadTotalRotationLimitDeg, HeadTotalRotationLimitDeg);
            rot = Quaternion.AngleAxis(totalDeg, totalAxis);

            var t = (isTracked ? lerpFactor : lerpFactorOnLost) * Time.deltaTime;
            rot = Quaternion.Slerp(_prevRotation, rot, t);
            _prevRotation = rot;
            
            //曲がりすぎを防止しつつ、首と頭に回転を割り振る(首が無いモデルなら頭に全振り)
            var totalRot = _hasNeck
                ? rot * _neck.localRotation * _head.localRotation
                : rot * _head.localRotation;
            
            totalRot.ToAngleAxis(
                out float totalHeadRotDeg,
                out Vector3 totalHeadRotAxis
            );

            totalHeadRotDeg = Mathf.Repeat(totalHeadRotDeg + 180f, 360f) - 180f;
            //合計値ベースであらためて制限
            totalHeadRotDeg = Mathf.Clamp(totalHeadRotDeg, -HeadTotalRotationLimitDeg, HeadTotalRotationLimitDeg);

            if (_hasNeck)
            {
                var halfRot = Quaternion.AngleAxis(totalHeadRotDeg * 0.5f, totalHeadRotAxis);
                _neck.localRotation = halfRot;
                _head.localRotation = halfRot;
            }
            else
            {
                _head.localRotation = Quaternion.AngleAxis(totalHeadRotDeg, totalHeadRotAxis);
            }
        }
    }
}

