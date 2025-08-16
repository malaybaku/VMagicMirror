using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.VMCP
{
    public class VMCPBasedFingerSetter : IInitializable
    {
        private static readonly int FingerIndexStart = (int)HumanBodyBones.LeftThumbProximal;
        private static readonly int FingerIndexEnd = (int)HumanBodyBones.RightLittleDistal + 1;

        private readonly IVRMLoadable _vrmLoadable;
        private readonly Dictionary<string, Transform> _modelFingerBones = new();
        private bool _hasModel;
        
        public VMCPBasedFingerSetter(IVRMLoadable vrmLoadable)
        {
            _vrmLoadable = vrmLoadable;
        }

        public void Initialize()
        {
            _vrmLoadable.VrmLoaded += info =>
            {
                var a = info.animator;
                for (var i = FingerIndexStart; i < FingerIndexEnd; i++)
                {
                    var bone = (HumanBodyBones)i;
                    var t = a.GetBoneTransform(bone);
                    if (t != null)
                    {
                        _modelFingerBones[bone.ToString()] = t;
                    }
                }
                _hasModel = true;
            };

            _vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _modelFingerBones.Clear();
            };
        }

        //NOTE: 比率みたいな値を引数で入れるのも検討してよい
        public void Set(VMCPBasedHumanoid source)
        {
            if (!_hasModel)
            {
                return;
            }

            foreach (var pair in _modelFingerBones)
            {
                pair.Value.localRotation = source.GetLocalRotation(pair.Key);
            }
        }
    }
}
