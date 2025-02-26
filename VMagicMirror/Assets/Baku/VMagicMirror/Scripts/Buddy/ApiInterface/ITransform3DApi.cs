namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    public interface ITransform3DApi
    {
        // NOTE: Position/RotationはAttachedBoneに対してのローカル姿勢を表すことに注意
        Vector3 Position { get; }
        Quaternion Rotation { get; }
        float Scale { get; }
        
        HumanBodyBones AttachedBone { get; }
    }
}
