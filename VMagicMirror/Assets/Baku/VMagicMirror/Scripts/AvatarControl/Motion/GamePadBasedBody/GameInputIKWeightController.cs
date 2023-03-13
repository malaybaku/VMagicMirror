using System;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror.GameInput
{
    public class GameInputIKWeightController : IInitializable, IDisposable
    {
        private readonly BodyMotionModeController _bodyMotionModeController;
        private readonly TaskBasedIkWeightFader _ikWeightFader;
        private readonly CompositeDisposable _disposable = new CompositeDisposable();
        
        public GameInputIKWeightController(
            BodyMotionModeController bodyMotionModeController,
            TaskBasedIkWeightFader ikWeightFader)
        {
            _bodyMotionModeController = bodyMotionModeController;
            _ikWeightFader = ikWeightFader;
        }
        
        public void Initialize()
        {
            _bodyMotionModeController.MotionMode
                .Select(mode => mode == BodyMotionMode.GameInputLocomotion)
                .DistinctUntilChanged()
                .Subscribe(isGameInputLocomotionMode =>
                {
                    _ikWeightFader.SetFullBodyIkWeight(isGameInputLocomotionMode ? 0f : 1f);
                })
                .AddTo(_disposable);
        }

        public void Dispose() => _disposable.Dispose();
    }
}
