using Zenject;

namespace Baku.VMagicMirror.WordToMotion
{
    public class WordToMotionInstaller : Installer<WordToMotionInstaller>
    {
        public override void InstallBindings()
        {
            //repository
            Container.BindInterfacesTo<WordToMotionRequestRepository>().AsSingle();
            Container.BindInterfacesTo<CustomMotionRepository>().AsSingle();

            //request sources
            Container.BindInterfacesTo<GamePadRequestSource>().AsSingle();
            Container.BindInterfacesTo<SingleKeyInputRequestSource>().AsSingle();
            Container.BindInterfacesTo<WordKeyInputRequestSource>().AsSingle();
            Container.BindInterfacesTo<EmptyRequestSource>().AsSingle();
            
            //presenter
            Container.Bind<WordToMotionRequester>().AsSingle();
            Container.BindInterfacesTo<WordToMotionPresenter>().AsSingle();
            Container.Bind<WordToMotionRunner>().AsSingle();
        }
    }
}
