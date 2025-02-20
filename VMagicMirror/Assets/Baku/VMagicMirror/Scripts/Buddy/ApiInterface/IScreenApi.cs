namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    public interface IScreenApi
    {
        int Width { get; }
        int Height { get; }
        bool IsTransparent { get; }
    }
}