using System;
using System.Linq;
using Baku.VMagicMirror.IK;
using UnityEngine;
using RootMotion;
using RootMotion.FinalIK;
using UniVRM10;

namespace Baku.VMagicMirror
{
    /// <summary> <see cref="VRMLoadControllerHelper"/>の後継版で、VRM1.0形式に対応したもの </summary>
    public static class VRM10LoadControllerHelper
    {
        public static void SetupVrm(Vrm10Instance instance, IKTargetTransforms ikTargets)
        {
            
            var animator = instance.GetComponent<Animator>();
            animator.applyRootMotion = false;

            var controlRig = instance.Runtime.ControlRig;
            var controlRigRoot = controlRig.GetBoneTransform(HumanBodyBones.Hips).parent;
            var bipedReferences = LoadReferencesFromVrm(controlRig, controlRigRoot);
            _ = AddFBBIK(controlRig.GetBoneTransform(HumanBodyBones.Hips).parent.gameObject, ikTargets, bipedReferences);
            
            // TODO: この辺でTwistRelaxerのセットアップコードを入れたい

            //NOTE: 要するに勝手にLookAt結果を代入しなければいい、という話.
            //VRM0ではCurveMapを勝手にいじってたが、これはモデルを尊重してない行為だと思うので廃止
            instance.LookAtTargetType = VRM10ObjectLookAt.LookAtTargetTypes.CalcYawPitchToGaze;
            instance.Gaze = ikTargets.LookAt;
            
            AddLookAtIK(controlRigRoot.gameObject, ikTargets.LookAt, controlRig, bipedReferences.root);
            AddFingerRigToRightIndex(controlRig, ikTargets);
        }

        private static FullBodyBipedIK AddFBBIK(GameObject go, IKTargetTransforms ikTargets, BipedReferences reference)
        {
            var fbbik = go.AddComponent<FullBodyBipedIK>();
            fbbik.SetReferences(reference, null);

            //IK目標をロードしたVRMのspineに合わせることで、BodyIKがいきなり動いてしまうのを防ぐ。
            //bodyTargetは実際には多階層なので当て方に注意
            ikTargets.Body.position = reference.spine[0].position;
            fbbik.solver.bodyEffector.target = ikTargets.Body;
            fbbik.solver.bodyEffector.positionWeight = 0.5f;
            //Editorで "FBBIK > Body > Mapping > Maintain Head Rot"を選んだ時の値を↓で入れてる(デフォルト0、ある程度大きくするとLook Atの見栄えがよい)
            fbbik.solver.boneMappings[0].maintainRotationWeight = 0.7f;

            fbbik.solver.leftHandEffector.target = ikTargets.LeftHand;
            fbbik.solver.leftHandEffector.positionWeight = 1.0f;
            fbbik.solver.leftHandEffector.rotationWeight = 1.0f;

            fbbik.solver.rightHandEffector.target = ikTargets.RightHand;
            fbbik.solver.rightHandEffector.positionWeight = 1.0f;
            fbbik.solver.rightHandEffector.rotationWeight = 1.0f;

            //右手のpullを小さくするのはプレゼンモード中にキャラが吹っ飛んでいかないための対策。
            //右だけやるとバランス的におかしいので、左も揃える。
            //※「0でも良いのでは？」という説も近年出ている
            fbbik.solver.rightArmChain.pull = 0.1f;
            fbbik.solver.leftArmChain.pull = 0.1f;

            //NOTE: 足についてはSimpleAnimationを使うとO脚みたくなっちゃうので、それを防ぐために入れる。
            //(pull = 0相当にしたいんだけどそういうの無い…？)
            fbbik.solver.leftFootEffector.target = ikTargets.LeftFoot;
            fbbik.solver.leftFootEffector.positionWeight = 1f;
            fbbik.solver.leftFootEffector.rotationWeight = 0f;
            fbbik.solver.leftLegChain.pull = 0f;
            
            fbbik.solver.rightFootEffector.target = ikTargets.RightFoot;
            fbbik.solver.rightFootEffector.positionWeight = 1f;
            fbbik.solver.rightFootEffector.rotationWeight = 0f;
            fbbik.solver.rightLegChain.pull = 0f;

            fbbik.solver.SetLimbOrientations(new BipedLimbOrientations(
                new BipedLimbOrientations.LimbOrientation(Vector3.forward, Vector3.forward, Vector3.up),
                new BipedLimbOrientations.LimbOrientation(Vector3.forward, Vector3.forward, Vector3.down),
                new BipedLimbOrientations.LimbOrientation(Vector3.forward, Vector3.forward, Vector3.left),
                new BipedLimbOrientations.LimbOrientation(Vector3.forward, Vector3.forward, Vector3.left)
            ));

            return fbbik;
        }

