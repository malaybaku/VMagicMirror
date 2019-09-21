﻿using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(FaceDetector))]
    public class FaceAttitudeController : MonoBehaviour
    {
        [SerializeField]
        private float speedLerpFactor = 0.2f;

        [SerializeField]
        [Range(0.1f, 1.0f)]
        private float speedDumpFactor = 0.8f;

        [SerializeField]
        [Range(0.05f, 1.0f)]
        private float timeScaleFactor = 1.0f;        

        private const float HeadYawRateToDegFactor = 50.00f;
        private const float HeadTotalRotationLimitDeg = 40.0f;
        private const float NoseBaseHeightDifToAngleDegFactor = 400f;
            
        private FaceDetector _faceDetector;
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
            _faceDetector = GetComponent<FaceDetector>();
            
            //鏡像姿勢をベースにしたいので反転(この値を適用するとユーザーから鏡に見えるハズ)
            _faceDetector.FaceParts.Outline.HeadRollRad.Subscribe(
                v => SetHeadRollDeg(-v * Mathf.Rad2Deg)
                );
            
            //もとの値は角度ではなく[-1, 1]の無次元量であることに注意
            _faceDetector.FaceParts.Outline.HeadYawRate.Subscribe(
                v => SetHeadYawDeg(v * HeadYawRateToDegFactor)
                );

            _faceDetector.FaceParts.Nose.NoseBaseHeightValue.Subscribe(
                v => SetHeadPitchDeg(NoseBaseHeightToNeckPitchDeg(v))
                );
        }

        private void LateUpdate()
        {
            if (_vrmHeadTransform == null || !_faceDetector.HasInitDone)
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
                speedLerpFactor
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

            _prevRotationEuler = rotationEuler;
            _prevRotationSpeedEuler = speed;
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

        private float NoseBaseHeightToNeckPitchDeg(float noseBaseHeight)
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

