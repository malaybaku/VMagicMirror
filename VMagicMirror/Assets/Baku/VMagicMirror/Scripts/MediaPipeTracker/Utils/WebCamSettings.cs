using System;
using UnityEngine;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    [CreateAssetMenu(fileName = "WebCamSettings", menuName = "MPP4U/WebCamSettings")]
    [Serializable]
    public class WebCamSettings : ScriptableObject
    {
        [SerializeField] private string preferredName = "";
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private int fps;

        public string PreferredName => preferredName;
        public int Width => width;
        public int Height => height;
        public int Fps => fps;
    }
}
