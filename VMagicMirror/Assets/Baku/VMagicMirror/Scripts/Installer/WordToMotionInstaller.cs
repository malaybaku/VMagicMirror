using Baku.VMagicMirror.WordToMotion;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    public class WordToMotionInstaller : InstallerBase
    {
        [SerializeField] private WordToMotionManager wordToMotionManager = null;
        [SerializeField] private WordToMotionBlendShape blendShape = null;
    
        public override void Install(DiContainer container)
        {
            container.Bind<WordToMotionAccessoryRequest>().AsSingle();
            container.BindInstance(wordToMotionManager).AsCached();
            container.BindInstance(blendShape).AsCached();
        }
    }
}
