using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    /// <summary> 顔周りでなんかインストールするやつ </summary>
    public class FaceControlInstaller : InstallerBase
    {
        [SerializeField] private BlendShapeInitializer blendShapeInitializer = null;
        
        public override void Install(DiContainer container)
        {
            container.Bind<VRMBlendShapeStore>().AsCached();
            container.BindInstance(blendShapeInitializer).AsCached();
        }
    }
}
