using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.WordToMotion
{
    public class WordToMotionRunner : PresenterBase
    {
        public const float IkFadeDuration = 0.5f;
        
        private readonly IVRMLoadable _vrmLoadable;
        private readonly IWordToMotionPlayer[] _players;
        private readonly WordToMotionBlendShape _blendShape;
        private readonly IkWeightCrossFade _ikWeightCrossFade;
        private readonly FingerController _fingerController;
        
        //TODO: こうじゃなくて、リクエストを受ける別のクラスが欲しい
        private readonly ReactiveProperty<string> _accessoryVisibilityRequest 
            = new ReactiveProperty<string>("");
        /// <summary> 表示してほしいアクセサリーのFileIdか、または空文字 </summary>
        public IReadOnlyReactiveProperty<string> AccessoryVisibilityRequest => _accessoryVisibilityRequest;        

        public WordToMotionRunner(
            IVRMLoadable vrmLoadable,
            IEnumerable<IWordToMotionPlayer> players,
            WordToMotionBlendShape blendShape,
            IkWeightCrossFade ikWeightCrossFade,
            FingerController fingerController)
        {
            _vrmLoadable = vrmLoadable;
            _players = players.ToArray();
            _blendShape = blendShape;
            _ikWeightCrossFade = ikWeightCrossFade;
            _fingerController = fingerController;
        }

        public override void Initialize()
        {
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmUnloaded;
        }

        public override void Dispose()
        {
            base.Dispose();
            
            _accessoryResetCts?.Cancel();
            _motionResetCts?.Cancel();
            _blendShapeResetCts?.Cancel();
            _vrmLoadable.VrmLoaded -= OnVrmLoaded;
            _vrmLoadable.VrmDisposing -= OnVrmUnloaded;
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _blendShape.Initialize(info.blendShape);
            _ikWeightCrossFade.OnVrmLoaded(info);            
        }

        private void OnVrmUnloaded()
        {
            _ikWeightCrossFade.OnVrmDisposing();
            _blendShape.DisposeProxy();
        }

        //NOTE: Previewかどうかによらず、実行中クリップがただひとつ存在する事にする
        private MotionRequest _currentRequest;
        private CancellationTokenSource _blendShapeResetCts;
        private CancellationTokenSource _motionResetCts;
        private CancellationTokenSource _accessoryResetCts;

        private bool _previewIsActive = false;
        
        public void Run(MotionRequest request)
        {
            if (_previewIsActive || (request.MotionType == MotionRequest.MotionTypeNone && !request.UseBlendShape))
            {
                return;
            }

            if (request.UseBlendShape)
            {
                CancelBlendShapeReset();
                _blendShape.SetBlendShapes(
                    request.BlendShapeValuesDic.Select(
                        pair => (BlendShapeKeyFactory.CreateFrom(pair.Key), pair.Value)
                    ),
                    request.PreferLipSync
                );
            }

            var duration = request.DurationWhenOnlyBlendShape;

            if (request.MotionType != MotionRequest.MotionTypeNone)
            {
                var playablePlayer = _players.FirstOrDefault(p => p.CanPlay(request));
                if (playablePlayer == null)
                {
                    //通常ここは通過しない
                    LogOutput.Instance.Write($"Error: unavailable motion specified, {JsonUtility.ToJson(request)}");
                }
                else
                {
                    //NOTE: 実行中のと同じモーションが指定された場合も最初から再生してよい、というのがポイント
                    foreach (var player in _players.Where(p => p != playablePlayer))
                    {
                        player.Abort();
                    }
                    playablePlayer?.Play(request, out duration);

                    if (playablePlayer.UseIkAndFingerFade)
                    {
                        _fingerController.FadeOutWeight(IkFadeDuration);
                        _ikWeightCrossFade.FadeOutArmIkWeights(IkFadeDuration);
                    }
                }
                
                CancelMotionReset();
                _motionResetCts = new CancellationTokenSource();
                ResetMotionAsync(duration, playablePlayer?.UseIkAndFingerFade ?? false, _motionResetCts.Token)
                    .Forget();
            }

            if (request.UseBlendShape)
            {
                _blendShapeResetCts = new CancellationTokenSource();
                ResetBlendShapeAsync(duration, _blendShapeResetCts.Token).Forget(); 
            }

            //アクセサリの処理
            CancelAccessoryReset();
            _accessoryVisibilityRequest.Value = request.AccessoryName;
            if (!string.IsNullOrEmpty(request.AccessoryName))
            {
                _accessoryResetCts = new CancellationTokenSource();
                ResetAccessoryAsync(duration, _accessoryResetCts.Token).Forget();
            }
        }

        public void RunAsPreview(MotionRequest request)
        {
            if (!_previewIsActive)
            {
                return;
            }

            if (request.UseBlendShape)
            {
                CancelBlendShapeReset();
                _blendShape.SetForPreview(
                    request.BlendShapeValuesDic.Select(
                        pair => (BlendShapeKeyFactory.CreateFrom(pair.Key), pair.Value)
                    ),
                    request.PreferLipSync
                );
            }

            var playablePlayer = _players.FirstOrDefault(p => p.CanPlay(request));
            foreach (var player in _players.Where(p => p != playablePlayer))
            {
                player.StopPreview();
            }
            //NOTE: ここで同じ値が指定され続けた場合に動作し続けるのはPlayer側で保証してる
            playablePlayer?.PlayPreview(request);

            _accessoryVisibilityRequest.Value = request.AccessoryName;
        }

        public void EnablePreview()
        {
            _previewIsActive = true;
            Stop();
        }

        public void StopPreview()
        {
            _previewIsActive = false;
            Stop();
        }
        
        public void Stop()
        {
            CancelBlendShapeReset();
            CancelMotionReset();
            CancelAccessoryReset();
            _blendShape.ResetBlendShape();
            _accessoryVisibilityRequest.Value = "";
            foreach (var player in _players)
            {
                player.Abort();
            }
        }

        
        
        private async UniTaskVoid ResetMotionAsync(float delay, bool fadeIkAndFinger, CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);
                //NOTE: player.Abort()とかは特に呼ばないのがポイント
                if (fadeIkAndFinger)
                {
                    _fingerController.FadeInWeight(IkFadeDuration);
                    _ikWeightCrossFade.FadeInArmIkWeights(IkFadeDuration);
                }
            }
            catch (OperationCanceledException)
            {
                //ignore
            }            
        }
        
        private async UniTaskVoid ResetBlendShapeAsync(float delay, CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);
                _blendShape.ResetBlendShape();
            }
            catch (OperationCanceledException)
            {
                //ignore
            }
        }

        private async UniTaskVoid ResetAccessoryAsync(float delay, CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);
                _accessoryVisibilityRequest.Value = "";
            }
            catch (OperationCanceledException)
            {
                //ignore
            }            
        }

        private void CancelMotionReset()
        {
            _motionResetCts?.Cancel();
            _motionResetCts?.Dispose();
        }  

        private void CancelBlendShapeReset()
        {
            _blendShapeResetCts?.Cancel();
            _blendShapeResetCts?.Dispose();
        }
        
        private void CancelAccessoryReset()
        {
            _accessoryResetCts?.Cancel();
            _accessoryResetCts?.Dispose();
        }
    }
}