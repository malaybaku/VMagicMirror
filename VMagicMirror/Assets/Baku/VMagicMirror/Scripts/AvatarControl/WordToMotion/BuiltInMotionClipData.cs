﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror
{
    [CreateAssetMenu(fileName = "BuiltInClips", menuName = "Baku/VMagicMirror/Built-in Motion Clip", order = 1)]
    public class BuiltInMotionClipData : ScriptableObject
    {
        [SerializeField] private RuntimeAnimatorController defaultAnimatorController;
        [SerializeField] private AnimationClip defaultStandingAnimation;
        [SerializeField] private List<BuiltInMotionClipItem> items;

        public RuntimeAnimatorController DefaultAnimatorController => defaultAnimatorController;
        public AnimationClip DefaultStandingAnimation => defaultStandingAnimation;
        public IEnumerable<BuiltInMotionClipItem> Items => items;
    }

    [Serializable]
    public class BuiltInMotionClipItem
    {
        public string name = "";
        public AnimationClip clip = null;
    }
}
