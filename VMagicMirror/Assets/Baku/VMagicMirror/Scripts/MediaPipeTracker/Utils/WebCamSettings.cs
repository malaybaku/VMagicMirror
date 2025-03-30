using System;
using UnityEngine;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    [CreateAssetMenu(fileName = "WebCamSettings", menuName = "MediaPipe/WebCamSettings")]
    [Serializable]
    public class WebCamSettings : ScriptableObject
    {
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private int fps;

        public int Width => width;
        public int Height => height;
        public int Fps => fps;
    }
}
