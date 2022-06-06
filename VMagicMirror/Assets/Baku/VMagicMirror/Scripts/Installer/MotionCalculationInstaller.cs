using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    public class MotionCalculationInstaller : InstallerBase
    {
        [SerializeField] private HandIKIntegrator handIKIntegrator = null;
        [SerializeField] private FaceAttitudeController faceAttitude = null;
        [SerializeField] private HeadMotionClipPlayer headMotionClipPlayer = null;

        public override void Install(DiContainer container)
        {
            container.BindInstances(
                handIKIntegrator,
                faceAttitude,
                headMotionClipPlayer
            );

            container.BindInterfacesAndSelfTo<ClapMotionPlayer>().AsSingle();
        }
    }
}
