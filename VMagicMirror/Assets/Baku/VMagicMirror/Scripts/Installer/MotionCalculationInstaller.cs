using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    public class MotionCalculationInstaller : InstallerBase
    {
        [SerializeField] private FaceAttitudeController faceAttitude = null;
        [SerializeField] private HeadMotionClipPlayer headMotionClipPlayer = null;
    
        public override void Install(DiContainer container)
        {
            container.BindInstance(faceAttitude).AsCached();
            container.BindInstance(headMotionClipPlayer).AsCached();
        }
    }
}
