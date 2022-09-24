using Zenject;

namespace Baku.VMagicMirror
{
    public class StartupLoadingCoverController : PresenterBase
    {
        private readonly IMessageReceiver _receiver;
        private readonly StartupLoadingCover _cover;
        private bool _fadeOutCalled;

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
                _ => FadeOutLoadingCover()
            );

            //エディタの場合は(Play Mode直後のエラーとか諸事情でカバーが邪魔になりうる + ビルドの動確で邪魔になるとは思えないので)外す
#if UNITY_EDITOR
            FadeOutLoadingCover();
#endif
        }

        private void FadeOutLoadingCover()
        {
            if (_fadeOutCalled)
            {
                return;
            }
            _fadeOutCalled = true;
            _cover.FadeOutAndDestroySelf();
        }
    }
}
