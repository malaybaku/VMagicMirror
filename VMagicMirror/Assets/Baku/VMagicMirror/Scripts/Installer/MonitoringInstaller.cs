using Baku.VMagicMirror.ExternalTracker;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    public class MonitoringInstaller : InstallerBase
    {
        [SerializeField] private GlobalHookInputChecker globalHookInputChecker = null;
        [SerializeField] private RawInputChecker robustRawInputChecker = null;
        [SerializeField] private MousePositionProvider mousePositionProvider = null;
        [SerializeField] private FaceTracker faceTracker = null;
        [SerializeField] private ExternalTrackerDataSource externalTracker = null;
        [SerializeField] private XInputGamePad gamepadListener = null;
        [SerializeField] private MidiInputObserver midiInputObserver = null;
        
        public override void Install(DiContainer container)
        {
            //NOTE: 2つの実装が合体したキメラ実装を適用します。コレが比較的安全でいちばん動きも良いので。
            container.Bind<IKeyMouseEventSource>()
                .FromInstance(new HybridInputChecker(robustRawInputChecker, globalHookInputChecker))
                .AsCached();
            container.BindInstance(mousePositionProvider);
            container.BindInstance(faceTracker);
            container.BindInstance(externalTracker);
            container.BindInstance(externalTracker.FaceSwitchExtractor);
            container.BindInstance(gamepadListener);
            container.BindInstance(midiInputObserver);

            //終了前に監視処理を安全にストップさせたいものは呼んでおく
            container.Bind<IReleaseBeforeQuit>()
                .FromInstance(robustRawInputChecker)
                .AsCached();

            container.Bind<IReleaseBeforeQuit>()
                .FromInstance(globalHookInputChecker)
                .AsCached();
            
            container.BindInterfacesAndSelfTo<HorizontalFlipController>().AsSingle();
        }
    }
}
