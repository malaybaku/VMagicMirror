using System;
using UnityEngine;

namespace Baku.VMagicMirror
{
    [Serializable]
    public class CalibrationData
    {
        /// <summary>低負荷モード時のピッチオフセット </summary>
        public float pitchRateOffset;
        public float dnnPitchRateOffset;
        
        //NOTE: 以下2つはdlibとdnnがほぼ同じ値を返すため、あえて分けない
        public float faceSize;
        public Vector2 faceCenter;

        public void SetDefaultValues()
        {
            pitchRateOffset = 0f;
            dnnPitchRateOffset = 0f;
            
            faceSize = 0.1f;
            faceCenter = Vector2.zero;
        }
    }
}
