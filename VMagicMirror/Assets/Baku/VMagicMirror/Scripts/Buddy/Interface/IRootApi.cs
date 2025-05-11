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
        /// メインアバターの姿勢や表情の状態出力、およびユーザーのマイク入力など、メインアバターの制御に関する情報にアクセス可能かどうかを取得します。
        /// </summary>
        /// <remarks>
        /// この値はアプリケーションのEditionおよびユーザー設定によって変化します。
        /// 値が <c>false</c> の場合、 <see cref="AvatarMotionEvent"/> のイベントが発火しなかったり、 <see cref="AvatarPose"/> で有効なポーズが取得できなかったりする状態になります。
        /// 
        /// ユーザー入力がないとスタックしてしまうような挙動をサブキャラに実装する場合、このフラグを組み合わせて挙動をカスタムすることでスタックを防げます。
        /// </remarks>
        bool AvatarOutputFeatureEnabled { get; }
        
        /// <summary>
        /// このサブキャラに関する一時的なデータを保存するためのディレクトリの絶対パスを取得します。
        /// </summary>
        /// <remarks>
        /// このディレクトリは "(My Documents)/VMM_Files/Cache/Buddy/{buddy_name}" のような形式のパスになります。
        /// スクリプトの実行前にディレクトリの生成が保証されます。
        ///
        /// ダウンロードしたデータ等、サブキャラ本体と別で動的に取得したデータはこのディレクトリ以下に保存することを推奨します。
        /// また、ユーザーはこのディレクトリに含まれるファイルを削除する場合があることに注意して下さい。
        /// </remarks>
        string CacheDirectory { get; }
        
        /// <summary>
        /// サブキャラのロード後に一度呼ばれます。
        /// </summary>
        event Action Start;
        
        /// <summary>
        /// 毎フレームごと、つまり描画内容の更新のたびに呼ばれます。
        /// 引数には前回のフレームからの経過時間が秒単位で渡されます。
        /// </summary>
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

        /// <summary> マニフェストで定義されたプロパティの現在値にアクセスできるAPIを取得します。 </summary>
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
        
        // NOTE: まだ実装が安定してないのでダメ
        //IAudio Audio { get; }

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

        // NOTE: Luaじゃないから不要 or 名前もうちょい短くしたい…？
        bool ValidateFilePath(string path);

        ISprite2D Create2DSprite();
        ISprite3D Create3DSprite();
        IGlb CreateGlb();
        IVrm CreateVrm();
        IVrmAnimation CreateVrmAnimation();
        
        /// <summary>
        /// アプリケーションに適用されている言語を取得します。
        /// </summary>
        /// <remarks>
        /// <para>
        /// VMagicMirrorではローカライズシステムの実装都合により、この値は日英以外の言語選択を正しく判別しません。
        /// 日英以外の言語が <see cref="AppLanguage.Unknown"/> として判定される場合があることに注意して下さい。
        /// </para>
        /// <para>
        /// 多言語に詳細に対応できるサブキャラを作る場合、 <see cref="IProperty"/> でユーザーが言語選択を選べるようにすることも検討して下さい。
        /// </para>
        /// </remarks>
        AppLanguage Language { get; }
    }

    /// <summary> VMagicMirrorの表示に使用している言語です。 </summary>
    /// <remarks>
    /// <para>
    /// VMagicMirrorではローカライズシステムの実装都合により、この値は日英以外の言語選択を正しく判別しません。
    /// 日英以外の言語が <see cref="Unknown"/> として判定される場合があることに注意して下さい。
    /// </para>
    /// <para>
    /// 多言語に詳細に対応できるサブキャラを作る場合、 <see cref="IProperty"/> でユーザーが言語選択を選べるようにすることも検討して下さい。
    /// </para>
    /// </remarks>
    public enum AppLanguage
    {
        /// <summary> 日本語、英語のいずれでもない言語 </summary>
        Unknown = 0,
        /// <summary> 日本語 </summary>
        Japanese,
        /// <summary> 英語 </summary>
        English,
    }
}
