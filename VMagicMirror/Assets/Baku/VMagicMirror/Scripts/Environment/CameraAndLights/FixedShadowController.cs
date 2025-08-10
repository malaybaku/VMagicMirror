using Baku.VMagicMirror.VMCP;
using R3;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class FixedShadowController : PresenterBase
    {
        private readonly IMessageReceiver _receiver;
        private readonly BodyMotionModeController _bodyMotionModeController;
        private readonly VMCPReceiver _vmcpReceiver;
        private readonly Light _fixedShadowLight;
        private readonly Renderer _fixedShadowBoardRenderer;

        // NOTE: この5つはGUIから直接降ってくるやつ
        private readonly ReactiveProperty<bool> _shadowEnabled = new(true);
        private readonly ReactiveProperty<bool> _fixedShadowEnabledAlways = new(false);
        private readonly ReactiveProperty<bool> _fixedShadowEnabledWhenLocomotionActive = new(true);
        
        private readonly ReactiveProperty<bool> _fixedShadowEnabled = new(false);
        public IReactiveProperty<bool> FixedShadowEnabled => _fixedShadowEnabled;

        //TODOかも: +- の扱い
        private Vector3 _fixedShadowLightRotationEuler = new(30, 50, 0);
        
        public FixedShadowController(
            IMessageReceiver receiver,
            BodyMotionModeController bodyMotionModeController,
            VMCPReceiver vmcpReceiver,
            Light fixedShadowLight,
            Renderer fixedShadowBoardRenderer
            )
        {
            _receiver = receiver;
            _bodyMotionModeController = bodyMotionModeController;
            _vmcpReceiver = vmcpReceiver;

            _fixedShadowLight = fixedShadowLight;
            _fixedShadowBoardRenderer = fixedShadowBoardRenderer;
        }
        
        public override void Initialize()
        {
            _receiver.BindBoolProperty(VmmCommands.ShadowEnable, _shadowEnabled);
            _receiver.BindBoolProperty(
                VmmCommands.FixedShadowAlwaysEnable,
                _fixedShadowEnabledAlways);
            _receiver.BindBoolProperty(
                VmmCommands.FixedShadowWhenLocomotionActiveEnable,
                _fixedShadowEnabledWhenLocomotionActive);

            _receiver.AssignCommandHandler(
                VmmCommands.ShadowIntensity,
                c => SetShadowIntensity(c.ParseAsPercentage())
                );
            
            _receiver.AssignCommandHandler(
                VmmCommands.FixedShadowYaw,
                c =>  SetFixedShadowLightYaw(c.ToInt())
                );
            _receiver.AssignCommandHandler(
                VmmCommands.FixedShadowPitch,
                c => SetFixedShadowLightPitch(c.ToInt())
                );
         
            _shadowEnabled.CombineLatest(
                    _fixedShadowEnabledAlways,
                    _fixedShadowEnabledWhenLocomotionActive,
                    _bodyMotionModeController.MotionMode,
                    _vmcpReceiver.IsLocomotionReceiveSettingActive,
                    (shadowEnabled,
                        fixedShadowAlways,
                        fixedShadowWhenLocomotionActive,
                        motionMode,
                        isLocomotionReceiveSettingActive) =>
                    {
                        if (!shadowEnabled)
                        {
                            return false;
                        }
                        
                        if (fixedShadowAlways)
                        {
                            return true;
                        }

                        return fixedShadowWhenLocomotionActive &&
                            (motionMode is BodyMotionMode.GameInputLocomotion || isLocomotionReceiveSettingActive);
                    })
                .Subscribe(v => _fixedShadowEnabled.Value = v)
                .AddTo(this);

            _fixedShadowEnabled
                .Subscribe(enabled =>
                {
                    _fixedShadowLight.gameObject.SetActive(enabled);
                    _fixedShadowBoardRenderer.gameObject.SetActive(enabled);
                })
                .AddTo(this);
        }

        private void SetShadowIntensity(float intensity) 
            => _fixedShadowLight.intensity = intensity;

        private void SetFixedShadowLightYaw(int angleDeg)
        {
            _fixedShadowLightRotationEuler.y = angleDeg;
            _fixedShadowLight.transform.localEulerAngles = _fixedShadowLightRotationEuler;
        }
        
        private void SetFixedShadowLightPitch(int angleDeg)
        {
            _fixedShadowLightRotationEuler.x = angleDeg;
            _fixedShadowLight.transform.localEulerAngles = _fixedShadowLightRotationEuler;
        }
    }
}
