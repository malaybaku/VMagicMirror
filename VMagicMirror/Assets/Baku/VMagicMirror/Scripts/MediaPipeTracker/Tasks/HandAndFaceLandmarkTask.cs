using System;
using System.Collections.Generic;
using Mediapipe;
using Mediapipe.Tasks.Core;
using Mediapipe.Tasks.Vision.FaceLandmarker;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public class HandAndFaceLandmarkTask : HandTask
    {
        private const string FaceModelFileName = "face_landmarker_v2_with_blendshapes.bytes";

        private FaceLandmarker _landmarker;
        private int _interlaceCount;

        private bool _blendShapeOutputActive;
        private bool _isRunningWithBlendShapeOutput;
        private readonly FaceLandmarkResultHandler _resultHandler;

        [Inject]
        public HandAndFaceLandmarkTask(
            MediaPipeTrackerRuntimeSettingsRepository settingsRepository,
            WebCamTextureSource textureSource,
            MediaPipeKinematicSetter mediaPipeKinematicSetter, 
            MediaPipeFacialValueRepository facialValueRepository,
            CameraCalibrator calibrator,
            LandmarksVisualizer landmarksVisualizer,
            MediaPipeFingerPoseCalculator fingerPoseCalculator,
            MediaPipeTrackerStatusPreviewSender previewSender
        ) : base(settingsRepository, textureSource, mediaPipeKinematicSetter, facialValueRepository, calibrator, landmarksVisualizer, fingerPoseCalculator, previewSender)
        {
            _resultHandler = new FaceLandmarkResultHandler(
                textureSource,
                settingsRepository, 
                mediaPipeKinematicSetter, 
                facialValueRepository,
                calibrator,
                previewSender
            );
        }

        public void SetBlendShapeOutputActive(bool active)
        {
            if (_blendShapeOutputActive == active)
            {
                return;
            }

            _blendShapeOutputActive = active;
            RestartTaskIfActive();
        }

        protected override void OnStartTask()
        {
            base.OnStartTask();

            _isRunningWithBlendShapeOutput = _blendShapeOutputActive;
            
            var options = new FaceLandmarkerOptions(
                baseOptions: new BaseOptions(
                    modelAssetPath: FilePathUtil.GetModelFilePath(FaceModelFileName)
                ),
                Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM,
                numFaces: 1,
                outputFaceBlendshapes: _isRunningWithBlendShapeOutput,
                outputFaceTransformationMatrixes: true,
                resultCallback: OnResult
            );
            _landmarker = FaceLandmarker.CreateFromOptions(options);
        }

        protected override void OnStopTask()
        {
            base.OnStopTask();

            ((IDisposable)_landmarker)?.Dispose();
            _landmarker = null;
        }

        protected override void OnWebCamImageUpdated(WebCamImageSource source)
        {
            if (SettingsRepository.UseInterlace.Value)
            {
                // インターレースあり: 1枚ずつ交代でHand / Faceで使う
                _interlaceCount = (_interlaceCount + 1) % 2;
                if (_interlaceCount == 0)
                {
                    base.OnWebCamImageUpdated(source);
                }
                else
                {
                    using var image = source.BuildImage();
                    _landmarker.DetectAsync(image, source.TimestampMilliseconds);
                }
            }
            else
            {
                // インターレース無し: 1つのテクスチャから二重に画像を作って2タスクで使う
                base.OnWebCamImageUpdated(source);

                if (_landmarker != null)
                {
                    using var image = source.BuildImage();
                    // NOTE: マルチスレッドに留意するため、null checkが冗長に入ってる
                    _landmarker?.DetectAsync(image, source.TimestampMilliseconds);
                }
            }
        }

        private void OnResult(FaceLandmarkerResult result, Image image, long timestamp)
        {
            _resultHandler.OnFaceLandmarkResult(result, _isRunningWithBlendShapeOutput);
        }
    }
}