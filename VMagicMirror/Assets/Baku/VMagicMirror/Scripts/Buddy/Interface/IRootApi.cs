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
        /// サブキャラのロード後に一度呼ばれます。
        /// </summary>
        event Action Start;
        
        /// <summary>
        /// 毎フレームごと、つまり描画内容の更新のたびに呼ばれます。
        /// 引数には前回のフレームからの経過時間が秒単位で渡されます。
        /// </summary>
        event Action<float> Update;

        SynchronizationContext MainThreadContext { get; }
        Task RunOnMainThread(Task task);
        
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
        IAudio Audio { get; }
        IScreen Screen { get; }
        IGui Gui { get; }

        //TODO: 出力先ファイルがどこなのか説明を書きたい
        /// <summary>
        /// ログ情報を出力します。
        /// </summary>
        /// <param name="value"></param>
        void Log(string value);
        void LogWarning(string value);
        void LogError(string value);
        
        /// <summary>
        /// 0以上、1未満のランダムな値を取得します。
        /// </summary>
        /// <returns></returns>
        float Random();
        void InvokeDelay(Action func, float delaySeconds);
        void InvokeInterval(Action func, float intervalSeconds);
        void InvokeInterval(Action func, float intervalSeconds, float firstDelay);

        // NOTE: Luaじゃないから不要 or 名前もうちょい短くしたい…？
        bool ValidateFilePath(string path);

        ISprite2D Create2DSprite();
        ISprite3D Create3DSprite();
        IGlb CreateGlb();
        IVrm CreateVrm();
        IVrmAnimation CreateVrmAnimation();
        
        //TODO: コレ系の設定がうまくbundleできると嬉しい
        AppLanguage Language { get; }
    }

    /// <summary> VMagicMirrorの表示に使用している言語です。 </summary>
    /// <remarks>
    /// <para>
    /// VMagicMirrorではローカライズシステムの実装都合により、この値は日英以外の言語選択を正しく判別しません。
    /// 日英以外の言語が <see cref="Unknown"/> として判定される場合があることに注意して下さい。
    /// </para>
    /// <para>
    /// 多言語に対応したサブキャラを作成したい場合、 <see cref="IProperty"/> でユーザーによる言語選択を個別にサポートすることを検討して下さい。
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
