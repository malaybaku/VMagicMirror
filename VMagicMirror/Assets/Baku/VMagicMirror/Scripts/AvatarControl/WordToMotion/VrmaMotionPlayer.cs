using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class VrmaMotionPlayer : PresenterBase, ILateTickable, IWordToMotionPlayer
    {
        public const float FadeDuration = 0.5f;
        
        public VrmaMotionPlayer(
            VrmaRepository repository,
            IVRMLoadable vrmLoadable,
            VrmaMotionSetter motionSetter)
        {
            _repository = repository;
            _vrmLoadable = vrmLoadable;
            _motionSetter = motionSetter;
        }
        
        private readonly IVRMLoadable _vrmLoadable;
        private readonly VrmaRepository _repository;
        private readonly VrmaMotionSetter _motionSetter;
        private readonly VrmaMotionSetterLocker _motionSetterLocker = new();

        private bool _hasModel;
        private bool _playing;
        private bool _playingPreview;
        //「VRMAの再生中にStop()が呼ばれて停止処理中である」という状態のときだけtrue
        private bool _stopRunning;

        //NOTE: Previewでないアニメーションの再生(in/outのフェード込み)に相当する。
        //停止処理をなめらかに行う…みたいなのも含む
        private CancellationTokenSource _cts;

        public override void Initialize()
        {
            _vrmLoadable.VrmLoaded += OnModelLoaded;
            _vrmLoadable.VrmDisposing += OnModelDisposed;
        }

        public override void Dispose()
        {
            base.Dispose();
            CancelCurrentPlay();
        }

        bool IWordToMotionPlayer.UseIkAndFingerFade => true;

        bool IWordToMotionPlayer.CanPlay(MotionRequest request)
        {
            if (!_hasModel)
            {
                return false;
            }

            var targetItem = FindFileItem(request.CustomMotionClipName);
            return targetItem.IsValid;
        }

        void IWordToMotionPlayer.Play(MotionRequest request, out float duration)
        {
            if (!_hasModel)
            {
                duration = 1f;
                return;
            }

            var targetItem = FindFileItem(request.CustomMotionClipName);
            if (!targetItem.IsValid)
            {
                duration = 1f;
                return;
            }

            if (!_repository.TryGetDuration(targetItem, out duration))
            {
                return;
            }

            //ゲーム入力のほうでVRMAが再生中だと反応できないようにしておく
            //※Word to Motionを常時優先する説もある (普段は何でもかんでもWtMが最優先ではあるので)
            if (!_motionSetter.TryLock(_motionSetterLocker))
            {
                return;
            }
            
            //NOTE: I/Fの戻り値としてはIK FadeInするの時間を引いて答えておく
            duration -= FadeDuration;
            if (targetItem.Loop)
            {
                duration = -1f;
            }
            
            CancelCurrentPlay();
            _cts = new();
            _playing = true;
            if (targetItem.Loop)
            {
                RunLoopAnimationAsync(targetItem, _cts.Token).Forget();
            }
            else
            {
                RunAnimationAsync(targetItem, _cts.Token).Forget();
            }
        }

        //NOTE: ゲーム入力とWtMで同じモーションを使うとちょっと変になる可能性がある。
        //いったんマイナーケースだと思って許容している
        public void Stop()
        {
            if (!_hasModel)
            {
                return;
            }

            if (_stopRunning && !_repository.PeekInstance.IsPlaying)
            {
                return;
            }

            CancelCurrentPlay();
            _cts = new();
            _stopRunning = true;
            StopAsync(_cts.Token).Forget();
        }

        void IWordToMotionPlayer.PlayPreview(MotionRequest request)
        {
            if (!_hasModel)
            {
                return;
            }

            var targetItem = FindFileItem(request.CustomMotionClipName);
            if (!targetItem.IsValid)
            {
                return;
            }

            if (_repository.PeekInstance.File.Equals(targetItem) &&
                _repository.PeekInstance.IsLoopPlaying)
            {
                return;
            }
            
            CancelCurrentPlay();
            _repository.StopAllAnimations();
            _repository.Run(targetItem, true);
            _playingPreview = true;
            _playing = false;
        }

        //TODO: Stop()と同じ
        void IWordToMotionPlayer.StopPreview()
        {
            if (!_hasModel)
            {
                return;
            }

            _repository.StopAllAnimations();
            _playing = false;
            _playingPreview = false;
        }

        //TODO: Previewもタスク志向で書いた方がクラスとして見通し良いかも…
        void ILateTickable.LateTick()
        {
            if (!_playingPreview)
            {
                return;
            }

            var anim = _repository.PeekInstance;
            if (anim == null)
            {
                //普通ここは通らない
                return;
            }

            _motionSetter.Set(anim, 1f);
        } 
        
        private void OnModelLoaded(VrmLoadedInfo info)
        {
            _hasModel = true;
        }
        
        private void OnModelDisposed()
        {
            _hasModel = false;
            _repository.StopAllAnimations();
            _playing = false;
            _playingPreview = false;
        }

        private async UniTaskVoid RunAnimationAsync(VrmaFileItem item, CancellationToken cancellationToken)
        {
            _motionSetter.FixHipLocalPosition = true;

            //やること: 適用率を0 > 1 > 0に遷移させつつ適用していく
            //prevのアニメーションを適用するかどうかは動的にチェックして決める
            _repository.Run(item, false);
            var anim = _repository.PeekInstance;
            var animDuration = _repository.PeekInstance.Duration;
            var count = 0f;
            while (count < animDuration)
            {
                var rate = 1f;

                if (count < FadeDuration)
                {
                    //0 -> 1, 始まってすぐ
                    rate = Mathf.Clamp01(count / FadeDuration);
                }
                else if (count > animDuration - FadeDuration)
                {
                    // 1 -> 0, 終了間近
                    _repository.StopPrevAnimation();
                    rate = Mathf.Clamp01((animDuration - count) / FadeDuration);
                }
                else
                {
                    // 中間部分。このタイミングでは補間が要らない
                    _repository.StopPrevAnimation();
                }

                //NOTE: rate == 1とか0のケースの最適化はmotionSetterにケアさせる
                if (_repository.PrevInstance is { IsPlaying: true } playingPrev)
                {
                    //VRMAどうしの補間中にしか通らない、珍しい寄りのパス
                    _motionSetter.Set(playingPrev, anim, rate);
                }
                else
                {
                    _motionSetter.Set(anim, rate);
                }
                
                //NOTE: LateTick相当くらいのタイミングを狙っていることに注意
                await UniTask.NextFrame(cancellationToken);
                count += Time.deltaTime;
            }
         
            StopImmediate();
        }

        private async UniTaskVoid RunLoopAnimationAsync(VrmaFileItem item, CancellationToken cancellationToken)
        {
            //非ループ版との違い
            // 1. 実行時にループ扱いでスタートする
            // 2. モーションはフェードインするだけで、フェードアウトはケアしない
            // 3. 2に基づき、StopImmediateは呼ぶ必要がない
            _motionSetter.FixHipLocalPosition = true;

            _repository.Run(item, true);
            var anim = _repository.PeekInstance;
            var count = 0f;
            while (!cancellationToken.IsCancellationRequested)
            {
                var rate = 1f;

                if (count < FadeDuration)
                {
                    //0 -> 1, 始まってすぐ
                    rate = Mathf.Clamp01(count / FadeDuration);
                }
                else
                {
                    // 中間部分。このタイミングでは補間が要らない
                    _repository.StopPrevAnimation();
                }

                //NOTE: rate == 1とか0のケースの最適化はmotionSetterにケアさせる
                if (_repository.PrevInstance is { IsPlaying: true } playingPrev)
                {
                    _motionSetter.Set(playingPrev, anim, rate);
                }
                else
                {
                    _motionSetter.Set(anim, rate);
                }
                
                //NOTE: LateTick相当くらいのタイミングを狙っていることに注意
                await UniTask.NextFrame(cancellationToken);
                count += Time.deltaTime;
            }
        }
        
        private async UniTaskVoid StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                var count = 0f;
                var anim = _repository.PeekInstance;
                if (anim == null)
                {
                    return;
                }

                while (count < FadeDuration && _repository.PeekInstance.IsPlaying)
                {
                    var rate = 1f - Mathf.Clamp01(count / FadeDuration);
                    _motionSetter.Set(anim, rate);

                    await UniTask.NextFrame(cancellationToken);
                    count += Time.deltaTime;
                }
                StopImmediate();
            }
            finally
            {
                _stopRunning = false;
                _motionSetter.ReleaseLock();
            }
        }
        
        //NOTE: コレを呼ぶ場合、明示的にアニメーションを停止なり直ちに開始なりすることが期待されるので注意
        private void CancelCurrentPlay()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private void StopImmediate()
        {
            CancelCurrentPlay();
            _repository.StopCurrentAnimation();
            _playing = false;
            _playingPreview = false;
            _motionSetter.ReleaseLock();
        }
        
        //NOTE: モーション名として拡張子付きのファイル名が使われている事を期待している
        private VrmaFileItem FindFileItem(string motionName)
        {
            return _repository
                .GetAvailableFileItems()
                .FirstOrDefault(i => string.Compare(
                        motionName, 
                        i.FileName,
                        StringComparison.InvariantCultureIgnoreCase
                    ) 
                    == 0
                );
        }
    }
}
