using System;

// NOTE: ビルド時の挙動が怪しい場合、interfaceメンバーに[Preserve]をつけるのを検討してもOK
namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    /// <summary>
    /// アバターのロード状態に関するAPIです。
    /// </summary>
    public interface IAvatarLoadEvent
    {
        /// <summary>
        /// アバターがロード済みであれば <c>true</c>、そうでなければ <c>false</c> を取得します。
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// アバターのロードが完了すると発火します。
        /// </summary>
        /// <remarks>
        /// サブキャラのロード時点ですでにアバターがロード済みだった場合、
        /// このイベントは <see cref="IRootApi.Start"/> よりも後、
        /// かつ <see cref="IRootApi.Update"/> より前に一度発火します。
        /// </remarks>
        event Action Loaded;
        
        /// <summary>
        /// アバターがアンロードされる時に発火します。
        /// このイベントは <see cref="IsLoaded"/> が <c>false</c> に切り替わった後で発火します。
        /// </summary>
        event Action Unloaded;
    }
    
    public interface IAvatarBodyParameter
    {
        //TODO: 身長っぽい値とかを入れるかもしれないやつ
        //あんまピンと来なければ無しにしてもいい
    }
    
    /// <summary>
    /// アバターの表情の状態に関するAPIです。
    /// </summary>
    public interface IAvatarFacial
    {
        /// <summary>
        /// 現在アバターに適用されている表情のうち、表情トラッキング以外の方法で明示的に適用された表情のBlendShapeClipの名称を取得します。
        /// </summary>
        /// <remarks>
        /// <para>
        ///   このプロパティでは、Word to Motion機能によって表情を切り替えたり、外部トラッキングによるFace Switch機能で表情を切り替えた場合の表情を取得できます。
        ///   HappyやSurprisedなど表情はこの方法で取得できます。
        /// </para>
        /// <para>
        ///   リップシンクやまばたきのブレンドシェイプはこのプロパティでは取得できません。
        /// </para>
        /// </remarks>
        string CurrentFacial { get; }
        
        /// <summary>
        /// マイク入力で発声を検知し、アバターにリップシンクが適用されている場合は<c>true</c>、それ以外の場合は<c>false</c>を取得します。
        /// </summary>
        /// <remarks>
        /// <para>
        ///   外部トラッキングによって口の開閉をトラッキングしている場合であっても、マイク入力が検知できなければ<c>false</c>を返します。
        /// </para>
        /// </remarks>
        bool IsTalking { get; }
        // TODO: LipSyncのAIUEOが分かるようなプロパティが欲しい & 声量のdB値も欲しい

        /// <summary>
        /// 外部トラッキング機能を使用し、かつパーフェクトシンクが有効な場合に<c>true</c>、それ以外の場合は<c>false</c>を取得します。
        /// </summary>
        bool UsePerfectSync { get; }

        /// <summary>
        /// アバターがまばたき動作を行うと発火します。
        /// </summary>
        /// <remarks>
        /// <para>
        ///   このイベントは自動まばたきを適用している場合の自動でのまばたき、外部トラッキングによる目のトラッキングによる瞬きに対して発火します。
        ///   外部トラッキングを使用していて目をゆっくり開閉した場合や、短時間で連続でまばたきを行った場合には発火しない場合があります。
        ///   ウインクに対してもこのイベントは発火しません。
        /// </para>
        /// </remarks>
        event Action OnBlinked;

        /// <summary>
        /// 現在ロードしているアバターに指定した名称のカスタムブレンドシェイプが定義されているかどうかを取得します。
        /// </summary>
        /// <param name="name">ブレンドシェイプ名</param>
        /// <returns></returns>
        /// <remarks>
        /// アバターがロードされていない場合、この関数は<c>false</c>を返します。
        /// </remarks>
        bool HasClip(string name);

        /// <summary>
        /// 指定したブレンドシェイプの現在の値を取得します。
        /// </summary>
        /// <param name="name">ブレンドシェイプ名</param>
        /// <param name="customKey">カスタムブレンドシェイプの値を取得する場合は<c>true</c>、そうでなければ<c>false</c>を指定します。</param>
        /// <returns>ブレンドシェイプの値。0以上、1以下の値を返します。</returns>
        /// <remarks>
        /// アバターがロードされていない場合や、指定したブレンドシェイプがアバターに定義されていない場合は、この関数は0を返します。
        /// </remarks>
        float GetCurrentValue(string name, bool customKey);
        
        //TODO: CurrentFacialあるから不要かも
        /// <summary>
        /// ユーザーが外部トラッキング機能に基づくFace Switch機能を使っている場合に、Face Switch機能で検出した表情の名称を取得します。
        /// </summary>
        /// <returns></returns>
        string GetActiveFaceSwitch();
    }

    public interface IAvatarPose
    {
        // NOTE: RootPositionはほぼゼロだが、Rotのほうはゲーム入力モードで回ることがあるので公開してもバチ当たらない…というモチベがある
        
        /// <summary>
        /// アバターの足元の位置をワールド座標の値として取得します。
        /// </summary>
        /// <returns>アバターの足元の位置</returns>
        Vector3 GetRootPosition();

        /// <summary>
        /// アバターの足元の回転をワールド座標の値として取得します。
        /// </summary>
        /// <returns>アバターの足元の回転</returns>
        /// <remarks>
        /// <para>
        ///   この値は <see cref="Quaternion.identity"/> に近い値であることが多いですが、
        ///   ゲーム入力モードによってアバターが正面以外の方向を向いている場合には、ヨー軸方向の回転が適用されます。
        /// </para>
        /// </remarks>
        Quaternion GetRootRotation();
        
        /// <summary>
        /// 指定したボーンの位置をワールド座標で取得します。
        /// </summary>
        /// <param name="bone">ボーン</param>
        /// <param name="useParentBone"><c>true</c> を指定した場合、<paramref name="bone"/> で指定したボーンが存在しないと親ボーンの値を代わりに返します。</param>
        /// <returns>ボーンの位置</returns>
        /// <remarks>
        /// <para>
        ///   アバターがロードされていない場合、この関数は <see cref="Vector3.zero"/> を返します。
        ///   また、任意ボーンがアバターに存在せず、かつ <paramref name="useParentBone"/> が <c>false</c> だった場合にも、この関数は <see cref="Vector3.zero"/> を返します。
        /// </para>
        /// <para>
        ///   <paramref name="useParentBone"/> が <c>true</c> の場合、任意ボーンがなければ有効な親ボーンまで遡ってボーン姿勢を返します。
        ///   例えば、親指ボーンがないアバターに対して <see cref="HumanBodyBones.LeftThumbProximal"/> を指定してこの関数を呼び出した場合、
        ///   左手首ボーンつまり <see cref="HumanBodyBones.LeftHand"/> のボーン位置を返します。
        /// </para>
        /// </remarks>
        Vector3 GetBoneGlobalPosition(HumanBodyBones bone, bool useParentBone = true);

        /// <summary>
        /// 指定したボーンの回転をワールド座標で取得します。
        /// </summary>
        /// <param name="bone">ボーン</param>
        /// <param name="useParentBone"><c>true</c> を指定した場合、<paramref name="bone"/> で指定したボーンが存在しないと親ボーンの値を代わりに返します。</param>
        /// <returns>ボーンの回転</returns>
        /// <remarks>
        /// <para>
        ///   アバターがロードされていない場合、この関数は <see cref="Quaternion.identity"/> を返します。
        ///   また、任意ボーンがアバターに存在せず、かつ <paramref name="useParentBone"/> が <c>false</c> だった場合にも、この関数は <see cref="Quaternion.identity"/> を返します。
        /// </para>
        /// <para>
        ///   <paramref name="useParentBone"/> が <c>true</c> の場合、任意ボーンがなければ有効な親ボーンまで遡ってボーン姿勢を返します。
        ///   例えば、親指ボーンがないアバターに対して <see cref="HumanBodyBones.LeftThumbProximal"/> を指定してこの関数を呼び出した場合、
        ///   左手首ボーンつまり <see cref="HumanBodyBones.LeftHand"/> のボーン回転を返します。
        /// </para>
        /// </remarks>
        Quaternion GetBoneGlobalRotation(HumanBodyBones bone, bool useParentBone = true);

        /// <summary>
        /// 指定したボーンの位置をローカル座標で取得します。
        /// </summary>
        /// <param name="bone">ボーン</param>
        /// <param name="useParentBone"><c>true</c> を指定した場合、<paramref name="bone"/> で指定したボーンが存在しないと親ボーンの値を代わりに返します。</param>
        /// <returns>ボーンの位置</returns>
        /// <remarks>
        /// ローカル座標は親ボーンに対しての姿勢になります。
        /// <paramref name="useParentBone"/> が <c>true</c> である場合の挙動の詳細は <see cref="GetBoneGlobalPosition"/> も参照してください。
        /// </remarks>
        Vector3 GetBoneLocalPosition(HumanBodyBones bone, bool useParentBone = true);
        
        /// <summary>
        /// 指定したボーンの回転をローカル座標で取得します。
        /// </summary>
        /// <param name="bone">ボーン</param>
        /// <param name="useParentBone"><c>true</c> を指定した場合、<paramref name="bone"/> で指定したボーンが存在しないと親ボーンの値を代わりに返します。</param>
        /// <returns>ボーンの回転</returns>
        /// <remarks>
        /// ローカル座標は親ボーンに対しての姿勢になります。
        /// <paramref name="useParentBone"/> が <c>true</c> である場合の挙動の詳細は <see cref="GetBoneGlobalPosition"/> も参照してください。
        /// </remarks>
        Quaternion GetBoneLocalRotation(HumanBodyBones bone, bool useParentBone = true);
    }

    public interface IAvatarMotionEvent
    {
        /// <summary>
        /// アバターがキーボードを打鍵するモーションを行う状態になっており、実際に打鍵モーションを開始した場合に発火します。
        /// </summary>
        /// <remarks>
        /// <para>
        /// キー名はENTERキーの打鍵時に <c>"Enter"</c> を引数とします。
        /// それ以外のキーについては、打鍵時してもキー名は特定できず、空文字列が引数となります。
        /// </para>
        /// <para>
        /// ユーザーが打鍵をランダム表示するオプションを有効にしている場合、常に空文字列が引数になります。
        /// </para>
        /// </remarks>
        event Action<string> OnKeyboardKeyDown;

        /// <summary>
        /// アバターがタッチパッド (マウスパッド) を操作するモーションを行う状態であり、かつマウスのクリック動作を開始したときに発火します。
        /// </summary>
        /// <remarks>
        /// このイベントではマウスのどのボタンに対してイベントが発火したかは公開されません。
        /// </remarks>
        event Action OnTouchPadMouseButtonDown;

        /// <summary>
        /// アバターがペンタブレットを操作するモーションを行う状態であり、かつペンタブレットのクリック動作を開始したときに発火します。
        /// </summary>
        /// <remarks>
        /// このイベントではマウスのどのボタンに対してイベントが発火したかは公開されません。
        /// </remarks>
        event Action OnPenTabletMouseButtonDown;

        /// <summary>
        /// アバターがゲームパッドを操作するモーションを行う状態であり、かつゲームパッドのボタンが押し下げられると発火します。
        /// </summary>
        event Action<GamepadButton> OnGamepadButtonDown;

        /// <summary>
        /// アバターがアーケードスティックを操作するモーションを行う状態であり、かつゲームパッドのボタンが押し下げられると発火します。
        /// </summary>
        event Action<GamepadButton> OnArcadeStickButtonDown;
    }
    
    /// <summary>
    /// UnityEngineのHumanBodyBonesと同じ順序で定義された人型ボーン情報の一覧です。
    /// ただし、UnityEngineの値とは異なり、無効なボーンを表す <see cref="HumanBodyBones.None"/> が追加で定義されています。
    /// </summary>
    public enum HumanBodyBones
    {
        None = -1,
        Hips = 0,
        LeftUpperLeg,
        RightUpperLeg,
        LeftLowerLeg,
        RightLowerLeg,
        LeftFoot,
        RightFoot,
        Spine,
        Chest,
        Neck,
        Head,
        LeftShoulder,
        RightShoulder,
        LeftUpperArm,
        RightUpperArm,
        LeftLowerArm,
        RightLowerArm,
        LeftHand,
        RightHand,
        LeftToes,
        RightToes,
        LeftEye,
        RightEye,
        Jaw,
        LeftThumbProximal,
        LeftThumbIntermediate,
        LeftThumbDistal,
        LeftIndexProximal,
        LeftIndexIntermediate,
        LeftIndexDistal,
        LeftMiddleProximal,
        LeftMiddleIntermediate,
        LeftMiddleDistal,
        LeftRingProximal,
        LeftRingIntermediate,
        LeftRingDistal,
        LeftLittleProximal,
        LeftLittleIntermediate,
        LeftLittleDistal,
        RightThumbProximal,
        RightThumbIntermediate,
        RightThumbDistal,
        RightIndexProximal,
        RightIndexIntermediate,
        RightIndexDistal,
        RightMiddleProximal,
        RightMiddleIntermediate,
        RightMiddleDistal,
        RightRingProximal,
        RightRingIntermediate,
        RightRingDistal,
        RightLittleProximal,
        RightLittleIntermediate,
        RightLittleDistal,
        UpperChest,
        LastBone,
    }
}
