using System.Collections;
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
        public bool HasValidHandArea => false;
        public Vector2 HandAreaCenter => Vector2.zero;
        public Vector2 HandAreaSize => Vector2.zero;
        public float HandAreaRotation => 0f;
        public List<Vector2> ConvexDefectVectors { get; } = new List<Vector2>();
        
        public void UpdateHandDetection(Color32[] colors, int width, int height, Rect faceRect)
        {
            //Do nothing.
        }

        public void UpdateHandDetectionWithoutFace()
        {
            //Do nothing.
        }
    }
}
