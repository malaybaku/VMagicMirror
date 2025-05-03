using System.Threading.Tasks;

namespace VMagicMirror.Buddy
{
    /// <summary>
    /// VRM Animationの読み込み処理に関するAPIです。
    /// </summary>
    public interface IVrmAnimation
    {
        /// <summary>
        /// ファイルパスを指定してVRM Animationをロードします。
        /// </summary>
        /// <param name="path">VRM Animation (.vrma) のファイルパス</param>
        /// <returns></returns>
        Task LoadAsync(string path);

        /// <summary>
        /// <see cref="LoadAsync"/> によるロード処理が終わっていれば <c>true</c>、そうでなければ <c>false</c> を返します。
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// <see cref="IsLoaded"/> が <c>true</c> の場合、読み込んだアニメーションの長さを秒単位で取得します。
        /// 読み込みが完了していない場合は -1 を返します。
        /// </summary>
        /// <returns>VRM Animationの再生時間の秒数</returns>
        float GetLength();
    }
}
