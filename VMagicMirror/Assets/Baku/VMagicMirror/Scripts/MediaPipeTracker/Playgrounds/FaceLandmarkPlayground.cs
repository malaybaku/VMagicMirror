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
    public class FaceLandmarkPlayground : MediaPipeTrackerTaskBase
    {
        private const string FaceModelFileName = "face_landmarker_v2_with_blendshapes.bytes";

        [Inject]
        public FaceLandmarkPlayground(
            WebCamTextureSource textureSource,
            KinematicSetter kinematicSetter, 
            FacialSetter facialSetter,
            CameraCalibrator calibrator,
            LandmarksVisualizer landmarksVisualizer
        ) : base(textureSource, kinematicSetter, facialSetter, calibrator, landmarksVisualizer)
        {
        }

        private FaceLandmarker _landmarker;
        private FaceResultSetter _faceSetter;
        private int _interlaceCount;

        private readonly Dictionary<string, float> _blendShapeValues = new(52);
        
        protected override void OnStartTask()
        {
            _faceSetter ??= new FaceResultSetter(KinematicSetter, FacialSetter);
            
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
            using var image = source.BuildImage();
            _landmarker.DetectAsync(image, source.TimestampMilliseconds);
        }

        private void OnResult(FaceLandmarkerResult result, Image image, long timestamp)
        {
            if (result.faceBlendshapes is not { Count: > 0 })
            {
                _faceSetter.ClearBlendShapes();
                KinematicSetter.ClearHeadPose();
                
                // LandmarksVisualizer.ClearPositions();
                // LandmarksVisualizer.UnsetSingleMatrix();
                // LandmarksVisualizer.Visualizer2D.Clear();
                return;
            }

            // 一度入った BlendShape はPlayMode中に消えない…という前提でこうしてます
            foreach (var c in result.faceBlendshapes[0].categories)
            {
                _blendShapeValues[c.categoryName] = c.score;
            }
            _faceSetter.SetPerfectSyncBlendShapes(_blendShapeValues);

            var matrix = result.facialTransformationMatrixes[0];
            var headPose = MediapipeMathUtil.GetCalibratedFaceLocalPose(matrix, Calibrator.GetCalibrationData());
            KinematicSetter.SetHeadPose6Dof(headPose);

            // VisualizeAllFaceLandmark2D(result);
            // VisualizeAllFaceLandmark3D(result);
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