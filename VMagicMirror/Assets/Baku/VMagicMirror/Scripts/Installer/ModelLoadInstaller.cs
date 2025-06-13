using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    /// <summary> モデルのロード処理周りの処理をインジェクトしてくれるすごいやつだよ </summary>
    public class ModelLoadInstaller : InstallerBase
    {
        [SerializeField] private VRM10InstanceUpdater instanceUpdater;
        [SerializeField] private VRMPreviewCanvas vrmPreviewCanvasPrefab;
        [SerializeField] private VRM10MetaView vrm10MetaViewPrefab;

        public override void Install(DiContainer container)
        {
            container.BindInstance(instanceUpdater).AsCached();
            container.Bind<VRMPreviewLanguage>().AsCached();
            container.Bind<VrmLoadProcessBroker>().AsSingle();
            
            container.Bind<VRMPreviewCanvas>()
                .FromComponentInNewPrefab(vrmPreviewCanvasPrefab)
                .AsCached();

            container.BindIFactory<VRM10MetaView>()
                .FromComponentInNewPrefab(vrm10MetaViewPrefab);
            container.Bind<VRM10MetaViewController>().AsSingle();
            
            container.BindInterfacesAndSelfTo<VRM10LoadController>().AsSingle();
            container.BindInterfacesTo<VRMPreviewPresenter>().AsSingle();
            container.BindInterfacesAndSelfTo<VRMPreloadDataOverrider>().AsSingle();
            
            container.Bind<SettingAutoAdjuster>().AsCached();
        }
    }
}