using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 実際には何もしないような、手検出のインターフェース実装です。
    /// OpenCVforUnityが無い状態でもVMagicMirrorのビルドが可能になるように作られています。
    /// </summary>
    public class EmptyHandAreaDetector : IHandAreaDetector
    {
        private readonly EmptyHandDetectResult _left = new EmptyHandDetectResult();
        private readonly EmptyHandDetectResult _right = new EmptyHandDetectResult();
        
        public IHandDetectResult LeftSideResult => _left;
        public IHandDetectResult RightSideResult => _right;

        public void UpdateHandDetection(FaceDetectionUpdateStatus status)
        {
            //Do nothing.
        }

        public void UpdateHandDetectionWithoutFace()
        {
            //Do nothing.
        }
    }

    /// <summary> 常に検出失敗状態として扱うような、手の検出結果です </summary>
    public class EmptyHandDetectResult : IHandDetectResult
    {
        public bool HasValidHandArea => false;
        public Vector2 HandAreaCenter => Vector2.zero;
        public Vector2 HandAreaSize => Vector2.zero;
        public float HandAreaRotation => 0f;
        public List<Vector2> ConvexDefectVectors { get; } = new List<Vector2>();
    }
}
