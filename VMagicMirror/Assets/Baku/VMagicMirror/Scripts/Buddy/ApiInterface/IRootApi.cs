using System;
using System.Threading;
using System.Threading.Tasks;

namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    /// <summary>
    /// スクリプトから <c>Api</c> 変数としてアクセスできるような、サブキャラの制御に利用できるAPI群です。
    /// </summary>
    public interface IRootApi
    {
        /// <summary>
        /// サブキャラのロード後に一度呼ばれます。
        /// </summary>
        event Action Start;
        event Action<float> Update;

        SynchronizationContext MainThreadContext { get; }
        Task RunOnMainThread(Task task);
        
        //TODO: FeatureLockについては、ここで記述されるプロパティ単位で
        //「丸ごとOK or 丸ごと塞がってる」となるのが分かりやすさ的には望ましい

        //NOTE: プロパティ形式で取得できるAPIは、スクリプトが最初に呼ばれる前に非nullで初期化されるのが期待値
        IPropertyApi Property { get; }
        ITransformsApi Transforms { get; }
        IDeviceLayoutApi DeviceLayout { get; }
        
        // NOTE: このへん `api.Avatar.MotionEvent` みたく書けたほうが字面がいいから修正しそう
        IAvatarLoadEventApi AvatarLoadEvent { get; }
        IAvatarPoseApi AvatarPose { get; }
        IAvatarMotionEventApi AvatarMotionEvent { get; }
        IAvatarFacialApi AvatarFacial { get; }
        IInputApi Input { get; }
        IAudioApi Audio { get; }
        IScreenApi Screen { get; }
        IGuiApi Gui { get; }

        void Log(string value);
        void LogWarning(string value);
        void LogError(string value);
        void SetLogLevel(LogLevel level);
        
        float Random();
        void InvokeDelay(Action func, float delaySeconds);
        void InvokeInterval(Action func, float intervalSeconds);
        void InvokeInterval(Action func, float intervalSeconds, float firstDelay);

        // NOTE: Luaじゃないから不要 or 名前もうちょい短くしたい…？
        bool ValidateFilePath(string path);

        ISprite2DApi Create2DSprite();
        ISprite3DApi Create3DSprite();
        IGlbApi CreateGlb();
        IVrmApi CreateVrm();
        
        //TODO: コレ系の設定がうまくbundleできると嬉しい
        AppLanguage Language { get; }
    }

    /// <summary>
    /// サブキャラのログ出力の詳細度です。
    /// </summary>
    /// <remarks>
    ///
    /// </remarks>
    public enum LogLevel
    {
        /// <summary>  </summary>
        None,
        Error,
        Warning,
        Log,
    }

    /// <summary> VMagicMirrorの表示に使用している言語です。 </summary>
    /// <remarks>
    /// <para>
    /// VMagicMirrorではローカライズシステムの実装都合により、この値は日英以外の言語選択を正しく判別しません。
    /// 日英以外の言語が <see cref="Unknown"/> として判定される場合があることに注意して下さい。
    /// </para>
    /// <para>
    /// 多言語に対応したサブキャラを作成したい場合、 <see cref="IPropertyApi"/> でユーザーによる言語選択を個別にサポートすることを検討して下さい。
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
