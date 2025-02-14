﻿using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Zenject;

namespace Baku.VMagicMirror
{
    public class LightingController : MonoBehaviour
    {
        //NOTE: 本質的な意味はない値だが、VRM 0.xから1.0に引き上げたら同等のライティングでも強すぎに見えるようになったため、
        //この係数をかけて光量を抑える。(MToonの何かが変わったものと思われるけど把握できてない)
        private const float LightIntensityConstFactor = 0.85f;
        
        [SerializeField] private Light mainLight = null;
        [SerializeField] private Vector3 mainLightLocalEulerAngle = default;
        
        [SerializeField] private Light shadowLight = null;
        [SerializeField] private Vector3 shadowLightLocalEulerAngle = default;
        [SerializeField] private ShadowBoardMotion shadowBoardMotion = null;

        [SerializeField] private PostProcessVolume postProcess = null;
        [SerializeField] private DesktopLightEstimator desktopLightEstimator = null;

        private Color _color = Color.white;
        private Bloom _bloom;
        private VmmAlphaEdge _vmmAlphaEdge;
        private VmmVhs _vmmVhs;
        private VmmMonochrome _vmmMonochrome;
        private bool _handTrackingEnabled = false;
        private bool _vmcpSendEnabled = false;
        //NOTE: この値自体はビルドバージョンによらずfalseがデフォルトで良いことに注意。
        //制限版でGUI側にtrue相当の値が表示されるが、これはGUI側が別途決め打ちしてくれてる
        private bool _showEffectDuringTracking = false;

        private bool _windowFrameVisible = true;
        private bool _enableOutlineEffect = false;
        
        [Inject]
        public void Initialize(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                VmmCommands.LightIntensity,
                message => SetLightIntensity(message.ParseAsPercentage())
                );
            receiver.AssignCommandHandler(
                VmmCommands.LightColor,
                message =>
                {
                    var lightRgb = message.ToColorFloats();
                    SetLightColor(lightRgb[0], lightRgb[1], lightRgb[2]);
                });

            receiver.AssignCommandHandler(
                VmmCommands.LightYaw,
                message => SetLightYaw(message.ToInt())
                );
            receiver.AssignCommandHandler(
                VmmCommands.LightPitch,
                message=> SetLightPitch(message.ToInt())
                );

            receiver.AssignCommandHandler(
                VmmCommands.ShadowEnable,
                message => EnableShadow(message.ToBoolean())
                );
            receiver.AssignCommandHandler(
                VmmCommands.ShadowIntensity,
                message => SetShadowIntensity(message.ParseAsPercentage())
            );
            receiver.AssignCommandHandler(
                VmmCommands.ShadowYaw,
                message => SetShadowYaw(message.ToInt())
                );
            receiver.AssignCommandHandler(
                VmmCommands.ShadowPitch,
                message => SetShadowPitch(message.ToInt())
                );
            receiver.AssignCommandHandler(
                VmmCommands.ShadowDepthOffset,
                message => SetShadowDepthOffset(message.ParseAsCentimeter())
               );

            receiver.AssignCommandHandler(
                VmmCommands.BloomIntensity,
                message => SetBloomIntensity(message.ParseAsPercentage())
                );
            receiver.AssignCommandHandler(
                VmmCommands.BloomThreshold,
                message => SetBloomThreshold(message.ParseAsPercentage())
                );
            receiver.AssignCommandHandler(
                VmmCommands.BloomColor,
                message =>
                {
                    float[] bloomRgb = message.ToColorFloats();
                    SetBloomColor(bloomRgb[0], bloomRgb[1], bloomRgb[2]);
                });

            receiver.AssignCommandHandler(
                VmmCommands.WindowFrameVisibility,
                message =>
                {
                    _windowFrameVisible = message.ToBoolean();
                    SetOutlineEffectActive(_windowFrameVisible, _enableOutlineEffect);
                });
            receiver.AssignCommandHandler(
                VmmCommands.OutlineEffectEnable,
                message =>
                {
                    _enableOutlineEffect = message.ToBoolean();
                    SetOutlineEffectActive(_windowFrameVisible, _enableOutlineEffect);
                });
            receiver.AssignCommandHandler(
                VmmCommands.OutlineEffectThickness,
                message => SetOutlineEffectThickness(message.ToInt())
            );
            receiver.AssignCommandHandler(
                VmmCommands.OutlineEffectColor,
                message =>
                {
                    var rgb = message.ToColorFloats();
                    var color = new Color(rgb[0], rgb[1], rgb[2]);
                    SetOutlineEffectEdgeColor(color);
                });
            receiver.AssignCommandHandler(
                VmmCommands.OutlineEffectHighQualityMode,
                message => SetOutlineEffectHighQualityMode(message.ToBoolean())
            );

            receiver.AssignCommandHandler(
                VmmCommands.EnableImageBasedHandTracking,
                message =>
                {
                    _handTrackingEnabled = message.ToBoolean();
                    UpdateRetroEffectStatus();
                });

