using UnityEngine;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    public class FaceAttitudeController : MonoBehaviour
    {
        //NOTE: バネマス系のパラメータ(いわゆるcとk)
        [SerializeField] private float speedDumpForceFactor = 10.0f;
        [SerializeField] private float posDumpForceFactor = 20.0f;
        
        [Tooltip("首ロールの一部を体ロールにすり替える比率です")]
        [SerializeField] private float rollToBodyRollFactor = 0.1f;

        private FaceTracker _faceTracker = null;
        
        [Inject]
        public void Initialize(FaceTracker faceTracker, IVRMLoadable vrmLoadable)
        {
            _faceTracker = faceTracker;
            
            //鏡像姿勢をベースにしたいので反転(この値を適用するとユーザーから鏡に見えるハズ)
            faceTracker.FaceParts.Outline.HeadRollRad.Subscribe(
                v => SetHeadRollDeg(-v * Mathf.Rad2Deg * HeadRollRateApplyFactor)
            );
            
            //もとの値は角度ではなく[-1, 1]の無次元量であることに注意
            faceTracker.FaceParts.Outline.HeadYawRate.Subscribe(
                v => SetHeadYawDeg(v * HeadYawRateToDegFactor)
            );

            faceTracker.FaceParts.Nose.NoseBaseHeightValue.Subscribe(
                v => SetHeadPitchDeg(NoseBaseHeightToNeckPitchDeg(v))
            );
            
            vrmLoadable.VrmLoaded += info =>
            {
                var animator = info.animator;
                _vrmNeckTransform = animator.GetBoneTransform(HumanBodyBones.Neck);
                _vrmHeadTransform = animator.GetBoneTransform(HumanBodyBones.Head);
            };
            
            vrmLoadable.VrmDisposing += () =>
            {
                _vrmNeckTransform = null;
                _vrmHeadTransform = null;
            };
        }
        
        public Quaternion BodyLeanSuggest { get; private set; } = Quaternion.identity;

        //体の回転に反映するとかの都合で首ロールを実際に検出した値より控えめに適用しますよ、というファクター
        private const float HeadRollRateApplyFactor = 0.8f;
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

        public bool IsActive { get; set; } = true;

        private void LateUpdate()
        {
            if (_vrmHeadTransform == null || !_faceTracker.HasInitDone || !IsActive)
            {
                _latestRotationEuler = Vector3.zero;
                _prevRotationEuler = Vector3.zero;
                _prevRotationSpeedEuler = Vector3.zero;
                return;
            }

            //やりたい事: ロール、ヨー、ピッチそれぞれを独立にsmoothingしてから最終的に適用する
            
            // いわゆるバネマス系扱いで陽的オイラー法を回す。やや過減衰方向に寄せてるので雑にやっても大丈夫(のはず)
            var accel = 
                -_prevRotationSpeedEuler * speedDumpForceFactor -
                (_prevRotationEuler - _latestRotationEuler) * posDumpForceFactor;
            var speed = _prevRotationSpeedEuler + Time.deltaTime * accel;
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

