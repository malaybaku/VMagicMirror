namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    public interface ITransformsApi
    {
        ITransform2DApi GetTransform2D(string key);
        ITransform3DApi GetTransform3D(string key);
    }
}