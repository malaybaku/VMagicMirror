using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror.WordToMotion
{
    /// <summary>
    /// ビルトインモーションを実行するクラス
    /// 元はWordToMotionManagerの一部だったが、リファクタにより分離した。
    /// </summary>
    public class BuiltInMotionPlayer : PresenterBase, IWordToMotionPlayer
    {
        private const string DefaultStateName = "Default";
        private const float CrossFadeDuration = WordToMotionRunner.IkFadeDuration;

        private readonly WordToMotionMapper _mapper;
        private readonly BuiltInMotionClipData _clipData;
        private readonly IVRMLoadable _vrmLoadable;
        private readonly BodyMotionModeController _bodyMotionModeController;

        private bool _hasModel;
        private SimpleAnimation _simpleAnimation;
        private Animator _animator;
        private bool _isGameInputMode;

        private bool _isPlayingPreview;
        private string _previewClipName = "";

        private CancellationTokenSource _cts = new CancellationTokenSource();

        private static readonly Dictionary<string, int> ClipNameToTriggerHash = new Dictionary<string, int>()
        {
            ["Good"] = Animator.StringToHash("Good"),
            ["Wave"] = Animator.StringToHash("Wave"),
            ["Rokuro"] = Animator.StringToHash("Rokuro"),
        };
        
        [Inject]
        public BuiltInMotionPlayer(
            IVRMLoadable vrmLoadable,
            BuiltInMotionClipData clipData,
            BodyMotionModeController bodyMotionModeController)
        {
            _clipData = clipData;
            _mapper = new WordToMotionMapper(clipData);
            _bodyMotionModeController = bodyMotionModeController;
            _vrmLoadable = vrmLoadable;
        }

        public override void Initialize()
        {
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmUnloaded;

            _bodyMotionModeController.MotionMode
                .Subscribe(mode => _isGameInputMode = mode == BodyMotionMode.GameInputLocomotion)
                .AddTo(this);
        }

        public override void Dispose()
        {
            base.Dispose();
            _vrmLoadable.VrmLoaded -= OnVrmLoaded;
            _vrmLoadable.VrmDisposing -= OnVrmUnloaded;
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _simpleAnimation = info.vrmRoot.gameObject.AddComponent<SimpleAnimation>();
            _simpleAnimation.playAutomatically = false;
            _simpleAnimation.AddState(_clipData.DefaultStandingAnimation, DefaultStateName);
            _simpleAnimation.Play(DefaultStateName);
            _animator = info.animator;
            _hasModel = true;
        }

        private void OnVrmUnloaded()
        {
            _hasModel = false;
            _simpleAnimation = null;
            _animator = null;
        }
        
        private void RefreshCts()
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = new CancellationTokenSource();
        }

        private void Play(string clipName, out float duration)
        {
            //キャラのロード前に数字キーとか叩いたケースをガードしています
            if (!_hasModel)
            {
                duration = 0f;
                return;
            }
            
            var clip = _mapper.FindBuiltInAnimationClipOrDefault(clipName);
            if (clip == null)
            {
                duration = 0f;
                return;
            }
        
            //NOTE: Removeがうまく動いてないように見えるのでRemoveしないようにしてる
            if (_simpleAnimation.GetState(clipName) == null)
            {
                _simpleAnimation.AddState(clip, clipName);
            }
            else
            {
                //2回目がきちんと動くために。
                _simpleAnimation.Rewind(clipName);
            }

            if (_isGameInputMode)
            {
                //あるはずだけど一応チェック
                if (ClipNameToTriggerHash.TryGetValue(clipName, out var triggerValue))
                {
                    _animator.SetTrigger(triggerValue);
                }
            }
            else
            {
                _simpleAnimation.Play(clipName);
            }
            duration = clip.length - WordToMotionRunner.IkFadeDuration;

            RefreshCts();

            //NOTE: ゲーム入力時はRuntimeAnimatorControllerの仕組みで遷移するので、明示的に戻す必要はない
            if (!_isGameInputMode)
            {
                ResetToDefaultClipAsync(clip.length, _cts.Token).Forget();
            }
        }

        private async UniTaskVoid ResetToDefaultClipAsync(float delay, CancellationToken cancellationToken)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);
            if (_hasModel)
            {
                _simpleAnimation.CrossFade(DefaultStateName, CrossFadeDuration);
            }
        }
        
        bool IWordToMotionPlayer.UseIkAndFingerFade => true;

        bool IWordToMotionPlayer.CanPlay(MotionRequest request)
        {
            return
                request.MotionType == MotionRequest.MotionTypeBuiltInClip &&
                _clipData.Items.Any(i => i.name == request.BuiltInAnimationClipName);   
        }
        
        void IWordToMotionPlayer.Play(MotionRequest request, out float duration)
        {
            Play(request.BuiltInAnimationClipName, out duration);
        }

        void IWordToMotionPlayer.PlayPreview(MotionRequest request)
        {
            if (!_hasModel)
            {
                return;
            }

            var clipName = request.BuiltInAnimationClipName;
            if (_isPlayingPreview && 
                clipName == _previewClipName && 
                //NOTE: 最後まで行ってたら再生し直す
                _simpleAnimation.GetState(clipName) != null && 
                _simpleAnimation.GetState(clipName).normalizedTime < 1.0f)
            {
                return;
            }

            //プレビュー中のクリップを別のクリップに変えるケース
            if (!string.IsNullOrEmpty(_previewClipName) && _previewClipName != clipName)
            {
                _simpleAnimation.Stop(_previewClipName);
            }

            var clipData = _clipData.Items.FirstOrDefault(c => c.name == clipName);
            if (clipData == null)
            {
                return;
            }

            if (_simpleAnimation.GetState(clipName) == null)
            {
                _simpleAnimation.AddState(clipData.clip, clipName);
            }
            else
            {
                //いちおう直す方が心臓に優しいので
                _simpleAnimation.Rewind(clipName);
            }

            _isPlayingPreview = true;
            _simpleAnimation.Play(clipName);
            _previewClipName = clipName;
        }

        void IWordToMotionPlayer.Stop()
        {
            RefreshCts();
            if (_hasModel)
            {
                _simpleAnimation.CrossFade(DefaultStateName, CrossFadeDuration);
            }
            _previewClipName = "";
            _isPlayingPreview = false;
        }

        void IWordToMotionPlayer.StopPreview()
        {
            if (!_isPlayingPreview)
            {
                _previewClipName = "";
                return;
            }

            if (_hasModel)
            {
                _simpleAnimation.Stop(_previewClipName);
            }

            _isPlayingPreview = false;
            _previewClipName = "";

            if (_hasModel)
            {
                _simpleAnimation.CrossFade(DefaultStateName, 0f);
            }
        }
    }
}
