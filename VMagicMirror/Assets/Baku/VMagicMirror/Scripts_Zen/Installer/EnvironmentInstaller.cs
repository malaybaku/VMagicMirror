using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    public class EnvironmentInstaller : InstallerBase
    {
        //NOTE: Camera.mainより多少行儀が良くなることを期待してる + ここをクラス的にするとメインじゃないカメラも渡せる
        [SerializeField] private Camera mainCam = null;
        
        public override void Install(DiContainer container)
        {
            container.Bind<Camera>().FromInstance(mainCam).AsCached();

        }
    }
}
