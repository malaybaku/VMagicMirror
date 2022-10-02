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
        public Vrm10Instance instance;
        public Vrm10RuntimeExpression FacialExpression => instance.Runtime.Expression;
        [Obsolete("use `FacialExpression` instead")]
        public VRMBlendShapeProxy blendShape;
        public Renderer[] renderers;
    }
}
