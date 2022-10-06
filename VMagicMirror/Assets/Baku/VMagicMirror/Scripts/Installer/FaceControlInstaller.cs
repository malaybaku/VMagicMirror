using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    /// <summary> 顔周りでなんかインストールするやつ </summary>
    public class FaceControlInstaller : InstallerBase
    {
        [SerializeField] private VmmLipSyncContextBase lipSyncContext = null;
        [SerializeField] private LipSyncIntegrator lipSyncIntegrator = null;
        [SerializeField] private VRMAutoBlink autoBlink = null;
        [SerializeField] private EyeBoneAngleSetter eyeBoneAngleSetter = null;

        public override void Install(DiContainer container)
        {
            container.Bind<VmmLipSyncContextBase>().FromInstance(lipSyncContext).AsCached();
            container.BindInstances(
                lipSyncIntegrator,
                autoBlink,
                eyeBoneAngleSetter
            );

            container.BindInterfacesAndSelfTo<ExpressionAccumulator>().AsSingle();
            container.Bind<EyeLookAt>().AsSingle();
            container.BindInterfacesTo<EyeLookAtUpdater>().AsSingle();
        }
    }
}
