using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public class MediaPipeTrackerSystemInstaller : Installer<MediaPipeTrackerSystemInstaller>
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<MediaPipeTrackerSettingsRepository>().AsSingle();
            Container.BindInterfacesAndSelfTo<BodyScaleCalculator>().AsSingle();
            Container.Bind<TrackingLostHandCalculator>().AsSingle();

            Container.Bind<CameraCalibrator>().AsSingle();

            Container.BindInterfacesAndSelfTo<WebCamTextureSource>().AsSingle();

            //TODO: たぶんTimingInvoker自体を削除できると思うので、なるべく消したい…
            Container.Bind<KinematicSetterTimingInvoker>().FromNewComponentOnNewGameObject().AsCached();
            Container.BindInterfacesAndSelfTo<KinematicSetter>().AsSingle();

            // TODO: 実装がちゃんとしてないので注意！
            Container.BindInterfacesAndSelfTo<FacialSetter>().AsSingle();
            
            Container.Bind<HandPlayground>().AsSingle();
            Container.Bind<FaceLandmarkPlayground>().AsSingle();
            Container.Bind<HandAndFaceLandmarkPlayground>().AsSingle();
            Container.BindInterfacesAndSelfTo<MediaPipeTrackerTaskController>().AsSingle();
        }
    }
}
