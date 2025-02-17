using Baku.VMagicMirror.Buddy;
using Baku.VMagicMirror.Buddy.Api;

namespace Baku.VMagicMirror
{
    public class BuddySystemInstaller : Zenject.Installer<BuddySystemInstaller>
    {
        public override void InstallBindings()
        {
            Container.BindIFactory<string, ScriptCaller>().AsSingle();
            Container.BindIFactory<RootApi, ScriptEventInvoker>().AsSingle();
            Container.BindInterfacesAndSelfTo<ScriptLoader>().AsSingle();

            Container.BindInterfacesAndSelfTo<BuddySettingsRepository>().AsSingle();
            Container.Bind<BuddyPropertyRepository>().AsSingle();
            Container.Bind<BuddyLayoutRepository>().AsSingle();
            Container.Bind<BuddyTransformInstanceRepository>().AsSingle();
            
            Container.BindInterfacesTo<BuddyPropertyUpdater>().AsSingle();
            Container.BindInterfacesTo<BuddyLayoutUpdater>().AsSingle();

            Container.BindInterfacesTo<BuddyLayoutEditNotifier>().AsSingle();

            Container.Bind<ApiImplementBundle>().AsSingle();
            Container.BindInterfacesAndSelfTo<AvatarMotionEventApiImplement>().AsSingle();
            Container.Bind<AvatarFacialApiImplement>().AsSingle();
            Container.Bind<AvatarLoadApiImplement>().AsSingle();
            Container.Bind<AvatarPoseApiImplement>().AsSingle();
            Container.Bind<DeviceLayoutApiImplement>().AsSingle();
            Container.BindInterfacesAndSelfTo<RawInputApiImplement>().AsSingle();
            Container.BindInterfacesAndSelfTo<WordToMotionEventApiImplement>().AsSingle();
        }
    }
}
