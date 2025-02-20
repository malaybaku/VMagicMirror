using System;

// NOTE: ビルド時の挙動が怪しい場合、interfaceメンバーに[Preserve]をつけるのを検討してもOK
namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    public interface IAvatarLoadEventApi
    {
        Action Loaded { get; set; }
        Action UnLoaded { get; set; }
    }
    
    public interface AvatarBodyParameterApi
    {
        //TODO: 身長っぽい値とかを入れるかもしれないやつ
        //あんまピンと来なければ無しにしてもいい
    }
    
    public interface IAvatarFacialApi
    {
        string CurrentFacial { get; }
        bool IsTalking { get; }
        // TODO: LipSyncのAIUEOが分かるようなプロパティが欲しい & 声量のdB値も欲しい

        bool UsePerfectSync { get; }
        Action OnBlinked { get; set; }

        bool HasClip(string name, bool customKey);
        float GetCurrentValue(string name, bool customKey);
        string GetActiveFaceSwitch();
    }

    public interface IAvatarPoseApi
    {
        // NOTE: RootPositionはほぼゼロだが、Rotのほうはゲーム入力モードで回ることがあるので公開してもバチ当たらない…というモチベがある
        Vector3 GetRootPosition();
        Quaternion GetRootRotation();
        
        // TODO: enumを「独自定義だけどUnityEngine.HumanBodyBonesと同等なやつ」にしたい
        Vector3 GetBoneGlobalPosition(HumanBodyBones bone);
        Quaternion GetBoneGlobalRotation(HumanBodyBones bone);
        Vector3 GetBoneLocalPosition(HumanBodyBones bone);
        Quaternion GetBoneLocalRotation(HumanBodyBones bone);
    }

    public interface IAvatarMotionEventApi
    {
        Action<string> OnKeyboardKeyDown { get; set; }
        
        Action OnTouchPadMouseButtonDown { get; set; }
        Action OnPenTabletMouseButtonDown { get; set; }

        // TODO: C#に対してであればenumを渡すスタイルにしちゃっても良いかも
        Action<string> OnGamepadButtonDown { get; set; }
        Action<string> OnArcadeStickButtonDown { get; set; }
    }
    
    /// <summary> UnityEngineのHumanBodyBonesと同じ順序で定義された人型ボーン情報の一覧です。 </summary>
    public enum HumanBodyBones
    {
        Hips = 0,
        LeftUpperLeg,
        RightUpperLeg,
        LeftLowerLeg,
        RightLowerLeg,
        LeftFoot,
        RightFoot,
        Spine,
        Chest,
        Neck,
        Head,
        LeftShoulder,
        RightShoulder,
        LeftUpperArm,
        RightUpperArm,
        LeftLowerArm,
        RightLowerArm,
        LeftHand,
        RightHand,
        LeftToes,
        RightToes,
        LeftEye,
        RightEye,
        Jaw,
        LeftThumbProximal,
        LeftThumbIntermediate,
        LeftThumbDistal,
        LeftIndexProximal,
        LeftIndexIntermediate,
        LeftIndexDistal,
        LeftMiddleProximal,
        LeftMiddleIntermediate,
        LeftMiddleDistal,
        LeftRingProximal,
        LeftRingIntermediate,
        LeftRingDistal,
        LeftLittleProximal,
        LeftLittleIntermediate,
        LeftLittleDistal,
        RightThumbProximal,
        RightThumbIntermediate,
        RightThumbDistal,
        RightIndexProximal,
        RightIndexIntermediate,
        RightIndexDistal,
        RightMiddleProximal,
        RightMiddleIntermediate,
        RightMiddleDistal,
        RightRingProximal,
        RightRingIntermediate,
        RightRingDistal,
        RightLittleProximal,
        RightLittleIntermediate,
        RightLittleDistal,
        UpperChest,
        LastBone,
    }
}
