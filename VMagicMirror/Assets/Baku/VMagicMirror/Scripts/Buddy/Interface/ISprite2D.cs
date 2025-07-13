using System;

namespace VMagicMirror.Buddy
{
    /// <summary>
    /// 画像をアバターウィンドウの最前面に平面的に表示するAPIです。
    /// </summary>
    /// <remarks>
    /// <para>
    /// このAPIでは .jpg または .png ファイル、およびプリセットとしてアプリケーションに組み込まれた画像をロードできます。
    /// </para>
    /// 
    /// <para>
    /// 画像のサイズ等は画面全体のサイズがおおよそ 1280x720 であるとした場合の値を用いて指定します。
    /// サイズ調整を直感的に行うために、画像アセットとスプライトはいずれも正方形の形状にしておくことを推奨しています。
    /// また、サブキャラに表情差分などの画像群がある場合、原則として各画像のサイズを揃えるようにしてください。
    /// </para>
    /// </remarks>
    public interface ISprite2D
    {
        /// <summary> 位置や回転などの、オブジェクトの基本的な配置に関するAPIを取得します。 </summary>
        ITransform2D Transform { get; }

        /// <summary>
        /// <see cref="SetupDefaultSprites"/> や <see cref="ShowDefaultSprites(Sprite2DTransitionStyle, float)"/> を用いて適用できる、
        /// デフォルトの立ち絵についての動作の設定を取得します。
        /// </summary>
        IDefaultSpritesSetting DefaultSpritesSetting { get; }
        
        /// <summary>
        /// この画像に紐づけたテキスト表示を行うAPIを取得します。
        /// </summary>
        ITalkText TalkText { get; }

        /// <summary>
        /// この画像上でポインター(マウスカーソルなど)で左クリックに相当するボタンを押すと発火します。
        /// </summary>
        /// <remarks>
        /// <para>
        /// クリックのエリアはスプライトのサイズ全体の矩形で決定されます。
        /// 透過部分の多い画像を表示している場合、画像がないエリアでの操作にも反応することに注意して下さい。
        /// </para>
        /// 
        /// <para>
        /// このイベントはボタン等のクリックと異なり、クリックの開始の瞬間に発火します。
        /// クリックを検出したい場合には <see cref="PointerClick"/> を使用します。
        /// </para>
        /// </remarks>
        event Action<Pointer2DData> PointerDown;

        /// <summary>
        /// この画像上でポインター(マウスカーソルなど)の左クリックに相当するボタンを離すと発火します。
        /// </summary>
        /// <remarks>
        /// <para>
        /// クリックのエリアはスプライトのサイズ全体の矩形で決定されます。
        /// 透過部分の多い画像を表示している場合、画像がないエリアでの操作にも反応することに注意して下さい。
        /// </para>
        /// 
        /// <para>
        /// このイベントはドラッグ操作を含むような操作の後でも発火します。
        /// クリックを検出したい場合には <see cref="PointerClick"/> を使用します。
        /// </para>
        /// </remarks>
        event Action<Pointer2DData> PointerUp;

        /// <summary>
        /// この画像上でポインター(マウスカーソルなど)で左クリックに相当するボタンでクリック操作を行うと発火します。
        /// </summary>
        /// <remarks>
        /// <para>
        /// クリックのエリアはスプライトのサイズ全体の矩形で決定されます。
        /// 透過部分の多い画像を表示している場合、画像がないエリアでの操作にも反応することに注意して下さい。
        /// </para>
        /// 
        /// <para>
        /// このイベントは、ボタンを押した場所でドラッグ操作を行わずに離すことで発火します。
        /// ボタンを押した瞬間が検出したい場合などは、代わりに <see cref="PointerDown"/> を使用します。
        /// </para>
        /// </remarks>
        event Action<Pointer2DData> PointerClick;
        
        /// <summary>
        /// この画像の領域にポインター(マウスカーソルなど)が進入すると発火します。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 画像の領域はスプライトのサイズ全体の矩形で決定されます。
        /// 透過部分の多い画像を表示している場合、画像がないエリアでの操作にも反応することに注意して下さい。
        /// </para>
        /// </remarks>
        event Action<Pointer2DData> PointerEnter;
        
