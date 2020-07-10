using Zenject;

namespace Baku.VMagicMirror.Installer
{
    /// <summary> 顔周りでなんかインストールするやつ </summary>
    public class FaceControlInstaller : InstallerBase
    {
        public override void Install(DiContainer container)
        {
            container.Bind<VRMBlendShapeStore>().AsCached();
        }
    }
}
