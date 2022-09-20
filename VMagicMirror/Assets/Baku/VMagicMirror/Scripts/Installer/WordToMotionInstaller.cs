using Baku.VMagicMirror.WordToMotion;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    public class WordToMotionInstaller : InstallerBase
    {
        [SerializeField] private CustomMotionPlayer customMotionPlayer = null;
        [SerializeField] private WordToMotionBlendShape blendShape = null;
        [SerializeField] private IkWeightCrossFade ikWeightCrossFade = null;
        
        public override void Install(DiContainer container)
        {
            container.Bind<IWordToMotionPlayer>()
                .FromInstance(customMotionPlayer)
                .AsCached();
            container.Bind<WordToMotionAccessoryRequest>().AsSingle();
            container.BindInstances(
                blendShape, 
                ikWeightCrossFade
                );
        }
    }
}
