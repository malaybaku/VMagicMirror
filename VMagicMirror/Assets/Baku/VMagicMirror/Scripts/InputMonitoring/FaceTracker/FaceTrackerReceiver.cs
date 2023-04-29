using System.Linq;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary> ウェブカメラによる顔トラッキングの制御に関するプロセス間通信を受信します。 </summary>
    /// <remarks>
    /// 名前に反しますが、このクラスでハンドトラッキングのオンオフも監視します。
    /// これにより、「顔トラッキングか手トラッキングのいずれかが有効ならカメラを起動」みたいな挙動を実現します。
    /// </remarks>
    public sealed class FaceTrackerReceiver 
    {
        private readonly FaceTracker _faceTracker;
        
        private bool _enableFaceTracking = true;
        private bool _enableHighPowerMode = false;
        private bool _enableHandTracking = false;
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
                VmmCommands.EnableImageBasedHandTracking,
                message => SetHandTrackingEnable(message.ToBoolean())
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

        private void SetHandTrackingEnable(bool enable)
        {
            _enableHandTracking = enable;
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
            //NOTE: ココ結構条件が複雑
            //カメラが未指定 → とにかくダメなのでカメラは止める
            //カメラが指定されてる→
            //  - 手トラッキングあり → 顔まわりがどうなっててもカメラは起こす
            //  - 手トラッキングなし → Ex.Trackerの状態と顔トラッキング自体のオンオフを見たうえで判断
            bool canUseCamera = !string.IsNullOrEmpty(_cameraDeviceName);
            if (canUseCamera)
            {
                canUseCamera =
                    _enableHandTracking ||
                    (_enableFaceTracking && !_enableExTracker);
            }

            if (canUseCamera)
            {
                var trackingMode = FaceTrackingMode.None;
                if (_enableFaceTracking && !_enableExTracker)
                {
                    trackingMode = _enableHighPowerMode ? FaceTrackingMode.HighPower : FaceTrackingMode.LowPower;
                }
                _faceTracker.ActivateCameraForFaceTracking(_cameraDeviceName, trackingMode);
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
