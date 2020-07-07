using System.Linq;
using UnityEngine;
using Zenject;
using Baku.VMagicMirror.InterProcess;

namespace Baku.VMagicMirror
{
    /// <summary> ウェブカメラによる顔トラッキングの制御に関するプロセス間通信を受信します。 </summary>
    [RequireComponent(typeof(FaceTracker))]
    public class FaceTrackerReceiver : MonoBehaviour
    {
        private FaceTracker _faceTracker;
        private bool _enableFaceTracking = true;
        private string _cameraDeviceName = "";
        private bool _enableExTracker = false;

        [Inject]
        public void Initialize(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                MessageCommandNames.SetCameraDeviceName,
                message => SetCameraDeviceName(message.Content)
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.EnableFaceTracking,
                message => SetEnableFaceTracking(message.ToBoolean())
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.ExTrackerEnable,
                message => SetEnableExTracker(message.ToBoolean())
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.CalibrateFace,
                message => _faceTracker.StartCalibration()
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.SetCalibrateFaceData,
                message => _faceTracker.SetCalibrateData(message.Content)
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.DisableFaceTrackingHorizontalFlip,
                message => _faceTracker.DisableHorizontalFlip = message.ToBoolean()
                );
         
            receiver.AssignQueryHandler(
                MessageQueryNames.CameraDeviceNames,
                query => query.Result = string.Join("\t", GetCameraDeviceNames())
                );
        }
        
        private void Start()
        {
            _faceTracker = GetComponent<FaceTracker>();
        }

        private void SetEnableFaceTracking(bool enable)
        {
            if (_enableFaceTracking == enable)
            {
                return;
            }

            _enableFaceTracking = enable;
            UpdateFaceDetectorState();
        }

        private void SetEnableExTracker(bool enable)
        {
            if (_enableExTracker == enable)
            {
                return;
            }

            _enableExTracker = enable;
            UpdateFaceDetectorState();
        }

        private void SetCameraDeviceName(string content)
        {
            _cameraDeviceName = content;
            UpdateFaceDetectorState();
        }
        
        private void UpdateFaceDetectorState()
        {
            if (_enableFaceTracking && !_enableExTracker && !string.IsNullOrWhiteSpace(_cameraDeviceName))
            {
                _faceTracker.ActivateCamera(_cameraDeviceName);
            }
            else
            {
                _faceTracker.StopCamera();
            }
        }

        private string[] GetCameraDeviceNames()
            => WebCamTexture.devices
            .Select(d => d.name)
            .ToArray();
        
    }
}
