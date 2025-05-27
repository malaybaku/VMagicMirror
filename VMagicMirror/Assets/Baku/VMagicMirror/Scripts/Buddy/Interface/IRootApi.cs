using System;
using System.Threading;
using System.Threading.Tasks;

namespace VMagicMirror.Buddy
{
    /// <summary>
    /// スクリプトから <c>Api</c> 変数としてアクセスできるような、サブキャラの制御に利用できるAPI群です。
    /// </summary>
    public interface IRootApi
    {
        /// <summary>
        /// メインアバターの姿勢や表情、およびユーザーのマイク入力などを取り扱えるインタラクションAPI全般にアクセス可能かどうかを取得します。
        /// </summary>
        /// <remarks>
        /// この値はアプリケーションのEditionおよびユーザー設定によって変化します。
        /// 値が <c>false</c> の場合、 <see cref="AvatarMotionEvent"/> のイベントが発火しなかったり、 <see cref="AvatarPose"/> で有効なポーズが取得できなかったりする状態になります。
        /// 
        /// ユーザー入力がないと進行不能になってしまうような挙動をサブキャラに対して実装する場合、
        /// このフラグも組み合わせて挙動をカスタムすることで意図しないスタックを防げます。
        /// </remarks>
        bool InteractionApiEnabled { get; }
        
        /// <summary>
        /// このサブキャラのデータが入っているディレクトリの絶対パスを取得します。
        /// </summary>
        /// <remarks>
        /// <para>
        /// このプロパティは <c>main.csx</c> スクリプトを含むディレクトリの絶対パスを返します。
        /// 静的なデータ定義を行ったテキストなど、サブキャラに同梱したファイルがある場合はこのディレクトリを参照してファイルを読み込めます。
        /// </para>
        ///
        /// <para>
        /// <see cref="ISprite2D.Show(string)"/> 等では、パスを相対パスとして評価する場合、このプロパティで取得できるディレクトリからの相対パスとして評価します。
        /// そのため、サブキャラのオブジェクトを単に表示する場合、必ずしもこのプロパティを参照する必要はありません。
        /// </para>
        /// </remarks>
        string BuddyDirectory { get; }

        // NOTE: File IOのAPIについて再検討することにしたのでナシ
        // /// <summary>
        // /// このサブキャラに関する一時的なデータを保存するためのディレクトリの絶対パスを取得します。
        // /// </summary>
        // /// <remarks>
        // /// このディレクトリは "(My Documents)/VMM_Files/Cache/Buddy/{buddy_name}" のような形式のパスになります。
        // /// スクリプトの実行前にディレクトリの生成が保証されます。
        // ///
        // /// スクリプトの実行時にダウンロードしたデータなど、サブキャラ本体とは別で動的に取得したデータをファイルに保存したい場合は
        // /// このディレクトリ以下に保存することを推奨します。
        // /// また、ユーザーはこのディレクトリに含まれるファイルを削除する場合があることに注意して下さい。
        // /// </remarks>
        // string CacheDirectory { get; }
        
        /// <summary>
        /// サブキャラのロード後に一度だけ呼ばれます。
        /// </summary>
        /// <remarks>
        /// <see cref="Update"/> の最初の呼び出しよりも前に呼ばれることが保証されます。
        /// </remarks>
        event Action Start;
        
        /// <summary>
        /// 毎フレームごと、つまり描画内容の更新のたびに呼ばれます。
        /// 引数には、前回のフレームからの経過時間が秒単位で渡されます。
        /// </summary>
        /// <remarks>
        /// 引数は通常、 1/60 に近い値になります。
        ///
        /// このイベントは毎フレーム呼ばれるため、長時間かかる処理をイベントハンドラとして実行するのは避けるようにしてください。
        /// </remarks>
        event Action<float> Update;

        /// <summary>
        /// アプリケーションのメインスレッドのコンテキストを取得します。
        /// </summary>
        /// <remarks>
        /// スクリプト上で非同期処理を行った結果をスプライトやVRMアバターに適用したい場合、メインスレッドに処理を戻るときに使用できます。
        ///
        /// より簡単にメインスレッドへアクセスしたい場合、変わりに <see cref="RunOnMainThread"/> を使用することを検討してください。
        /// </remarks>
        SynchronizationContext MainThreadContext { get; }

        /// <summary>
        /// サブキャラが終了するときにキャンセル扱いされるようなCancellationTokenを取得します。
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// サブキャラの挙動として非同期処理を実装する場合、この値を使用することで、サブキャラが意図せず実行され続けることを防止できます。
        /// </remarks>
        CancellationToken GetCancellationTokenOnDisabled();
        
