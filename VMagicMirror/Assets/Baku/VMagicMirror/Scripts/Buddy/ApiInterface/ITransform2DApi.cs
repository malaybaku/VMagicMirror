namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    //TODO: manifest.jsonの定義方法をリンクさせたremarks docが欲しい
    /// <summary>
    /// マニフェストの定義に基づいて生成・取得できる、ユーザーがレイアウトを編集可能な2DのTransform情報を表すAPIです。
    /// </summary>
    public interface ITransform2DApi
    {
        /// <summary>
        /// 画面上での位置を取得します。
        /// </summary>
        /// <remarks>
        /// <para>
        ///   x成分は画面の左端が0、右端が1に対応します。
        /// </para>
        /// <para>
        ///   y成分は画面の下端が0、上端が1に対応します。
        /// </para>
        /// </remarks>
        Vector2 Position { get; }
        
        /// <summary>
        /// スケールを取得します。
        /// </summary>
        float Scale { get; }
        
        /// <summary>
        /// 回転を取得します。
        /// </summary>
        /// <remarks>
        /// 通常、この回転はz軸回りのみでの回転を表します。
        /// </remarks>
        Quaternion Rotation { get; }
    }
}
