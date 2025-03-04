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
    }

    public enum LogLevel
    {
        None,
        Error,
        Warning,
        Log,
    }
}
