using System;
using System.Collections.Generic;

namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    /// <summary>
    /// Scriptから `Api` 変数としてアクセスできるような、APIのベースになるインスタンス。
    /// </summary>
    public interface IRootApi
    {
        public IReadOnlyList<ISprite2DApi> Sprites { get; }

        Action Start { get; set; }
        Action<float> Update { get; set; }

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

        void Log(string value);
        float Random();
        void InvokeDelay(Action func, float delaySeconds);
        void InvokeInterval(Action func, float intervalSeconds);
        void InvokeInterval(Action func, float intervalSeconds, float firstDelay);

        // NOTE: Luaじゃないから不要 or 名前もうちょい短くしたい…？
        bool ValidateFilePath(string path);

        ISprite2DApi Create2DSprite();
    }
}
