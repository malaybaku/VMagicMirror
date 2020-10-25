using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class FaceTrackerToEyeOpen 
    {
        //NOTE: 点を貰った瞬間に目の開閉を計算し終えてしまう
        //TODO: 68点向けの処理を消してもよさそう
        public void UpdatePoints(List<Vector2> points)
        {
            float leftEyeHeight = 0f;
            float rightEyeHeight = 0f;
            float noseHeight = 0f;
            
            if (points.Count == 68) {
                leftEyeHeight = new Vector2 ((points [47].x + points [46].x) / 2 - (points [43].x + points [44].x) / 2, (points [47].y + points [46].y) / 2 - (points [43].y + points [44].y) / 2).sqrMagnitude;
                rightEyeHeight = new Vector2 ((points [40].x + points [41].x) / 2 - (points [38].x + points [37].x) / 2, (points [40].y + points [41].y) / 2 - (points [38].y + points [37].y) / 2).sqrMagnitude;
                noseHeight = new Vector2 (points [33].x - (points [39].x + points [42].x) / 2, points [33].y - (points [39].y + points [42].y) / 2).sqrMagnitude;
            } else if (points.Count == 17) {
                leftEyeHeight = new Vector2 (points [12].x - points [11].x, points [12].y - points [11].y).sqrMagnitude;
                rightEyeHeight = new Vector2 (points [10].x - points [9].x, points [10].y - points [9].y).sqrMagnitude;
                noseHeight = new Vector2 (points [1].x - (points [3].x + points [4].x) / 2, points [1].y - (points [3].y + points [4].y) / 2).sqrMagnitude;
            }

            float leftEyeOpenRatio = leftEyeHeight / noseHeight;
            _leftEyeBlink = 1.0f - Mathf.InverseLerp (0.003f, 0.009f, leftEyeOpenRatio);

            float rightEyeOpenRatio = rightEyeHeight / noseHeight;
            _rightEyeBlink = 1.0f - Mathf.InverseLerp (0.003f, 0.009f, rightEyeOpenRatio);
        }

        public bool DisableHorizontalFlip { get; set; }
        public float LeftEyeBlink => DisableHorizontalFlip ? _leftEyeBlink : _rightEyeBlink;
        public float RightEyeBlink => DisableHorizontalFlip ? _rightEyeBlink : _leftEyeBlink;

        private float _leftEyeBlink = 0f;
        private float _rightEyeBlink = 0f;

        public void LerpToDefault(float lerpFactor)
        {
            _leftEyeBlink *= 1.0f - lerpFactor;
            _rightEyeBlink *= 1.0f - lerpFactor;
        }
    }
}
