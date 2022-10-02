using System;
using UnityEngine;
using UniVRM10;
using VRM;

namespace Baku.VMagicMirror
{
    [Serializable]
    public struct VrmLoadedInfo
    {
        public CurrentModelVersion modelVersion;
        public Transform vrmRoot;
        public Animator animator;
        //NOTE: property細分化してトレーサビリティとってもいいかも、ExpressionSettingsとか
        public Vrm10Instance instance;
        public Vrm10RuntimeExpression RuntimeFacialExpression => instance.Runtime.Expression;
        [Obsolete("use `FacialExpression` instead")]
        public VRMBlendShapeProxy blendShape;
        public Renderer[] renderers;
    }
}
