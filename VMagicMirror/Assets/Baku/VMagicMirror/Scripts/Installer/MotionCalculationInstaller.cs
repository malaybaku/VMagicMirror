using Baku.VMagicMirror.GameInput;
using Baku.VMagicMirror.IK;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    public class MotionCalculationInstaller : InstallerBase
    {
        [SerializeField] private HandIKIntegrator handIKIntegrator = null;
        [SerializeField] private FaceAttitudeController faceAttitude = null;
        [SerializeField] private HeadMotionClipPlayer headMotionClipPlayer = null;
        [SerializeField] private ColliderBasedAvatarParamLoader colliderBasedAvatarParamLoader = null;
        [SerializeField] private NonImageBasedMotion nonImageBasedMotion = null;
        [SerializeField] private FingerController fingerController = null;

        public override void Install(DiContainer container)
        {
            container.BindInterfacesAndSelfTo<HandDownIkCalculator>().AsSingle();
            container.BindInterfacesAndSelfTo<CustomizedDownHandIk>().AsSingle();
            container.Bind<SwitchableHandDownIkData>().AsSingle();
            container.BindInstances(
                handIKIntegrator,
                faceAttitude,
                colliderBasedAvatarParamLoader,
                nonImageBasedMotion,
                fingerController
            );

            container.Bind(typeof(HeadMotionClipPlayer), typeof(IWordToMotionPlayer))
                .FromInstance(headMotionClipPlayer)
                .AsCached();

            container.BindInterfacesAndSelfTo<ClapMotionPlayer>().AsSingle();
            container.BindInterfacesTo<FootIkSetter>().AsSingle();


            container.BindInterfacesAndSelfTo<GamepadGameInputSource>().AsSingle();
            container.BindInterfacesAndSelfTo<KeyboardGameInputSource>().AsSingle();
            container.BindInterfacesTo<GameInputSourceSwitcher>()
                .FromNewComponentOn(_ => gameObject)
                .AsSingle();
            container.BindInterfacesTo<GamepadInputBodyMotionController>().AsSingle();

            container.BindInterfacesAndSelfTo<BodyMotionModeController>().AsSingle();
        }
    }
}
