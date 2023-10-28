using Zenject;

namespace Baku.VMagicMirror.WordToMotion
{
    public class WordToMotionInstaller : Installer<WordToMotionInstaller>
    {
        public override void InstallBindings()
        {
            //repository
            Container.Bind<WordToMotionRequestRepository>().AsSingle();
            Container.Bind<CustomMotionRepository>().AsSingle();

            //request sources
            Container.BindInterfacesTo<GamePadRequestSource>().AsSingle();
            Container.BindInterfacesTo<SingleKeyInputRequestSource>().AsSingle();
            Container.BindInterfacesTo<WordKeyInputRequestSource>().AsSingle();
            Container.BindInterfacesTo<MidiRequestSource>().AsSingle();
            //Empty入れても入れなくても挙動に影響しない…はず…
            //Container.BindInterfacesTo<EmptyRequestSource>().AsSingle();
            
            Container.Bind<VrmaRepository>().AsSingle();
            Container.Bind<VrmaMotionSetter>().AsSingle();
            Container.BindInterfacesAndSelfTo<VrmaMotionPlayer>().AsSingle();

            //どっちが良いか微妙なライン…うーん…
            Container.BindInterfacesAndSelfTo<BuiltInMotionPlayer>().AsSingle();
            //Container.BindInterfacesTo<BuiltInMotionPlayerV2>().AsSingle();

            //presenter
            Container.Bind<WordToMotionRequester>().AsSingle();
            Container.BindInterfacesTo<WordToMotionPresenter>().AsSingle();
            Container.BindInterfacesAndSelfTo<WordToMotionRunner>().AsSingle();
            
        }
    }
}
