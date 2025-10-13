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
        public override void Install(DiContainer container)
        {
            container.BindInstance(mainCamera);
            container.BindInstance(refCameraForRay).WithId("RefCameraForRay");
            container.Bind<PostProcessLayer>().FromMethod(_ => GetComponent<PostProcessLayer>()).AsCached();
            container.BindInterfacesTo<CameraFovController>().AsSingle();
            container.Bind<CameraUtilWrapper>().AsSingle();

            container.BindInterfacesTo<AntiAliasSettingSetter>().AsSingle();
            container.BindInterfacesAndSelfTo<LanguageSettingRepository>().AsSingle();
            container.BindInterfacesAndSelfTo<CurrentFramerateChecker>().AsSingle();

            container.BindInterfacesTo<ImageQualitySettingReceiver>().AsSingle();
            
            // NOTE: サブキャラに依存している
            container.BindInterfacesAndSelfTo<Buddy.BuddyObjectRaycastChecker>().AsSingle();
        }
    }
}
