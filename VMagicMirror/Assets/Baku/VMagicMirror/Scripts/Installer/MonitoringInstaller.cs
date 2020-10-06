using Baku.VMagicMirror.ExternalTracker;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    public class MonitoringInstaller : InstallerBase
    {
        [SerializeField] private RawInputChecker robustRawInputChecker = null;
        [SerializeField] private MousePositionProvider mousePositionProvider = null;
        [SerializeField] private FaceTracker faceTracker = null;
        [SerializeField] private HandTracker handTracker = null;
        [SerializeField] private ExternalTrackerDataSource externalTracker = null;
        [SerializeField] private XInputGamePad gamepadListener = null;
        [SerializeField] private MidiInputObserver midiInputObserver = null;
        [SerializeField] private OpenCVFacePose openCvFacePose = null;
        
        public override void Install(DiContainer container)
        {
            container.Bind<IKeyMouseEventSource>()
                .FromInstance(robustRawInputChecker)
                .AsCached();
            container.BindInstance(mousePositionProvider);
            container.BindInstance(faceTracker);
            container.BindInstance(handTracker);
            container.BindInstance(externalTracker);
            container.BindInstance(gamepadListener);
            container.BindInstance(midiInputObserver);
            container.BindInstance(openCvFacePose);

            //終了前に監視処理を安全にストップさせたいものは呼んでおく
            container.Bind<IReleaseBeforeQuit>()
                .FromInstance(robustRawInputChecker)
                .AsCached();
        }
    }
}
