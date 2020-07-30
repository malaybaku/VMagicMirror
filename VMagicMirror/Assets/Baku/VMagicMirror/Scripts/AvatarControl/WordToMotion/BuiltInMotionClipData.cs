using System;
using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror
{
    [CreateAssetMenu(fileName = "BuiltInClips", menuName = "Baku/VMagicMirror/Built-in Motion Clip", order = 1)]
    public class BuiltInMotionClipData : ScriptableObject
    {
        public List<BuiltInMotionClipItem> items;
    }

    [Serializable]
    public class BuiltInMotionClipItem
    {
        public string name = "";
        public AnimationClip clip = null;
    }
}


