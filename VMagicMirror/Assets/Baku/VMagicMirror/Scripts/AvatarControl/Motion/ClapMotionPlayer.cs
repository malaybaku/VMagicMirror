using Baku.VMagicMirror.IK;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class ClapMotionPlayer : IInitializable, ITickable, IWordToMotionPlayer
    {
        public const string ClapClipName = "Clap";

        //NOTE: HeadMotionの仕組みにならっているため、いちおうenumにしてる
        private enum ClipPlayState
        {
            None,
            Clapping,
        }

        private ClipPlayState _playState = ClipPlayState.None;
        private string _previewClipName = "";

        public bool IsPlaying => _playState != ClipPlayState.None && _clapMotion.ClapMotionRunning;

        //TODO: Fingerだけ何かしたい可能性あるかも
        bool IWordToMotionPlayer.UseIkAndFingerFade => false;

        bool IWordToMotionPlayer.CanPlay(MotionRequest request)
        {
            return 
                request.MotionType == MotionRequest.MotionTypeBuiltInClip &&
                request.BuiltInAnimationClipName == ClapClipName;
        }

        void IWordToMotionPlayer.Play(MotionRequest request, out float duration)
        {
            Play(request.BuiltInAnimationClipName, out duration);
        }

        void IWordToMotionPlayer.Stop()
        {
            //NOTE: IKStateが変わることとか(必要に応じて)IK Weight自体が下がることに任せるので、特に何もしない
        }
        
        void IWordToMotionPlayer.PlayPreview(MotionRequest request)
        {
            PlayPreview(request.BuiltInAnimationClipName);
        }

        private bool PreviewIsActive => !string.IsNullOrEmpty(_previewClipName);

        private bool IsPlayingPreview => !string.IsNullOrEmpty(_previewClipName) && _clapMotion.ClapMotionRunning;


        [Inject]
        public ClapMotionPlayer(HandIKIntegrator handIKIntegrator)
        {
            _handIKIntegrator = handIKIntegrator;
        }

        private readonly HandIKIntegrator _handIKIntegrator;
        private ClapMotionHandIKGenerator _clapMotion;

        public void Initialize()
        {
            _clapMotion = _handIKIntegrator.ClapMotion;
        }

        public void Tick()
        {
            //プレビューが止まってたら必ず再生することを保証する
            if (PreviewIsActive && !_clapMotion.ClapMotionRunning)
            {
                _clapMotion.RunClapMotion();
            }
        }

        public bool CanPlay(string clipName)
        {
            return clipName == ClapClipName;
        }
        
        //durationは通常は副作用の一貫として拾って欲しいのでoutで渡す
        public void Play(string clipName, out float duration)
        {
            if (!CanPlay(clipName))
            {
                duration = 0f;
                return;
            }

            _clapMotion.RunClapMotion();
            _playState = ClipPlayState.Clapping;
            duration = _clapMotion.MotionDuration;
        }

        public void Stop()
        {
            _playState = ClipPlayState.None;
            _clapMotion.StopClapMotion();
        }
        
        public void PlayPreview(string clipName)
        {
            if (CanPlay(clipName))
            {
                _previewClipName = clipName;
            }
            else
            {
                _previewClipName = "";
            }
        }

        public void StopPreview()
        {
            _previewClipName = "";
            Stop();
        }
    }
}
