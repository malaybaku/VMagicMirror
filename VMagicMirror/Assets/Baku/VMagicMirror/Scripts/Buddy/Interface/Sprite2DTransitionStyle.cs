namespace VMagicMirror.Buddy
{
    /// <summary>
    /// <see cref="ISprite2D.Show(string)"/> でスプライト画像を切り替えるプリセット演出の種類です。
    /// </summary>
    public enum Sprite2DTransitionStyle
    {
        /// <summary> 画像の切り替えを行わないことを表します。通常は使用しません。 </summary>
        None = 0,
        /// <summary> ただちにスプライト画像を切り替えます。 </summary>
        Immediate = 1,
        /// <summary> 左方向に画像を回転させながらスプライト画像を切り替えます。 </summary>
        LeftFlip = 2,
        /// <summary> 右方向に画像を回転させながらスプライト画像を切り替えます。 </summary>
        RightFlip = 3,
        /// <summary> 画像の底を軸にして画像を奥に倒し、スプライト画像を切り替えてから立ち上がらせます。 </summary>
        BottomFlip = 4,
    }
}
