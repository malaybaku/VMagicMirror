using System;
using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(FaceDetector))]
    public class FaceAttitudeController : MonoBehaviour
    {
        private FaceDetector _faceDetector;

        [SerializeField]
        private Transform headRotationReferenceTransform = null;

        public float speedFactor = 0.2f;
        public float rotationUpdateInterval = 0.19f;

        private Transform _vrmHeadTransform = null;

        private float _latestHeadRotationZ = 0f;
        private float _goalHeadRotationZ = 0f;
        private float _easedHeadRotationZ = 0f;
        private float _count = 0;

        void Start()
        {
            _faceDetector = GetComponent<FaceDetector>();

            _faceDetector.FaceParts.Outline.FaceOrientationOffset.Subscribe(
                v => UpdateFaceOrientation(v)
                );
        }

        private void LateUpdate()
        {
            if (_vrmHeadTransform == null)
            {
                return;
            }

            _count -= Time.deltaTime;
            if (_count < 0)
            {
                _count = rotationUpdateInterval;
                _goalHeadRotationZ = _latestHeadRotationZ;
            }

            _easedHeadRotationZ = Mathf.Lerp(_easedHeadRotationZ, _goalHeadRotationZ, speedFactor);

            _vrmHeadTransform.localRotation *= Quaternion.AngleAxis(
                _easedHeadRotationZ * Mathf.Rad2Deg,
                Vector3.forward
                );
        }

        private void UpdateFaceOrientation(float angleRad)
        {
            _latestHeadRotationZ = angleRad;
            //headRotationReferenceTransform.localRotation = Quaternion.AngleAxis(
            //    angleRad * Mathf.Rad2Deg,
            //    Vector3.forward
            //    );
        }

        internal void DisposeHead()
        {
            _vrmHeadTransform = null;
        }

        internal void Initialize(Transform transform)
        {
            _vrmHeadTransform = transform;
        }
    }
}

