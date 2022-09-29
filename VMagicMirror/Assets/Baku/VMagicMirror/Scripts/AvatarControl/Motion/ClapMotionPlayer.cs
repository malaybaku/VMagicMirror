using Baku.VMagicMirror.IK;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class ClapMotionPlayer : IInitializable, ITickable, IWordToMotionPlayer
    {
        public const string ClapClipName = "Clap";

        private string _previewClipName = "";

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
            //プレビューが止まってたら再生
            if (!string.IsNullOrEmpty(_previewClipName) && !_clapMotion.ClapMotionRunning)
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
            duration = _clapMotion.MotionDuration;
        }

        public void Stop()
        {
            _clapMotion.StopClapMotion();
            _previewClipName = "";
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
