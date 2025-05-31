namespace VMagicMirror.Buddy
{
    // NOTE: 名前は変えるかも、本当に純粋な数学演算というよりは「計算っぽい処理」を押し込んでおきたいので

    /// <summary>
    /// 数学的な処理を提供するAPIです。
    /// </summary>
    /// <exclude />
    internal interface IMathUtil
    {
        /// <summary>
        /// ワールド上の位置を画面から見た座標に変換します。
        /// </summary>
        /// <param name="position">ワールド上の座標</param>
        /// <returns>画面上の座標</returns>
        /// <remarks>
        /// 画面上の座標について、x成分は左端を0、右端を1とする値になります。y成分は下端を0、上端を1とする値になります。
        /// 画面外になるような座標を指定した場合、計算結果のx成分やy成分は [0, 1] の範囲を超えることがあります。
        /// </remarks>
        Vector2 GetScreenPositionOfWorldPoint(Vector3 position);
    }
}
