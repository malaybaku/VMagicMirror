using System;
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
            Renderer[] renderers,
            VRMAvatarBones avatarBones
        )
        {
            this.modelVersion = modelVersion;
            this.vrmRoot = vrmRoot;
            this.animator = animator;
            this.instance = instance;
            this.fbbIk = fbbIk;
            this.leftLegIk = leftLegIk;
            this.rightLegIk = rightLegIk;
            this.leftArmTwistRelaxer = leftArmTwistRelaxer;
            this.rightArmTwistRelaxer = rightArmTwistRelaxer;
            this.renderers = renderers;
            AvatarBones = avatarBones;
        }
        
        public Vrm10RuntimeExpression RuntimeFacialExpression => instance.Runtime.Expression;
        public CurrentModelVersion modelVersion { get; }
        public Transform vrmRoot { get; }
        public Animator animator { get; }
        //NOTE: property細分化してトレーサビリティとってもいいかも、ExpressionSettingsとか
        public Vrm10Instance instance { get; }
        public FullBodyBipedIK fbbIk { get; }
        public LimbIK leftLegIk { get; }
        public LimbIK rightLegIk { get; }

        public TwistRelaxer leftArmTwistRelaxer { get; }
        public TwistRelaxer rightArmTwistRelaxer { get; }
        public Renderer[] renderers { get; }

        public VRMAvatarBones AvatarBones { get; }
    }
}
