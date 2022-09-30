using Baku.VMagicMirror.IK;
using Zenject;

namespace Baku.VMagicMirror
{
    public class ClapMotionPlayer : IInitializable, ITickable, IWordToMotionPlayer
    {
        public const string ClapClipName = "Clap";

        private string _previewClipName = "";

        private readonly HandIKIntegrator _handIKIntegrator;
        private ClapMotionHandIKGenerator _clapMotion;

        [Inject]
        public ClapMotionPlayer(HandIKIntegrator handIKIntegrator)
        {
            _handIKIntegrator = handIKIntegrator;
        }

        void IInitializable.Initialize()
        {
            _clapMotion = _handIKIntegrator.ClapMotion;
        }

        void ITickable.Tick()
        {
            //プレビューが止まってたら再生
            if (!string.IsNullOrEmpty(_previewClipName) && !_clapMotion.ClapMotionRunning)
            {
                _clapMotion.RunClapMotion();
            }
        }

        //Fingerだけ何かしたい可能性あるが、ここでは実施しない
        bool IWordToMotionPlayer.UseIkAndFingerFade => false;

        bool IWordToMotionPlayer.CanPlay(MotionRequest request)
        {
            return 
                request.MotionType == MotionRequest.MotionTypeBuiltInClip &&
                request.BuiltInAnimationClipName == ClapClipName;
        }

        void IWordToMotionPlayer.Play(MotionRequest request, out float duration)
        {
            var clipName = request.BuiltInAnimationClipName;
            if (!CanPlay(clipName))
            {
                duration = 0f;
                return;
            }

            _clapMotion.RunClapMotion();
            duration = _clapMotion.MotionDuration;
        }

        //NOTE: IKStateが変わることとか(必要に応じて)IK Weight自体が下がることに任せるので、特に何もしない
        void IWordToMotionPlayer.Stop() => _previewClipName = "";

        void IWordToMotionPlayer.PlayPreview(MotionRequest request)
        {
            var clipName = request.BuiltInAnimationClipName;
            if (CanPlay(clipName))
            {
                _previewClipName = clipName;
            }
            else
            {
                _previewClipName = "";
            }
        }

        //NOTE: ここはすぐ停止するのが理想だが、止め方がちょっと難しいので「プレビューが繰り返さない」というだけにする
        void IWordToMotionPlayer.StopPreview() => _previewClipName = "";

        private bool CanPlay(string clipName)
        {
            return clipName == ClapClipName;
        }
    }
}
