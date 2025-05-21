namespace VMagicMirror.Buddy
{
    /// <summary>
    /// 画像を3D空間上のスプライトとして表示するAPIです。
    /// </summary>
    /// <remarks>
    /// <para>
    /// このAPIでは .jpg または .png ファイル、およびプリセットとしてアプリケーションに組み込まれた画像をロードできます。
    /// </para>
    /// 
    /// <para>
    /// 画像は長いほうの辺を1mにするようなスケールでロードされます。
    /// このスケールは <see cref="IManifestTransforms.GetTransform3D"/> または <see cref="Transform"/> で得られるTransformのスケールを通じて調整できます。
    /// 直感的なサイズ調整のために、スプライトとしては正方形の画像を用意することを推奨しています。
    /// また、サブキャラに表情差分などの画像群がある場合、原則として各画像のサイズを揃えるようにしてください。
    /// </para>
    /// </remarks>
    public interface ISprite3D
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
        /// プリセット画像の名称を指定して画像を表示します。
        /// </summary>
        /// <param name="name">プリセット画像の名称</param>
        /// <remarks>
        /// プリセット画像とは、アプリケーション自体に組み込まれていてサブキャラとして利用可能な画像のことです。
        /// 
        /// <paramref name="name"/> に指定可能な値については
        /// <see cref="ISprite2D.ShowPreset(string, Sprite2DTransitionStyle, float)"/> を参照して下さい。
        /// </remarks>
        void ShowPreset(string name);
        
        /// <summary>
        /// スプライトを非表示にします。
        /// </summary>
        void Hide();
    }
}
