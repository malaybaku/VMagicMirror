using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class KeyboardInstaller : MonoInstaller
    {
        [SerializeField] private KeyboardVisibilityView visibilityView;

        public override void InstallBindings()
        {
            Container.BindInstance(visibilityView);
            Container.BindInterfacesTo<KeyboardVisibilityUpdater>().AsSingle();
        }
    }
}
