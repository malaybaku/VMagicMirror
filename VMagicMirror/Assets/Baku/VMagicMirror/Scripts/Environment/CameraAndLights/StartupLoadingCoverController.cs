using Zenject;

namespace Baku.VMagicMirror
{
    public class StartupLoadingCoverController : PresenterBase
    {
        private readonly IMessageReceiver _receiver;
        private readonly StartupLoadingCover _cover;
        private bool _unfadeCalled;

        [Inject]
        public StartupLoadingCoverController(IMessageReceiver receiver, StartupLoadingCover cover)
        {
            _receiver = receiver;
            _cover = cover;
        }
        
        public override void Initialize()
        {
            _receiver.AssignCommandHandler(
                VmmCommands.StartupEnded,
                _ => UnfadeLoadingCover()
            );

#if UNITY_EDITOR
            //UnfadeLoadingCover();
#endif
        }

        private void UnfadeLoadingCover()
        {
            if (_unfadeCalled)
            {
                return;
            }
            _unfadeCalled = true;
            _cover.UnfadeAndDestroySelf();
        }
    }
}
