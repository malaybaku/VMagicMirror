using System;
using UnityEngine;

namespace Baku.VMagicMirror
{
    [Serializable]
    public class CalibrationData
    {
        public float eyeBrowPosition;
        public float eyeOpenHeight;
        public float noseHeight;
        public float eyeFaceYDiff;
        //タテヨコを掛けたピクセル総数にあたる値
        public float faceSize;        
        public Vector2 faceCenter;

        public void SetDefaultValues()
        {
            eyeBrowPosition = 1.43f;
            eyeOpenHeight = 0.06f;
            noseHeight = 0.14f;
            eyeFaceYDiff = 0.0f;
            faceSize = 0.1f;
            faceCenter = Vector2.zero;
        }
    }
}
