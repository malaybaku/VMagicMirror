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

            var currentCombinedRot = _hasNeck
                ? _neck.localRotation * _head.localRotation
                : _head.localRotation;
            
            //首と頭に半分ずつ回転適用すると具合がいいのでは、という説です
            var halfRot = Quaternion.AngleAxis(rawAngle * angleApplyFactor * 0.5f, rawAxis);
            halfRot.y *= -1;
            halfRot.z *= -1;
            
            //※この計算は実はちょっとヘンです(実際はhalfRot * neck * halfRot * neckを適用するので)が、
            //首の曲げすぎ判定の前後でとる姿勢を連続にしようとするとコレが比較的ラクなので、こうしてます
            var totalRot = halfRot * halfRot * currentCombinedRot;
            totalRot.ToAngleAxis(
                out float totalHeadRotDeg,
                out Vector3 totalHeadRotAxis
            );

            //素朴に値を適用すると首が曲がりすぎる、と判断されたケース
            if (Mathf.Abs(totalHeadRotDeg) > HeadTotalRotationLimitDeg)
            {
                var limitedRotation =
                    Quaternion.Inverse(currentCombinedRot) *
                    Quaternion.AngleAxis(HeadTotalRotationLimitDeg, totalHeadRotAxis);
                limitedRotation.ToAngleAxis(out var limitedAngle, out var limitedAxis);
                halfRot = Quaternion.AngleAxis(limitedAngle * 0.5f, limitedAxis);
            }

            if (_hasNeck)
            {
                _neck.localRotation = halfRot * _neck.localRotation;
                _head.localRotation = halfRot * _head.localRotation;
            }
            else
            {
                _head.localRotation = halfRot * halfRot * _head.localRotation;
            }
        }
    }
}