        /// <summary>
        /// 指定したタスクをメインスレッドで実行します。
        /// </summary>
        /// <param name="task">メインスレッドで実行したいタスク</param>
        /// <returns></returns>
        /// <remarks>
        /// VMagicMirrorはUnity Engineで実行されており、VRMやスプライトの操作はゲームエンジンのメインスレッド上で行う必要があります。
        /// スクリプト上でTaskによる非同期処理を行う場合、このメソッドを用いることで、非同期処理の結果をサブキャラに適用しやすくなります。
        /// </remarks>
        void RunOnMainThread(Func<Task> task);
        
        //TODO: FeatureLockについては、ここで記述されるプロパティ単位で
        //「丸ごとOK or 丸ごと塞がってる」となるのが分かりやすさ的には望ましい

        /// <summary> マニフェストで定義されたプロパティにアクセスできるAPIを取得します。 </summary>
        IProperty Property { get; }

        /// <summary> マニフェストで定義されたTransformの参照にアクセスできるAPIを取得します。 </summary>
        IManifestTransforms Transforms { get; }
        
        /// <summary> アバターの周辺のデバイス配置に関するAPIを取得します。 </summary>
        IDeviceLayout DeviceLayout { get; }
        
        // NOTE: このへん `api.Avatar.MotionEvent` みたく書けたほうが字面がいいから修正しそう
        IAvatarLoadEvent AvatarLoadEvent { get; }
        IAvatarPose AvatarPose { get; }
        IAvatarMotionEvent AvatarMotionEvent { get; }
        IAvatarFacial AvatarFacial { get; }
        IInput Input { get; }

        /// <summary>
        /// 音声ファイルの再生に関するAPIを取得します。
        /// </summary>
        /// <remarks>
        /// v4.0.0の時点でこのAPIはサウンドエフェクト等、短い音声の再生のみを想定したAPIとなっています。
        /// </remarks>
        IAudio Audio { get; }

        /// <summary>
        /// アバターを表示しているウィンドウに関するAPIを取得します。
        /// </summary>
        IScreen Screen { get; }
        
        // NOTE: まだ実装が安定してないのでダメ
        //IGui Gui { get; }

        //TODO: 出力先ファイルがどこなのか説明を書きたい
        /// <summary>
        /// ログ情報を出力します。
        /// </summary>
        /// <param name="value"></param>
        /// <remarks>
        /// <para>
        /// ログファイルは <c>VMagicMirror_Files/Logs/{buddy_name}.txt</c> として出力されます。
        /// <c>buddy_name</c> には、サブキャラのデータを格納しているフォルダ名が入ります。
        /// </para>
        ///
        /// <para>
        /// このメソッドで出力するログは、VMagicMirrorのサブキャラ設定で開発者モードを有効にし、ログ詳細度を <c>Info</c> かそれより詳細なレベルに設定した場合のみ出力されます。
        /// </para>
        /// </remarks>
        void Log(string value);
 
        /// <summary>
        /// 警告相当のログ情報を出力します。
        /// </summary>
        /// <param name="value"></param>
        /// <remarks>
        /// <para>
        /// ログファイルは <c>VMagicMirror_Files/Logs/{buddy_name}.txt</c> として出力されます。
        /// <c>buddy_name</c> には、サブキャラのデータを格納しているフォルダ名が入ります。
        /// </para>
        ///
        /// <para>
        /// このメソッドで出力するログは、VMagicMirrorのサブキャラ設定で開発者モードを有効にし、ログ詳細度を <c>Warning</c> かそれより詳細なレベルに設定した場合のみ出力されます。
        /// </para>
        /// </remarks>
        void LogWarning(string value);

        /// <summary>
        /// エラー相当のログ情報を出力します。
        /// </summary>
        /// <param name="value"></param>
        /// <remarks>
        /// <para>
        /// ログファイルは <c>VMagicMirror_Files/Logs/{buddy_name}.txt</c> として出力されます。
        /// <c>buddy_name</c> には、サブキャラのデータを格納しているフォルダ名が入ります。
        /// </para>
        ///
        /// <para>
        /// このメソッドで出力するログは、VMagicMirrorのサブキャラ設定で開発者モードがオフであるか、または開発者モードでログ詳細度を <c>Error</c> かそれより詳細なレベルに設定した場合のみ出力されます。
        /// </para>
        /// </remarks>
        void LogError(string value);
        
