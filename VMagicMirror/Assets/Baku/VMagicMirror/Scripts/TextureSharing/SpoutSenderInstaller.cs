using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class SpoutSenderInstaller : MonoInstaller
    {
        [SerializeField] private SpoutSenderWrapperView spoutSenderView;

        public override void InstallBindings()
        {
            Container.BindInstance(spoutSenderView);
            Container.BindInterfacesTo<SpoutSenderController>().AsSingle();
        }
    }
}
