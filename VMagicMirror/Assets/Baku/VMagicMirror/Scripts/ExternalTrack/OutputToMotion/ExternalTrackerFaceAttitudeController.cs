using UnityEngine;
using Zenject;
using Baku.VMagicMirror.ExternalTracker;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 外部トラッキングベースで<see cref="FaceAttitudeController"/>と同等の処理を提供します。
    /// どっちか片方だけがEnableされているのを期待しています
    /// </summary>
    public class ExternalTrackerFaceAttitudeController : MonoBehaviour
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
        
        public bool IsActive { get; set; } = true;

        private bool _hasModel = false;
        private bool _hasNeck = false;
        private Transform _neck = null;
        private Transform _head = null;
        private ExternalTrackerDataSource _externalTracker = null;
        
        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable, ExternalTrackerDataSource externalTracker)
        {
            _externalTracker = externalTracker;

            vrmLoadable.VrmLoaded += info =>
            {
                var animator = info.animator;
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
                //NOTE: 画像ベースの方みたくフィルタリングをする場合、ここのガード時にフィルタの値をリセットするが、今は不要
                return;
            }
            
            _externalTracker.HeadRotation.ToAngleAxis(out float rawAngle, out var rawAxis);
            
            //角度を0側に寄せる: 動きが激しすぎるとアレなので
            var rot = Quaternion.AngleAxis(rawAngle * angleApplyFactor, rawAxis);
            
            //ピッチだけ追加で絞る
            var pitchCheck = rot * Vector3.forward;
            var pitchAngle = Mathf.Asin(-pitchCheck.y) * Mathf.Rad2Deg;

            var reducedPitch = Mathf.Clamp(pitchAngle * pitchFactor, -pitchLimitDeg, pitchLimitDeg);
            var pitchResetRot = Quaternion.AngleAxis(
                reducedPitch - pitchAngle,
                Vector3.right
                );
            rot *= pitchResetRot;

            //鏡像反転
            rot.y *= -1;
            rot.z *= -1;

            //曲がりすぎを防止しつつ、首と頭に回転を割り振る(首が無いモデルなら頭に全振り)
            var totalRot = _hasNeck
                ? rot * _neck.localRotation * _head.localRotation
                : rot * _head.localRotation;
            
            totalRot.ToAngleAxis(
                out float totalHeadRotDeg,
                out Vector3 totalHeadRotAxis
            );
            
            //素朴に値を適用すると首が曲がりすぎる、と判断されたケース
            if (Mathf.Abs(totalHeadRotDeg) > HeadTotalRotationLimitDeg)
            {
                totalHeadRotDeg = Mathf.Sign(totalHeadRotDeg) * HeadTotalRotationLimitDeg;
            }

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

