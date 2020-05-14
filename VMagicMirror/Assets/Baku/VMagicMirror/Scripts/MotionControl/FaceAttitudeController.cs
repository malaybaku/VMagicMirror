using UnityEngine;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    public class FaceAttitudeController : MonoBehaviour
    {
        [SerializeField] private float speedLerpFactor = 12f;

        [SerializeField]
        [Range(0.1f, 1.0f)]
        private float speedDumpFactor = 0.8f;

        [SerializeField]
        [Range(0.05f, 1.0f)]
        private float timeScaleFactor = 1.0f;

        [Tooltip("首ロールの一部を体ロールにすり替える比率です")]
        [SerializeField] private float rollToBodyRollFactor = 0.1f;

        [Tooltip("首ヨーの一部を体のヨーにすり替える比率です")]
        [SerializeField] private float yawToBodyYawFactor = 0.1f;

        [Inject] private FaceTracker _faceTracker = null;
        [Inject] private IVRMLoadable _vrmLoadable = null;
        
        public Quaternion BodyLeanSuggest { get; private set; } = Quaternion.identity;

        //体の回転に反映するとかの都合で首ロールを実際に検出した値より控えめに適用しますよ、というファクター
        private const float HeadRollRateApplyFactor = 0.7f;
        //NOTE: もとは50だったんだけど、腰曲げに反映する値があることを想定して小さくしてます
        private const float HeadYawRateToDegFactor = 28.00f; 

        private const float HeadTotalRotationLimitDeg = 40.0f;
        private const float NoseBaseHeightDifToAngleDegFactor = 400f;
            
        private Transform _vrmNeckTransform = null;
        private Transform _vrmHeadTransform = null;

        private void SetHeadRollDeg(float value) => _latestRotationEuler.z = value;
        private void SetHeadYawDeg(float value) => _latestRotationEuler.y = value;
        private void SetHeadPitchDeg(float value) => _latestRotationEuler.x = value;

        //NOTE: Quaternionを使わないのは角度別にローパスっぽい処理するのに都合がよいため
        private Vector3 _latestRotationEuler;
        private Vector3 _prevRotationEuler;
        private Vector3 _prevRotationSpeedEuler;

        private void Start()
        {
            //鏡像姿勢をベースにしたいので反転(この値を適用するとユーザーから鏡に見えるハズ)
            _faceTracker.FaceParts.Outline.HeadRollRad.Subscribe(
                v => SetHeadRollDeg(-v * Mathf.Rad2Deg * HeadRollRateApplyFactor)
                );
            
            //もとの値は角度ではなく[-1, 1]の無次元量であることに注意
            _faceTracker.FaceParts.Outline.HeadYawRate.Subscribe(
                v => SetHeadYawDeg(v * HeadYawRateToDegFactor)
                );

            _faceTracker.FaceParts.Nose.NoseBaseHeightValue.Subscribe(
                v => SetHeadPitchDeg(NoseBaseHeightToNeckPitchDeg(v))
                );
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmDisposing;
        }

        private void LateUpdate()
        {
            if (_vrmHeadTransform == null || !_faceTracker.HasInitDone)
            {
                _latestRotationEuler = Vector3.zero;
                _prevRotationEuler = Vector3.zero;
                _prevRotationSpeedEuler = Vector3.zero;
                return;
            }

            //やりたい事: ロール、ヨー、ピッチそれぞれを独立にsmoothingしてから最終的に適用する

            //直線的に動かす場合の速度。ここが差分ベースで、PD制御のPっぽい感じ
            var idealSpeedEuler = (_latestRotationEuler - _prevRotationEuler) / timeScaleFactor;

            //慣性っぽい動きを付けてからチャタリング防止用のダンピング(PD制御のDっぽい項)
            var speed = Vector3.Lerp(
                _prevRotationSpeedEuler,
                idealSpeedEuler,
                speedLerpFactor * Time.deltaTime
                );

            //チャタリング防止
            speed *= speedDumpFactor;

            var rotationEuler = _prevRotationEuler + speed * Time.deltaTime;

            //このスクリプトより先にLookAtIKが走るハズなので、その回転と合成
            var nextRotation = Quaternion.Euler(rotationEuler) * _vrmHeadTransform.localRotation;

            //首と頭のトータルで曲がり過ぎを防止
            (_vrmNeckTransform.localRotation * nextRotation).ToAngleAxis(
                out float totalHeadRotDeg,
                out Vector3 totalHeadRotAxis
                );

            if (Mathf.Abs(totalHeadRotDeg) > HeadTotalRotationLimitDeg)
            {
                nextRotation =
                    Quaternion.Inverse(_vrmNeckTransform.localRotation) *
                    Quaternion.AngleAxis(HeadTotalRotationLimitDeg, totalHeadRotAxis);
            }

            _vrmHeadTransform.localRotation = nextRotation;
            BodyLeanSuggest = Quaternion.Euler(0, 0, rotationEuler.z * rollToBodyRollFactor);

            _prevRotationEuler = rotationEuler;
            _prevRotationSpeedEuler = speed;
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            var animator = info.animator;
            _vrmNeckTransform = animator.GetBoneTransform(HumanBodyBones.Neck);
            _vrmHeadTransform = animator.GetBoneTransform(HumanBodyBones.Head);
        }

        private void OnVrmDisposing()
        {
            _vrmNeckTransform = null;
            _vrmHeadTransform = null;
        }
        
        private float NoseBaseHeightToNeckPitchDeg(float noseBaseHeight)
        {
            if (_faceTracker != null)
            {
                return -(noseBaseHeight - _faceTracker.CalibrationData.noseHeight) * NoseBaseHeightDifToAngleDegFactor;
            }
            else
            {
                //とりあえず顔が取れないなら水平にしとけばOK
                return 0;
            }
        }
    }
}

