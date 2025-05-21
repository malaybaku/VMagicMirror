using UniRx;
using Zenject;

namespace Baku.VMagicMirror.Buddy
{
    public class BuddyRuntimeObjectUpdater : PresenterBase
    {
        private readonly ScriptLoader _scriptLoader;
        private readonly BuddySpriteCanvas _spriteCanvas;
        private readonly Buddy3DInstanceCreator _instanceCreator;
        private readonly BuddyRuntimeObjectRepository _repository;
        
        [Inject]
        public BuddyRuntimeObjectUpdater(
            ScriptLoader scriptLoader,
            BuddySpriteCanvas spriteCanvas,
            Buddy3DInstanceCreator instanceCreator,
            BuddyRuntimeObjectRepository repository)
        {
            _scriptLoader = scriptLoader;
            _spriteCanvas = spriteCanvas;
            _instanceCreator = instanceCreator;
            _repository = repository;
        }

        public override void Initialize()
        {
            _spriteCanvas.SpriteCreated
                .Subscribe(instance => _repository.AddSprite2D(instance))
                .AddTo(this);
            
            _instanceCreator.Sprite3DCreated
                .Subscribe(instance => _repository.AddSprite3D(instance))
                .AddTo(this);

            _instanceCreator.GlbCreated
                .Subscribe(instance => _repository.AddGlb(instance))
                .AddTo(this);
            
            _instanceCreator.VrmCreated
                .Subscribe(instance => _repository.AddVrm(instance))
                .AddTo(this);
            
            _instanceCreator.VrmAnimationCreated
                .Subscribe(instance => _repository.AddVrmAnimation(instance))
                .AddTo(this);
            
            _scriptLoader.ScriptDisposing
                .Subscribe(caller => _repository.DeleteBuddy(caller.BuddyFolder))
                .AddTo(this);
        }
    }
}
