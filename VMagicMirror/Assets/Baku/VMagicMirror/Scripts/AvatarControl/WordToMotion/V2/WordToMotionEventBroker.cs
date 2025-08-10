using System;
using R3;

namespace Baku.VMagicMirror.WordToMotion
{
    public class WordToMotionEventBroker
    {
        private readonly Subject<(MotionRequest request, float duration)> _started = new();
        public IObservable<(MotionRequest request, float duration)> Started => _started;

        private readonly Subject<Unit> _stopped = new();
        public IObservable<Unit> Stopped => _stopped;
        
        /// <summary>
        /// PreviewではないWord to Motionが実行されたとき、その実行時間とセットで呼び出す。
        /// durationは秒単位であり、ループモーションに対しては負の値を指定する
        /// </summary>
        /// <param name="request"></param>
        /// <param name="duration"></param>
        public void NotifyStarted(MotionRequest request, float duration)
            => _started.OnNext((request, duration));

        /// <summary>
        /// プレビューではないWtMが明示的に停止した場合に呼び出す。
        /// 「実行中のWtMがあるときの他のWtMを実行した」みたいな状況では呼ばないでOK
        /// </summary>
        public void NotifyStopped() => _stopped.OnNext(Unit.Default);

    }
}
