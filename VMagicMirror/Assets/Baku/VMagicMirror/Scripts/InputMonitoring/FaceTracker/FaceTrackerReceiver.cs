using System.Linq;
using Baku.VMagicMirror.VMCP;
using R3;
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
                message => _faceTracker.SetCalibrateData(message.GetStringValue())
                );

            receiver.BindBoolProperty(VmmCommands.EnableImageBasedHandTracking, _enableHandTracking);
            receiver.BindBoolProperty(
                VmmCommands.SetDisableCameraDuringVMCPActive, _disableCameraDuringVmcpActive
                );

            receiver.AssignQueryHandler(
                VmmCommands.CameraDeviceNames,
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
            // NOTE: MediaPipe実装が増えたため、カメラの起動する条件は限定的になった。下記すべてが該当することが必要
            // - 有効なカメラ名を指定している
            // - 顔トラが有効
            // - 高負荷モードがオフ、かつExTrackerもオフ == 顔トラッキングのモードとして低負荷モードを指定されている
            // - ハンドトラッキングがオフ (※ハンドトラッキングがオンの場合、高負荷モードがオフでもMediaPipeのほうが起動する)
            // - VMC Protocolがオフ or オンだけどカメラをそのまま動かしてよい
            //   - ※ここの存在価値は怪しいので条件として外すかもだが…

            var useCameraForLowPowerFaceTracking = 
                !string.IsNullOrEmpty(_cameraDeviceName.Value) &&
                _enableFaceTracking.Value &&
                !_enableHighPowerMode.Value &&
                !_enableExTracker.Value &&
                !_enableHandTracking.Value &&
                (!_vmcpActiveness.IsActive.CurrentValue || !_disableCameraDuringVmcpActive.Value);

            if (useCameraForLowPowerFaceTracking)
            {
                _faceTracker.ActivateCameraForFaceTracking(_cameraDeviceName.Value, FaceTrackingMode.LowPower);
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
