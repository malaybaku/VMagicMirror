using System.Threading.Tasks;

namespace VMagicMirror.Buddy
{
    /// <summary>
    /// VRM Animationの読み込み処理に関するAPIです。
    /// 本APIは作成途上のものであり、VMagicMirror v4.0.0の時点では本APIは利用できません。
    /// </summary>
    /// <remarks>
    /// <para>
    /// VMagicMirror v4.0.0の時点では機能整備が完了していないため、本APIの利用手段は提供していません。
    /// ここでは、想定している機能を提示する目的でドキュメントを公開しています。
    /// </para>
    ///
    /// <para>
    /// VRMやGLBによる3Dオブジェクトをサブキャラとして表示する機能は、 v4.0.0 以降のマイナーアップデートとして提供予定です。
    /// </para>
    /// </remarks>
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