        /// <summary>
        /// アプリケーションに適用されている言語を取得します。
        /// </summary>
        /// <remarks>
        /// <para>
        /// VMagicMirrorのローカライズシステムの実装都合により、この値は日本語・英語以外の言語選択を行うと <see cref="AppLanguage.Unknown"/> を返します。
        /// </para>
        /// <para>
        /// 多くの言語に対応できるようなサブキャラを作成する場合、このプロパティの代わりに <see cref="IProperty"/> によって
        /// ユーザーが言語選択を別途選択できるようにすることも検討して下さい。
        /// </para>
        /// </remarks>
        AppLanguage Language { get; }
        
        /// <summary>
        /// 0以上、1未満のランダムな値を取得します。
        /// </summary>
        /// <returns></returns>
        float Random();

        /// <summary>
        /// 指定した処理を、指定された秒数のあとで実行します。
        /// </summary>
        /// <param name="func">実行したい関数</param>
        /// <param name="delaySeconds">実行のディレイ秒数</param>
        /// <remarks>
        /// このメソッドは、メインアバターの仕草を検出したとき、やや遅れてリアクションを取る場合などに有効です。
        /// </remarks>
        void InvokeDelay(Action func, float delaySeconds);

        /// <summary>
        /// 指定した処理を、指定された間隔で実行します。
        /// </summary>
        /// <param name="func">実行したい関数</param>
        /// <param name="intervalSeconds">実行間隔の秒数</param>
        /// <remarks>
        /// このメソッドは、Updateより低頻度で処理を実行したい場合などに有効です。
        ///
        /// このメソッドを呼び出すと、直ちに <paramref name="func"/> が1回実行されます。
        /// 初回の呼び出しを遅延させる場合、 <see cref="InvokeInterval(Action, float, float)"/> を使用します。
        /// </remarks>
        void InvokeInterval(Action func, float intervalSeconds);

        /// <summary>
        /// 指定した処理を、指定された間隔で実行します。
        /// </summary>
        /// <param name="func">実行したい関数</param>
        /// <param name="intervalSeconds">実行間隔の秒数</param>
        /// <param name="firstDelay">初回に処理を実行するまでの遅延の秒数</param>
        /// <remarks>
        /// このメソッドは、Updateより低頻度で処理を実行したい場合などに有効です。
        /// </remarks>
        void InvokeInterval(Action func, float intervalSeconds, float firstDelay);

        /// <summary>
        /// アバターウィンドウの最前面に画像を表示するためのスプライトのインスタンスを生成します。
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// 空間上へ3次元的に板状の画像を表示したい場合、代わりに <see cref="Create3DSprite"/> を使用します。
        /// </remarks>
        ISprite2D Create2DSprite();
        
        /// <summary>
        /// 画像を空間上で板状のオブジェクトとして配置するためのスプライトのインスタンスを生成します。
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// 平面的に画像を表示したい場合、代わりに <see cref="Create2DSprite"/> を使用します。
        /// </remarks>
        ISprite3D Create3DSprite();

        // NOTE: ISprite3D以外の3D系APIは安定性は検証不十分のためv4.0.0ではオミットされている

        // /// <summary>
        // /// 空間上にGLBオブジェクトを配置するためのインスタンスを生成します。
        // /// </summary>
        // /// <returns></returns>
        // IGlb CreateGlb();
        //
        // /// <summary>
        // /// 空間上にVRMアバターを配置するためのインスタンスを生成します。
        // /// </summary>
        // /// <returns></returns>
        // IVrm CreateVrm();
        //
        // /// <summary>
        // /// <see cref="IVrm"/> として表示したアバターに適用するためのVRMアニメーションのためのインスタンスを生成します。
        // /// </summary>
        // /// <returns></returns>
        // IVrmAnimation CreateVrmAnimation();
    }

    /// <summary> VMagicMirrorの表示に使用している言語です。 </summary>
    /// <remarks>
    /// <para>
    /// VMagicMirrorのローカライズシステムの実装都合により、日本語・英語以外の言語は <see cref="Unknown"/> として扱われます。
    /// </para>
    /// <para>
    /// 多言語に詳細に対応できるサブキャラを作る場合、 <see cref="IProperty"/> を通じて言語選択UIを提供することも検討してください。
    /// </para>
    /// </remarks>
    public enum AppLanguage
    {
        /// <summary> 日本語、英語のいずれでもない言語 </summary>
        Unknown = 0,
        /// <summary> 日本語 </summary>
        Japanese = 1,
        /// <summary> 英語 </summary>
        English = 2,
    }
}
