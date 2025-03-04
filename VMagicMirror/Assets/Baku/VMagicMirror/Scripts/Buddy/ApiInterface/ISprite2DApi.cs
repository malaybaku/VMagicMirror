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
        void Preload(string path);
        void Show(string path);
        void Show(string path, Sprite2DTransitionStyle style);
        void Hide();

        // NOTE: PositionとLocalPositionを区別するかも
        Vector2 LocalPosition { get; set; }
        // NOTE: Z軸以外に回転させた場合、エフェクトの見映えは担保されない
        Quaternion LocalRotation { get; set; }
        // NOTE: Sizeがあり、Scaleはない (似ててややこしい)
        Vector2 Size { get; set; } 
        /// <summary>
        /// スプライトを回転および拡大/縮小するときの中心になる位置を、[0, 1]の範囲を示す座標で指定します。
        /// 初期値は (0.5, 0.0) です。
        /// </summary>
        Vector2 Pivot { get; set; }
        void SetPosition(Vector2 position);
        
        void SetParent(ITransform2DApi parent);

        ISpriteEffectApi Effects { get; }
    }
}
