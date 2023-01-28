using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UniVRM10;

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
        private readonly WordToMotionAccessoryRequest _accessoryRequest;
        
        public WordToMotionRunner(
            IVRMLoadable vrmLoadable,
            IEnumerable<IWordToMotionPlayer> players,
            WordToMotionBlendShape blendShape,
            IkWeightCrossFade ikWeightCrossFade,
            FingerController fingerController,
            WordToMotionAccessoryRequest accessoryRequest)
        {
            _vrmLoadable = vrmLoadable;
            _players = players.ToArray();
            _blendShape = blendShape;
            _ikWeightCrossFade = ikWeightCrossFade;
            _fingerController = fingerController;
            _accessoryRequest = accessoryRequest;
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
            _blendShape.Initialize(info.instance.Vrm.Expression);
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
        
        //NOTE: モーションの終了処理をキャンセルした場合に要考慮になるので…
        private bool _restoreIkOnMotionEnd = false;
        private bool _previewIsActive = false;
        private bool _previewUseIkFade = false;
        
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
                        pair => (ExpressionKeyUtils.CreateKeyByName(pair.Key), pair.Value)
                    ).ToArray(),
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
                    CancelMotionReset();
                    //NOTE: 実行中のと同じモーションが指定された場合も最初から再生してよい、というのがポイント
                    foreach (var player in _players.Where(p => p != playablePlayer))
                    {
                        player.Stop();
                    }
                    playablePlayer.Play(request, out duration);

                    if (playablePlayer.UseIkAndFingerFade && !_restoreIkOnMotionEnd)
                    {
                        // 直前モーションでIKオフにしてない場合、オフにしたいので実際そうする
                        _fingerController.FadeOutWeight(IkFadeDuration);
                        _ikWeightCrossFade.FadeOutArmIkWeights(IkFadeDuration);
                    }
                    else if (!playablePlayer.UseIkAndFingerFade && _restoreIkOnMotionEnd)
                    {
                        // IKオフのモーション中にIK有効が期待されたモーションを行う場合、IKがオンになる
                        _fingerController.FadeInWeight(IkFadeDuration);
                        _ikWeightCrossFade.FadeInArmIkWeights(IkFadeDuration);
                    }
                }
                
                _motionResetCts = new CancellationTokenSource();
                _restoreIkOnMotionEnd = playablePlayer?.UseIkAndFingerFade ?? false;
                ResetMotionAsync(duration, _restoreIkOnMotionEnd, _motionResetCts.Token)
                    .Forget();
            }

            if (request.UseBlendShape)
            {
                _blendShapeResetCts = new CancellationTokenSource();
                //NOTE: HoldBlendShape == falseの場合、必ず有効なdurationが入ってるはず
                if (!request.HoldBlendShape)
                {
                    ResetBlendShapeAsync(duration, _blendShapeResetCts.Token).Forget();
                }
            }

            //アクセサリの処理
            CancelAccessoryReset();
            _accessoryRequest.SetAccessoryRequest(request.AccessoryName);
            //2つ目の条件は「表情が変えっぱなしになるならアクセサリも出っぱなしで良い」ということ。
            if (!string.IsNullOrEmpty(request.AccessoryName) && 
                !(request.UseBlendShape && request.HoldBlendShape))
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

            CancelBlendShapeReset();
            if (request.UseBlendShape)
            {
                _blendShape.SetForPreview(
                    request.BlendShapeValuesDic.Select(
                        pair => (ExpressionKeyUtils.CreateKeyByName(pair.Key), pair.Value)
                    ).ToArray(),
                    request.PreferLipSync
                );
            }
            else
            {
                _blendShape.ResetBlendShape();
            }

            var playablePlayer = _players.FirstOrDefault(p => p.CanPlay(request));
            foreach (var player in _players.Where(p => p != playablePlayer))
            {
                player.StopPreview();
            }
            //NOTE: ここで同じ値が指定され続けた場合に動作し続けるのはPlayer側で保証してる
            playablePlayer?.PlayPreview(request);

            var useIkFade = 
                request.MotionType != MotionRequest.MotionTypeNone &&
                playablePlayer?.UseIkAndFingerFade == true;
            
            if (_previewUseIkFade != useIkFade)
            {
                _previewUseIkFade = useIkFade;
                if (_previewUseIkFade)
                {
                    _fingerController.FadeOutWeight(IkFadeDuration);
                    _ikWeightCrossFade.FadeOutArmIkWeights(IkFadeDuration);
                }
                else
                {
                    _fingerController.FadeInWeight(IkFadeDuration);
                    _ikWeightCrossFade.FadeInArmIkWeights(IkFadeDuration);
                }
            }
            
            //空の場合も通すことにより、アクセサリが非表示になる
            _accessoryRequest.SetAccessoryRequest(request.AccessoryName);
        }

        public void EnablePreview()
        {
            _previewIsActive = true;
            Stop();
        }

        public void StopPreview()
        {
            _previewIsActive = false;
            _previewUseIkFade = false;
            Stop();
        }
        
        public void Stop()
        {
            CancelBlendShapeReset();
            CancelMotionReset();
            CancelAccessoryReset();
            _blendShape.ResetBlendShape();
            _accessoryRequest.Reset();
            foreach (var player in _players)
            {
                player.Stop();
            }
            _fingerController.FadeInWeight(0f);
            _ikWeightCrossFade.FadeInArmIkWeightsImmediately();
            _restoreIkOnMotionEnd = false;
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
                _restoreIkOnMotionEnd = false;
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
                _accessoryRequest.Reset();
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
            _motionResetCts = null;
        }  

        private void CancelBlendShapeReset()
        {
            _blendShapeResetCts?.Cancel();
            _blendShapeResetCts?.Dispose();
            _blendShapeResetCts = null;
        }
        
        private void CancelAccessoryReset()
        {
            _accessoryResetCts?.Cancel();
            _accessoryResetCts?.Dispose();
            _accessoryResetCts = null;
        }
    }
}