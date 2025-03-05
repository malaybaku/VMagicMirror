namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    public interface ISprite3DApi
    {
        /// <summary> オブジェクトの基本姿勢に関する値を取得します。 </summary>
        ITransform3D Transform { get; }

        /// <summary>
        /// ファイルパスを指定して、該当パスの画像を事前にロードします。
        /// </summary>
        /// <param name="path">画像ファイルのパス</param>
        /// <remarks>
        /// この関数を事前に呼ぶことは必須ではありませんが、起動時に呼び出しておくことにより、スプライトの切り替え時に時間がかかるのを防ぐことができます。
        /// </remarks>
        void Preload(string path);

        /// <summary>
        /// ファイルパスを指定して、該当パスの画像をスプライトとしてロードします。
        /// </summary>
        /// <param name="path"></param>
        /// <remarks>
        /// サブキャラが起動中に同一ファイルパスの画像を複数回指定すると、すでにロード済みの画像が再利用されます。
        /// </remarks>
        void Show(string path);

        /// <summary>
        /// スプライトを非表示にします。
        /// </summary>
        void Hide();
    }
}
