namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    public interface ISprite3DApi : IObject3DApi
    {
        void Preload(string path);
        void Show(string path);
    }
}