            receiver.AssignCommandHandler(
                VmmCommands.ShowEffectDuringHandTracking,
                message =>
                {
                    _showEffectDuringTracking = message.ToBoolean();
                    UpdateRetroEffectStatus();
                });
            
            receiver.AssignCommandHandler(
                VmmCommands.EnableVMCPSend,
                message =>
                {
                    _vmcpSendEnabled = message.ToBoolean();
                    UpdateRetroEffectStatus();
                });
        }
        
        private void Start()
        {
            _bloom = postProcess.profile.GetSetting<Bloom>();
            _vmmAlphaEdge = postProcess.profile.GetSetting<VmmAlphaEdge>();
            _vmmMonochrome = postProcess.profile.GetSetting<VmmMonochrome>();
            _vmmVhs = postProcess.profile.GetSetting<VmmVhs>();
        }
        
        private void Update()
        {
            //GUIで色をいじってなくても補正値が効きがちなので、随時反映する
            SetMainLightColor();
        }

        private void SetLightColor(float r, float g, float b)
        {
            _color = new Color(r, g, b);
        }

        private void SetMainLightColor()
        {
            var factor = desktopLightEstimator.RgbFactor;
            var color = new Color(
                _color.r * factor.x,
                _color.g * factor.y,
                _color.b * factor.z
            );

            mainLight.color = color;

            //ライトの色がそのまま環境光にのる、ただしEquator以下では弱めに。
            RenderSettings.ambientSkyColor = color;
            Color.RGBToHSV(color, out var h, out var s, out var v);
            RenderSettings.ambientEquatorColor = Color.HSVToRGB(h, s, v * 0.4f);
            RenderSettings.ambientGroundColor = Color.HSVToRGB(h, s, v * 0.06f);
        }

        private void SetLightIntensity(float intensity)
            => mainLight.intensity = intensity * LightIntensityConstFactor;

        private void SetLightYaw(int yawDeg)
        {
            mainLightLocalEulerAngle = new Vector3(
                mainLightLocalEulerAngle.x,
                yawDeg,
                mainLightLocalEulerAngle.z
                );
            mainLight.transform.localEulerAngles = mainLightLocalEulerAngle;
        }

        private void SetLightPitch(int pitchDeg)
        {
            mainLightLocalEulerAngle = new Vector3(
                pitchDeg,
                mainLightLocalEulerAngle.y,
                mainLightLocalEulerAngle.z
                );
            mainLight.transform.localEulerAngles = mainLightLocalEulerAngle;
        }

        private void EnableShadow(bool enable)
        {
            shadowLight.enabled = enable;
            shadowBoardMotion.EnableShadowRenderer = enable;
        }

        private void SetShadowIntensity(float shadowStrength)
        {
            shadowLight.shadowStrength = shadowStrength;
        }

        private void SetShadowYaw(int yawDeg)
        {
            shadowLightLocalEulerAngle = new Vector3(
                shadowLightLocalEulerAngle.x,
                yawDeg,
                shadowLightLocalEulerAngle.z
                );
            shadowLight.transform.localEulerAngles = shadowLightLocalEulerAngle;
        }

        private void SetShadowPitch(int pitchDeg)
        {
            shadowLightLocalEulerAngle = new Vector3(
                pitchDeg,
                shadowLightLocalEulerAngle.y,
                shadowLightLocalEulerAngle.z
                );
            shadowLight.transform.localEulerAngles = shadowLightLocalEulerAngle;
        }

        private void SetShadowDepthOffset(float depthOffset) 
            => shadowBoardMotion.ShadowBoardWaistDepthOffset = depthOffset;
        
        private void SetBloomColor(float r, float g, float b)
            => _bloom.color.value = new Color(r, g, b);

        private void SetBloomIntensity(float intensity)
            => _bloom.intensity.value = intensity;

        private void SetBloomThreshold(float threshold)
            => _bloom.threshold.value = threshold;

        private void SetOutlineEffectActive(bool windowFrameVisible, bool enableOutlineEffect) 
            => _vmmAlphaEdge.active = !windowFrameVisible && enableOutlineEffect;

        //NOTE: GUIからは整数指定するが設定上はfloat
        private void SetOutlineEffectThickness(int thickness)
            => _vmmAlphaEdge.thickness.Override(thickness);

        private void SetOutlineEffectEdgeColor(Color color)
            => _vmmAlphaEdge.edgeColor.Override(color);

        private void SetOutlineEffectHighQualityMode(bool useHighQualityMode)
            => _vmmAlphaEdge.highQualityMode.Override(useHighQualityMode);
        
        private void UpdateRetroEffectStatus()
        {
            var enableEffect = 
                (_handTrackingEnabled || _vmcpSendEnabled) &&
                (FeatureLocker.IsFeatureLocked || _showEffectDuringTracking);

            _vmmMonochrome.active = enableEffect;
            _vmmVhs.active = enableEffect;
        }
    }
}
