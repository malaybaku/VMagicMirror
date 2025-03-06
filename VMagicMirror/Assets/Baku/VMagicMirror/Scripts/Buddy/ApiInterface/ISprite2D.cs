namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    public enum Sprite2DTransitionStyle
    {
        None = 0,
        Immediate = 1,
        LeftFlip = 2,
        RightFlip = 3,
    }
    
    public interface ISprite2D
    {
        /// <summary> 位置や回転などの、オブジェクトの基本的な配置に関するAPIを取得します。 </summary>
        ITransform2D Transform { get; }
        
        /// <summary>
        /// ファイルパスを指定して画像を事前にロードします。
        /// </summary>
        /// <param name="path">画像ファイルのパス</param>
        /// <remarks>
        /// この関数をサブキャラのロード直後に呼び出すことで、サブキャラの動作中の画像切り替えがスムーズに行えます。
        /// </remarks>
        void Preload(string path);
        
        /// <summary>
        /// ファイルパスを指定して画像を直ちに表示します。
        /// </summary>
        /// <param name="path">画像ファイルのパス</param>
        /// <remarks>
        /// <see cref="Preload"/> を呼び出したことのある画像や起動後に表示したことのある画像のパスを指定した場合、
        /// すでに読み込み済みの画像が再利用されます。
        ///
        /// この関数は初期状態のサブキャラを表示するのに適しています。
        /// </remarks>
        void Show(string path);

        /// <summary>
        /// ファイルパスと切り替え演出を指定して画像を表示します。
        /// </summary>
        /// <param name="path">画像ファイルのパス</param>
        /// <param name="style">画像切り替えのスタイル</param>
        /// <remarks>
        /// <see cref="Show(string)"/> と異なり、この関数では演出つきで画像を切り替えられます。
        /// 
        /// この関数はサブキャラの表情を切り替える場合などに適しています。
        /// </remarks>
        void Show(string path, Sprite2DTransitionStyle style);
        
        /// <summary>
        /// サブキャラを非表示にします。
        /// </summary>
        void Hide();

        // TODO: Sizeのdocをいい感じに書く + そのためにそもそもSizeの扱いをいい感じに定義したい
        // NOTE: Sprite特有の設定としてはSizeだけがあり、ScaleやPivotはTransformのほうで定義される
        Vector2 Size { get; set; } 

        /// <summary> スプライトに適用するエフェクトの設定を取得します。 </summary>
        ISpriteEffect Effects { get; }
    }
}
