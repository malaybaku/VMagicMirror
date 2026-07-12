using System;
using System.Threading;
using Mediapipe;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Core;
using Mediapipe.Tasks.Vision.HolisticLandmarker;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    ///  <summary>
    /// webカメラで顔、手、肘のうち2つ以上をトラッキングするとき、内部的に単一のHolisticLandmarkerでそれを実施するクラス。
    /// 実際には次の3パターンのいずれかで起動し、いずれにもハンドトラッキングが含まれる。
    ///
    /// - 顔 + 手
    /// - 手 + 肘
    /// - 顔 + 手 + 肘
    /// </summary>
    public class HolisticTask : MediaPipeTrackerTaskBase
    {
        private const string ModelFileName = "holistic_landmarker.bytes";

        protected MediaPipeTrackerStatusPreviewSender PreviewSender { get; }

        private readonly FaceLandmarkResultHandler _resultHandler;
        private readonly MediaPipeFingerPoseCalculator _fingerPoseCalculator;
        private HolisticLandmarker _landmarker;

        private HolisticTaskMode _taskMode;
        private int _taskVersion;

        public enum HolisticTaskMode
        {
            FaceAndHand,
            HandAndElbow,
            FaceHandAndElbow
        }

        [Inject]
        public HolisticTask(
            MediaPipeTrackerRuntimeSettingsRepository settingsRepository,
            WebCamTextureSource textureSource,
            MediaPipeKinematicSetter mediaPipeKinematicSetter,
            MediaPipeFacialValueRepository facialValueRepository,
            CameraCalibrator calibrator,
            LandmarksVisualizer landmarksVisualizer,
            MediaPipeFingerPoseCalculator fingerPoseCalculator,
            MediaPipeTrackerStatusPreviewSender previewSender
        ) : base(settingsRepository, textureSource, mediaPipeKinematicSetter, facialValueRepository, calibrator, landmarksVisualizer)
        {
            _fingerPoseCalculator = fingerPoseCalculator;
            PreviewSender = previewSender;

            _resultHandler = new FaceLandmarkResultHandler(
                textureSource,
                settingsRepository,
                mediaPipeKinematicSetter,
                facialValueRepository,
                calibrator,
                previewSender
            );
        }

        /// <summary>
        /// トラッキングの種類を指定しつつタスクを動かす。
        /// </summary>
        public void SetTaskActive(bool isActive, HolisticTaskMode taskMode)
        {
            var shouldRestart = IsActive && isActive && _taskMode != taskMode;

            _taskMode = taskMode;
            SetTaskActive(isActive);
            if (shouldRestart)
            {
                RestartTaskIfActive();
            }
        }

        protected override void OnStartTask()
        {
            var taskVersion = Interlocked.Increment(ref _taskVersion);

            void OnResultForCurrentTask(
                in HolisticLandmarkerResult result,
                Image image,
                long timestamp)
                => OnResult(taskVersion, in result);

            var options = new HolisticLandmarkerOptions(
                baseOptions: new BaseOptions(
                    modelAssetPath: FilePathUtil.GetModelFilePath(ModelFileName)
                ),
                Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM,
                minHandLandmarksConfidence: 0.7f,
                minFaceDetectionConfidence: 0.7f,
                minPoseDetectionConfidence: 0.6f,
                // HolisticのBlendShapeはFaceLandmarkerより品質が低いため、意図的に使わない
                outputFaceBlendshapes: false,
                resultCallback: OnResultForCurrentTask
            );
            _landmarker = HolisticLandmarker.CreateFromOptions(options);
        }

        protected override void OnStopTask()
        {
            Interlocked.Increment(ref _taskVersion);
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
            // NOTE: タイミングバグを考慮して、null checkをこっちにも入れる
            _landmarker?.DetectAsync(image, source.TimestampMilliseconds);
        }

        private void OnResult(int taskVersion, in HolisticLandmarkerResult result)
        {
            if (!IsActive || taskVersion != Volatile.Read(ref _taskVersion))
            {
                return;
            }

            switch (_taskMode)
            {
                case HolisticTaskMode.FaceAndHand:
                    OnFaceResult(result);
                    OnHandAndElbowResult(result, false);
                    break;
                case HolisticTaskMode.HandAndElbow:
                    OnHandAndElbowResult(result, true);
                    break;
                case HolisticTaskMode.FaceHandAndElbow:
                    OnFaceResult(result);
                    OnHandAndElbowResult(result, true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnFaceResult(in HolisticLandmarkerResult result)
        {
            _resultHandler.OnFaceLandmarkResult(
                result, WebCamTextureWidth, WebCamTextureHeight);
        }

        private void OnHandAndElbowResult(in HolisticLandmarkerResult result, bool useElbowPose)
        {
            var hasLeftHand =
                result.HasLeftHandResult() &&
                !IsWristPosOnEdgeAndUntracked(result.leftHandLandmarks.landmarks[0], true) &&
                !IsCrossedWristPos(result.leftHandLandmarks.landmarks[0], true);
            var hasRightHand =
                result.HasRightHandResult() &&
                !IsWristPosOnEdgeAndUntracked(result.rightHandLandmarks.landmarks[0], false) &&
                !IsCrossedWristPos(result.rightHandLandmarks.landmarks[0], false);

            // NOTE: Poseの信頼性がないケースは甘めに見て通す: バストアップしか映ってないときにconfidenceが下がる可能性があるので
            if (!hasLeftHand && !hasRightHand)
            {
                MediaPipeKinematicSetter.ClearLeftHandPose();
                MediaPipeKinematicSetter.ClearRightHandPose();
                SetElbowPose(result.poseLandmarks, false, false, false);
                return;
            }

            if (!useElbowPose)
            {
                // NOTE: 肘トラをしない間は明示的に切り続けておく
                MediaPipeKinematicSetter.SetLeftShoulderToElbow(null);
                MediaPipeKinematicSetter.SetRightShoulderToElbow(null);
            }

            var hasPose = result.poseLandmarks.landmarks is { Count: > 0 };

            if (hasLeftHand)
            {
                SetLeftHandPose(result.leftHandLandmarks, result.leftHandWorldLandmarks, _fingerPoseCalculator);
            }
            else
            {
                MediaPipeKinematicSetter.ClearLeftHandPose();
            }

            if (hasRightHand)
            {
                SetRightHandPose(result.rightHandLandmarks, result.rightHandWorldLandmarks, _fingerPoseCalculator);
            }
            else
            {
                MediaPipeKinematicSetter.ClearRightHandPose();
            }

            if (hasLeftHand || hasRightHand)
            {
                PreviewSender.SetHandTrackingResult(result);
            }

            if (useElbowPose)
            {
                SetElbowPose(result.poseLandmarks, hasLeftHand, hasRightHand, hasPose);
            }
        }

        //TODO: 開き具合ではなく幾何的な角度を計算するように直す。
        //(11~16 のindexを使う、という情報を保全したいので一旦commitしてるけど実装はほぼ全修正になる予定)
        private void SetElbowPose(NormalizedLandmarks poseLandmarks, bool hasLeftHand, bool hasRightHand, bool hasPose)
        {
            if (!hasPose)
            {
                MediaPipeKinematicSetter.SetLeftShoulderToElbow(null);
                MediaPipeKinematicSetter.SetRightShoulderToElbow(null);
                return;
            }

            //NOTE: 手自体が検出出来てない場合、肘のRateは0扱いする
            if (hasLeftHand)
            {
                var shoulder = poseLandmarks.landmarks[11].ToTrackingVector2(WebCamTextureAspect);
                var elbow = poseLandmarks.landmarks[13].ToTrackingVector2(WebCamTextureAspect);
                //var wrist = poseLandmarks.landmarks[15].ToTrackingVector2(WebCamTextureAspect);
                MediaPipeKinematicSetter.SetLeftShoulderToElbow(elbow - shoulder);
            }
            else
            {
                MediaPipeKinematicSetter.SetLeftShoulderToElbow(null);
            }

            if (hasRightHand)
            {
                var shoulder = poseLandmarks.landmarks[12].ToTrackingVector2(WebCamTextureAspect);
                var elbow = poseLandmarks.landmarks[14].ToTrackingVector2(WebCamTextureAspect);
                //var wrist = poseLandmarks.landmarks[16].ToTrackingVector2(WebCamTextureAspect);
                MediaPipeKinematicSetter.SetRightShoulderToElbow(elbow - shoulder);
            }
            else
            {
                MediaPipeKinematicSetter.SetRightShoulderToElbow(null);
            }
        }
    }
}
