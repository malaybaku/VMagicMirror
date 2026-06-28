using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    [RequireComponent(typeof(Camera))]
    public class EnvironmentInstaller : InstallerBase
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera refCameraForRay;

        public override void Install(DiContainer container)
        {
            container.BindInstance(mainCamera);
            container.BindInstance(refCameraForRay).WithId("RefCameraForRay");
            container.Bind<RuntimeTransformControlFactory>().AsSingle();

            container.BindInterfacesTo<CameraFovController>().AsSingle();
            container.BindInterfacesAndSelfTo<CameraUtilWrapper>().AsSingle();
            container.Bind<WindowStateRepository>().AsSingle();

            container.BindInterfacesTo<AntiAliasSettingSetter>().AsSingle();
            container.BindInterfacesAndSelfTo<LanguageSettingRepository>().AsSingle();
            container.BindInterfacesAndSelfTo<CurrentFramerateChecker>().AsSingle();

            container.BindInterfacesTo<ImageQualitySettingReceiver>().AsSingle();
            container.BindInterfacesAndSelfTo<CropAndOutlineController>().AsSingle();
            container.BindInterfacesAndSelfTo<CameraBackgroundColorController>().AsSingle();
            container.BindInterfacesAndSelfTo<AvatarMaskTextureController>().AsSingle();
            container.BindInterfacesTo<AvatarOffsetRimController>().AsSingle();
            
            // NOTE: サブキャラに依存している
            container.BindInterfacesAndSelfTo<Buddy.BuddyObjectRaycastChecker>().AsSingle();
        }
    }
}
