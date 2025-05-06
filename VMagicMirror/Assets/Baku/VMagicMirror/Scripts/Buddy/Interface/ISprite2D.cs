namespace VMagicMirror.Buddy
{
    public interface ISprite2D
    {
        /// <summary> 位置や回転などの、オブジェクトの基本的な配置に関するAPIを取得します。 </summary>
        ITransform2D Transform { get; }

        /// <summary>
        /// <see cref="SetupDefaultSprites"/> や <see cref="ShowDefaultSprites(Sprite2DTransitionStyle)"/> を用いて適用できる、
        /// デフォルトの立ち絵についての動作の設定を取得します。
        /// </summary>
        IDefaultSpritesSetting DefaultSpritesSetting { get; }
        
        /// <summary>
        /// ファイルパスを指定して画像を事前にロードします。
        /// </summary>
        /// <param name="path">画像ファイルのパス</param>
        /// <remarks>
        /// この関数をサブキャラのロード直後に呼び出すことで、サブキャラの動作中の画像切り替えがスムーズに行えます。
        /// </remarks>
        void Preload(string path);

        /// <summary>
        /// まばたき、および口の開閉の組み合わせからなる4枚の画像を指定することで、
        /// サブキャラの基本となる立ち絵スプライトをセットアップします。
        /// </summary>
        /// <param name="defaultImagePath">目を開き、口を閉じている立ち絵の画像ファイルのパス</param>
        /// <param name="blinkImagePath">目を閉じ、口を開いている立ち絵の画像ファイルのパス</param>
        /// <param name="mouthOpenImagePath">目を開き、口を開いている立ち絵の画像ファイルのパス</param>
        /// <param name="blinkMouthOpenImagePath">目を閉じ、口を開いている立ち絵の画像ファイルのパス</param>
        /// <remarks>
        /// この関数はセットアップのために一度だけ呼び出します。
        /// その後、 <see cref="ShowDefaultSprites(Sprite2DTransitionStyle)"/> を呼び出すことで指定した画像が表示されます。
        ///
        /// 簡易的なセットアップでサブキャラのまばたき、口パクを動かす場合、この関数で基本の立ち絵をセットアップします。
        /// まばたきや口パクを詳細に制御したい場合、このメソッドは使用せず、
        /// <see cref="Show(string, Sprite2DTransitionStyle)"/> を使用します。
        /// </remarks>
        void SetupDefaultSprites(
            string defaultImagePath,
            string blinkImagePath,
            string mouthOpenImagePath,
            string blinkMouthOpenImagePath
        );

        /// <summary>
        /// サブキャラの立ち絵スプライトとして、プリセットで定義されたキャラクター画像を適用します。
        /// </summary>
        /// <remarks>
        /// このメソッドではプリセットのサブキャラを立ち絵として適用します。
        /// </remarks>
        void SetupDefaultSpritesByPreset();

        /// <summary>
        /// <see cref="SetupDefaultSprites"/> でセットアップした立ち絵を表示します。
        /// メソッドの詳細は　<see cref="ShowDefaultSprites(Sprite2DTransitionStyle)"/> を参照して下さい。
        /// </summary>
        void ShowDefaultSprites();

        /// <summary>
        /// <see cref="SetupDefaultSprites"/> でセットアップしたデフォルトの立ち絵を表示します。
        /// </summary>
        /// <param name="style">画像切り替えのスタイル。指定しない場合、ただちに画像が切り替わります。</param>
        /// <remarks>
        /// この関数を呼び出す場合、あらかじめ <see cref="SetupDefaultSprites"/> でセットアップを行う必要があります。
        /// 事前に <see cref="SetupDefaultSprites"/> を呼び出していなかった場合、このメソッドを呼び出しても何も起こりません。
        /// </remarks>
        void ShowDefaultSprites(Sprite2DTransitionStyle style);
        
        /// <summary>
        /// ファイルパスを指定して画像を表示します。。
        /// メソッドの詳細は　<see cref="Show(string, Sprite2DTransitionStyle)"/> を参照して下さい。
        /// </summary>
        /// <param name="path">画像ファイルのパス</param>
        void Show(string path);

        /// <summary>
        /// ファイルパスと切り替え演出を指定して画像を表示します。
        /// </summary>
        /// <param name="path">画像ファイルのパス</param>
        /// <param name="style">画像切り替えのスタイル。指定しない場合、ただちに画像が切り替わります。</param>
        /// <remarks>
        /// <see cref="Preload"/> を呼び出したことのある画像や起動後に表示したことのある画像のパスを指定した場合、
        /// すでに読み込み済みの画像が再利用されます。
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
