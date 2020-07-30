using Baku.VMagicMirror.ExternalTracker;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    public class MonitoringInstaller : InstallerBase
    {
        [SerializeField] private RawInputChecker rawInputChecker = null;
        [SerializeField] private MousePositionProvider mousePositionProvider = null;
        [SerializeField] private FaceTracker faceTracker = null;
        [SerializeField] private HandTracker handTracker = null;
        [SerializeField] private ExternalTrackerDataSource externalTracker = null;
        [SerializeField] private XInputGamePad gamepadListener = null;
        [SerializeField] private MidiInputObserver midiInputObserver = null;
        
        public override void Install(DiContainer container)
        {
            container.BindInstance(rawInputChecker);
            container.BindInstance(mousePositionProvider);
            container.BindInstance(faceTracker);
            container.BindInstance(handTracker);
            container.BindInstance(externalTracker);
            container.BindInstance(gamepadListener);
            container.BindInstance(midiInputObserver);

            //終了前に監視処理を安全にストップさせたいものは呼んでおく
            container.Bind<IReleaseBeforeQuit>()
                .FromInstance(mousePositionProvider)
                .AsCached();
            container.Bind<IReleaseBeforeQuit>()
                .FromInstance(rawInputChecker)
                .AsCached();
        }
    }
}
