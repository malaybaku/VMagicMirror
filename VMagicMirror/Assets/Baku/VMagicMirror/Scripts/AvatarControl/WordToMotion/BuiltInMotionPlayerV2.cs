using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.WordToMotion
{
    /// <summary>
    /// ビルトインモーションを実行するクラス。
    /// SimpleAnimationを使うのをやめてる実装(下半身ボーンを制御しないため)
    /// </summary>
    public class BuiltInMotionPlayerV2 : PresenterBase, IWordToMotionPlayer
    {
        private static readonly int StandingTrigger = Animator.StringToHash("Standing");
        private static readonly Dictionary<string, int> _clipNameToAnimatorTrigger = new Dictionary<string, int>()
        {
            ["Wave"] = Animator.StringToHash("Wave"),
            ["Rokuro"] = Animator.StringToHash("Rokuro"),
            ["Good"] = Animator.StringToHash("Good"),
        };

        private readonly WordToMotionMapper _mapper;
        private readonly BuiltInMotionClipData _clipData;
        private readonly IVRMLoadable _vrmLoadable;

        private bool _hasModel = false;
        private Animator _animator = null;

        private bool _isPlaying;
        private string _previewClipName;

        private CancellationTokenSource _cts = new CancellationTokenSource();

        [Inject]
        public BuiltInMotionPlayerV2(IVRMLoadable vrmLoadable, BuiltInMotionClipData clipData)
        {
            _clipData = clipData;
            _mapper = new WordToMotionMapper(clipData);
            _vrmLoadable = vrmLoadable;
        }

        public override void Initialize()
        {
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmUnloaded;
        }

        public override void Dispose()
        {
            base.Dispose();
            _vrmLoadable.VrmLoaded -= OnVrmLoaded;
            _vrmLoadable.VrmDisposing -= OnVrmUnloaded;
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _animator = info.animator;
            _animator.runtimeAnimatorController = _clipData.AnimatorController;
            _hasModel = true;
        }

        private void OnVrmUnloaded()
        {
            _hasModel = false;
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

            //NOTE: ここclipが二重管理されててちょっと気持ち悪いが、一旦許容で…
            var clip = _mapper.FindBuiltInAnimationClipOrDefault(clipName);
            if (clip == null)
            {
                duration = 0f;
                return;
            }
        
            Debug.Log($"set trigger, {clipName}");
            _animator.SetTrigger(_clipNameToAnimatorTrigger[clipName]);
            duration = clip.length - WordToMotionRunner.IkFadeDuration;

            RefreshCts();
            ResetToDefaultClipAsync(clip.length, _cts.Token).Forget();
        }

        private async UniTaskVoid ResetToDefaultClipAsync(float delay, CancellationToken cancellationToken)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);
            if (_hasModel)
            {
                _animator.SetTrigger(StandingTrigger);
            }
        }
        
        bool IWordToMotionPlayer.IsPlaying => _isPlaying;
        
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
            if (_isPlaying && 
                clipName == _previewClipName && 
                //NOTE: 最後まで行ってたら再生し直す
                _animator.GetCurrentAnimatorStateInfo(0).shortNameHash != StandingTrigger)
            {
                return;
            }

            var clipData = _clipData.Items.FirstOrDefault(c => c.name == clipName);
            if (clipData == null)
            {
                return;
            }

            _isPlaying = true;
            _animator.SetTrigger(_clipNameToAnimatorTrigger[clipName]);
            _previewClipName = clipName;
        }

        void IWordToMotionPlayer.Stop()
        {
            RefreshCts();
            if (_hasModel)
            {
                _animator.SetTrigger(StandingTrigger);
            }
        }

        void IWordToMotionPlayer.StopPreview()
        {
            if (!_isPlaying)
            {
                _previewClipName = "";
                return;
            }

            if (_hasModel)
            {
                _animator.SetTrigger(StandingTrigger);
            }

            _isPlaying = false;
            _previewClipName = "";
        }
    }
}
