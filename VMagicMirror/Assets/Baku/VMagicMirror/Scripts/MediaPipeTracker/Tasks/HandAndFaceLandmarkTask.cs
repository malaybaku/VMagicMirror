using System;
using System.Collections.Generic;
using UnityEngine;
using Mediapipe;
using Mediapipe.Tasks.Core;
using Mediapipe.Tasks.Vision.FaceLandmarker;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public class HandAndFaceLandmarkTask : HandTask
    {
        //TODO: インターレース可能なときにやるかどうか」という設定はGUIに公開したい
        private bool useInterlace = false;
        // TODO: GUIのキャリブレーションボタンの結果を受けてフラグを(このクラスじゃないどこかで)立てたい
        private bool requestCalibration;

        private const string FaceModelFileName = "face_landmarker_v2_with_blendshapes.bytes";

        private FaceLandmarker _landmarker;
        private FaceResultSetter _faceSetter;
        private int _interlaceCount;

        private readonly Dictionary<string, float> _blendShapeValues = new(52);

        [Inject]
        public HandAndFaceLandmarkTask(
            WebCamTextureSource textureSource,
            KinematicSetter kinematicSetter, 
            FacialSetter facialSetter,
            CameraCalibrator calibrator,
            LandmarksVisualizer landmarksVisualizer
        ) : base(textureSource, kinematicSetter, facialSetter, calibrator, landmarksVisualizer)
        {
        }
        
        protected override void OnStartTask()
        {
            base.OnStartTask();

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
            base.OnStopTask();

            ((IDisposable)_landmarker)?.Dispose();
            _landmarker = null;
        }

        protected override void OnWebCamImageUpdated(WebCamImageSource source)
        {
            if (useInterlace)
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
                _faceSetter.ClearBlendShapes();
                KinematicSetter.ClearHeadPose();
                LandmarksVisualizer.Visualizer2D.Clear();
                return;
            }

            // LandmarksVisualizer.Visualizer2D.SetPositions(
            //     result.faceLandmarks[0].landmarks.Select(m => m.ToVector2())
            //     );
            
            // 一度入った BlendShape はPlayMode中に消えない…という前提でこうしてます
            foreach (var c in result.faceBlendshapes[0].categories)
            {
                _blendShapeValues[c.categoryName] = c.score;
            }
            _faceSetter.SetPerfectSyncBlendShapes(_blendShapeValues);

            var matrix = result.facialTransformationMatrixes[0];
            var headPose = MediapipeMathUtil.GetCalibratedFaceLocalPose(matrix, Calibrator.GetCalibrationData());
            KinematicSetter.SetHeadPose6Dof(headPose);

            if (requestCalibration)
            {
                if (Calibrator.TrySetSixDofData(result))
                {
                    Debug.Log("6dof calibration success!");
                    var data = Calibrator.GetCalibrationData();
                    // NOTE: cm単位くらいのはず…という前提でposは0.01倍しちゃう
                    var rawPose = data.CameraLocalPose;
                    var scaledPose = new Pose(rawPose.position * 0.01f, rawPose.rotation);
                    LandmarksVisualizer.SetPose(scaledPose);
                }
                else
                {
                    Debug.Log("6dof calibration failed!");
                }

                // NOTE: ホントはマルチスレッドからアクセスしないほうがヨイが…
                requestCalibration = false;
            }
        }
    }
}