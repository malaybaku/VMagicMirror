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
            Container.BindInterfacesTo<BuddyPropertyUpdater>().AsSingle();

            Container.BindInterfacesTo<ScriptCallerRegisterer>().AsSingle();
        }
    }
}
