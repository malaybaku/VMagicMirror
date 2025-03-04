namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    //TODO: manifest.jsonの定義方法をリンクさせたremarks docが欲しい
    /// <summary>
    /// マニフェスト定義で指定した、ユーザーが編集可能な2Dまたは3DのTransformの参照を取得できるAPIです。
    /// </summary>
    public interface ITransformsApi
    {
        /// <summary>
        /// 名称を指定して、2DのTransformへの参照を取得します。
        /// </summary>
        /// <param name="key">Transformの名称を</param>
        /// <returns><paramref name="key"/>が実際に定義されていればそれに対応するTransformの参照、そうでなければ<c>null</c></returns>
        ITransform2DApi GetTransform2D(string key);

        /// <summary>
        /// 名称を指定して、3DのTransformへの参照を取得します。
        /// </summary>
        /// <param name="key">Transformの名称を</param>
        /// <returns><paramref name="key"/>が実際に定義されていればそれに対応するTransformの参照、そうでなければ<c>null</c></returns>
        ITransform3DApi GetTransform3D(string key);
    }
}