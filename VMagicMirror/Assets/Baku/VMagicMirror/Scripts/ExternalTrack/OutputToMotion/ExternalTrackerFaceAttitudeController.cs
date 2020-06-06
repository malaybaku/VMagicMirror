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
        private const float HeadTotalRotationLimitDeg = 50f;

        [Tooltip("首ロールの一部を体ロールにすり替える比率です")]
        [SerializeField] private float rollToBodyRollFactor = 0.1f;
        
        public bool IsActive { get; set; } = true;
        public Quaternion BodyLeanSuggest { get; private set; } = Quaternion.identity;

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
            };
            vrmLoadable.VrmDisposing += () =>
            {
                _neck = null;
                _head = null;
            };
        }

        private void LateUpdate()
        {
            if (_head == null || !IsActive)
            {
                //NOTE: 画像ベースの方みたくフィルタリングをする場合、ここのガード時にフィルタの値をリセットしましょう
                return;
            }

            //このスクリプトより先にLookAtIKが走るハズなので、その回転と合成すればOK
            //ここでは顔トラッキングを完全に信じるイケない実装を行っております…
            var nextRotation = _externalTracker.HeadRotation * _head.localRotation;

            //首と頭のトータルで曲がり過ぎを防止
            (_neck.localRotation * nextRotation).ToAngleAxis(
                out float totalHeadRotDeg,
                out Vector3 totalHeadRotAxis
                );

            if (Mathf.Abs(totalHeadRotDeg) > HeadTotalRotationLimitDeg)
            {
                nextRotation =
                    Quaternion.Inverse(_neck.localRotation) *
                    Quaternion.AngleAxis(HeadTotalRotationLimitDeg, totalHeadRotAxis);
            }

            _head.localRotation = nextRotation;
            
            nextRotation.ToAngleAxis(out float resultAngle, out var resultAxis);
            //NOTE: ややいい加減な計算のハズですよコレ。
            float roll = resultAngle * Vector3.Dot(resultAxis, Vector3.forward);
            
            BodyLeanSuggest = Quaternion.Euler(0, 0, roll * rollToBodyRollFactor);
        }

    }
}

