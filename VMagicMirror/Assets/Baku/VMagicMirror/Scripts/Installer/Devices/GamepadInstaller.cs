using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class GamepadInstaller : MonoInstaller
    {
        [SerializeField] private GamepadVisibilityView visibilityView;

        public override void InstallBindings()
        {
            Container.BindInstance(visibilityView);
            Container.BindInterfacesTo<GamepadVisibilityUpdater>().AsSingle();
        }
    }
}
