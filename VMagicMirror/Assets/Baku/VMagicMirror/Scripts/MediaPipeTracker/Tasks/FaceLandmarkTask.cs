using System;
using System.Linq;
using UnityEngine;
using Mediapipe;
using Mediapipe.Tasks.Core;
using Mediapipe.Tasks.Vision.FaceLandmarker;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public class FaceLandmarkTask : MediaPipeTrackerTaskBase
    {
        private const string FaceModelFileName = "face_landmarker_v2_with_blendshapes.bytes";

        [Inject]
        public FaceLandmarkTask(
            MediaPipeTrackerRuntimeSettingsRepository settingsRepository,
            WebCamTextureSource textureSource,
            MediaPipeKinematicSetter mediaPipeKinematicSetter, 
            MediaPipeFacialValueRepository facialValueRepository,
            CameraCalibrator calibrator,
            LandmarksVisualizer landmarksVisualizer,
            MediaPipeTrackerStatusPreviewSender previewSender
        ) : base(settingsRepository, textureSource, mediaPipeKinematicSetter, facialValueRepository, calibrator, landmarksVisualizer)
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

        private readonly FaceLandmarkResultHandler _resultHandler;
        private FaceLandmarker _landmarker;

        private bool _blendShapeOutputActive;
        private bool _isRunningWithBlendShapeOutput;

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
            ((IDisposable)_landmarker)?.Dispose();
            _landmarker = null;
        }

        protected override void OnWebCamImageUpdated(WebCamImageSource source)
        {
            if (_landmarker == null)
            {
                return;
            }

            using var image = source.BuildImage();
            // NOTE: マルチスレッド対策でこっちにもnull checkを入れておく
            _landmarker?.DetectAsync(image, source.TimestampMilliseconds);
        }

        private void OnResult(FaceLandmarkerResult result, Image image, long timestamp)
        {
            _resultHandler.OnFaceLandmarkResult(result, _isRunningWithBlendShapeOutput);
        }

        private void VisualizeAllFaceLandmark2D(FaceLandmarkerResult result)
        {
            LandmarksVisualizer.Visualizer2D.SetPositions(
                result.faceLandmarks[0].landmarks.Select(m => new Vector2(m.x, m.y))
                );
        }

        // NOTE: Matrixをどう扱うといい感じになるかの検証するためにビジュアライズしてみるやつです
        private void VisualizeAllFaceLandmark3D(FaceLandmarkerResult result)
        {
            var matrix = result.facialTransformationMatrixes[0];
            LandmarksVisualizer.SetPositions(
                result.faceLandmarks[0].landmarks.Select(m =>
                {
                    var point = new Vector3(m.x, m.y, m.z);
                    return matrix.MultiplyPoint3x4(point);
                }));
            LandmarksVisualizer.SetSingleMatrix(matrix);
        }
    }
}