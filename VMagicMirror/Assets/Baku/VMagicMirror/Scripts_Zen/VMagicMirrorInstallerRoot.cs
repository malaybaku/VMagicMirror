using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    //TODO: このクソダサい名前を治すまではコミット禁止です。
    public class VMagicMirrorInstallerRoot : MonoInstaller
    {
        [SerializeField] private DevicesInstaller devices;
        [SerializeField] private EnvironmentInstaller environment;
        [SerializeField] private IKInstaller ik;
        [SerializeField] private MonitoringInstaller monitoring;
        [SerializeField] private ModelLoadInstaller modelLoad;
        [SerializeField] private ScreenshotCountDownUiInstaller screenshotCountDown;
        [SerializeField] private DeformableCounter deformableCounterPrefab = null;
        [SerializeField] private InterProcessCommunicationInstaller interProcess;
        
        public override void InstallBindings()
        {
            base.InstallBindings();

            var installers = new InstallerBase[]
            {
                devices,
                environment,
                ik,
                monitoring,
                modelLoad,
                interProcess,
                screenshotCountDown,
            };

            foreach (var installer in installers)
            {
                installer.Install(Container);
            }
            
            
            //TEMP: ここから下もサブクラスにしたほうが良いのでは

            //Deformを使うオブジェクトがここを参照することで、DeformableManagerを必要な時だけ動かせるようにする
            Container.Bind<DeformableCounter>()
                .FromComponentInNewPrefab(deformableCounterPrefab)
                .AsSingle();
            
            Container
                .BindInstance(new FaceControlConfiguration())
                .AsSingle();
            
            //TODO: FindObjectOfTypeを卒業しろ…というか目ボーンの処理自体を統合したい…
            Container.BindInstance(FindObjectOfType<EyeBoneResetter>());
        }
    }
}
