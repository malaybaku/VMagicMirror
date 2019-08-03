using UnityEngine;
using UniRx;
using System;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(FaceDetector))]
    public class FaceAttitudeController : MonoBehaviour
    {
        [SerializeField]
        private float speedLerpFactor = 0.2f;

        [SerializeField]
        [Range(0.05f, 1.0f)]
        private float timeScaleFactor = 1.0f;        

        private const float HeadYawRateToDegFactor = 40.0f;
        private const float HeadTotalYawLimitDeg = 40.0f;

        private const float NoseBaseHeightDifToAngleDegFactor = 400f;
            
        private FaceDetector _faceDetector;
        private Transform _vrmNeckTransform = null;
        private Transform _vrmHeadTransform = null;

        //値の出どころが異なる都合でRadと比率値とDegが混在してる
        private float _headRollRad = 0;
        private float _headYawRate = 0;
        private float _headPitchDeg = 0;


        private float _prevSpeed = 0;

        private Quaternion _goalRotation = Quaternion.identity;
        private Quaternion _prevRotation = Quaternion.identity;

        void Start()
        {
            _faceDetector = GetComponent<FaceDetector>();
            _faceDetector.FaceParts.Outline.HeadRollRad.Subscribe(
                //鏡像姿勢をベースにしたいので反転(この値を適用するとユーザーから鏡に見えるハズ)
                v => _headRollRad = -v
                );
            _faceDetector.FaceParts.Outline.HeadYawRate.Subscribe(
                v => _headYawRate = v
                );
            _faceDetector.FaceParts.Nose.NoseBaseHeightValue.Subscribe(
                v => _headPitchDeg = NoseBaseHeightToNeckPitch(v)
                );

        }

        private void LateUpdate()
        {
            if (_vrmHeadTransform == null)
            {
                _headRollRad = 0;
                _headYawRate = 0;
                //_prevAngle = 0;
                _prevSpeed = 0;
                _prevRotation = Quaternion.identity;
                return;
            }

            var latestGoalRotation = Quaternion.Euler(
                _headPitchDeg, 
                HeadYawRateToDeg(),
                _headRollRad * Mathf.Rad2Deg
                );

            _goalRotation = latestGoalRotation;

            Quaternion totalDiffRotation = _goalRotation * Quaternion.Inverse(_prevRotation);
            totalDiffRotation.ToAngleAxis(out float diffAngle, out Vector3 diffAxis);

            //このへんのスピードはぜんぶ[deg/sec]が単位
            float idealSpeed = diffAngle / timeScaleFactor;
            float speed = Mathf.Lerp(_prevSpeed, idealSpeed, speedLerpFactor);
            Quaternion difRotation = Quaternion.AngleAxis(Time.deltaTime * speed, diffAxis);
            Quaternion rotation = difRotation * _prevRotation;

            _vrmHeadTransform.localRotation = rotation;
            _prevRotation = rotation;
            _prevSpeed = speed;

            Debug.Log($"Prev Speed: {_prevSpeed:00.00}");
        }

        public void Initialize(Transform neckTransform, Transform headTransform)
        {
            _vrmNeckTransform = neckTransform;
            _vrmHeadTransform = headTransform;
        }

        public void DisposeHead()
        {
            _vrmNeckTransform = null;
            _vrmHeadTransform = null;
        }

        private float NoseBaseHeightToNeckPitch(float noseBaseHeight)
        {
            if (_faceDetector != null)
            {
                return -(noseBaseHeight - _faceDetector.CalibrationData.noseHeight) * NoseBaseHeightDifToAngleDegFactor;
            }
            else
            {
                //とりあえず顔が取れないなら水平にしとけばOK
                return 0;
            }
        }

        private float HeadYawRateToDeg()
        {
            float result = _headYawRate * HeadYawRateToDegFactor;
            //考え方: 方向によらず、LookAtで首がじゅうぶん曲がっている場合、それ以上ムリして角度をつけないようにする
            _vrmNeckTransform.localRotation.ToAngleAxis(out float neckBendingAngleDeg, out var _);

            neckBendingAngleDeg = Mathf.Repeat(neckBendingAngleDeg, 360);
            if (neckBendingAngleDeg > 180)
            {
                neckBendingAngleDeg = 360f - neckBendingAngleDeg;
            }

            return Mathf.Clamp(
                result,
                -HeadTotalYawLimitDeg - neckBendingAngleDeg,
                HeadTotalYawLimitDeg + neckBendingAngleDeg
                );
        }
    }
}

