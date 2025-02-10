using Baku.VMagicMirror.Buddy;
using Baku.VMagicMirror.LuaScript;

namespace Baku.VMagicMirror
{
    public class ScriptApiInstaller : Zenject.Installer<ScriptApiInstaller>
    {
        public override void InstallBindings()
        {
            Container.BindIFactory<string, ScriptCaller>().AsSingle();
            Container.BindInterfacesAndSelfTo<ScriptLoader>().AsSingle();

            Container.BindInterfacesAndSelfTo<BuddySettingsRepository>().AsSingle();
            Container.Bind<BuddyPropertyRepository>().AsSingle();
            Container.Bind<BuddyLayoutRepository>().AsSingle();
            Container.Bind<BuddyTransformInstanceRepository>().AsSingle();
            
            Container.BindInterfacesTo<BuddyPropertyUpdater>().AsSingle();
            Container.BindInterfacesTo<BuddyLayoutUpdater>().AsSingle();

            Container.BindInterfacesTo<ScriptCallerRegisterer>().AsSingle();
            Container.BindInterfacesTo<BuddyLayoutEditNotifier>().AsSingle();
        }
    }
}
