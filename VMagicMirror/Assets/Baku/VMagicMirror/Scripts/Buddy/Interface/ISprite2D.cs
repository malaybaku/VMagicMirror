namespace VMagicMirror.Buddy
{
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
        /// プリセット画像の名称を指定して画像を表示します。
        /// </summary>
        /// <param name="name">プリセット画像の名称</param>
        /// <remarks>
        /// プリセット画像とは、アプリケーション自体に組み込まれていてサブキャラとして利用可能な画像のことです。
        /// 
        /// <paramref name="name"/> に指定可能な値の詳細や、画像切り替え時の演出を調整する場合の呼び出しについては <see cref="ShowPreset(string, Sprite2DTransitionStyle)"/> を参照して下さい。
        /// </remarks>
        void ShowPreset(string name);

        /// <summary>
        /// プリセット画像の名称、および切り替え演出を指定して画像を表示します。
        /// </summary>
        /// <param name="name">プリセット画像の名称</param>
        /// <param name="style">画像切り替えのスタイル</param>
        /// <remarks>
        /// プリセット画像とは、アプリケーション自体に組み込まれていてサブキャラとして利用可能な画像のことです。
        /// v4.0.0では <paramref name="name"/> として以下の値を指定できます。
        /// 
        /// <list type="bullet">
        ///   <item> "A_default" </item>
        ///   <item> "A_blink" </item>
        ///   <item> "A_mouthOpen" </item>
        ///   <item> "A_blink_mouthOpen" </item>
        ///   <item> "A_happy" </item>
        ///   <item> "A_angry" </item>
        ///   <item> "A_sad" </item>
        ///   <item> "A_relaxed" </item>
        ///   <item> "A_surprised" </item>
        ///   <item> "A_wink" </item>
        ///   <item> "A_smug_face" </item>
        /// </list>
        ///
        /// この関数はサブキャラの表情を切り替える場合などに適しています。
        /// </remarks>
        void ShowPreset(string name, Sprite2DTransitionStyle style);
        
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
