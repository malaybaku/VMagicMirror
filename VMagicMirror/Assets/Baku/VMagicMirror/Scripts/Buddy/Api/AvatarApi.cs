using System;
using VMagicMirror.Buddy;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class AvatarLoadEventApi : IAvatarLoadEvent
    {
        internal void InvokeLoadedInternal() => Loaded?.Invoke();
        internal void InvokeUnloadedInternal() => Unloaded?.Invoke();

        private readonly AvatarLoadApiImplement _impl;
        public AvatarLoadEventApi(AvatarLoadApiImplement impl)
        {
            _impl = impl;
        }

        public override string ToString() => nameof(IAvatarLoadEvent);

        // NOTE:
        // これだとイベントの発火より先にフラグが変わるケースが出てくる。
        // - Invoke関数の中でフラグが変わるような書き方もアリ
        // - が、falseに倒すのはなるべく素早くやったほうがいいという考え方もある
        public bool IsLoaded => _impl.IsLoaded;

        public event Action Loaded;
        public event Action Unloaded;
    }

    // NOTE: I/F側のコメントを参照。
    public class AvatarBodyParameterApi : IAvatarBodyParameter
    {
    }
    
    public class AvatarFacialApi : IAvatarFacial
    {
        private readonly AvatarFacialApiImplement _impl;
        public AvatarFacialApi(AvatarFacialApiImplement impl)
        {
            _impl = impl;
        }

        public override string ToString() => nameof(IAvatarFacial);

        internal void InvokeOnBlinkedInternal() => OnBlinked?.Invoke();

        public string CurrentFacial => _impl.GetUserOperationActiveBlendShape()?.ToString() ?? "";
        public bool IsTalking
        {
            get
            {
                _impl.RegisterApiInstanceAsMicrophoneRequire(this);
                return _impl.IsTalking.CurrentValue;
            }
        }
        
        // TODO: LipSyncのAIUEOが分かるようなプロパティが欲しい & 声量のdB値も欲しい

        public bool UsePerfectSync => _impl.UsePerfectSync;

        public event Action OnBlinked;

        public bool HasClip(string name) => _impl.HasKey(name, true);
        public float GetCurrentValue(string name, bool customKey) => _impl.GetBlendShapeValue(name, customKey);

        public FaceSwitchState GetActiveFaceSwitch()
        {
            var rawAction = _impl.GetActiveFaceSwitch();
            // NOTE: intキャストを使わないのは、内部挙動だけ変えたときに壊れにくくするため
            return rawAction switch
            {
                FaceSwitchAction.MouthSmile => FaceSwitchState.MouthSmile,
                FaceSwitchAction.EyeSquint => FaceSwitchState.EyeSquint,
                FaceSwitchAction.EyeWide => FaceSwitchState.EyeWide,
                FaceSwitchAction.BrowUp => FaceSwitchState.BrowUp,
                FaceSwitchAction.BrowDown => FaceSwitchState.BrowDown,
                FaceSwitchAction.CheekPuff => FaceSwitchState.CheekPuff,
                FaceSwitchAction.TongueOut => FaceSwitchState.TongueOut,
                _ => FaceSwitchState.None,
            };
        } 

        internal void Dispose()
        {
            _impl.UnregisterApiInstanceAsMicrophoneRequire(this);
        }
    }

    public class AvatarPoseApi : IAvatarPose
    {
        private readonly AvatarPoseApiImplement _impl;
        public AvatarPoseApi(AvatarPoseApiImplement impl)
        {
            _impl = impl;
        }

        public override string ToString() => nameof(IAvatarPose);

        // NOTE: RootPositionはほぼゼロだが、Rotのほうはゲーム入力モードで回ることがあるので公開してもバチ当たらない…というモチベがある
        public Vector3 GetRootPosition() => _impl.GetRootPosition().ToApiValue();
        public Quaternion GetRootRotation() => _impl.GetRootRotation().ToApiValue();

        bool IAvatarPose.HasBone(HumanBodyBones bone) => _impl.HasBone(bone.ToEngineValue());
        
        //TODO: useParentBoneオプションをimplにわたす
        public Vector3 GetBoneGlobalPosition(HumanBodyBones bone, bool useParentBone)
            => _impl.GetBoneGlobalPosition(bone.ToEngineValue(), useParentBone).ToApiValue();
        public Quaternion GetBoneGlobalRotation(HumanBodyBones bone, bool useParentBone) 
            => _impl.GetBoneGlobalRotation(bone.ToEngineValue(), useParentBone).ToApiValue();
        public Vector3 GetBoneLocalPosition(HumanBodyBones bone, bool useParentBone) 
            => _impl.GetBoneLocalPosition(bone.ToEngineValue(), useParentBone).ToApiValue();
        public Quaternion GetBoneLocalRotation(HumanBodyBones bone, bool useParentBone) 
            => _impl.GetBoneLocalRotation(bone.ToEngineValue(), useParentBone).ToApiValue();
    }

    public class AvatarMotionEventApi : IAvatarMotionEvent
    {
        internal void InvokeOnKeyboardKeyDownInternal(string key) => OnKeyboardKeyDown?.Invoke(key);
        internal void InvokeOnTouchPadMouseButtonDownInternal() => OnTouchPadMouseButtonDown?.Invoke();
        internal void InvokeOnPenTabletMouseButtonDownInternal() => OnPenTabletMouseButtonDown?.Invoke();
        internal void InvokeOnGamepadButtonDownInternal(GamepadKey button)
            => OnGamepadButtonDown?.Invoke(button.ToApiValue());
        internal void InvokeOnArcadeStickButtonDownInternal(GamepadKey button) 
            => OnArcadeStickButtonDown?.Invoke(button.ToApiValue());

        public override string ToString() => nameof(IAvatarMotionEvent);
        
        public event Action<string> OnKeyboardKeyDown;
        public event Action OnTouchPadMouseButtonDown;
        public event Action OnPenTabletMouseButtonDown;
        public event Action<GamepadButton> OnGamepadButtonDown;
        public event Action<GamepadButton> OnArcadeStickButtonDown;
    }
}
