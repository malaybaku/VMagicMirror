using UnityEngine;

namespace Baku.VMagicMirror
{
    public class FaceBasedCameraPositionAdjuster : MonoBehaviour
    {
        const float CameraDisplacementToMoveAmountFactor = 0.5f;

        //2階微分方程式メソッドです
        public float Dump { get; set; } = 10f;
        public float Spring { get; set; } = 5f;

        [SerializeField]
        FaceDetector faceDetector = null;

        [SerializeField]
        CameraController cameraController = null;

        private bool _isPositionInitialized = false;
        private Vector3 _prevPosition;
        private Vector3 _prevSpeed;

        private void LateUpdate()
        {
            if (faceDetector == null) { return; }

            if (cameraController.IsInFreeCameraMode)
            {
                return;
            }

            var basePosition = cameraController.BaseCameraPosition;

            if (!_isPositionInitialized)
            {
                _prevPosition = basePosition;
                _prevSpeed = Vector3.zero;
                _isPositionInitialized = true;
                return;
            }

            if (faceDetector.DetectedRect.width < Mathf.Epsilon ||
                faceDetector.DetectedRect.height < Mathf.Epsilon
                )
            {
                return;
            }

            var center = faceDetector.DetectedRect.center;

            var goalPosition = 
                basePosition + 
                transform.right * center.x * CameraDisplacementToMoveAmountFactor +
                transform.up * center.y * CameraDisplacementToMoveAmountFactor;

            //一次陽的オイラー法(3行で済むわりによく動くので偉い)
            var accel = -Spring * (_prevPosition - goalPosition) - Dump * _prevSpeed;
            var speed = _prevSpeed + accel * Time.deltaTime;
            var pos = _prevPosition + speed * Time.deltaTime;

            _prevSpeed = speed;
            _prevPosition = pos;

            transform.position = pos;

            //transform.position = Vector3.Lerp(
            //    transform.position,
            //    goalPosition,
            //    CameraMovePositionFactor
            //    );
        }
    }
}
