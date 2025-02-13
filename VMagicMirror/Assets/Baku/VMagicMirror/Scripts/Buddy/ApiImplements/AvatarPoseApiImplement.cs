using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    // TODO: API呼び出し時の姿勢を見るより毎フレームキャッシュした姿勢を使う方がタイミング依存が減って良さそう
    // ただし、ボーン姿勢をキャッシュする処理もタダではないので、ケチり方を考えてから対策したい
    public class AvatarPoseApiImplement
    {
        private readonly IVRMLoadable _vrmLoadable;

        private Animator _animator;
        private readonly Dictionary<HumanBodyBones, Transform> _bones = new();
        private BodyMotionModeController _bodyMotionMode;

        public AvatarPoseApiImplement(
            IVRMLoadable vrmLoadable,
            BodyMotionModeController bodyMotionMode)
        {
            _vrmLoadable = vrmLoadable;
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmUnloaded;
            _bodyMotionMode = bodyMotionMode;
        }

        private bool _isLoaded;

        public bool UseGameInputMotion => _bodyMotionMode.MotionMode.Value is BodyMotionMode.GameInputLocomotion;
        public bool UseStandingOnlyMode => _bodyMotionMode.MotionMode.Value is BodyMotionMode.StandingOnly;
        
        public Vector3 GetBoneGlobalPosition(HumanBodyBones bone)
        {
            if (!TryGetBone(bone, out var boneTransform))
            {
                return Vector3.zero;
            }
            return boneTransform.position;
        }

        public Quaternion GetBoneGlobalRotation(HumanBodyBones bone)
        {
            if (!TryGetBone(bone, out var boneTransform))
            {
                return Quaternion.identity;
            }
            return boneTransform.rotation;
        }

        public Vector3 GetBoneLocalPosition(HumanBodyBones bone)
        {
            if (!TryGetBone(bone, out var boneTransform))
            {
                return Vector3.zero;
            }
            return boneTransform.localPosition;
        }

        public Quaternion GetBoneLocalRotation(HumanBodyBones bone)
        {
            if (!TryGetBone(bone, out var boneTransform))
            {
                return Quaternion.identity;
            }
            return boneTransform.localRotation;
        }

        private bool TryGetBone(HumanBodyBones bone, out Transform result)
        {
            if (!_isLoaded || !_bones.TryGetValue(bone, out var boneTransform))
            {
                result = null;
                return false;
            }

            result = boneTransform;
            return true;
        }
        
        private void OnVrmUnloaded()
        {
            _isLoaded = false;
            _animator = null;
            _bones.Clear();
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _animator = info.animator;
            for (var i = 0; i < (int)HumanBodyBones.LastBone - 1; i++)
            {
                var bone = (HumanBodyBones)i;
                var boneTransform = _animator.GetBoneTransform(bone);
                if (boneTransform != null)
                {
                    _bones[bone] = boneTransform;
                }
            }
            _isLoaded = true;
        }
    }
}
