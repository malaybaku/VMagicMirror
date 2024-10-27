using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class PenTabletInstaller : MonoInstaller
    {
        [SerializeField] private PenTabletVisibilityView visibilityView;

        public override void InstallBindings()
        {
            Container.BindInstance(visibilityView);
            Container.BindInterfacesTo<PenTabletVisibilityUpdater>().AsSingle();
        }
    }
}
