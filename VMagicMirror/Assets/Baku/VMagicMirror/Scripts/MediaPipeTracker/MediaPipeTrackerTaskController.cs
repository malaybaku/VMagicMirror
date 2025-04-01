using System;
using System.Linq;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    /// <summary>
    /// MediaPipeによるトラッキング関連のIPCぜんぶを管理するクラス。
    /// 以下を行う
    /// - 設定に応じて、MediaPipeのタスクとかWebカメラのStart/Sop
    /// - 「bool値とかで持っていればOK」系の設定を適切なクラスに横流し
    /// </summary>
    public class MediaPipeTrackerTaskController : PresenterBase
    {
        private readonly IMessageReceiver _receiver;
        private readonly HandTask _hand;
        private readonly FaceLandmarkTask _face;
        private readonly HandAndFaceLandmarkTask _handAndFace;

        private readonly MediaPipeTrackerSettingsRepository _settingsRepository;

        // TODO: Dlib用のFaceTrackerもWebCamTextureを使っているが、集約したい
        // 楽観的にはDlibFaceLandmarkも使うのやめれば解決する)
        // 各タスクの稼働状況から定まるフラグ
        private readonly WebCamTextureSource _webCamTextureSource;

        [Inject]
        public MediaPipeTrackerTaskController(
            IMessageReceiver receiver,
            MediaPipeTrackerSettingsRepository settingsRepository,
            WebCamTextureSource webCamTextureSource,
            HandTask hand,
            FaceLandmarkTask face,
            HandAndFaceLandmarkTask handAndFace
            )
        {
            _receiver = receiver;
            _settingsRepository = settingsRepository;
            _webCamTextureSource = webCamTextureSource;

            _hand = hand;
            _face = face;
            _handAndFace = handAndFace;
        }

        // IPCで直接受け取る値
        private readonly ReactiveProperty<bool> _faceTrackingEnabled = new(true);
        private readonly ReactiveProperty<string> _cameraDeviceName = new("");
        private readonly ReactiveProperty<bool> _useWebCamHighPowerMode = new();
        private readonly ReactiveProperty<bool> _useHandTracking = new();
        private readonly ReactiveProperty<bool> _useExternalTracking = new();
        private readonly ReactiveProperty<bool> _disableFaceHorizontalFlip = new();
        private readonly ReactiveProperty<bool> _disableHandHorizontalFlip = new();
        
        // それぞれのMediaPipeタスクの稼働状況
        private readonly ReactiveProperty<bool> _isFaceTaskRunning = new();
        private readonly ReactiveProperty<bool> _isHandTaskRunning = new();
        private readonly ReactiveProperty<bool> _isHandAndFaceTaskRunning = new();
        
        public override void Initialize()
        {
            SubscribeIpcMessages();
            SubscribeTaskRunningFlags();
            SetupTaskAndWebCamTextureActiveStatus();
        }

        private void SubscribeIpcMessages()
        {
            _receiver.BindBoolProperty(VmmCommands.EnableFaceTracking, _faceTrackingEnabled);
            _receiver.BindBoolProperty(VmmCommands.DisableFaceTrackingHorizontalFlip, _disableFaceHorizontalFlip);
            _receiver.BindBoolProperty(VmmCommands.DisableHandTrackingHorizontalFlip, _disableHandHorizontalFlip);
            _receiver.BindStringProperty(VmmCommands.SetCameraDeviceName, _cameraDeviceName);
            _receiver.BindBoolProperty(VmmCommands.EnableWebCamHighPowerMode, _useWebCamHighPowerMode);
            _receiver.BindBoolProperty(VmmCommands.EnableImageBasedHandTracking, _useHandTracking);
            _receiver.BindBoolProperty(VmmCommands.ExTrackerEnable, _useExternalTracking);
            
            // TODO: SettingsRepository あるいは KinematicSetter にわたす
            // _receiver.AssignCommandHandler(VmmCommands.UsePerfectSyncWithWebCamera,);
            // _receiver.AssignCommandHandler(VmmCommands.EnableWebCameraHighPowerModeBlink,);
            // _receiver.AssignCommandHandler(VmmCommands.EnableWebCameraHighPowerModeLipSync,);
            // _receiver.AssignCommandHandler(VmmCommands.EnableWebCameraHighPowerModeMoveZ,);

            // TODO: ハンドトラッキングだけ動いてるときのキャリブレーションの実装 = 一瞬だけFaceTaskを起こす処理の実装
            // NOTE: MediaPipeのトラッキングが動いてない場合、キャリブレーションは実行されない
            _receiver.BindAction(VmmCommands.CalibrateFace, () =>
            {
                if (_isFaceTaskRunning.Value || 
                    _isHandTaskRunning.Value || 
                    _isHandAndFaceTaskRunning.Value)
                {
                    _settingsRepository.RaiseCalibrationRequest();
                }
            });
            _receiver.AssignCommandHandler(
                VmmCommands.SetCalibrateFaceDataHighPower, 
                message => _settingsRepository.ApplyReceivedCalibrationData(message.Content)
                );

            _disableFaceHorizontalFlip
                .Subscribe(disableMirror => _settingsRepository.IsFaceMirrored.Value = !disableMirror)
                .AddTo(this);
            _disableHandHorizontalFlip
                .Subscribe(disableMirror => _settingsRepository.IsHandMirrored.Value = !disableMirror)
                .AddTo(this);
        }
        
        private void SubscribeTaskRunningFlags()
        {
            // 下記の3つのフラグは2つ以上同時にtrueにならないように実装している (ちょっと読み取りにくいが)
            
            _faceTrackingEnabled
                .CombineLatest(
                    _cameraDeviceName,
                    _useWebCamHighPowerMode,
                    _useExternalTracking,
                    _useHandTracking,
                    (faceTrackingEnabled, deviceName, isHighPowerMode, useExTracker, useHandTracking) =>
                        faceTrackingEnabled &&
                        isHighPowerMode &&
                        !useExTracker &&
                        !useHandTracking &&
                        IsAvailableWebCamDevice(deviceName))
                .DistinctUntilChanged()
                .Subscribe(runTask => _isFaceTaskRunning.Value = runTask)
                .AddTo(this);
            
            // ↑とかなり似ているが、ハンドトラッキングのフラグ条件が逆だったり、高負荷モードのフラグを無視したりすることに注意
            _faceTrackingEnabled
                .CombineLatest(
                    _cameraDeviceName,
                    _useExternalTracking,
                    _useHandTracking,
                    (faceTrackingEnabled, deviceName, useExTracker, useHandTracking) =>
                        faceTrackingEnabled &&
                        !useExTracker &&
                        useHandTracking &&
                        IsAvailableWebCamDevice(deviceName))
                .DistinctUntilChanged()
                .Subscribe(runTask => _isHandAndFaceTaskRunning.Value = runTask)
                .AddTo(this);

            // 書いてる通りではあるが、MediaPipeで手だけをトラッキングするのは「外部トラッキングで顔をトラッキングしてるとき」だけになる
            _cameraDeviceName
                .CombineLatest(
                    _useExternalTracking,
                    _useHandTracking,
                    (deviceName, useExTracker, useHandTracking) =>
                        useExTracker &&
                        useHandTracking &&
                        IsAvailableWebCamDevice(deviceName))
                .DistinctUntilChanged()
                .Subscribe(runTask => _isHandTaskRunning.Value = runTask)
                .AddTo(this);
        }

        private void SetupTaskAndWebCamTextureActiveStatus()
        {
            _isFaceTaskRunning
                .Subscribe(run => _face.SetTaskActive(run))
                .AddTo(this);

            _isHandTaskRunning
                .Subscribe(run => _hand.SetTaskActive(run))
                .AddTo(this);
            
            _isHandAndFaceTaskRunning
                .Subscribe(run => _handAndFace.SetTaskActive(run))
                .AddTo(this);
            
            // NOTE: FaceTrackerとの競合回避するうえで、オフにする処理の一部はThrottleFrameできないかも…
            // - ↑が実際に合っていた場合、FaceTrackerと本クラスでWebCamTextureの取得排他できるようなクラスを用意するのが良さそう
            _cameraDeviceName
                .CombineLatest(
                    _isHandTaskRunning,
                    _isHandAndFaceTaskRunning,
                    _isFaceTaskRunning,
                    (cameraDeviceName, x, y, z) => (cameraDeviceName, useWebCam: x || y || z)
                )
                // NOTE: 「ハンドトラッキングを停止し、すぐにハンド + 表情トラッキングを開始」のようなケースがありうるので、
                // throttleをやっとかないとムダなWebCamTextureの再生成が発生してしまう
                .ThrottleFrame(1)
                .DistinctUntilChanged()
                .Subscribe(value => _webCamTextureSource.SetActive(value.useWebCam, value.cameraDeviceName))
                .AddTo(this);

            _isHandTaskRunning
                .CombineLatest(
                    _isHandAndFaceTaskRunning, 
                    (x, y) => x || y
                )
                .Subscribe(v => _settingsRepository.IsHandTrackingActive = v)
                .AddTo(this);
        }
        
        public override void Dispose()
        {
            base.Dispose();
            _face.StopTask();
            _hand.StopTask();
            _handAndFace.StopTask();
        }

        private static bool IsAvailableWebCamDevice(string name) => WebCamTexture.devices.Any(d => d.name == name);
    }
}
