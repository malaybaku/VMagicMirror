using RootMotion.FinalIK;
using UnityEngine;
using UniVRM10;

namespace Baku.VMagicMirror
{
    public readonly struct VrmLoadedInfo
    {
        public VrmLoadedInfo(
            CurrentModelVersion modelVersion,
            Transform vrmRoot,
            Animator animator,
            Vrm10Instance instance,
            FullBodyBipedIK fbbIk,
            LimbIK leftLegIk,
            LimbIK rightLegIk,
            TwistRelaxer leftArmTwistRelaxer,
            TwistRelaxer rightArmTwistRelaxer,
            Renderer[] renderers
        )
        {
            ModelVersion = modelVersion;
            VrmRoot = vrmRoot;
            Animator = animator;
            Instance = instance;
            FbbIk = fbbIk;
            LeftLegIk = leftLegIk;
            RightLegIk = rightLegIk;
            LeftArmTwistRelaxer = leftArmTwistRelaxer;
            RightArmTwistRelaxer = rightArmTwistRelaxer;
            Renderers = renderers;
            AvatarBones = new VRMAvatarBones(animator);
        }
        
        public CurrentModelVersion ModelVersion { get; }
        public Transform VrmRoot { get; }
        public Animator Animator { get; }
        public VRMAvatarBones AvatarBones { get; }
        //NOTE: property細分化してトレーサビリティとってもいいかも、ExpressionSettingsとか
        public Vrm10Instance Instance { get; }
        public FullBodyBipedIK FbbIk { get; }
        public LimbIK LeftLegIk { get; }
        public LimbIK RightLegIk { get; }
        public TwistRelaxer LeftArmTwistRelaxer { get; }
        public TwistRelaxer RightArmTwistRelaxer { get; }
        public Renderer[] Renderers { get; }

        public Vrm10RuntimeExpression RuntimeFacialExpression => Instance.Runtime.Expression;
    }
}
