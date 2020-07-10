using Baku.VMagicMirror.InterProcess;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    /// <summary>
    /// プロセス間通信のインフラをInstallする処理
    /// </summary>
    public class InterProcessCommunicationInstaller : InstallerBase
    {
        
        public override void Install(DiContainer container)
        {
            container
                .BindInterfacesTo<MmfBasedMessageIo>()
                .AsCached();
        }
    }
}
