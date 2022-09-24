using Baku.VMagicMirror.WordToMotion;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    public class WordToMotionInstaller : InstallerBase
    {
        [SerializeField] private CustomMotionPlayerV2 customMotionPlayerV2 = null;
        [SerializeField] private WordToMotionBlendShape blendShape = null;
        [SerializeField] private IkWeightCrossFade ikWeightCrossFade = null;
        
        public override void Install(DiContainer container)
        {
            // container.Bind<IWordToMotionPlayer>()
            //     .FromInstance(customMotionPlayer)
            //     .AsCached();
            container.Bind<IWordToMotionPlayer>()
                .FromInstance(customMotionPlayerV2)
                .AsCached();
            container.Bind<WordToMotionAccessoryRequest>().AsSingle();
            container.BindInstances(
                blendShape, 
                ikWeightCrossFade
                );
        }
    }
}
