using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public static class AnimatorExtensions
    {
        public static Transform GetBoneTransformAscending(this Animator animator, HumanBodyBones bone)
        {
            var result = animator.GetBoneTransform(bone);
            while (result == null)
            {
                // NOTE: 親方向に遡っていけば必ず必須ボーンにぶつかるので、この走査が終わらないことはない
                bone = _boneParentMap[bone];
                result = animator.GetBoneTransform(bone);
            }
            return result;
        }

        private static readonly Dictionary<HumanBodyBones, HumanBodyBones> _boneParentMap = new()
        {
            // Lower Body
            [HumanBodyBones.LeftUpperLeg] = HumanBodyBones.Hips,
            [HumanBodyBones.RightUpperLeg] = HumanBodyBones.Hips,
            [HumanBodyBones.LeftLowerLeg] = HumanBodyBones.LeftUpperLeg,
            [HumanBodyBones.RightLowerLeg] = HumanBodyBones.RightUpperLeg,
            [HumanBodyBones.LeftFoot] = HumanBodyBones.LeftLowerLeg,
            [HumanBodyBones.RightFoot] = HumanBodyBones.RightLowerLeg,
            [HumanBodyBones.LeftToes] = HumanBodyBones.LeftFoot,
            [HumanBodyBones.RightToes] = HumanBodyBones.RightFoot,

            // Spine & Torso
            [HumanBodyBones.Spine] = HumanBodyBones.Hips,
            [HumanBodyBones.Chest] = HumanBodyBones.Spine,
            [HumanBodyBones.UpperChest] = HumanBodyBones.Chest,
            [HumanBodyBones.Neck] = HumanBodyBones.UpperChest,
            [HumanBodyBones.Head] = HumanBodyBones.Neck,

            // Upper Body - Shoulders & Arms
            [HumanBodyBones.LeftShoulder] = HumanBodyBones.Chest,
            [HumanBodyBones.RightShoulder] = HumanBodyBones.Chest,
            [HumanBodyBones.LeftUpperArm] = HumanBodyBones.LeftShoulder,
            [HumanBodyBones.RightUpperArm] = HumanBodyBones.RightShoulder,
            [HumanBodyBones.LeftLowerArm] = HumanBodyBones.LeftUpperArm,
            [HumanBodyBones.RightLowerArm] = HumanBodyBones.RightUpperArm,
            [HumanBodyBones.LeftHand] = HumanBodyBones.LeftLowerArm,
            [HumanBodyBones.RightHand] = HumanBodyBones.RightLowerArm,

            // Head Details
            [HumanBodyBones.LeftEye] = HumanBodyBones.Head,
            [HumanBodyBones.RightEye] = HumanBodyBones.Head,
            [HumanBodyBones.Jaw] = HumanBodyBones.Head,

            // Left Hand Fingers
            [HumanBodyBones.LeftThumbProximal] = HumanBodyBones.LeftHand,
            [HumanBodyBones.LeftThumbIntermediate] = HumanBodyBones.LeftThumbProximal,
            [HumanBodyBones.LeftThumbDistal] = HumanBodyBones.LeftThumbIntermediate,
            [HumanBodyBones.LeftIndexProximal] = HumanBodyBones.LeftHand,
            [HumanBodyBones.LeftIndexIntermediate] = HumanBodyBones.LeftIndexProximal,
            [HumanBodyBones.LeftIndexDistal] = HumanBodyBones.LeftIndexIntermediate,
            [HumanBodyBones.LeftMiddleProximal] = HumanBodyBones.LeftHand,
            [HumanBodyBones.LeftMiddleIntermediate] = HumanBodyBones.LeftMiddleProximal,
            [HumanBodyBones.LeftMiddleDistal] = HumanBodyBones.LeftMiddleIntermediate,
            [HumanBodyBones.LeftRingProximal] = HumanBodyBones.LeftHand,
            [HumanBodyBones.LeftRingIntermediate] = HumanBodyBones.LeftRingProximal,
            [HumanBodyBones.LeftRingDistal] = HumanBodyBones.LeftRingIntermediate,
            [HumanBodyBones.LeftLittleProximal] = HumanBodyBones.LeftHand,
            [HumanBodyBones.LeftLittleIntermediate] = HumanBodyBones.LeftLittleProximal,
            [HumanBodyBones.LeftLittleDistal] = HumanBodyBones.LeftLittleIntermediate,

            // Right Hand Fingers
            [HumanBodyBones.RightThumbProximal] = HumanBodyBones.RightHand,
            [HumanBodyBones.RightThumbIntermediate] = HumanBodyBones.RightThumbProximal,
            [HumanBodyBones.RightThumbDistal] = HumanBodyBones.RightThumbIntermediate,
            [HumanBodyBones.RightIndexProximal] = HumanBodyBones.RightHand,
            [HumanBodyBones.RightIndexIntermediate] = HumanBodyBones.RightIndexProximal,
            [HumanBodyBones.RightIndexDistal] = HumanBodyBones.RightIndexIntermediate,
            [HumanBodyBones.RightMiddleProximal] = HumanBodyBones.RightHand,
            [HumanBodyBones.RightMiddleIntermediate] = HumanBodyBones.RightMiddleProximal,
            [HumanBodyBones.RightMiddleDistal] = HumanBodyBones.RightMiddleIntermediate,
            [HumanBodyBones.RightRingProximal] = HumanBodyBones.RightHand,
            [HumanBodyBones.RightRingIntermediate] = HumanBodyBones.RightRingProximal,
            [HumanBodyBones.RightRingDistal] = HumanBodyBones.RightRingIntermediate,
            [HumanBodyBones.RightLittleProximal] = HumanBodyBones.RightHand,
            [HumanBodyBones.RightLittleIntermediate] = HumanBodyBones.RightLittleProximal,
            [HumanBodyBones.RightLittleDistal] = HumanBodyBones.RightLittleIntermediate,
        };
    }
}
