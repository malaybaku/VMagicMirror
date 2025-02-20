namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    public enum Sprite2DTransitionStyle
    {
        None = 0,
        Immediate = 1,
        LeftFlip = 2,
        RightFlip = 3,
    }
    
    public interface ISprite2DApi
    {
        // NOTE: PositionとLocalPositionを区別するかも
        Vector2 Position { get; set; }
        Vector2 Size { get; set; }
        Vector2 Scale { get; set; }
        Vector2 Pivot { get; set; }
        
        ISpriteEffectApi Effects { get; }

        void SetPosition(Vector2 position);

        void Hide();

        void Show(string path);
        void Show(string path, Sprite2DTransitionStyle style);

        void Preload(string path);

        void SetParent(ITransform2DApi parent);
    }
}
