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
            
            Container.BindInterfacesAndSelfTo<AvatarBoneInitialLocalOffsets>().AsSingle();
            Container.BindInterfacesAndSelfTo<VMCPHandPose>().AsSingle();
            Container.BindInterfacesAndSelfTo<VMCPHeadPose>().AsSingle();
            Container.BindInterfacesAndSelfTo<VMCPBasedFingerSetter>().AsSingle();
            Container.BindInterfacesAndSelfTo<VMCPBlendShape>().AsSingle();
            Container.BindInterfacesTo<VMCPReceiver>().AsSingle();
        }
    }
}
