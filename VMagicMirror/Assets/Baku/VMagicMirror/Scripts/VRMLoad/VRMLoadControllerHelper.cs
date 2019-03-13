using RootMotion;
using RootMotion.FinalIK;
using System.Linq;
using UnityEngine;
using VRM;

namespace Baku.VMagicMirror
{
    public static class VRMLoadControllerHelper
    {
        public static void SetupVrm(GameObject go, VrmLoadSetting setting)
        {
            var animator = go.GetComponent<Animator>();
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = setting.runtimeAnimatorController;

            var bipedReferences = LoadReferencesFromVrm(go.transform, animator);

            AddFBBIK(go, setting, bipedReferences);

            var vrmLookAt = go.GetComponent<VRMLookAtHead>();
            vrmLookAt.Target = setting.headTarget;

            AddLookAtIK(go, setting.headTarget, animator, bipedReferences.root);
            AddHorizontalHand(go, animator);
            AddFingerAnimator(go, animator);

            go.AddComponent<VRMBlink>();
            setting.inputToMotion.rightHandBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
        }

        private static void AddFBBIK(GameObject go, VrmLoadSetting setting, BipedReferences reference)
        {
            var fbbik = go.AddComponent<FullBodyBipedIK>();
            fbbik.SetReferences(reference, null);

            fbbik.solver.leftHandEffector.target = setting.leftHandTarget;
            fbbik.solver.leftHandEffector.positionWeight = 1.0f;

            fbbik.solver.rightHandEffector.target = setting.rightHandTarget;
            fbbik.solver.rightHandEffector.positionWeight = 1.0f;
        }

        private static void AddLookAtIK(GameObject go, Transform headTarget, Animator animator, Transform referenceRoot)
        {
            var lookAtIk = go.AddComponent<LookAtIK>();
            lookAtIk.solver.SetChain(
                new Transform[]
                {
                    animator.GetBoneTransform(HumanBodyBones.Hips),
                    animator.GetBoneTransform(HumanBodyBones.Spine),
                    animator.GetBoneTransform(HumanBodyBones.UpperChest),
                    animator.GetBoneTransform(HumanBodyBones.Neck),
                }
                    .Where(t => t != null)
                    .ToArray(),
                animator.GetBoneTransform(HumanBodyBones.Head),
                new Transform[0],
                referenceRoot
                );

            lookAtIk.solver.target = headTarget;
            lookAtIk.solver.headWeight = 0.7f;
        }

        private static void AddHorizontalHand(GameObject go, Animator animator)
        {
            var horizontalHand = go.AddComponent<HorizontalHand>();
            horizontalHand.leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            horizontalHand.rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            horizontalHand.animator = animator;
        }

        private static void AddFingerAnimator(GameObject go, Animator animator)
        {
            var fingerAnimator = go.AddComponent<FingerAnimator>();
            fingerAnimator.Initialize(animator);
        }

        private static BipedReferences LoadReferencesFromVrm(Transform root, Animator animator)
        {
            return new BipedReferences()
            {
                root = root,
                pelvis = animator.GetBoneTransform(HumanBodyBones.Hips),

                leftThigh = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg),
                leftCalf = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg),
                leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot),

                rightThigh = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg),
                rightCalf = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg),
                rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot),

                leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm),
                leftForearm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm),
                leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand),

                rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm),
                rightForearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm),
                rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand),

                head = animator.GetBoneTransform(HumanBodyBones.Head),

                spine = new Transform[]
                {
                    animator.GetBoneTransform(HumanBodyBones.Spine),
                },

                eyes = new Transform[0],
            };
        }

        public struct VrmLoadSetting
        {
            public RuntimeAnimatorController runtimeAnimatorController;
            public Transform leftHandTarget;
            public Transform rightHandTarget;
            public Transform headTarget;
            public InputDeviceToMotion inputToMotion;
        }
    }
}
