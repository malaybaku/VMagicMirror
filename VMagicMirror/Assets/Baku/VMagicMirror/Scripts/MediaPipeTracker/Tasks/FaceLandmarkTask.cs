using System;
using System.Collections.Generic;
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
            _previewSender = previewSender;
        }

        private readonly MediaPipeTrackerStatusPreviewSender _previewSender;
        private FaceLandmarker _landmarker;
        private readonly Dictionary<string, float> _blendShapeValues = new(52);
        
        protected override void OnStartTask()
        {
            var options = new FaceLandmarkerOptions(
                baseOptions: new BaseOptions(
                    modelAssetPath: FilePathUtil.GetModelFilePath(FaceModelFileName)
                ),
                Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM,
                numFaces: 1,
                outputFaceBlendshapes: true,
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
            if (result.faceBlendshapes is not { Count: > 0 })
            {
                FacialValueRepository.RequestReset();
                MediaPipeKinematicSetter.ClearHeadPose();
                return;
            }

            // 一度入った BlendShape はPlayMode中に消えない…という前提でこうしてます
            foreach (var c in result.faceBlendshapes[0].categories)
            {
                _blendShapeValues[c.categoryName] = c.score;
            }
            FacialValueRepository.SetValues(_blendShapeValues);

            var eye = FacialValueRepository.BlendShapes.Eye;
            _previewSender.SetBlinkResult(eye.LeftBlink, eye.RightBlink);

            var matrix = result.facialTransformationMatrixes[0];
            var headPose = MediapipeMathUtil.GetCalibratedFaceLocalPose(matrix, Calibrator.GetCalibrationData());
            MediaPipeKinematicSetter.SetHeadPose6Dof(headPose);

            if (SettingsRepository.HasCalibrationRequest)
            {
                _ = Calibrator.TrySetSixDofData(result, WebCamTextureAspect);
            }
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