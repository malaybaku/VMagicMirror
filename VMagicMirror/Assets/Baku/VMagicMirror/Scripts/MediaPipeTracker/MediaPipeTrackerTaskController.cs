using System.Linq;
using R3;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    /// <summary>
    /// MediaPipeによるトラッキング関連のIPCぜんぶを管理するクラス。
    /// 以下を行う
    /// - 設定に応じて、MediaPipeのタスクとかWebカメラのStart/Stop
    /// - 「bool値とかで持っていればOK」系の設定を適切なクラスに横流し
    /// </summary>
    public class MediaPipeTrackerTaskController : PresenterBase
    {
        private readonly IMessageReceiver _receiver;
        private readonly FaceLandmarkTask _face;

        private readonly HandTask _hand;
        private readonly HolisticTask _holistic;

        private readonly MediaPipeTrackerRuntimeSettingsRepository _settingsRepository;
        private readonly HorizontalFlipController _horizontalFlipController;
        private readonly WebCamTextureSource _webCamTextureSource;

        [Inject]
        public MediaPipeTrackerTaskController(
            IMessageReceiver receiver,
            MediaPipeTrackerRuntimeSettingsRepository settingsRepository,
            HorizontalFlipController horizontalFlipController,
            WebCamTextureSource webCamTextureSource,
            FaceLandmarkTask face,
            HandTask hand,
            HolisticTask holistic
            )
        {
            _receiver = receiver;
            _settingsRepository = settingsRepository;
            _webCamTextureSource = webCamTextureSource;
            _horizontalFlipController = horizontalFlipController;

            _face = face;
            _hand = hand;
            _holistic = holistic;
        }

        // IPCで直接受け取る値
        private readonly ReactiveProperty<bool> _faceTrackingEnabled = new(true);
        private readonly ReactiveProperty<string> _cameraDeviceName = new("");
        private readonly ReactiveProperty<bool> _useWebCamHighPowerMode = new();
        private readonly ReactiveProperty<bool> _useHandTracking = new();
        private readonly ReactiveProperty<bool> _useElbowTracking = new();
        private readonly ReactiveProperty<bool> _useExternalTracking = new();

        // カメラの状態や外部トラッキングの利用状況も踏まえて実際にタスクを動かすかどうか決めるフラグ。
        // 下記の2フラグと _useElbowTracking を組み合わせることで、3種類のタスクのどれが動くがが決定する
        private readonly ReactiveProperty<bool> _runFaceTask = new();
        private readonly ReactiveProperty<bool> _runHandTask = new();
        
        public override void Initialize()
        {
            SubscribeIpcMessages();
            SubscribeTaskRunningFlags();
            SubscribeHighPowerModeFlag();
            SetupTaskAndWebCamTextureActiveStatus();
        }

        private void SubscribeIpcMessages()
        {
            _receiver.BindBoolProperty(VmmCommands.EnableFaceTracking, _faceTrackingEnabled);
            _receiver.BindStringProperty(VmmCommands.SetCameraDeviceName, _cameraDeviceName);
            _receiver.BindBoolProperty(VmmCommands.EnableWebCamHighPowerMode, _useWebCamHighPowerMode);
            _receiver.BindBoolProperty(VmmCommands.EnableImageBasedHandTracking, _useHandTracking);
            _receiver.BindBoolProperty(VmmCommands.EnableImageBasedElbowTracking, _useElbowTracking);
            _receiver.BindBoolProperty(VmmCommands.ExTrackerEnable, _useExternalTracking);
            
            // NOTE: WPF側ではパーフェクトシンクのon/offフラグは一種類だけである…というスタンスを取っているので、その値を拾う。
            // が、Unity目線だと「webcamでパーフェクトシンクするか否か」と「ExTrackerでパーフェクトシンクするか否か」が共通のフラグという
            // 必然性はあまりないので、別のフラグがあるつもりで管理しておく
            _receiver.AssignCommandHandler(
                VmmCommands.ExTrackerEnablePerfectSync,
                m => _settingsRepository.SetShouldUsePerfectSyncResult(m.ToBoolean())
                );

            _receiver.AssignCommandHandler(
                VmmCommands.EnableWebCameraHighPowerModeLipSync,
                m => _settingsRepository.SetShouldUseLipSyncResult(m.ToBoolean())
                );
            
            _receiver.AssignCommandHandler(
                VmmCommands.EnableBodyLeanZ,
                m => _settingsRepository.EnableBodyMoveZAxis = m.ToBoolean()
                );
            _receiver.AssignCommandHandler(
                VmmCommands.SetWebCamEyeOpenBlinkValue,
                m => _settingsRepository.EyeOpenBlinkValue = m.ParseAsPercentage()
                );
            _receiver.AssignCommandHandler(
                VmmCommands.SetWebCamEyeCloseBlinkValue,
                m => _settingsRepository.EyeCloseBlinkValue = m.ParseAsPercentage()
                );
            _receiver.AssignCommandHandler(
                VmmCommands.SetWebCamEyeApplySameBlinkBothEye,
                m => _settingsRepository.EyeUseMeanBlinkValue.Value = m.ToBoolean()
                );
            _receiver.AssignCommandHandler(
                VmmCommands.SetWebCamEyeApplyBlinkCorrectionToPerfectSync,
                m => _settingsRepository.EyeApplyCorrectionToPerfectSync.Value = m.ToBoolean()
                );

            // TODO: ハンドトラッキングだけ動いてるときのキャリブレーションの実装 = 一瞬だけFaceTaskを起こす処理の実装?
            // NOTE: MediaPipeのトラッキングが動いてない場合、キャリブレーションは実行されない
            _receiver.BindAction(VmmCommands.CalibrateFace, () =>
            {
                if (_runFaceTask.Value || _runHandTask.Value)
                {
                    _settingsRepository.RaiseCalibrationRequest();
                }
            });
            _receiver.AssignCommandHandler(
                VmmCommands.SetCalibrateFaceDataHighPower, 
                message => _settingsRepository.ApplyReceivedCalibrationData(message.GetStringValue())
                );

            _receiver.AssignCommandHandler(
                VmmCommands.SetHandTrackingMotionScale,
                m => _settingsRepository.HandTrackingMotionScale.Value = m.ParseAsPercentage()
            );
            _receiver.AssignCommandHandler(
                VmmCommands.SetHandTrackingOffsetX,
                m => _settingsRepository.HandTrackingOffsetX.Value = m.ParseAsCentimeter()
            );
            _receiver.AssignCommandHandler(
                VmmCommands.SetHandTrackingOffsetY,
                m => _settingsRepository.HandTrackingOffsetY.Value = m.ParseAsCentimeter()
            );

            _horizontalFlipController.DisableFaceHorizontalFlip
                .Subscribe(disableMirror => _settingsRepository.IsFaceMirrored.Value = !disableMirror)
                .AddTo(this);

            _horizontalFlipController.DisableHandHorizontalFlip
                .Subscribe(disableMirror => _settingsRepository.IsHandMirrored.Value = !disableMirror)
                .AddTo(this);
        }
        
        private void SubscribeTaskRunningFlags()
        {
            // 顔トラッキングの条件として外部トラッキングが無効かどうかのチェックが入ることに注意
            _faceTrackingEnabled
                .CombineLatest(
                    _cameraDeviceName,
                    _useExternalTracking,
                    (faceTrackingEnabled, deviceName, useExTracker) =>
                        faceTrackingEnabled &&
                        !useExTracker &&
                        IsAvailableWebCamDevice(deviceName))
                .DistinctUntilChanged()
                .Subscribe(runTask => _runFaceTask.Value = runTask)
                .AddTo(this);

            // ハンドトラッキングについてはカメラの有効性だけで評価できる
            _useHandTracking
                .CombineLatest(
                    _cameraDeviceName,
                    (useHandTracking, deviceName) =>
                        useHandTracking && IsAvailableWebCamDevice(deviceName))
                .DistinctUntilChanged()
                .Subscribe(runTask => _runHandTask.Value = runTask)
                .AddTo(this);
        }

        private void SubscribeHighPowerModeFlag()
        {
            _useWebCamHighPowerMode
                .Subscribe(value =>
                {
                    _face.SetBlendShapeOutputActive(value);
                    _holistic.SetBlendShapeOutputActive(value);
                })
                .AddTo(this);
        }
        
        private void SetupTaskAndWebCamTextureActiveStatus()
        {
            // 顔のみのトラッキングタスク: 顔だけトラッキングするとき発動
            _runFaceTask
                .CombineLatest(_runHandTask, (face, hand) => face && !hand)
                .DistinctUntilChanged()
                .Subscribe(run => _face.SetTaskActive(run))
                .AddTo(this);

            // 手だけのトラッキング: 手だけトラッキングするとき発動。肘トラを含むときも停止させることに注意
            _runHandTask
                .CombineLatest(
                    _runFaceTask,
                    _useElbowTracking, 
                    (hand, face, elbow) => hand && !face && !elbow)
                .DistinctUntilChanged()
                .Subscribe(run => _hand.SetTaskActive(run))
                .AddTo(this);
            
            // 複数のトラッキングをする場合: 内容に即してholisticタスクを動かす
            _runFaceTask
                .CombineLatest(
                    _runHandTask,
                    _useElbowTracking,
                    (face, hand, elbow) =>
                    {
                        // NOTE: 「trueが2つ以上」みたいな条件も無くはないが、下記の3種類しか想定してないので明示的にそういう分岐にしている
                        var runHolistic = (face && hand) || (hand && elbow) || (face && hand && elbow);
                        HolisticTask.HolisticTaskMode? taskMode = (face, hand, elbow) switch
                        {
                            (true, true, true) => HolisticTask.HolisticTaskMode.FaceHandAndElbow,
                            (true, true, false) => HolisticTask.HolisticTaskMode.FaceAndHand,
                            (false, true, true) => HolisticTask.HolisticTaskMode.HandAndElbow,
                            _ => null,
                        };
                        
                        return (runHolistic, taskMode);
                    })
                .DistinctUntilChanged()
                .Subscribe(value =>
                {
                    if (value is { runHolistic: true, taskMode: not null })
                    {
                        _holistic.SetTaskActive(true, value.taskMode.Value);
                    }
                    else
                    {
                        _holistic.SetTaskActive(false);
                    }
                })
                .AddTo(this);

            // NOTE: FaceTrackerとの競合回避するうえで、オフにする処理の一部はThrottleFrameできないかも…
            // - ↑が実際に合っていた場合、FaceTrackerと本クラスでWebCamTextureの取得排他できるようなクラスを用意するのが良さそう
            _cameraDeviceName
                .CombineLatest(
                    _runHandTask,
                    _runFaceTask,
                    (cameraDeviceName, x, y) => (cameraDeviceName, useWebCam: x || y)
                )
                // NOTE: 「ハンドトラッキングを停止し、すぐにハンド + 表情トラッキングを開始」のようなケースの場合に最後の状態だけ通すようにしたい
                .DebounceFrame(1)
                .DistinctUntilChanged()
                .Subscribe(value => _webCamTextureSource.SetActive(value.useWebCam, value.cameraDeviceName))
                .AddTo(this);
        }
        
        public override void Dispose()
        {
            base.Dispose();
            _face.StopTask();
            _hand.StopTask();
            _holistic.StopTask();
        }

        private static bool IsAvailableWebCamDevice(string name) => WebCamTexture.devices.Any(d => d.name == name);
    }
}
