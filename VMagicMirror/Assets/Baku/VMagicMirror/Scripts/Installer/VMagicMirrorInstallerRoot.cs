﻿using Baku.VMagicMirror.Buddy;
﻿using Baku.VMagicMirror.MediaPipeTracker;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    public class VMagicMirrorInstallerRoot : MonoInstaller
    {
        [SerializeField] private BuiltInMotionClipData builtInClip = null;
        [SerializeField] private DevicesInstaller devices = null;
        [SerializeField] private EnvironmentInstaller environment = null;
        [SerializeField] private IKInstaller ik = null;
        [SerializeField] private MonitoringInstaller monitoring = null;
        [SerializeField] private ModelLoadInstaller modelLoad = null;
        [SerializeField] private ScreenshotCountDownUiInstaller screenshotCountDown = null;
        [SerializeField] private MotionCalculationInstaller motionCalculationInstaller = null;
        [SerializeField] private FaceControlInstaller faceControl = null;
        [SerializeField] private WordToMotionInstaller wordToMotion = null;
        [SerializeField] private AccessoryItemController accessoryControllerPrefab = null;
        [SerializeField] private DeformableCounter deformableCounterPrefab = null;
        [SerializeField] private InterProcessCommunicationInstaller interProcess = null;
        [SerializeField] private StartupLoadingCover loadingCoverController = null;

        [SerializeField] private BuddySpriteCanvas buddySpriteCanvas;
        [SerializeField] private BuddyGuiCanvas buddyGuiCanvas;
        [SerializeField] private BuddyAudioSources buddyAudioSources;
        [SerializeField] private BuddyManifestTransform3DInstance buddyTransform3DInstancePrefab;
        [SerializeField] private BuddySprite3DInstance buddySprite3DInstancePrefab;
        
        public override void InstallBindings()
        {
            Container.BindInstance(loadingCoverController);
            Container.BindInterfacesTo<StartupLoadingCoverController>().AsSingle();
            Container.Bind<ICoroutineSource>().To<CoroutineSource>().FromNewComponentOnNewGameObject().AsSingle();

            foreach (var installer in new InstallerBase[]
                {
                    devices,
                    environment,
                    ik,
                    monitoring,
                    modelLoad,
                    interProcess,
                    screenshotCountDown,
                    motionCalculationInstaller,
                    faceControl,
                    wordToMotion,
                })
            {
                installer.Install(Container);
            }
            
            //ここから下もサブクラスにしたほうが良いかもしれない
            Container.BindInstance(builtInClip);

            //Deformを使うオブジェクトがここを参照することで、DeformableManagerを必要な時だけ動かせるようにする
            Container.Bind<DeformableCounter>()
                .FromComponentInNewPrefab(deformableCounterPrefab)
                .AsSingle();
            
            Container.Bind<FaceControlConfiguration>()
                .AsSingle();
            
            Container.Bind<AccessoryItemController>()
                .FromComponentInNewPrefab(accessoryControllerPrefab)
                .AsCached()
                .NonLazy();

            Container.Bind<BuddySpriteCanvas>()
                .FromComponentInNewPrefab(buddySpriteCanvas)
                .AsCached();
            Container.Bind<BuddyGuiCanvas>()
                .FromComponentInNewPrefab(buddyGuiCanvas)
                .AsCached();
            Container.Bind<BuddyAudioSources>()
                .FromComponentInNewPrefab(buddyAudioSources)
                .AsCached();
            Container.BindIFactory<BuddyManifestTransform3DInstance>()
                .FromComponentInNewPrefab(buddyTransform3DInstancePrefab)
                .AsSingle();
            Container.BindIFactory<BuddySprite3DInstance>()
                .FromComponentInNewPrefab(buddySprite3DInstancePrefab)
                .AsSingle();
            BuddySystemInstaller.Install(Container);
            
            WordToMotion.WordToMotionInstaller.Install(Container);
            
            MediaPipeTrackerSystemInstaller.Install(Container);

            if (DebugEnvChecker.IsDevEnvOrEditor)
            {
                Container.BindInterfacesTo<DebugVmmCommandReceiver>().AsSingle();
            }
        }
    }
}
