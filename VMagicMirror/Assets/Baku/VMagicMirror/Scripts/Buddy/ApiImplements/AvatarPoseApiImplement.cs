using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    public class AvatarPoseApiImplement
    {
        private readonly BuddySettingsRepository _buddySettingsRepository;
        private readonly IVRMLoadable _vrmLoadable;

        private Animator _animator;
        private readonly Dictionary<HumanBodyBones, Transform> _bones = new();
        private readonly BodyMotionModeController _bodyMotionMode;

        public AvatarPoseApiImplement(
            BuddySettingsRepository buddySettingsRepository,
            IVRMLoadable vrmLoadable,
            BodyMotionModeController bodyMotionMode)
        {
            _buddySettingsRepository = buddySettingsRepository;
            _vrmLoadable = vrmLoadable;
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmUnloaded;
            _bodyMotionMode = bodyMotionMode;
        }

        private bool _isLoaded;
        private bool IsMainAvatarOutputActive =>
            _buddySettingsRepository.MainAvatarOutputActive.Value;

        public bool UseGameInputMotion =>
            IsMainAvatarOutputActive && 
            _bodyMotionMode.MotionMode.Value is BodyMotionMode.GameInputLocomotion;
        public bool UseStandingOnlyMode =>
            IsMainAvatarOutputActive &&
            _bodyMotionMode.MotionMode.Value is BodyMotionMode.StandingOnly;
        
        public Vector3 GetBoneGlobalPosition(HumanBodyBones bone)
        {
            if (!IsMainAvatarOutputActive)
            {
                return Vector3.zero;
            }

            if (!TryGetBone(bone, out var boneTransform))
            {
                return Vector3.zero;
            }
            return boneTransform.position;
        }

        public Quaternion GetBoneGlobalRotation(HumanBodyBones bone)
        {
            if (!IsMainAvatarOutputActive)
            {
                return Quaternion.identity;
            }

            if (!TryGetBone(bone, out var boneTransform))
            {
                return Quaternion.identity;
            }
            return boneTransform.rotation;
        }

        public Vector3 GetBoneLocalPosition(HumanBodyBones bone)
        {
            if (!IsMainAvatarOutputActive)
            {
                return Vector3.zero;
            }

            if (!TryGetBone(bone, out var boneTransform))
            {
                return Vector3.zero;
            }
            return boneTransform.localPosition;
        }

        public Quaternion GetBoneLocalRotation(HumanBodyBones bone)
        {
            if (!IsMainAvatarOutputActive)
            {
                return Quaternion.identity;
            }

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

        public Vector3 GetRootPosition()
        {
            if (!IsMainAvatarOutputActive)
            {
                return Vector3.zero;
            }
            
            if (!_isLoaded)
            {
                return Vector3.zero;
            }
            //TODO: ControlRigの場合もコレで大丈夫だったっけ…というのは確認したほうが無難
            return _animator.transform.position;
        }

        public Quaternion GetRootRotation()
        {
            if (!IsMainAvatarOutputActive)
            {
                return Quaternion.identity;
            }
            
            if (!_isLoaded)
            {
                return Quaternion.identity;
            }
            return _animator.transform.rotation;
        }
    }
}
