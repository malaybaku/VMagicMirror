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

        private readonly Dictionary<string, float> _blendShapeValues = new(52);

        [Inject]
        public HandAndFaceLandmarkTask(
            MediaPipeTrackerRuntimeSettingsRepository settingsRepository,
            WebCamTextureSource textureSource,
            MediaPipeKinematicSetter mediaPipeKinematicSetter, 
            MediaPipeFacialValueRepository facialValueRepository,
            CameraCalibrator calibrator,
            LandmarksVisualizer landmarksVisualizer,
            MediaPipeFingerPoseCalculator fingerPoseCalculator
        ) : base(settingsRepository, textureSource, mediaPipeKinematicSetter, facialValueRepository, calibrator, landmarksVisualizer, fingerPoseCalculator)
        {
        }
        
        protected override void OnStartTask()
        {
            base.OnStartTask();
            
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

                using var image = source.BuildImage();
                _landmarker.DetectAsync(image, source.TimestampMilliseconds);
            }
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

            var matrix = result.facialTransformationMatrixes[0];
            var headPose = MediapipeMathUtil.GetCalibratedFaceLocalPose(matrix, Calibrator.GetCalibrationData());
            MediaPipeKinematicSetter.SetHeadPose6Dof(headPose);

            if (SettingsRepository.HasCalibrationRequest)
            {
                _ = Calibrator.TrySetSixDofData(result, WebCamTextureAspect);
            }
        }
    }
}