        private static void AddLookAtIK(GameObject go, Transform headTarget, Vrm10RuntimeControlRig controlRig, Transform referenceRoot)
        {
            var lookAtIk = go.AddComponent<LookAtIK>();
            lookAtIk.solver.SetChain(
                new[]
                {
                    controlRig.GetBoneTransform(HumanBodyBones.Spine),
                    controlRig.GetBoneTransform(HumanBodyBones.Chest),
                    controlRig.GetBoneTransform(HumanBodyBones.UpperChest),
                    controlRig.GetBoneTransform(HumanBodyBones.Neck),
                }
                    .Where(t => t != null)
                    .ToArray(),
                controlRig.GetBoneTransform(HumanBodyBones.Head),
                Array.Empty<Transform>(),
                referenceRoot
                );

            lookAtIk.solver.target = headTarget;
            lookAtIk.solver.bodyWeight = 0.2f;
            lookAtIk.solver.headWeight = 0.5f;
        }
        
        private static BipedReferences LoadReferencesFromVrm(Vrm10RuntimeControlRig controlRig, Transform root)
        {
            return new BipedReferences()
            {
                root = root,
                pelvis = controlRig.GetBoneTransform(HumanBodyBones.Hips),

                leftThigh = controlRig.GetBoneTransform(HumanBodyBones.LeftUpperLeg),
                leftCalf = controlRig.GetBoneTransform(HumanBodyBones.LeftLowerLeg),
                leftFoot = controlRig.GetBoneTransform(HumanBodyBones.LeftFoot),

                rightThigh = controlRig.GetBoneTransform(HumanBodyBones.RightUpperLeg),
                rightCalf = controlRig.GetBoneTransform(HumanBodyBones.RightLowerLeg),
                rightFoot = controlRig.GetBoneTransform(HumanBodyBones.RightFoot),

                leftUpperArm = controlRig.GetBoneTransform(HumanBodyBones.LeftUpperArm),
                leftForearm = controlRig.GetBoneTransform(HumanBodyBones.LeftLowerArm),
                leftHand = controlRig.GetBoneTransform(HumanBodyBones.LeftHand),

                rightUpperArm = controlRig.GetBoneTransform(HumanBodyBones.RightUpperArm),
                rightForearm = controlRig.GetBoneTransform(HumanBodyBones.RightLowerArm),
                rightHand = controlRig.GetBoneTransform(HumanBodyBones.RightHand),

                head = controlRig.GetBoneTransform(HumanBodyBones.Head),

                spine = new []
                {
                    controlRig.GetBoneTransform(HumanBodyBones.Spine),
                },

                eyes = Array.Empty<Transform>(),
            };
        }

        private static void AddFingerRigToRightIndex(Vrm10RuntimeControlRig controlRig, IKTargetTransforms ikTargets)
        {
            //NOTE: FinalIKのサンプルにあるFingerRigを持ち込んでみる。
            var rightHand = controlRig.GetBoneTransform(HumanBodyBones.RightHand);

            var fingerRig = rightHand.gameObject.AddComponent<FingerRig>();
            fingerRig.AddFinger(
                controlRig.GetBoneTransform(HumanBodyBones.RightIndexProximal),
                controlRig.GetBoneTransform(HumanBodyBones.RightIndexIntermediate),
                controlRig.GetBoneTransform(HumanBodyBones.RightIndexDistal),
                ikTargets.RightIndex
                );
            //とりあえず無効化し、有効にするのはInputDeviceToMotionの責務にする
            fingerRig.weight = 0.0f;
            fingerRig.fingers[0].rotationDOF = Finger.DOF.One;
            fingerRig.fingers[0].weight = 1.0f;
            fingerRig.fingers[0].rotationWeight = 0;
        }
    }
}
