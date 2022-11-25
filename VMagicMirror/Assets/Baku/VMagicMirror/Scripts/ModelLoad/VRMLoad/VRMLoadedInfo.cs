using System;
using RootMotion.FinalIK;
using UnityEngine;
using UniVRM10;

namespace Baku.VMagicMirror
{
    [Serializable]
    public struct VrmLoadedInfo
    {
        public CurrentModelVersion modelVersion;
        public Transform vrmRoot;
        public Animator animator;
        public Animator controlRig => animator;
        //NOTE: property細分化してトレーサビリティとってもいいかも、ExpressionSettingsとか
        public Vrm10Instance instance;
        public Vrm10RuntimeExpression RuntimeFacialExpression => instance.Runtime.Expression;
        public FullBodyBipedIK fbbIk;
        //[Obsolete("use `FacialExpression` instead")]
        //public VRMBlendShapeProxy blendShape;
        public Renderer[] renderers;
    }
}
