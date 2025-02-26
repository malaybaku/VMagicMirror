namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    public interface ITransform2DApi
    {
        Vector2 Position { get; }
        float Scale { get; }
        Quaternion Rotation { get; }
    }
}
