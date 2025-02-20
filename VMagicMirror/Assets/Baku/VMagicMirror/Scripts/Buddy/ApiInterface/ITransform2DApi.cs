namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    public interface ITransformsApi
    {
        ITransform2DApi GetTransform2D(string key);
    }
    
    public interface ITransform2DApi
    {
        Vector2 Position { get; }
        float Scale { get; }
        Quaternion Rotation { get; }
    }
}
