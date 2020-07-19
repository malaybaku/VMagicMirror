using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    public class EnvironmentInstaller : InstallerBase
    {
        //NOTE: Camera.mainより多少行儀が良くなることを期待してる + ここをクラス的にするとメインじゃないカメラも渡せる
        [SerializeField] private Camera mainCam = null;
        [SerializeField] private PostProcessLayer postProcessLayer = null;
        
        public override void Install(DiContainer container)
        {
            container.BindInstance(mainCam).AsCached();
            container.BindInstance(postProcessLayer).AsCached();
        }
    }
}
