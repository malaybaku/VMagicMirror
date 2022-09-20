using Baku.VMagicMirror.WordToMotion;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    public class WordToMotionInstaller : InstallerBase
    {
        [SerializeField] private WordToMotionBlendShape blendShape = null;
        [SerializeField] private IkWeightCrossFade ikWeightCrossFade = null;
        
        public override void Install(DiContainer container)
        {
            container.Bind<WordToMotionAccessoryRequest>().AsSingle();
            container.BindInstances(
                blendShape, 
                ikWeightCrossFade
                );
        }
    }
}
