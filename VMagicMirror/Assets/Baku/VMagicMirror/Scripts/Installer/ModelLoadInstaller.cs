using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    /// <summary> モデルのロード処理周りの処理をインジェクトしてくれるすごいやつだよ </summary>
    public class ModelLoadInstaller : InstallerBase
    {
        [SerializeField] private VRMLoadController loadController = null;
        [SerializeField] private VRMPreviewCanvas vrmPreviewCanvasPrefab = null;

        public override void Install(DiContainer container)
        {
            // container.Bind<IVRMLoadable>()
            //     .FromInstance(loadController)
            //     .AsCached();

            container.BindInterfacesTo<VRM10LoadController>().AsSingle();
            
            container.Bind<VRMPreviewCanvas>()
                .FromComponentInNewPrefab(vrmPreviewCanvasPrefab)
                .AsCached();
            
            container.Bind<VRMPreviewLanguage>().AsCached();
            container.Bind<SettingAutoAdjuster>().AsCached();
        }
    }
}