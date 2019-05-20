using RootMotion.FinalIK;
using System.Collections;
using UnityEngine;

namespace Baku.VMagicMirror
{
    //Z方向の制御はVRM側で、肩に対してかける点に注意
    public class FaceBasedBodyIKAdjuster : MonoBehaviour
    {
        //カメラ領域のうち標準では(320x240のうち)30% x 30%の領域くらいが顔の標準の映り込みかなー、という意味。要調整。
        //const float BaseFaceSizeFactor = 0.1f;

        public Vector3 offsetAmplifier = new Vector3(
            0.3f,
            0.3f,
            1.0f
            );

        [SerializeField]
        private Vector3 offsetLowerLimit = new Vector3(
            -1.0f,
            -1.0f,
            -0.05f
            );

        [SerializeField]
        private Vector3 offsetUpperLimit = new Vector3(
            1.0f,
            1.0f,
            0.1f
            );

        //[SerializeField]
        //private float shoulderZOffsetLimit = 0.2f;

        [SerializeField]
        private FaceDetector faceDetector = null;

        [SerializeField]
        private float speedLerpFactor = 0.2f;
        [SerializeField]
        [Range(0.05f, 1.0f)]
        private float timeScaleFactor = 0.3f;

        private bool applyForwardLength = false;
        private Transform _vrmRoot = null;
        private Transform _leftShoulderEffector = null;
        private Transform _rightShoulderEffector = null;
        private Vector3 _leftShoulderDefaultOffset = Vector3.zero;
        private Vector3 _rightShoulderDefaultOffset = Vector3.zero;

        private Vector3 _prevPosition;
        private Vector3 _prevSpeed;

        private void LateUpdate()
        {
            if (faceDetector == null ||
                faceDetector.DetectedRect.width < Mathf.Epsilon ||
                faceDetector.DetectedRect.height < Mathf.Epsilon
                )
            {
                return;
            }

            float forwardLength = 0.0f;
            if (applyForwardLength)
            {
                float faceSize = faceDetector.DetectedRect.width * faceDetector.DetectedRect.height;
                float faceSizeFactor = Mathf.Sqrt(faceSize / faceDetector.CalibrationData.faceSize);
                //とりあえず簡単に。値域はもっと決めようあるよねここは。
                forwardLength = Mathf.Clamp(
                    (faceSizeFactor - 1.0f) * offsetAmplifier.z,
                    offsetLowerLimit.z,
                    offsetUpperLimit.z
                    );
            }

            var center = faceDetector.DetectedRect.center - faceDetector.CalibrationData.faceCenter;
            var idealPosition =
                transform.right * center.x * offsetAmplifier.x +
                transform.up * center.y * offsetAmplifier.y +
                transform.forward * forwardLength;

            Vector3 idealSpeed = (idealPosition - _prevPosition) / timeScaleFactor;
            Vector3 speed = Vector3.Lerp(_prevSpeed, idealSpeed, speedLerpFactor);
            Vector3 pos = _prevPosition + Time.deltaTime * speed;

            transform.localPosition = new Vector3(pos.x, pos.y, 0);
            if (_leftShoulderEffector != null && _rightShoulderEffector != null)
            {
                _leftShoulderEffector.localPosition =
                    _leftShoulderDefaultOffset +
                    _vrmRoot.forward * pos.z;

                _rightShoulderEffector.localPosition = 
                    _rightShoulderDefaultOffset +
                    _vrmRoot.forward * pos.z;
            }

            _prevPosition = pos;
            _prevSpeed = speed;
        }

        public void Initialize(FaceDetector faceDetector, Animator animator, FullBodyBipedIK ik)
        {
            _vrmRoot = animator.transform;
            applyForwardLength = true;
            this.faceDetector = faceDetector;

            _leftShoulderEffector = new GameObject("LeftUpperArmEffector").transform;
            _leftShoulderEffector.parent = _vrmRoot;

            _rightShoulderEffector = new GameObject("RightUpperArmEffector").transform;
            _rightShoulderEffector.parent = _vrmRoot;

            _leftShoulderDefaultOffset = animator.transform.InverseTransformPoint(
                animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position
                );
            ik.solver.leftShoulderEffector.target = _leftShoulderEffector;
            ik.solver.leftShoulderEffector.positionWeight = 0.5f;
            _leftShoulderEffector.localPosition = _leftShoulderDefaultOffset;
            _leftShoulderEffector.rotation = Quaternion.identity;

            _rightShoulderDefaultOffset = animator.transform.InverseTransformPoint(
                animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position
                );
            ik.solver.rightShoulderEffector.target = _rightShoulderEffector;
            ik.solver.rightShoulderEffector.positionWeight = 0.5f;
            _rightShoulderEffector.localPosition = _rightShoulderDefaultOffset;
            _rightShoulderEffector.rotation = Quaternion.identity;
        }
    }
}
