using UnityEngine;
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

            // 透過背景の場合は蓋絵が残るとかえって邪魔なため、早めに外す
            var settingReader = new DirectSettingFileReader();
            settingReader.Load();
            if (settingReader.TransparentBackground)
            {
                FadeOutLoadingCoverImmediate();
            }
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

        private void FadeOutLoadingCoverImmediate()
        {
            if (_fadeOutCalled)
            {
                return;
            }
            _fadeOutCalled = true;
            _cover.FadeOutImmediate();
        }
    }
}
