using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.VMCP
{
    public class AvatarBoneInitialLocalOffsets : IInitializable
    {
        private static readonly HumanBodyBones[] Bones;

        private readonly IVRMLoadable _vrmLoadable;

        private readonly ReactiveProperty<bool> _hasModel = new ReactiveProperty<bool>();
        public IReadOnlyReactiveProperty<bool> HasModel => _hasModel;

        private readonly Dictionary<HumanBodyBones, Vector3> _initialLocalOffsets
            = new Dictionary<HumanBodyBones, Vector3>();

        static AvatarBoneInitialLocalOffsets()
        {
            Bones = Enum.GetValues(typeof(HumanBodyBones))
                .Cast<HumanBodyBones>()
                .Where(b => b != HumanBodyBones.LastBone)
                .ToArray();
        }

        public AvatarBoneInitialLocalOffsets(IVRMLoadable vrmLoadable)
        {
            _vrmLoadable = vrmLoadable;
        }

        public void Initialize()
        {
            _vrmLoadable.VrmLoaded += info =>
            {
                SetLocalOffsets(info.animator);
                _hasModel.Value = true;
            };

            _vrmLoadable.VrmDisposing += () =>
            {
                _hasModel.Value = false;
                ResetLocalOffsets();
            };

            ResetLocalOffsets();
        }

        private void ResetLocalOffsets()
        {
            foreach (var b in Bones)
            {
                _initialLocalOffsets[b] = Vector3.zero;
            }
        }

        private void SetLocalOffsets(Animator animator)
        {
            foreach (var b in Bones)
            {
                var bone = animator.GetBoneTransform(b);
                if (bone != null)
                {
                    _initialLocalOffsets[b] = bone.localPosition;
                }
                else
                {
                    _initialLocalOffsets[b] = Vector3.zero;
                }
            }
        }
    }
}
