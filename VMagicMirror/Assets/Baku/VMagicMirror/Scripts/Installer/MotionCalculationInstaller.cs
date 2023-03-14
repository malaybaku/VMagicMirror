using Baku.VMagicMirror.GameInput;
using Baku.VMagicMirror.IK;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    public class MotionCalculationInstaller : InstallerBase
    {
        [SerializeField] private HandIKIntegrator handIKIntegrator;
        [SerializeField] private FaceAttitudeController faceAttitude;
        [SerializeField] private HeadMotionClipPlayer headMotionClipPlayer;
        [SerializeField] private ColliderBasedAvatarParamLoader colliderBasedAvatarParamLoader;
        [SerializeField] private NonImageBasedMotion nonImageBasedMotion;
        [SerializeField] private FingerController fingerController;
        [SerializeField] private ElbowMotionModifier elbowMotionModifier;

        public override void Install(DiContainer container)
        {
            container.BindInterfacesAndSelfTo<BodyMotionModeController>().AsSingle();

            container.BindInterfacesAndSelfTo<HandDownIkCalculator>().AsSingle();
            container.BindInterfacesAndSelfTo<CustomizedDownHandIk>().AsSingle();
            container.Bind<SwitchableHandDownIkData>().AsSingle();
            container.BindInstances(
                handIKIntegrator,
                faceAttitude,
                colliderBasedAvatarParamLoader,
                nonImageBasedMotion,
                fingerController,
                elbowMotionModifier
            );

            container.Bind(typeof(HeadMotionClipPlayer), typeof(IWordToMotionPlayer))
                .FromInstance(headMotionClipPlayer)
                .AsCached();

            container.BindInterfacesAndSelfTo<ClapMotionPlayer>().AsSingle();
            container.BindInterfacesTo<FootIkSetter>().AsSingle();


            container.BindInterfacesAndSelfTo<GamepadGameInputSource>().AsSingle();
            container.BindInterfacesAndSelfTo<KeyboardGameInputSource>().AsSingle();
            container.BindInterfacesAndSelfTo<GameInputSourceSet>().AsSingle();

            container.BindInterfacesAndSelfTo<TaskBasedIkWeightFader>().AsSingle();
            container.BindInterfacesTo<GameInputIKWeightController>().AsSingle();
            container.BindInterfacesTo<GamepadInputBodyMotionController>().AsSingle();
        }
    }
}
