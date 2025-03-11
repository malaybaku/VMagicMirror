namespace VMagicMirror.Buddy
{
    //TODO: 座標系について内容そのもの or 基本ルール的なのが書いてあるdocに導線を張る
    /// <summary>
    /// 画面の最前面に表示されたオブジェクトについて、2D空間上の姿勢が取得、設定できるインスタンスです。
    /// </summary>
    public interface ITransform2D
    {
        /// <summary>
        /// インスタンスを読み取り専用とみなした値に変換します。
        /// </summary>
        /// <returns>読み取り専用扱いに変換した値</returns>
        /// <remarks>
        /// このメソッドの戻り値を経由するとTransformの状態は編集できなくなります。
        /// ただし、呼び出し元のインスタンス自体は引き続き編集可能です。
        /// </remarks>
        IReadOnlyTransform2D AsReadOnly();

        /// <summary>
        /// Transformの位置をローカル座標の値として取得、設定します。
        /// </summary>
        Vector2 LocalPosition { get; set; }
        /// <summary>
        /// Transformの回転をローカル座標の値として取得、設定します。
        /// </summary>
        Quaternion LocalRotation { get; set; }
        /// <summary>
        /// Transformのスケールをローカル座標の値として取得、設定します。
        /// </summary>
        Vector2 LocalScale { get; set; } 
        
        /// <summary>
        /// スプライトを回転および拡大/縮小するときの中心になる位置を、[0, 1]の範囲を示す座標で指定します。
        /// 初期値は (0.5, 0.0) です。
        /// </summary>
        Vector2 Pivot { get; set; }

        /// <summary>
        /// Transformの位置を画面座標で取得、設定します。
        /// </summary>
        Vector2 Position { get; set; }

        /// <summary>
        /// Transformの回転を画面座標で取得、設定します。
        /// </summary>
        Quaternion Rotation { get; set; }

        /// <summary>
        /// 親オブジェクトを設定します。
        /// </summary>
        /// <param name="parent">親要素となるオブジェクト</param>
        /// <remarks>
        /// <para>
        /// この関数を呼び出すと、最終的なTransformの姿勢は親オブジェクトの姿勢やスケールの影響を受けるようになります。
        /// 設定した親オブジェクトを解除する場合、<see cref="RemoveParent"/> を呼び出します。
        /// </para>
        /// <para>
        /// この関数の呼び出し前後では <see cref="LocalPosition"/> や <see cref="LocalRotation"/> は変化しませんが、
        /// <see cref="Position"/> や <see cref="Rotation"/> は変化します。
        /// 画面上での位置を保ったまま親オブジェクトを指定したい場合、あらかじめ <see cref="Position"/> や <see cref="Rotation"/> の値をキャッシュしておき、
        /// この関数の呼び出し後に適用します。
        /// </para>
        /// </remarks>
        void SetParent(IReadOnlyTransform2D parent);
        
        /// <inheritdoc cref="SetParent(IReadOnlyTransform2D)"/>
        void SetParent(ITransform2D parent);
        
        /// <summary>
        /// 親オブジェクトを外します。
        /// </summary>
        /// <remarks>
        /// <para>
        /// この関数の呼び出し前後では <see cref="LocalPosition"/> や <see cref="LocalRotation"/> は変化しませんが、
        /// <see cref="Position"/> や <see cref="Rotation"/> は変化します。
        /// 画面上での位置を保ったまま親オブジェクトを指定したい場合、あらかじめ <see cref="Position"/> や <see cref="Rotation"/> の値をキャッシュしておき、
        /// この関数の呼び出し後に適用します。
        /// </para>
        /// </remarks>
        void RemoveParent();
    }
}
