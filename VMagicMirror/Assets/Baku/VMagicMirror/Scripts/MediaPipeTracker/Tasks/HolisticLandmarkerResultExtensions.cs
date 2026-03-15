using Mediapipe.Tasks.Vision.HolisticLandmarker;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    static class HolisticLandmarkerResultExtensions
    {
        public static bool HasLeftHandResult(this in HolisticLandmarkerResult result) =>
            result.leftHandLandmarks.landmarks is { Count: > 0 } &&
            result.leftHandWorldLandmarks.landmarks is { Count : > 0 };

        public static bool HasRightHandResult(this in HolisticLandmarkerResult result) =>
            result.rightHandLandmarks.landmarks is { Count: > 0 } &&
            result.rightHandWorldLandmarks.landmarks is { Count : > 0 };
    }
}