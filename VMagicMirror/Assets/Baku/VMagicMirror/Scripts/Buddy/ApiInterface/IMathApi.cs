namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    // NOTE: 名前は変えるかも
    // 純粋な数学演算というよりは「計算っぽい処理」を押し込んでおきたいやつ
    public interface IMathApi
    {
        /// <summary>
        /// ワールド上の位置をスクリーン座標(左下=(0,0), 右上=(1,1))に変換する
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        Vector2 GetScreenPositionOfWorldPoint(Vector3 position);
    }
}
