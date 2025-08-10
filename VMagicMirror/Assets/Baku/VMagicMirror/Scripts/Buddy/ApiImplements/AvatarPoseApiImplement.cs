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
        // NOTE: 全ボーンではなく、_bonesに入ってないキーの分だけが入る
        private readonly Dictionary<HumanBodyBones, Transform> _bonesAscending = new();
        
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
        private bool InteractionApiEnabled =>
            _buddySettingsRepository.InteractionApiEnabled.CurrentValue;

        public bool UseGameInputMotion =>
            InteractionApiEnabled && 
            _bodyMotionMode.MotionMode.CurrentValue is BodyMotionMode.GameInputLocomotion;

        public bool UseStandingOnlyMode =>
            InteractionApiEnabled &&
            _bodyMotionMode.MotionMode.CurrentValue is BodyMotionMode.StandingOnly;
        
        public bool HasBone(HumanBodyBones bone)
        {
            return 
                InteractionApiEnabled &&
                _isLoaded &&
                _bones.ContainsKey(bone);
        }
        
        public Vector3 GetBoneGlobalPosition(HumanBodyBones bone, bool useParentBone)
        {
            if (!InteractionApiEnabled)
            {
                return Vector3.zero;
            }

            if (!TryGetBone(bone, useParentBone, out var boneTransform))
            {
                return Vector3.zero;
            }
            return boneTransform.position;
        }

        public Quaternion GetBoneGlobalRotation(HumanBodyBones bone, bool useParentBone)
        {
            if (!InteractionApiEnabled)
            {
                return Quaternion.identity;
            }

            if (!TryGetBone(bone, useParentBone, out var boneTransform))
            {
                return Quaternion.identity;
            }
            return boneTransform.rotation;
        }

        public Vector3 GetBoneLocalPosition(HumanBodyBones bone, bool useParentBone)
        {
            if (!InteractionApiEnabled)
            {
                return Vector3.zero;
            }

            if (!TryGetBone(bone, useParentBone, out var boneTransform))
            {
                return Vector3.zero;
            }
            return boneTransform.localPosition;
        }

        public Quaternion GetBoneLocalRotation(HumanBodyBones bone, bool useParentBone)
        {
            if (!InteractionApiEnabled)
            {
                return Quaternion.identity;
            }

            if (!TryGetBone(bone, useParentBone, out var boneTransform))
            {
                return Quaternion.identity;
            }
            return boneTransform.localRotation;
        }

        private bool TryGetBone(HumanBodyBones bone, bool useParentBone, out Transform result)
        {
            if (!_isLoaded)
            {
                result = null;
                return false;
            }

            if (_bones.TryGetValue(bone, out var boneTransform))
            {
                result = boneTransform;
                return true;
            }

            if (useParentBone && _bonesAscending.TryGetValue(bone, out var ascendBoneTransform))
            {
                result = ascendBoneTransform;
                return true;
            }

            result = null;
            return false;
        }
        
        private void OnVrmUnloaded()
        {
            _isLoaded = false;
            _animator = null;
            _bones.Clear();
            _bonesAscending.Clear();
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _animator = info.animator;
            for (var i = 0; i < (int)HumanBodyBones.LastBone - 1; i++)
            {
                var bone = (HumanBodyBones)i;
                var boneTransform = _animator.GetBoneTransform(bone);
                if (boneTransform == null)
                {
                    _bonesAscending[bone] = _animator.GetBoneTransformAscending(bone);
                }
                else
                {
                    _bones[bone] = boneTransform;
                }
            }
            _isLoaded = true;
        }

        public Vector3 GetRootPosition()
        {
            if (!InteractionApiEnabled)
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
            if (!InteractionApiEnabled)
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
