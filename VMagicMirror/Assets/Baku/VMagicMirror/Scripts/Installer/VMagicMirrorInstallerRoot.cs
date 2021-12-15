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
        
        public override void InstallBindings()
        { 
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
            
            Container
                .BindInstance(new FaceControlConfiguration())
                .AsSingle();
            
            //TODO: FindObjectOfTypeを卒業しろ…というか目ボーンの処理自体を統合したい…
            Container.BindInstance(FindObjectOfType<EyeBonePostProcess>());
            
            Container.Bind<AccessoryItemController>()
                .FromComponentInNewPrefab(accessoryControllerPrefab)
                .NonLazy();
        }
    }
}
