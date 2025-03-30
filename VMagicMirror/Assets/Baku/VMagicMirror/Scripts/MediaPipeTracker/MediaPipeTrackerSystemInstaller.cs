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

            Container.Bind<KinematicSetterTimingInvoker>().FromNewComponentOnNewGameObject();
            Container.BindInterfacesAndSelfTo<KinematicSetter>().AsSingle();
            
            Container.Bind<HandPlayground>().AsSingle();
            Container.Bind<FaceLandmarkPlayground>().AsSingle();
            Container.Bind<HandAndFaceLandmarkPlayground>().AsSingle();
            Container.BindInterfacesAndSelfTo<MediaPipeTrackerTaskController>().AsSingle();
        }
    }
}
