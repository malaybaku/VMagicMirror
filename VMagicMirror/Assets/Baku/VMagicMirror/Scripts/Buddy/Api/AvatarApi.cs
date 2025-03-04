using System;
using Baku.VMagicMirror.Buddy.Api.Interface;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class AvatarLoadEventApi : IAvatarLoadEventApi
    {
        internal void InvokeLoadedInternal() => Loaded?.Invoke();
        internal void InvokeUnloadedInternal() => Unloaded?.Invoke();

        private readonly AvatarLoadApiImplement _impl;
        public AvatarLoadEventApi(AvatarLoadApiImplement impl)
        {
            _impl = impl;
        }

        // NOTE:
        // これだとイベントの発火より先にフラグが変わるケースが出てくる。
        // - Invoke関数の中でフラグが変わるような書き方もアリ
        // - が、falseに倒すのはなるべく素早くやったほうがいいという考え方もある
        public bool IsLoaded => _impl.IsLoaded;

        public event Action Loaded;
        public event Action Unloaded;
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

        internal void InvokeOnBlinkedInternal() => OnBlinked?.Invoke();

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

        public event Action OnBlinked;

        public bool HasClip(string name) => _impl.HasKey(name, true);
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
        public Vector3 GetRootPosition() => _impl.GetRootPosition().ToApiValue();
        public Quaternion GetRootRotation() => _impl.GetRootRotation().ToApiValue();
        
        //TODO: useParentBoneオプションをimplにわたす
        public Vector3 GetBoneGlobalPosition(HumanBodyBones bone, bool useParentBone)
            => _impl.GetBoneGlobalPosition(bone.ToEngineValue()).ToApiValue();
        public Quaternion GetBoneGlobalRotation(HumanBodyBones bone, bool useParentBone) 
            => _impl.GetBoneGlobalRotation(bone.ToEngineValue()).ToApiValue();
        public Vector3 GetBoneLocalPosition(HumanBodyBones bone, bool useParentBone) 
            => _impl.GetBoneLocalPosition(bone.ToEngineValue()).ToApiValue();
        public Quaternion GetBoneLocalRotation(HumanBodyBones bone, bool useParentBone) 
            => _impl.GetBoneLocalRotation(bone.ToEngineValue()).ToApiValue();
    }

    public class AvatarMotionEventApi : IAvatarMotionEventApi
    {
        internal void InvokeOnKeyboardKeyDownInternal(string key) => OnKeyboardKeyDown?.Invoke(key);
        internal void InvokeOnTouchPadMouseButtonDownInternal() => OnTouchPadMouseButtonDown?.Invoke();
        internal void InvokeOnPenTabletMouseButtonDownInternal() => OnPenTabletMouseButtonDown?.Invoke();
        internal void InvokeOnGamepadButtonDownInternal(GamepadButton button) => OnGamepadButtonDown?.Invoke(button);
        internal void InvokeOnArcadeStickButtonDownInternal(GamepadButton button) => OnArcadeStickButtonDown?.Invoke(button);
        
        public event Action<string> OnKeyboardKeyDown;
        public event Action OnTouchPadMouseButtonDown;
        public event Action OnPenTabletMouseButtonDown;
        public event Action<GamepadButton> OnGamepadButtonDown;
        public event Action<GamepadButton> OnArcadeStickButtonDown;
    }
}
