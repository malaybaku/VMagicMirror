namespace VMagicMirror.Buddy
{
    //TODO: manifest.jsonの定義方法をリンクさせたremarks docが欲しい
    /// <summary>
    /// `manifest.json` で定義され、ユーザーがレイアウトを編集可能な2Dまたは3DのTransform情報を取得できるAPIです。
    /// </summary>
    public interface IManifestTransforms
    {
        /// <summary>
        /// 名称を指定して、2DのTransformへの参照を取得します。
        /// </summary>
        /// <param name="key">Transformの名称</param>
        /// <returns><paramref name="key"/>が実際に定義されていればそれに対応するTransformの参照、そうでなければ<c>null</c></returns>
        IReadOnlyTransform2D GetTransform2D(string key);

        /// <summary>
        /// 名称を指定して、3DのTransformへの参照を取得します。
        /// </summary>
        /// <param name="key">Transformの名称</param>
        /// <returns><paramref name="key"/>が実際に定義されていればそれに対応するTransformの参照、そうでなければ<c>null</c></returns>
        IReadOnlyTransform3D GetTransform3D(string key);
    }
}