        /// <summary>
        /// この画像の領域からポインター(マウスカーソルなど)が離れると発火します。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 画像の領域はスプライトのサイズ全体の矩形で決定されます。
        /// 透過部分の多い画像を表示している場合、画像がないエリアでの操作にも反応することに注意して下さい。
        /// </para>
        /// </remarks>
        event Action<Pointer2DData> PointerLeave;
        
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
        /// その後、 <see cref="ShowDefaultSprites(Sprite2DTransitionStyle, float)"/> を呼び出すことで指定した画像が表示されます。
        ///
        /// 簡易的なセットアップでサブキャラのまばたき、口パクを動かしたい場合、この関数で基本の立ち絵をセットアップします。
        /// まばたきや口パクをより詳細に制御したい場合、このメソッドは使用せず、代わりに
        /// <see cref="Show(string)"/> や <see cref="Show(string, Sprite2DTransitionStyle, float)"/> を使用します。
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
        /// <see cref="SetupDefaultSprites"/> でセットアップした立ち絵を直ちに表示します。
        /// メソッドの詳細は　<see cref="ShowDefaultSprites(Sprite2DTransitionStyle, float)"/> を参照して下さい。
        /// </summary>
        void ShowDefaultSprites();

        /// <summary>
        /// <see cref="SetupDefaultSprites"/> でセットアップしたデフォルトの立ち絵を表示します。
        /// </summary>
        /// <param name="style">画像切り替えのスタイル。指定しない場合、ただちに画像が切り替わります。</param>
        /// <param name="duration">画像切り替え動作を実行する秒数。0以下の値を指定した場合、ただちに画像が切り替わります。</param>
        /// <remarks>
        /// この関数を呼び出す場合、あらかじめ <see cref="SetupDefaultSprites"/> でセットアップを行う必要があります。
        /// 事前に <see cref="SetupDefaultSprites"/> を呼び出していなかった場合、このメソッドを呼び出しても何も起こりません。
        /// </remarks>
        void ShowDefaultSprites(Sprite2DTransitionStyle style, float duration);
        
        /// <summary>
        /// ファイルパスを指定して画像を表示します。
        /// </summary>
        /// <param name="path">画像ファイルのパス</param>
        /// <remarks>
        /// このメソッドでは画像は指定されたものへ直ちに切り替わります。
        /// 
        /// アニメーションを適用しながら画像を切り替えたい場合は　<see cref="Show(string, Sprite2DTransitionStyle, float)"/> を使用します。
        /// </remarks>
        void Show(string path);

        /// <summary>
        /// ファイルパスと切り替え演出を指定して画像を表示します。
        /// </summary>
        /// <param name="path">画像ファイルのパス</param>
        /// <param name="style">画像切り替えのスタイル。。</param>
        /// <param name="duration">画像切り替え動作を実行する秒数。0以下の値を指定した場合、ただちに画像が切り替わります。</param>
        /// <remarks>
        /// <see cref="Preload"/> を呼び出したことのある画像や起動後に表示したことのある画像のパスを指定した場合、
        /// すでに読み込み済みの画像が再利用されます。
        ///
        /// <paramref name="style"/> として <see cref="Sprite2DTransitionStyle.Immediate"/> を指定した場合、 <paramref name="duration"/> の値は無視され、ただちに画像が切り替わります。
        /// これは <see cref="Show(string)"/> と同じ動作です。
        /// </remarks>
        void Show(string path, Sprite2DTransitionStyle style, float duration);

        /// <summary>
        /// プリセット画像の名称を指定して画像を表示します。
        /// </summary>
        /// <param name="name">プリセット画像の名称</param>
        /// <remarks>
        /// プリセット画像とは、アプリケーション自体に組み込まれていてサブキャラとして利用可能な画像のことです。
        /// 
        /// <paramref name="name"/> に指定可能な値の詳細や、画像切り替え時の演出を調整する場合の呼び出しについては <see cref="ShowPreset(string, Sprite2DTransitionStyle, float)"/> を参照して下さい。
        /// </remarks>
        void ShowPreset(string name);

        /// <summary>
        /// プリセット画像の名称、および切り替え演出を指定して画像を表示します。
        /// </summary>
        /// <param name="name">プリセット画像の名称</param>
        /// <param name="style">画像切り替えのスタイル</param>
        /// <param name="duration">画像切り替え動作を実行する秒数。0以下の値を指定した場合、ただちに画像が切り替わります。</param>
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
        /// 
        /// その他の用途については <see cref="Show(string, Sprite2DTransitionStyle, float)"/> を参照して下さい。
        /// </remarks>
        void ShowPreset(string name, Sprite2DTransitionStyle style, float duration);
        
        /// <summary>
        /// サブキャラを非表示にします。
        /// </summary>
        void Hide();

        /// <summary>
        /// スプライトのサイズを取得、設定します。
        /// </summary>
        /// <remarks>
        /// サイズは、アバターウィンドウ全体のサイズがおおよそ 1280x720 であるとした場合の値として指定します。
        /// 例えば、<c>new Vector2(150, 150)</c> 程度の値はスプライトの基本的なサイズとして適しています。
        /// </remarks>
        Vector2 Size { get; set; } 

        /// <summary> スプライトに適用するエフェクトの設定を取得します。 </summary>
        ISpriteEffect Effects { get; }
    }
}
