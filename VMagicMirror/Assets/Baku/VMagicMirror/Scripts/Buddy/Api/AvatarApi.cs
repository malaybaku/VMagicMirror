using NLua;
using UnityEngine;
using UnityEngine.Scripting;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class AvatarLoadEventApi
    {
        [Preserve] public LuaFunction Loaded { get; set; }
        [Preserve] public LuaFunction UnLoaded { get; set; }
    }
    
    public class AvatarBodyParameterApi
    {
        //TODO: 身長っぽい値とか？
        //あんまピンと来なければ無しにしてもいい
    }
    
    public class AvatarFacialApi
    {
        private readonly AvatarFacialApiImplement _impl;
        public AvatarFacialApi(AvatarFacialApiImplement impl)
        {
            _impl = impl;
        }

        [Preserve] public string CurrentFacial => _impl.GetUserOperationActiveBlendShape()?.ToString() ?? "";
        [Preserve] public bool IsTalking => _impl.IsTalking.Value;
        [Preserve] public bool UsePerfectSync => _impl.UsePerfectSync;
        [Preserve] public LuaFunction OnBlinked { get; set; }

        [Preserve] public bool HasClip(string name, bool customKey) => _impl.HasKey(name, customKey);
        [Preserve] public float GetCurrentValue(string name, bool customKey) => _impl.GetBlendShapeValue(name, customKey);
        [Preserve] public string GetActiveFaceSwitch() => _impl.GetActiveFaceSwitch();
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
        [Preserve] public LuaFunction OnKeyboardKeyDown { get; set; }
        
        [Preserve] public LuaFunction OnTouchPadMouseButtonDown { get; set; }
        [Preserve] public LuaFunction OnPenTabletMouseButtonDown { get; set; }

        [Preserve] public LuaFunction OnGamepadButtonDown { get; set; }
        [Preserve]public LuaFunction OnArcadeStickButtonDown { get; set; }
    }
}
