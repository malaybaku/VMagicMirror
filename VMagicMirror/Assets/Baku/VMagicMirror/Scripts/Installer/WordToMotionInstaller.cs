using Baku.VMagicMirror.WordToMotion;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    public class WordToMotionInstaller : InstallerBase
    {
        [SerializeField] private CustomMotionPlayer customMotionPlayerV2 = null;
        [SerializeField] private WordToMotionBlendShape blendShape = null;
        
        public override void Install(DiContainer container)
        {
            container.Bind<IWordToMotionPlayer>()
                .FromInstance(customMotionPlayerV2)
                .AsCached();
            container.Bind<WordToMotionAccessoryRequest>().AsSingle();
            container.BindInstance(blendShape).AsSingle();
        }
    }
}
