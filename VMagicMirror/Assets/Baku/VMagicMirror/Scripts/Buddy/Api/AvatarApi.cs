using System;
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
        [Preserve] public bool IsTalking
        {
            get
            {
                _impl.RegisterApiInstanceAsMicrophoneRequire(this);
                return _impl.IsTalking.Value;
            }
        }
        
        // TODO: LipSyncのAIUEOが分かるようなプロパティが欲しい & 声量のdB値も欲しい

        [Preserve] public bool UsePerfectSync => _impl.UsePerfectSync;
        [Preserve] public LuaFunction OnBlinked { get; set; }

        [Preserve] public bool HasClip(string name, bool customKey) => _impl.HasKey(name, customKey);
        [Preserve] public float GetCurrentValue(string name, bool customKey) => _impl.GetBlendShapeValue(name, customKey);
        [Preserve] public string GetActiveFaceSwitch() => _impl.GetActiveFaceSwitch();

        internal void Dispose()
        {
            _impl.UnregisterApiInstanceAsMicrophoneRequire(this);
        }
    }

    public class AvatarPoseApi
    {
        private readonly AvatarPoseApiImplement _impl;
        public AvatarPoseApi(AvatarPoseApiImplement impl)
        {
            _impl = impl;
        }

        // NOTE: RootPositionはほぼゼロだが、Rotのほうはゲーム入力モードで回ることがあるので公開してもバチ当たらない…というモチベがある
        [Preserve] public Vector3 GetRootPosition() => _impl.GetRootPosition();
        [Preserve] public Quaternion GetRootRotation() => _impl.GetRootRotation();
        
        // TODO?: intを受け取るように明示的に書く必要あるかも
        [Preserve] public Vector3 GetBoneGlobalPosition(HumanBodyBones bone) => _impl.GetBoneGlobalPosition(bone);
        [Preserve] public Quaternion GetBoneGlobalRotation(HumanBodyBones bone) => _impl.GetBoneGlobalRotation(bone);
        [Preserve] public Vector3 GetBoneLocalPosition(HumanBodyBones bone) => _impl.GetBoneLocalPosition(bone);
        [Preserve] public Quaternion GetBoneLocalRotation(HumanBodyBones bone) => _impl.GetBoneLocalRotation(bone);
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
