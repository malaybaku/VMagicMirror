using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    [RequireComponent(typeof(Camera))]
    [RequireComponent(typeof(PostProcessLayer))]
    public class EnvironmentInstaller : InstallerBase
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera refCameraForRay;
        [SerializeField] private PostProcessVolume postProcessVolume;

        public override void Install(DiContainer container)
        {
            container.BindInstance(mainCamera);
            container.BindInstance(refCameraForRay).WithId("RefCameraForRay");
            container.BindInstance(postProcessVolume);

            container.Bind<PostProcessLayer>().FromMethod(_ => GetComponent<PostProcessLayer>()).AsCached();
            container.BindInterfacesTo<CameraFovController>().AsSingle();
            container.Bind<CameraUtilWrapper>().AsSingle();

            container.BindInterfacesTo<AntiAliasSettingSetter>().AsSingle();
            container.BindInterfacesAndSelfTo<LanguageSettingRepository>().AsSingle();
            container.BindInterfacesAndSelfTo<CurrentFramerateChecker>().AsSingle();

            container.BindInterfacesTo<ImageQualitySettingReceiver>().AsSingle();
            container.BindInterfacesAndSelfTo<CropAndOutlineController>().AsSingle();
            container.BindInterfacesAndSelfTo<CameraBackgroundColorController>().AsSingle();
            
            // NOTE: サブキャラに依存している
            container.BindInterfacesAndSelfTo<Buddy.BuddyObjectRaycastChecker>().AsSingle();
        }
    }
}
