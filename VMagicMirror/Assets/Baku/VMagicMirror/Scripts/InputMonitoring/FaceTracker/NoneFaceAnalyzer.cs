using UnityEngine;

namespace Baku.VMagicMirror
{
    public class NoneFaceAnalyzer : FaceAnalyzeRoutineBase
    {
        public override IFaceAnalyzeResult Result { get; } = new NoneFaceAnalyzeResult();

        public override void LerpToDefault(float lerpFactor)
        {
        }

        protected override void RunFaceDetection()
        {
        }

        public override void ApplyResult(CalibrationData calibration, bool shouldCalibrate)
        {
        }

        class NoneFaceAnalyzeResult : IFaceAnalyzeResult
        {
            public void LerpToDefault(float lerpFactor)
            {
            }

            public bool HasFaceRect { get; } = false;
            public Rect FaceRect { get; } 

            public Vector2 FacePosition { get; }
            public float ZOffset { get; }
            public float YawRate { get; }
            public float PitchRate { get; }
            public float RollRad { get; }
            
            public bool CanAnalyzeBlink { get; } = false;
            public float LeftBlink { get; }
            public float RightBlink { get; }
        }
    }
}
