using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    public class MotionCalculationInstaller : InstallerBase
    {
        [SerializeField] private FaceAttitudeController faceAttitude;
    
        public override void Install(DiContainer container)
        {
            container.BindInstance(faceAttitude).AsCached();
        }
    }
}
