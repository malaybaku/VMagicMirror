using Baku.VMagicMirror.Buddy;
using Baku.VMagicMirror.Buddy.Api;

namespace Baku.VMagicMirror
{
    public class BuddySystemInstaller : Zenject.Installer<BuddySystemInstaller>
    {
        public override void InstallBindings()
        {
            // Lua
            // Container.BindInterfacesTo<LuaScriptCallerPathGenerator>().AsSingle();
            // Container.BindIFactory<string, IScriptCaller>().To<ScriptCallerLua>().AsSingle();
            // Container.BindIFactory<RootApi, ScriptEventInvoker>().AsSingle();
            // NOST: ↓これは多分消せる
            //Container.BindInterfacesAndSelfTo<ScriptLoader>().AsSingle();

            // C#
            Container.BindInterfacesTo<CSharpScriptCallerPathGenerator>().AsSingle();
            Container.BindIFactory<string, IScriptCaller>().To<ScriptCallerCSharp>().AsSingle();
            Container.BindIFactory<RootApi, ScriptEventInvokerCSharp>().AsSingle();

            // 以下はだいたい共通 (Lua/C#差でコードいじるとこも多少あるかもだが)
            Container.BindInterfacesAndSelfTo<ScriptLoaderGeneric>().AsSingle();

            Container.BindInterfacesAndSelfTo<BuddySettingsRepository>().AsSingle();
            Container.Bind<BuddyPropertyRepository>().AsSingle();
            Container.Bind<BuddyLayoutRepository>().AsSingle();
            Container.Bind<BuddyTransformInstanceRepository>().AsSingle();
            
            Container.BindInterfacesTo<BuddyPropertyUpdater>().AsSingle();
            Container.BindInterfacesTo<BuddyLayoutUpdater>().AsSingle();

            Container.BindInterfacesTo<BuddyLayoutEditNotifier>().AsSingle();

            Container.Bind<ApiImplementBundle>().AsSingle();

            Container.Bind<ScreenApiImplement>().AsSingle();
            Container.Bind<AudioApiImplement>().AsSingle();
            Container.Bind<AvatarLoadApiImplement>().AsSingle();
            Container.Bind<AvatarPoseApiImplement>().AsSingle();
            Container.BindInterfacesAndSelfTo<AvatarMotionEventApiImplement>().AsSingle();
            Container.Bind<AvatarFacialApiImplement>().AsSingle();
            Container.Bind<DeviceLayoutApiImplement>().AsSingle();
            Container.BindInterfacesAndSelfTo<InputApiImplement>().AsSingle();
            Container.BindInterfacesAndSelfTo<WordToMotionEventApiImplement>().AsSingle();
        }
    }
}
