using System;
using System.Threading;
using Baku.VMagicMirror.MotionExporter;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.WordToMotion
{
    //カスタムモーションの実行状態を管理できるクラス。
    //このクラスが2つ以上動くことによって補間が働く、はず
    public class CustomMotionPlayState : IDisposable
    {
        private enum PlayPhase
        {
            Idle,
            FadeIn,
            Playing,
            FadeOut,
        }

        public const float FadeDuration = 0.5f;

        public CustomMotionPlayState(
            HumanPoseHandler humanPoseHandler,
            HumanoidAnimationSetter setter, 
            IObservable<Unit> lateUpdateSource)
        {
            _humanPoseHandler = humanPoseHandler;
            _setter = setter;
            _lateUpdateSource = lateUpdateSource;
        }

        private readonly HumanPoseHandler _humanPoseHandler;
        private readonly HumanoidAnimationSetter _setter;
        private readonly IObservable<Unit> _lateUpdateSource;

        //NOTE: HumanPoseHandlerで使うだけ
        private HumanPose _humanPose;

        private CustomMotionItem _item;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private float _count;
        private PlayPhase _phase = PlayPhase.Idle;
        
        public bool IsRunningLoopMotion { get; private set; }
        public CustomMotionItem CurrentItem => _item;
        public bool HasUpdate { get; private set; }

        public void RunMotion(CustomMotionItem item)
        {
            //3モーションを立て続けに実行しようとする、とかの場合だけidleでないstateをとる
            if (_phase != PlayPhase.Idle)
            {
                StopImmediate();
            }
            _item = item;
            RunMotionAsync(item, _cts.Token).Forget();
        }
        
        public void RunLoopMotion(CustomMotionItem item)
        {
            if (_phase != PlayPhase.Idle)
            {
                StopImmediate();
            }
            _item = item;
            IsRunningLoopMotion = true;
            RunLoopMotionAsync(item, _cts.Token).Forget();
        }

        public void FadeOutCurrentMotion()
        {
            if (_item == null || 
                _phase == PlayPhase.Idle || 
                _phase == PlayPhase.FadeOut)
            {
                return;
            }
            
            RefreshCts();
            //countはリセットしないで引き継ぐことに注意
            FadeOutMotionAsync(_item, _cts.Token).Forget();
        }

        public void StopImmediate()
        {
            RefreshCts();
            _count = 0f;
            _item = null;
            _phase = PlayPhase.Idle;
            IsRunningLoopMotion = false;
        }

        private void RefreshCts()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
        }
        
        private void PrepareItem(CustomMotionItem item)
        {
            _item = item;
            _setter.SetUsedFlags(_item.UsedFlags);
            _count = 0f;
        }

        private void ClearItem()
        {
            _item = null;
            _count = 0f;
            _phase = PlayPhase.Idle;
        }
        
        private async UniTaskVoid RunMotionAsync(CustomMotionItem item, CancellationToken cancellationToken)
        {
            PrepareItem(item);
            var count = 0f;
            var duration = item.Motion.Duration;

            _phase = PlayPhase.FadeIn;
            while (count < duration)
            {
                //NOTE: 同じモーションを複数のStateで使ってる可能性があるため、毎回Targetをチェックする
                item.Motion.Target = _setter;
                item.Motion.Evaluate(count);
                var useRate = (count < FadeDuration || count > duration - FadeDuration);
                //モーションの出入りが補間される
                var rate =
                    count < FadeDuration ? Mathf.Clamp01(count / FadeDuration) :
                    count > duration - FadeDuration ? Mathf.Clamp01((duration - count) / FadeDuration) :
                    1f;
                WriteCurrentPose(useRate, rate);
                await _lateUpdateSource.ToUniTask(true, cancellationToken);
                count += Time.deltaTime;

                if (_phase == PlayPhase.FadeIn && count > FadeDuration)
                {
                    _phase = PlayPhase.Playing;
                }
                else if (_phase == PlayPhase.Playing && count > duration - FadeDuration)
                {
                    _phase = PlayPhase.FadeOut;
                }
            }

            ClearItem();
        }

        private async UniTaskVoid RunLoopMotionAsync(CustomMotionItem item, CancellationToken cancellationToken)
        {
            try
            {
                PrepareItem(item);
                _phase = PlayPhase.FadeIn;
                var isFirstRun = true;
                while (!cancellationToken.IsCancellationRequested)
                {
                    var count = 0f;
                    while (count < item.Motion.Duration)
                    {
                        //NOTE: 同じモーションを複数のStateで使ってる可能性があるため、毎回Targetをチェックする
                        item.Motion.Target = _setter;
                        item.Motion.Evaluate(count);
                        var useRate = isFirstRun && count < FadeDuration;
                        var rate = useRate ? count / FadeDuration : 1f;
                        WriteCurrentPose(useRate, rate);

                        await _lateUpdateSource.ToUniTask(true, cancellationToken);
                        count += Time.deltaTime;

                        if (isFirstRun && count > FadeDuration)
                        {
                            _phase = PlayPhase.Playing;
                        }
                    }

                    isFirstRun = false;
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                IsRunningLoopMotion = false;
            }
        }
        
        private async UniTaskVoid FadeOutMotionAsync(CustomMotionItem item, CancellationToken cancellationToken)
        {
            //モーションの出だしでキャンセルした場合だけフェードアウトの所要時間が短い
            var startRate = 1f;
            var duration = FadeDuration;
            if (_count < FadeDuration)
            {
                duration = _count;
                startRate = _count / FadeDuration;
            }

            if (duration < 0.01f)
            {
                ClearItem();
                return;
            }

            _phase = PlayPhase.FadeOut;
            var startCount = _count;
            var endTime = _count + duration;
            while (_count < endTime)
            {
                var rate = Mathf.InverseLerp(startRate, 0, (_count - startCount) / duration);
                //NOTE: 同じモーションを複数のStateで使ってる可能性があるため、毎回Targetをチェックする
                item.Motion.Target = _setter;
                item.Motion.Evaluate(_count);
                WriteCurrentPose(true, rate);

                await _lateUpdateSource.ToUniTask(true, cancellationToken);
                _count += Time.deltaTime;
            }
            
            ClearItem();
        }
        
        private void WriteCurrentPose(bool useRate, float rate)
        {
            _humanPoseHandler.GetHumanPose(ref _humanPose);
            if (useRate)
            {
                _setter.WriteToPose(ref _humanPose, rate);
            }
            else
            {
                _setter.WriteToPose(ref _humanPose);
            }
            _humanPoseHandler.SetHumanPose(ref _humanPose);
            HasUpdate = true;
        }

        public void ResetUpdateFlag()
        {
            HasUpdate = false;
        }
        
        public void Dispose()
        {
            RefreshCts();
        }
    }
}
