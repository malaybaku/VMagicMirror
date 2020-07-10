using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    public class IKInstaller : InstallerBase
    {
        [SerializeField] private IK.IKTargetTransforms ikTargets = null;
        
        public override void Install(DiContainer container)
        {
            container.BindInstance(ikTargets);
        }
    }
}
