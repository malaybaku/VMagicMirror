using System;
using Baku.VMagicMirror.Buddy.Api.Interface;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class AvatarLoadEventApi : IAvatarLoadEventApi
    {
        public Action Loaded { get; set; }
        public Action UnLoaded { get; set; }
    }

    // NOTE: I/F側のコメントを参照。
    public class AvatarBodyParameterApi : IAvatarBodyParameterApi
    {
    }
    
    public class AvatarFacialApi : IAvatarFacialApi
    {
        private readonly AvatarFacialApiImplement _impl;
        public AvatarFacialApi(AvatarFacialApiImplement impl)
        {
            _impl = impl;
        }

        public string CurrentFacial => _impl.GetUserOperationActiveBlendShape()?.ToString() ?? "";
        public bool IsTalking
        {
            get
            {
                _impl.RegisterApiInstanceAsMicrophoneRequire(this);
                return _impl.IsTalking.Value;
            }
        }
        
        // TODO: LipSyncのAIUEOが分かるようなプロパティが欲しい & 声量のdB値も欲しい

        public bool UsePerfectSync => _impl.UsePerfectSync;
        public Action OnBlinked { get; set; }

        public bool HasClip(string name, bool customKey) => _impl.HasKey(name, customKey);
        public float GetCurrentValue(string name, bool customKey) => _impl.GetBlendShapeValue(name, customKey);
        public string GetActiveFaceSwitch() => _impl.GetActiveFaceSwitch();

        internal void Dispose()
        {
            _impl.UnregisterApiInstanceAsMicrophoneRequire(this);
        }
    }

    public class AvatarPoseApi : IAvatarPoseApi
    {
        private readonly AvatarPoseApiImplement _impl;
        public AvatarPoseApi(AvatarPoseApiImplement impl)
        {
            _impl = impl;
        }

        // NOTE: RootPositionはほぼゼロだが、Rotのほうはゲーム入力モードで回ることがあるので公開してもバチ当たらない…というモチベがある
        public Interface.Vector3 GetRootPosition() => _impl.GetRootPosition().ToApiValue();
        public Interface.Quaternion GetRootRotation() => _impl.GetRootRotation().ToApiValue();
        
        public Interface.Vector3 GetBoneGlobalPosition(HumanBodyBones bone)
            => _impl.GetBoneGlobalPosition(bone.ToEngineValue()).ToApiValue();
        public Interface.Quaternion GetBoneGlobalRotation(HumanBodyBones bone) 
            => _impl.GetBoneGlobalRotation(bone.ToEngineValue()).ToApiValue();
        public Interface.Vector3 GetBoneLocalPosition(HumanBodyBones bone) 
            => _impl.GetBoneLocalPosition(bone.ToEngineValue()).ToApiValue();
        public Interface.Quaternion GetBoneLocalRotation(HumanBodyBones bone) 
            => _impl.GetBoneLocalRotation(bone.ToEngineValue()).ToApiValue();
    }

    public class AvatarMotionEventApi : IAvatarMotionEventApi
    {
        public Action<string> OnKeyboardKeyDown { get; set; }
        
        public Action OnTouchPadMouseButtonDown { get; set; }
        public Action OnPenTabletMouseButtonDown { get; set; }

        public Action<Interface.GamepadKey> OnGamepadButtonDown { get; set; }
        public Action<Interface.GamepadKey> OnArcadeStickButtonDown { get; set; }
    }
}
