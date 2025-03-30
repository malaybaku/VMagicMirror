using System.Linq;
using Baku.VMagicMirror.VMCP;
using UniRx;
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
        private readonly VMCPActiveness _vmcpActiveness;
        
        private readonly ReactiveProperty<string> _cameraDeviceName = new("");

        private readonly ReactiveProperty<bool> _enableFaceTracking = new(true);
        private readonly ReactiveProperty<bool> _enableHighPowerMode = new(false);
        private readonly ReactiveProperty<bool> _enableHandTracking = new(false);
        private readonly ReactiveProperty<bool> _enableExTracker = new(false);
        private readonly ReactiveProperty<bool> _disableCameraDuringVmcpActive = new(true);
        
        public FaceTrackerReceiver(
            IMessageReceiver receiver, 
            FaceTracker faceTracker,
            VMCPActiveness vmcpActiveness
            )
        {
            _faceTracker = faceTracker;
            _vmcpActiveness = vmcpActiveness;

            receiver.BindStringProperty(VmmCommands.SetCameraDeviceName, _cameraDeviceName);
            receiver.BindBoolProperty(VmmCommands.EnableFaceTracking, _enableFaceTracking);
            receiver.BindBoolProperty(VmmCommands.EnableWebCamHighPowerMode, _enableHighPowerMode);
            receiver.BindBoolProperty(VmmCommands.ExTrackerEnable, _enableExTracker);
            receiver.BindAction(VmmCommands.CalibrateFace, _faceTracker.StartCalibration);
            receiver.AssignCommandHandler(
                VmmCommands.SetCalibrateFaceData,
                message => _faceTracker.SetCalibrateData(message.Content)
                );

            receiver.BindBoolProperty(VmmCommands.EnableImageBasedHandTracking, _enableHandTracking);
            receiver.BindBoolProperty(
                VmmCommands.SetDisableCameraDuringVMCPActive, _disableCameraDuringVmcpActive
                );

            receiver.AssignQueryHandler(
                VmmQueries.CameraDeviceNames,
                query => query.Result = DeviceNames.CreateDeviceNamesJson(GetCameraDeviceNames())
                );

            Observable.Merge(
                _cameraDeviceName.Skip(1).AsUnitObservable(),
                _enableFaceTracking.Skip(1).AsUnitObservable(),
                _enableHighPowerMode.Skip(1).AsUnitObservable(),
                _enableHandTracking.Skip(1).AsUnitObservable(),
                _enableExTracker.Skip(1).AsUnitObservable(),
                _disableCameraDuringVmcpActive.Skip(1).AsUnitObservable(),
                _vmcpActiveness.IsActive.Skip(1).AsUnitObservable()
                )
                .Subscribe(_ => UpdateFaceDetectorState())
                .AddTo(_faceTracker);
        }
        
        private void UpdateFaceDetectorState()
        {
            //NOTE: ココ結構条件が複雑
            //カメラが未指定 → とにかくダメなのでカメラは止める
            //カメラが指定されてる→
            //  - 手トラッキングあり → 顔まわりがどうなっててもカメラは起こす
            //  - 手トラッキングなし → Ex.Trackerの状態と顔トラッキング自体のオンオフを見たうえで判断
            var canUseCamera = !string.IsNullOrEmpty(_cameraDeviceName.Value);
            if (canUseCamera)
            {
                canUseCamera =
                    _enableHandTracking.Value ||
                    (_enableFaceTracking.Value && !_enableExTracker.Value);
            }

            if (canUseCamera)
            {
                var trackingMode = FaceTrackingMode.None;
                if (_enableFaceTracking.Value && !_enableExTracker.Value)
                {
                    trackingMode = _enableHighPowerMode.Value ? FaceTrackingMode.HighPower : FaceTrackingMode.LowPower;
                }
                _faceTracker.ActivateCameraForFaceTracking(_cameraDeviceName.Value, trackingMode);
            }
            else
            {
                _faceTracker.StopCamera();
            }
        }

        private static string[] GetCameraDeviceNames()
            => WebCamTexture.devices
            .Select(d => d.name)
            .ToArray();
    }
}
