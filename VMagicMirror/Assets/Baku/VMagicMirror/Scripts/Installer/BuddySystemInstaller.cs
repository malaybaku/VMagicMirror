using Baku.VMagicMirror.Buddy;
using Baku.VMagicMirror.Buddy.Api;

namespace Baku.VMagicMirror
{
    public class BuddySystemInstaller : Zenject.Installer<BuddySystemInstaller>
    {
        public override void InstallBindings()
        {
            // ScriptがC#であることに高度に依存してるクラスはこの辺
            Container.BindIFactory<string, IScriptCaller>().To<ScriptCallerCSharp>().AsSingle();
            Container.BindIFactory<RootApi, ScriptEventInvokerCSharp>().AsSingle();

            // 以下は言語依存性が低めのクラス (せいぜいファイル名とかで拡張子を見に行く程度)
            Container.BindInterfacesAndSelfTo<ScriptLoader>().AsSingle();

            // NOTE: BuddyLoggerを経由せずにFileLoggerを使うのは今のところ想定ユースケースがないので禁止している
            Container.Bind<BuddyFileLogger>().AsSingle().WhenInjectedInto<BuddyLogger>();
            Container.BindInterfacesAndSelfTo<BuddyLogger>().AsSingle();
            Container.BindInterfacesAndSelfTo<BuddyAdvancedSettingsRepository>().AsSingle();

            Container.BindInterfacesAndSelfTo<BuddySettingsRepository>().AsSingle();
            Container.Bind<BuddyPropertyRepository>().AsSingle();
            Container.Bind<BuddyLayoutRepository>().AsSingle();
            Container.Bind<BuddyManifestTransformInstanceRepository>().AsSingle();
            Container.Bind<BuddyRuntimeObjectRepository>().AsSingle();
            
            Container.Bind<Buddy3DInstanceCreator>().AsSingle();
            Container.Bind<BuddyPropertyActionBroker>().AsSingle();
            Container.BindInterfacesTo<BuddyPropertyUpdater>().AsSingle();
            Container.BindInterfacesTo<BuddyLayoutUpdater>().AsSingle();
            Container.BindInterfacesTo<BuddyTransform3DBoneAttachUpdater>().AsSingle();
            Container.BindInterfacesTo<BuddyRuntimeObjectUpdater>().AsSingle();
            Container.BindInterfacesTo<BuddySpriteShadowSettingUpdater>().AsSingle();
            Container.Bind<BuddySprite2DUpdater>().AsSingle();
            Container.BindInterfacesAndSelfTo<BuddyTalkTextUpdater>().AsSingle();

            Container.BindInterfacesTo<BuddyLayoutEditNotifier>().AsSingle();

            Container.Bind<ApiImplementBundle>().AsSingle();

            Container.Bind<ScreenApiImplement>().AsSingle();
            Container.Bind<BuddyAudioEventBroker>().AsSingle();
            Container.Bind<BuddyTalkTextEventBroker>().AsSingle();
            Container.Bind<BuddySpriteEventBroker>().AsSingle();
            Container.BindInterfacesAndSelfTo<AudioApiImplement>().AsSingle();
            Container.Bind<AvatarLoadApiImplement>().AsSingle();
            Container.Bind<AvatarPoseApiImplement>().AsSingle();
            Container.BindInterfacesAndSelfTo<AvatarMotionEventApiImplement>().AsSingle();
            Container.Bind<AvatarFacialApiImplement>().AsSingle();
            Container.Bind<DeviceLayoutApiImplement>().AsSingle();
            Container.BindInterfacesAndSelfTo<InputApiImplement>().AsSingle();
            Container.BindInterfacesAndSelfTo<WordToMotionEventApiImplement>().AsSingle();

            // ユーザースクリプト編集のサポート用の機能
            Container.BindInterfacesTo<BuddyApiFileCopyInitializer>().AsSingle();
        }
    }
}
