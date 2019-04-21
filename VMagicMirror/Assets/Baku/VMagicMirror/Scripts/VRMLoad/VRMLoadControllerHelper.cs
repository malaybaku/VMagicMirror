using System.Linq;
using UnityEngine;
using RootMotion;
using RootMotion.FinalIK;
using VRM;

namespace Baku.VMagicMirror
{
    public static class VRMLoadControllerHelper
    {
        public static void SetupVrm(GameObject go, VRMLoadController.VrmLoadSetting setting)
        {
            var animator = go.GetComponent<Animator>();
            animator.applyRootMotion = false;

            var bipedReferences = LoadReferencesFromVrm(go.transform, animator);
            AddFBBIK(go, setting, bipedReferences);

            var vrmLookAt = go.GetComponent<VRMLookAtHead>();
            vrmLookAt.Target = setting.headTarget;

            AddLookAtIK(go, setting.headTarget, animator, bipedReferences.root);

            var fingerAnimator = go.AddComponent<FingerAnimator>();
            fingerAnimator.Initialize(animator);

            go.AddComponent<VRMBlink>();
            setting.inputToMotion.rightHandBone = animator.GetBoneTransform(HumanBodyBones.RightHand);

            SetupAimIk(go, animator, setting);
        }

        private static void AddFBBIK(GameObject go, VRMLoadController.VrmLoadSetting setting, BipedReferences reference)
        {
            var fbbik = go.AddComponent<FullBodyBipedIK>();
            fbbik.SetReferences(reference, null);

            //IK目標をロードしたVRMのspineに合わせることで、BodyIKがいきなり動いてしまうのを防ぐ。
            //bodyTarget自体は(呼吸ライクに動かすための)別スクリプトが入ってるため、親を移動する
            setting.bodyTarget.parent.position = reference.spine[0].position;
            fbbik.solver.bodyEffector.target = setting.bodyTarget;
            fbbik.solver.bodyEffector.positionWeight = 0.5f;
            //Editorで "FBBIK > Body > Mapping > Maintain Head Rot"を選んだ時の値を↓で入れてる(デフォルト0、ある程度大きくするとLook Atの見栄えがよい)
            fbbik.solver.boneMappings[0].maintainRotationWeight = 0.7f;

            fbbik.solver.leftHandEffector.target = setting.leftHandTarget;
            fbbik.solver.leftHandEffector.positionWeight = 1.0f;
            fbbik.solver.leftHandEffector.rotationWeight = 1.0f;

            fbbik.solver.rightHandEffector.target = setting.rightHandTarget;
            fbbik.solver.rightHandEffector.positionWeight = 1.0f;
            fbbik.solver.rightHandEffector.rotationWeight = 1.0f;
            //small pull: プレゼンモード中にキャラが吹っ飛んでいかないための対策です
            fbbik.solver.rightArmChain.pull = 0.1f;
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

        private static void SetupAimIk(GameObject go, Animator animator, VRMLoadController.VrmLoadSetting setting)
        {
            return;

            var aim = go.AddComponent<AimIK>();
            aim.solver.target = setting.presentationTarget;

            var bones = new Transform[]
            {
                animator.GetBoneTransform(HumanBodyBones.RightIndexProximal),
                animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate),
                animator.GetBoneTransform(HumanBodyBones.RightIndexDistal),
                animator.GetBoneTransform(HumanBodyBones.RightHand),
            };

            aim.solver.transform = bones.FirstOrDefault(b => b != null);
                
        }
    }
}
