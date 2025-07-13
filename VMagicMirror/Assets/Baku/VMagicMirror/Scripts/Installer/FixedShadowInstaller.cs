using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class FixedShadowInstaller : MonoInstaller
    {
        [SerializeField] private Light fixedShadowLight;
        [SerializeField] private Renderer fixedShadowBoard;

        public override void InstallBindings()
        {
            Container.BindInstance(fixedShadowLight).WhenInjectedInto<FixedShadowController>();
            Container.BindInstance(fixedShadowBoard).WhenInjectedInto<FixedShadowController>();
            Container.BindInterfacesTo<FixedShadowController>().AsSingle();
        }
    }
}
