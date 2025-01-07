using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class TouchpadInstaller : MonoInstaller
    {
        [SerializeField] private TouchpadVisibilityView visibilityView;

        public override void InstallBindings()
        {
            Container.BindInstance(visibilityView);
            Container.BindInterfacesTo<TouchpadVisibilityUpdater>().AsSingle();
        }
    }
}
