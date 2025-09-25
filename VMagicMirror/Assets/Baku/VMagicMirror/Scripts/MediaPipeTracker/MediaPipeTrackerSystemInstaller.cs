using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public class MediaPipeTrackerSystemInstaller : Installer<MediaPipeTrackerSystemInstaller>
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<MediaPipeTrackerRuntimeSettingsRepository>().AsSingle();
            Container.BindInterfacesAndSelfTo<BodyScaleCalculator>().AsSingle();
            Container.Bind<TrackingLostHandCalculator>().AsSingle();

            Container.Bind<CameraCalibrator>().AsSingle();
            Container.BindInterfacesAndSelfTo<WebCamTextureSource>().AsSingle();
            Container.BindInterfacesAndSelfTo<MediaPipeKinematicSetter>().AsSingle();
            Container.BindInterfacesAndSelfTo<MediaPipeFacialValueRepository>().AsSingle();
            
            Container.Bind<FaceLandmarkTask>().AsSingle();
            if (true)
            {
                Container.Bind<IHandLandmarkTask>().To<HandTaskV2>().AsSingle();
                Container.Bind<IHandAndFaceLandmarkTask>().To<HandAndFaceLandmarkTaskV2>().AsSingle();
            }
            else
            {
                Container.Bind<IHandLandmarkTask>().To<HandTask>().AsSingle();
                Container.Bind<IHandAndFaceLandmarkTask>().To<HandAndFaceLandmarkTask>().AsSingle();
            }
            Container.BindInterfacesAndSelfTo<MediaPipeTrackerTaskController>().AsSingle();

            Container.Bind<MediaPipeFingerPoseCalculator>().AsSingle();
            Container.BindInterfacesAndSelfTo<MediaPipeBlink>().AsSingle();
            Container.BindInterfacesAndSelfTo<MediaPipeEyeJitter>().AsSingle();
            Container.BindInterfacesAndSelfTo<MediaPipeLipSync>().AsSingle();
            
            Container.BindInterfacesAndSelfTo<MediaPipeHand>().AsSingle();

            Container.BindInterfacesTo<MediaPipeFaceSwitchSetter>().AsSingle();
            Container.BindInterfacesAndSelfTo<MediaPipeTrackerStatusPreviewSender>().AsSingle();

            // NOTE: 基本のトラッキングシステムというよりはモーション適用時の後処理みたいなやつ
            Container.BindInterfacesTo<MediaPipeHandLocalRotLimiter>().AsSingle();
        }
    }
}
