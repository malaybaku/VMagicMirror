using System;
using R3;

namespace Baku.VMagicMirror.WordToMotion
{
    //Word to Motionの専用入力に使うデバイスを指定する定数値
    public enum SourceType
    {
        None = -1,
        KeyboardTyping = 0,
        Gamepad = 1,
        KeyboardTenKey = 2,
        Midi = 3,
        //NOTE: 処理フロー次第でここにUDPとかホットキーを入れてもよい
    }

    public interface IRequestSource
    {
        SourceType SourceType { get; }
        IObservable<int> RunMotionRequested { get; }
        void SetActive(bool active);
    }

    public class EmptyRequestSource : IRequestSource
    {
        public SourceType SourceType => SourceType.None;
        public IObservable<int> RunMotionRequested { get; } = Observable.Empty<int>();

        public void SetActive(bool active)
        {
        }
    }
}
