using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    /// <summary> 顔周りでなんかインストールするやつ </summary>
    public class FaceControlInstaller : InstallerBase
    {
        [SerializeField] private BlendShapeInitializer blendShapeInitializer = null;
        [SerializeField] private DeviceSelectableLipSyncContext lipSyncContext = null;
        [SerializeField] private LipSyncIntegrator lipSyncIntegrator = null;
        [SerializeField] private VRMAutoBlink autoBlink = null;

        public override void Install(DiContainer container)
        {
            container.BindInstance(blendShapeInitializer).AsCached();
            container.BindInstance(lipSyncContext).AsCached();
            container.BindInstance(lipSyncIntegrator).AsCached();
            container.BindInstance(autoBlink).AsCached();
        }
    }
}
