using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class CarHandleInstaller : MonoInstaller
    {
        [SerializeField] private CarHandleVisibilityView visibilityView;

        public override void InstallBindings()
        {
            Container.BindInstance(visibilityView);
            Container.BindInterfacesTo<CarHandleVisibilityUpdater>().AsSingle();
        }
    }
}
