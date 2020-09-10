using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    public class WordToMotionInstaller : InstallerBase
    {
        [SerializeField] private WordToMotionBlendShape blendShape = null;
    
        public override void Install(DiContainer container)
        {
            container.BindInstance(blendShape).AsCached();
        }
    }
}
