using UnityEngine;
using UniRx;
using System;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(FaceDetector))]
    public class FaceAttitudeController : MonoBehaviour
    {
        public float speedLerpFactor = 0.2f;
        [Range(0.05f, 1.0f)]
        public float timeScaleFactor = 1.0f;

        private const float NoseBaseHeightDifToAngleDegFactor = 400f;
            
        private FaceDetector _faceDetector;
        private Transform _vrmHeadTransform = null;

        //値の出どころが異なる都合でRadとDegが混在してます
        private float _headRollRad = 0;
        private float _headPitchDeg = 0;
        private float _prevSpeed = 0;

        private Quaternion _prevRotation = Quaternion.identity;

        void Start()
        {
            _faceDetector = GetComponent<FaceDetector>();
            _faceDetector.FaceParts.Outline.FaceOrientationOffset.Subscribe(
                //鏡像姿勢をベースにしたいので反転(この値を適用するとユーザーから鏡に見えるハズ)
                v => _headRollRad = -v
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
                //_prevAngle = 0;
                _prevSpeed = 0;
                _prevRotation = Quaternion.identity;
                return;
            }

            Quaternion goalRotation = Quaternion.Euler(
                _headPitchDeg, 
                0,
                _headRollRad * Mathf.Rad2Deg
                );

            //_vrmHeadTransform.localRotation = goalRotation;

            Quaternion totalDiffRotation = goalRotation * Quaternion.Inverse(_prevRotation);
            totalDiffRotation.ToAngleAxis(out float diffAngle, out Vector3 diffAxis);

            //このへんのスピードはぜんぶ[deg/sec]が単位
            float idealSpeed = diffAngle / timeScaleFactor;
            float speed = Mathf.Lerp(_prevSpeed, idealSpeed, speedLerpFactor);
            Quaternion difRotation = Quaternion.AngleAxis(Time.deltaTime * speed, diffAxis);
            Quaternion rotation = difRotation * _prevRotation;

            _vrmHeadTransform.localRotation = rotation;
            _prevRotation = rotation;
            _prevSpeed = speed;


            #region 1 dof 
            //float idealSpeed = (_headRollRad - _prevAngle) / timeScaleFactor;
            //float speed = Mathf.Lerp(_prevSpeed, idealSpeed, speedLerpFactor);
            //float angle = _prevAngle + Time.deltaTime * speed;

            //_vrmHeadTransform.localRotation *= Quaternion.AngleAxis(
            //    angle * Mathf.Rad2Deg,
            //    Vector3.forward
            //    );

            //_prevAngle = angle;
            //_prevSpeed = speed;
            #endregion
        }

        public void Initialize(Transform transform) => _vrmHeadTransform = transform;

        public void DisposeHead() => _vrmHeadTransform = null;

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


    }
}

