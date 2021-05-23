using System.Linq;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary> ウェブカメラによる顔トラッキングの制御に関するプロセス間通信を受信します。 </summary>
    public sealed class FaceTrackerReceiver 
    {
        private readonly FaceTracker _faceTracker;
        
        private bool _enableFaceTracking = true;
        private bool _enableHighPowerMode = false;
        private string _cameraDeviceName = "";
        private bool _enableExTracker = false;

        public FaceTrackerReceiver(IMessageReceiver receiver, FaceTracker faceTracker)
        {
            _faceTracker = faceTracker;
            
            receiver.AssignCommandHandler(
                VmmCommands.SetCameraDeviceName,
                message => SetCameraDeviceName(message.Content)
                );
            receiver.AssignCommandHandler(
                VmmCommands.EnableFaceTracking,
                message => SetEnableFaceTracking(message.ToBoolean())
                );
            receiver.AssignCommandHandler(
                VmmCommands.EnableWebCamHighPowerMode,
                message => SetWebCamHighPowerMode(message.ToBoolean())
                );
            receiver.AssignCommandHandler(
                VmmCommands.ExTrackerEnable,
                message => SetEnableExTracker(message.ToBoolean())
                );
            receiver.AssignCommandHandler(
                VmmCommands.CalibrateFace,
                message => _faceTracker.StartCalibration()
                );
            receiver.AssignCommandHandler(
                VmmCommands.SetCalibrateFaceData,
                message => _faceTracker.SetCalibrateData(message.Content)
                );
            receiver.AssignCommandHandler(
                VmmCommands.DisableFaceTrackingHorizontalFlip,
                message => _faceTracker.DisableHorizontalFlip = message.ToBoolean()
                );
         
            receiver.AssignQueryHandler(
                VmmQueries.CameraDeviceNames,
                query => query.Result = DeviceNames.CreateDeviceNamesJson(GetCameraDeviceNames())
                );
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
        
        private void SetWebCamHighPowerMode(bool enable)
        {
            if (_enableHighPowerMode == enable)
            {
                return;
            }

            _enableHighPowerMode = enable;
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
                _faceTracker.ActivateCamera(_cameraDeviceName, _enableHighPowerMode);
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
