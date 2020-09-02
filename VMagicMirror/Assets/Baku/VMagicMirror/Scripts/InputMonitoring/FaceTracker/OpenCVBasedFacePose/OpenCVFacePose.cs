using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// OpenCVのSolvePnPで推定した頭の位置+回転をくれるやつ。
    /// </summary>
    /// <remarks>
    /// ロス時のゼロ点戻しとかキャリブとか、左右反転の設定の世話をここでやる。
    /// で、このクラスの出力値を<see cref="FaceAttitudeController"/>とかでさばいてもらう
    /// </remarks>
    public class OpenCVFacePose : MonoBehaviour
    {
        [SerializeField] private float notTrackedLimit = 0.5f;
        [SerializeField] private float resetSpeedFactor = 6f;

        private readonly OpenCVFacePoseEstimator _estimator = new OpenCVFacePoseEstimator();
        
        private FaceTracker _faceTracker;
        private FaceControlConfiguration _config;
        private float _count = 0f;

        /// <summary> 現在の頭部姿勢データが有効かどうかを取得します。 </summary>
        public bool HasValidData => _estimator.HasValidPoseData;

        /// <summary> 設定に沿って後処理された頭部の位置を取得します。 </summary>
        public Vector3 HeadPosition
        {
            get
            {
                if (!HasValidData)
                {
                    return Vector3.zero;
                }

                //キャリブの回転考慮が必要なことに注意！外部トラッキングと同じような処理ですね。
                var pos = _hasCalibrationData                
                    ? _calibrateRotInv * (_headPosition - _calibratePos)
                    : _headPosition;

                //ちょっとややこしいが、デフォルトは鏡写し + オプトインで元に戻すよ、という話
                if (!_faceTracker.DisableHorizontalFlip)
                {
                    pos = new Vector3(-pos.x, pos.y, pos.z);
                }
                
                return pos;
            }
        }

        /// <summary> 設定に沿って後処理された頭部の回転を取得します。 </summary>
        public Quaternion HeadRotation
        {
            get
            {
                if (!HasValidData)
                {
                    return Quaternion.identity;
                }

                var rot = _hasCalibrationData
                    ? _headRotation * _calibrateRotInv
                    : _headRotation;

                //回転軸をクイッと。
                rot.x *= -1f;
                if (_faceTracker.DisableHorizontalFlip)
                {
                    rot.z *= -1f;
                }
                else
                {
                    rot.y *= -1f;
                }

                return rot;
            }
        }

        private Vector3 _headPosition = Vector3.zero;
        private Quaternion _headRotation = Quaternion.identity;
        
        private bool _hasCalibrationData = false;
        private Vector3 _calibratePos = Vector3.zero;
        private Quaternion _calibrateRot = Quaternion.identity;
        private Quaternion _calibrateRotInv = Quaternion.identity;
        
        [Inject]
        public void Initialize(FaceTracker faceTracker, FaceControlConfiguration config)
        {
            _faceTracker = faceTracker;
            _config = config;
            faceTracker.FaceDetectionUpdated += OnFaceDetectionUpdated;
            faceTracker.FaceLandmarksUpdated += OnFaceLandmarksUpdated;
            faceTracker.CalibrationRequired += OnCalibrationRequired;
            faceTracker.CalibrationDataReceived += OnCalibrationDataReceived;
        }

        private void OnCalibrationDataReceived(CalibrationData data)
        {
            if (!data.hasOpenCvPose)
            {
                return;
            }

            _hasCalibrationData = true;
            _calibratePos = data.openCvFacePos;
            _calibrateRot = Quaternion.Euler(data.openCvFaceRotEuler);
            _calibrateRotInv = Quaternion.Inverse(_calibrateRot);
        }

        private void OnCalibrationRequired(CalibrationData data)
        {
            //TODO: 現在値さえ保存してればいいんだっけ、というのは実態に合わせて直す。多分大丈夫なはずだけど
            if (!_estimator.HasValidPoseData)
            {
                data.hasOpenCvPose = false;
                return;
            }

            _hasCalibrationData = true;
            _calibratePos = _estimator.HeadPosition;
            _calibrateRot = _estimator.HeadRotation;
            _calibrateRotInv = Quaternion.Inverse(_calibrateRot);

            data.hasOpenCvPose = true;
            data.openCvFacePos = _calibratePos;
            data.openCvFaceRotEuler = _calibrateRot.eulerAngles;
        }

        private void OnFaceLandmarksUpdated(FaceLandmarksUpdateStatus status)
        {
            _estimator.EstimatePose(status.Landmarks);
            //NOTE: OpenCVで姿勢を取りそこねるケースもあることに注意。
            if (_estimator.HasValidPoseData)
            {
                _count = notTrackedLimit;
                _headRotation = _estimator.HeadRotation;
                _headPosition = _estimator.HeadPosition;
            }
        }

        private void OnFaceDetectionUpdated(FaceDetectionUpdateStatus status) 
            => _estimator.SetImageSize(status.Width, status.Height);

        private void Update()
        {
            if (_config.ControlMode != FaceControlModes.WebCam)
            {
                return;
            }
            
            if (_count > 0 && !_estimator.HasValidPoseData)
            {
                _count -= Time.deltaTime;
            }

            if (_count < 0)
            {
                _headPosition *= Mathf.Clamp01(1.0f - resetSpeedFactor * Time.deltaTime);
                _headRotation = Quaternion.Slerp(
                    _headRotation, Quaternion.identity, resetSpeedFactor * Time.deltaTime
                    );
            }
        }
    }
}
