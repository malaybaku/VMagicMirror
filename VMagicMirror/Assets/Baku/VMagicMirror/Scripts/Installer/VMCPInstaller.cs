using Baku.VMagicMirror.VMCP;
using UnityEngine;
using uOSC;
using Zenject;

namespace Baku.VMagicMirror
{
    public class VMCPInstaller : MonoInstaller
    {
        [SerializeField] private uOscServer oscServerPrefab;
        [SerializeField] private uOscClient oscClientPrefab;

        public override void InstallBindings()
        {
            // receiver
            Container.BindIFactory<uOscServer>()
                .FromComponentInNewPrefab(oscServerPrefab)
                .AsSingle();
            
            Container.BindInterfacesAndSelfTo<AvatarBoneInitialLocalOffsets>().AsSingle();
            Container.BindInterfacesAndSelfTo<VMCPHandPose>().AsSingle();
            Container.BindInterfacesAndSelfTo<VMCPHeadPose>().AsSingle();
            Container.BindInterfacesAndSelfTo<VMCPBasedFingerSetter>().AsSingle();
            Container.BindInterfacesAndSelfTo<VMCPBlendShape>().AsSingle();
            Container.BindInterfacesAndSelfTo<VMCPActiveness>().AsSingle();
            Container.BindInterfacesTo<VMCPReceiver>().AsSingle();
            Container.Bind<VMCPFingerController>().FromNewComponentOnNewGameObject().AsSingle();
            Container.Bind<VMCPNaiveBoneTransfer>().FromNewComponentOnNewGameObject().AsSingle();
            
            // sender:
            // NOTE: VMCP有効中のエフェクト表示に対してはLightingControllerも関与してる
            Container.BindIFactory<uOscClient>()
                .FromComponentInNewPrefab(oscClientPrefab)
                .AsSingle();
            Container.BindInterfacesTo<VMCPSender>()
                .AsSingle();
        }
    }
}
