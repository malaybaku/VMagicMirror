using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class ArcadeStickInstaller : MonoInstaller
    {
        [SerializeField] private ArcadeStickVisibilityView visibilityView;

        public override void InstallBindings()
        {
            Container.BindInstance(visibilityView);
            Container.BindInterfacesTo<ArcadeStickVisibilityUpdater>().AsSingle();
        }
    }
}
