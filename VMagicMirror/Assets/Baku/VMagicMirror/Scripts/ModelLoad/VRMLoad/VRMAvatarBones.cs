using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// アバターのHumanoidボーンをプロパティベースで取得出来るクラス。
    /// 必須ボーンじゃないプロパティはnullになる可能性があることに注意。
    /// </summary>
    public sealed class VRMAvatarBones
    {
        public VRMAvatarBones(Animator animator)
        {
            Hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            LeftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            RightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            LeftLowerLeg = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            RightLowerLeg = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            LeftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            RightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            Spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            Chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            Neck = animator.GetBoneTransform(HumanBodyBones.Neck);
            Head = animator.GetBoneTransform(HumanBodyBones.Head);
            LeftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            RightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
            LeftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            RightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            LeftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            RightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            LeftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            RightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            LeftToes = animator.GetBoneTransform(HumanBodyBones.LeftToes);
            RightToes = animator.GetBoneTransform(HumanBodyBones.RightToes);
            LeftEye = animator.GetBoneTransform(HumanBodyBones.LeftEye);
            RightEye = animator.GetBoneTransform(HumanBodyBones.RightEye);
            LeftThumbProximal = animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal);
            LeftThumbIntermediate = animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate);
            LeftThumbDistal = animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal);
            LeftIndexProximal = animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal);
            LeftIndexIntermediate = animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate);
            LeftIndexDistal = animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal);
            LeftMiddleProximal = animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);
            LeftMiddleIntermediate = animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate);
            LeftMiddleDistal = animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal);
            LeftRingProximal = animator.GetBoneTransform(HumanBodyBones.LeftRingProximal);
            LeftRingIntermediate = animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate);
            LeftRingDistal = animator.GetBoneTransform(HumanBodyBones.LeftRingDistal);
            LeftLittleProximal = animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal);
            LeftLittleIntermediate = animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate);
            LeftLittleDistal = animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal);
            RightThumbProximal = animator.GetBoneTransform(HumanBodyBones.RightThumbProximal);
            RightThumbIntermediate = animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate);
            RightThumbDistal = animator.GetBoneTransform(HumanBodyBones.RightThumbDistal);
            RightIndexProximal = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
            RightIndexIntermediate = animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate);
            RightIndexDistal = animator.GetBoneTransform(HumanBodyBones.RightIndexDistal);
            RightMiddleProximal = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
            RightMiddleIntermediate = animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate);
            RightMiddleDistal = animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal);
            RightRingProximal = animator.GetBoneTransform(HumanBodyBones.RightRingProximal);
            RightRingIntermediate = animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate);
            RightRingDistal = animator.GetBoneTransform(HumanBodyBones.RightRingDistal);
            RightLittleProximal = animator.GetBoneTransform(HumanBodyBones.RightLittleProximal);
            RightLittleIntermediate = animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate);
            RightLittleDistal = animator.GetBoneTransform(HumanBodyBones.RightLittleDistal);
            UpperChest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
        }
        public Transform Hips { get; }
        public Transform LeftUpperLeg { get; }
        public Transform RightUpperLeg { get; }
        public Transform LeftLowerLeg { get; }
        public Transform RightLowerLeg { get; }
        public Transform LeftFoot { get; }
        public Transform RightFoot { get; }
        public Transform Spine { get; }
        public Transform Chest { get; }
        public Transform Neck { get; }
        public Transform Head { get; }
        public Transform LeftShoulder { get; }
        public Transform RightShoulder { get; }
        public Transform LeftUpperArm { get; }
        public Transform RightUpperArm { get; }
        public Transform LeftLowerArm { get; }
        public Transform RightLowerArm { get; }
        public Transform LeftHand { get; }
        public Transform RightHand { get; }
        public Transform LeftToes { get; }
        public Transform RightToes { get; }
        public Transform LeftEye { get; }
        public Transform RightEye { get; }
        public Transform LeftThumbProximal { get; }
        public Transform LeftThumbIntermediate { get; }
        public Transform LeftThumbDistal { get; }
        public Transform LeftIndexProximal { get; }
        public Transform LeftIndexIntermediate { get; }
        public Transform LeftIndexDistal { get; }
        public Transform LeftMiddleProximal { get; }
        public Transform LeftMiddleIntermediate { get; }
        public Transform LeftMiddleDistal { get; }
        public Transform LeftRingProximal { get; }
        public Transform LeftRingIntermediate { get; }
        public Transform LeftRingDistal { get; }
        public Transform LeftLittleProximal { get; }
        public Transform LeftLittleIntermediate { get; }
        public Transform LeftLittleDistal { get; }
        public Transform RightThumbProximal { get; }
        public Transform RightThumbIntermediate { get; }
        public Transform RightThumbDistal { get; }
        public Transform RightIndexProximal { get; }
        public Transform RightIndexIntermediate { get; }
        public Transform RightIndexDistal { get; }
        public Transform RightMiddleProximal { get; }
        public Transform RightMiddleIntermediate { get; }
        public Transform RightMiddleDistal { get; }
        public Transform RightRingProximal { get; }
        public Transform RightRingIntermediate { get; }
        public Transform RightRingDistal { get; }
        public Transform RightLittleProximal { get; }
        public Transform RightLittleIntermediate { get; }
        public Transform RightLittleDistal { get; }
        public Transform UpperChest { get; }
    }
}