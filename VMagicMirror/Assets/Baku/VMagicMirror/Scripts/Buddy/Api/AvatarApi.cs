using NLua;
using UnityEngine;
using UnityEngine.Scripting;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class AvatarApi
    {
        public AvatarBodyParameterApi BodyParameter { get; } = new();
        public AvatarFacialApi Facial { get; } = new();
        public AvatarPoseApi Pose { get; } = new();
    }

    public class AvatarBodyParameterApi
    {
        //TODO: 身長っぽい値とか？
        //あんまピンと来なければ無しにしてもいい
    }
    
    public class AvatarFacialApi
    {
        public string CurrentFacial => "";
        public bool HasClip(string name) => false;
        
    }

    public class AvatarPoseApi
    {
        public bool IsLoaded => false;
        public Vector3 GetBoneGlobalPosition(HumanBodyBones bone) => Vector3.zero;
        public Quaternion GetBoneGlobalRotation(HumanBodyBones bone) => Quaternion.identity;
        public Vector3 GetBoneLocalPosition(HumanBodyBones bone) => Vector3.zero;
        public Quaternion GetBoneLocalRotation(HumanBodyBones bone) => Quaternion.identity;

        public string CurrentMotionName() => "";
    }

    public class AvatarMotionEventApi
    {
        [Preserve]
        public LuaFunction OnKeyboardKeyDown { get; set; }
    }
}
