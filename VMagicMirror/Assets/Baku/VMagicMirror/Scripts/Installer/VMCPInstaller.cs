using Baku.VMagicMirror.VMCP;
using UnityEngine;
using uOSC;
using Zenject;

namespace Baku.VMagicMirror
{
    public class VMCPInstaller : MonoInstaller
    {
        [SerializeField] private uOscServer oscServerPrefab;

        public override void InstallBindings()
        {
            Container.BindIFactory<uOscServer>()
                .FromComponentInNewPrefab(oscServerPrefab)
                .AsSingle();
            
            Container.Bind<VMCPHandPose>().AsSingle();
            Container.Bind<VMCPHeadPose>().AsSingle();
            Container.Bind<VMCPBlendShape>().AsSingle();
            Container.BindInterfacesTo<VMCPReceiver>().AsSingle();
        }
    }
}
