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
        private readonly HandAndFaceLandmarkTask _handAndFace;
        private readonly HandTaskV2 _handWithElbow;
        private readonly HandAndFaceLandmarkTaskV2 _handAndFaceWithElbow;
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
            HandAndFaceLandmarkTask handAndFace,
            HandTaskV2 handWithElbow,
            HandAndFaceLandmarkTaskV2 handAndFaceWithElbow,
            HolisticTask holistic
            )
        {
            _receiver = receiver;
            _settingsRepository = settingsRepository;
            _webCamTextureSource = webCamTextureSource;
            _horizontalFlipController = horizontalFlipController;

            _face = face;
            _hand = hand;
            _handAndFace = handAndFace;
            _handWithElbow = handWithElbow;
            _handAndFaceWithElbow = handAndFaceWithElbow;
            _holistic = holistic;
        }

        // IPCで直接受け取る値
        private readonly ReactiveProperty<bool> _faceTrackingEnabled = new(true);
        private readonly ReactiveProperty<string> _cameraDeviceName = new("");
        private readonly ReactiveProperty<bool> _useWebCamExpressionTracking = new();
        private readonly ReactiveProperty<bool> _useHandTracking = new();
        private readonly ReactiveProperty<bool> _useElbowTracking = new();
        private readonly ReactiveProperty<bool> _useExternalTracking = new();
        private readonly ReactiveProperty<bool> _alwaysUseSingleMediaPipeTask = new();
        
        // MediaPipeタスクの稼働状況で、「顔だけ」「手だけ」「顔と手」のどれを行うか決めるフラグ。
        // 下記3つのフラグは2つ以上は同時にtrueにならないように制御する。
        // これらのフラグと _useElbowTracking の値によって、5種類のTaskクラスのうち最大で1つが動作する
        private readonly ReactiveProperty<bool> _isFaceTaskRunning = new();
        private readonly ReactiveProperty<bool> _isHandTaskRunning = new();
        private readonly ReactiveProperty<bool> _isHandAndFaceTaskRunning = new();
        
        public override void Initialize()
        {
            SubscribeIpcMessages();
            SubscribeTaskRunningFlags();
            SubscribeExpressionTrackingFlag();
            SetupTaskAndWebCamTextureActiveStatus();
        }

        private void SubscribeIpcMessages()
        {
            _receiver.BindBoolProperty(VmmCommands.EnableFaceTracking, _faceTrackingEnabled);
            _receiver.BindStringProperty(VmmCommands.SetCameraDeviceName, _cameraDeviceName);
            _receiver.BindBoolProperty(VmmCommands.EnableWebCamExpressionTracking, _useWebCamExpressionTracking);
            _receiver.BindBoolProperty(VmmCommands.EnableImageBasedHandTracking, _useHandTracking);
            _receiver.BindBoolProperty(VmmCommands.EnableImageBasedElbowTracking, _useElbowTracking);
            _receiver.BindBoolProperty(VmmCommands.EnableAlwaysUseSingleMediaPipeTask, _alwaysUseSingleMediaPipeTask);
            _receiver.BindBoolProperty(VmmCommands.ExTrackerEnable, _useExternalTracking);
            _receiver.BindBoolProperty(VmmCommands.EnableWebCamQuickMotion, _settingsRepository.EnableQuickMotion);
            
            // NOTE: WPF側ではパーフェクトシンクのon/offフラグは一種類だけである…というスタンスを取っているので、その値を拾う。
            // が、Unity目線だと「webcamでパーフェクトシンクするか否か」と「ExTrackerでパーフェクトシンクするか否か」が共通のフラグという
            // 必然性はあまりないので、別のフラグがあるつもりで管理しておく
            _receiver.AssignCommandHandler(
                VmmCommands.ExTrackerEnablePerfectSync,
                m => _settingsRepository.SetShouldUsePerfectSyncResult(m.ToBoolean())
                );

            _receiver.AssignCommandHandler(
                VmmCommands.EnableWebCamApplyLipSync,
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
                if (_isFaceTaskRunning.Value || 
                    _isHandTaskRunning.Value || 
                    _isHandAndFaceTaskRunning.Value)
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
            // 下記の3つのフラグは2つ以上同時にtrueにならないように実装している (ちょっと読み取りにくいが)
            
            _faceTrackingEnabled
                .CombineLatest(
                    _cameraDeviceName,
                    _useExternalTracking,
                    _useHandTracking,
                    (faceTrackingEnabled, deviceName, useExTracker, useHandTracking) =>
                        faceTrackingEnabled &&
                        !useExTracker &&
                        !useHandTracking &&
                        IsAvailableWebCamDevice(deviceName))
                .DistinctUntilChanged()
                .Subscribe(runTask => _isFaceTaskRunning.Value = runTask)
                .AddTo(this);
            
            // ↑とかなり似ているが、ハンドトラッキングのフラグ条件が逆なすることに注意
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

        private void SubscribeExpressionTrackingFlag()
        {
            _useWebCamExpressionTracking
                .Subscribe(value =>
                {
                    _face.SetBlendShapeOutputActive(value);
                    _handAndFace.SetBlendShapeOutputActive(value);
                    _handAndFaceWithElbow.SetBlendShapeOutputActive(value);
                })
                .AddTo(this);
        }
        
        private void SetupTaskAndWebCamTextureActiveStatus()
        {
            _isFaceTaskRunning
                .CombineLatest(
                    _isHandTaskRunning,
                    _isHandAndFaceTaskRunning,
                    _useElbowTracking,
                    _alwaysUseSingleMediaPipeTask,
                    SelectTrackingTask)
                // 顔/手のフラグが別々に更新されるとき、中間状態ではなく最終状態だけを適用する
                .DebounceFrame(1)
                .DistinctUntilChanged()
                .Subscribe(ApplyTrackingTaskSelection)
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
                // NOTE: 「ハンドトラッキングを停止し、すぐにハンド + 表情トラッキングを開始」のようなケースの場合に最後の状態だけ通すようにしたい
                .DebounceFrame(1)
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
            _handWithElbow.StopTask();
            _handAndFaceWithElbow.StopTask();
            _holistic.StopTask();
        }

        private static TrackingTaskSelection SelectTrackingTask(
            bool runFaceTask,
            bool runHandTask,
            bool runHandAndFaceTask,
            bool useElbowTracking,
            bool alwaysUseSingleTask)
        {
            if (runFaceTask)
            {
                return TrackingTaskSelection.Face;
            }

            if (runHandTask)
            {
                return (useElbowTracking, alwaysUseSingleTask) switch
                {
                    (false, _) => TrackingTaskSelection.Hand,
                    (true, false) => TrackingTaskSelection.HandWithElbow,
                    (true, true) => TrackingTaskSelection.HolisticHandAndElbow,
                };
            }

            if (runHandAndFaceTask)
            {
                return (useElbowTracking, alwaysUseSingleTask) switch
                {
                    (false, false) => TrackingTaskSelection.HandAndFace,
                    (true, false) => TrackingTaskSelection.HandAndFaceWithElbow,
                    (false, true) => TrackingTaskSelection.HolisticFaceAndHand,
                    (true, true) => TrackingTaskSelection.HolisticFaceHandAndElbow,
                };
            }

            return TrackingTaskSelection.None;
        }

        private void ApplyTrackingTaskSelection(TrackingTaskSelection selection)
        {
            // 新しいタスクを起動する前に、現在動いているタスクをすべて止める。
            // SetTaskActiveは同値更新を無視するため、停止済みタスクへの副作用はない。
            _face.SetTaskActive(false);
            _hand.SetTaskActive(false);
            _handAndFace.SetTaskActive(false);
            _handWithElbow.SetTaskActive(false);
            _handAndFaceWithElbow.SetTaskActive(false);
            _holistic.SetTaskActive(false);

            switch (selection)
            {
                case TrackingTaskSelection.None:
                    break;
                case TrackingTaskSelection.Face:
                    _face.SetTaskActive(true);
                    break;
                case TrackingTaskSelection.Hand:
                    _hand.SetTaskActive(true);
                    break;
                case TrackingTaskSelection.HandWithElbow:
                    _handWithElbow.SetTaskActive(true);
                    break;
                case TrackingTaskSelection.HandAndFace:
                    _handAndFace.SetTaskActive(true);
                    break;
                case TrackingTaskSelection.HandAndFaceWithElbow:
                    _handAndFaceWithElbow.SetTaskActive(true);
                    break;
                case TrackingTaskSelection.HolisticHandAndElbow:
                    _holistic.SetTaskActive(true, HolisticTask.HolisticTaskMode.HandAndElbow);
                    break;
                case TrackingTaskSelection.HolisticFaceAndHand:
                    _holistic.SetTaskActive(true, HolisticTask.HolisticTaskMode.FaceAndHand);
                    break;
                case TrackingTaskSelection.HolisticFaceHandAndElbow:
                    _holistic.SetTaskActive(true, HolisticTask.HolisticTaskMode.FaceHandAndElbow);
                    break;
            }
        }

        private enum TrackingTaskSelection
        {
            None,
            Face,
            Hand,
            HandWithElbow,
            HandAndFace,
            HandAndFaceWithElbow,
            HolisticHandAndElbow,
            HolisticFaceAndHand,
            HolisticFaceHandAndElbow,
        }

        private static bool IsAvailableWebCamDevice(string name) => WebCamTexture.devices.Any(d => d.name == name);
    }
}
