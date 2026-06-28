using R3;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Zenject;

namespace Baku.VMagicMirror
{
    public class LightingController : MonoBehaviour
    {
        //NOTE: 本質的な意味はない値だが、VRM 0.xから1.0に引き上げたら同等のライティングでも強すぎに見えるようになったため、
        //この係数をかけて光量を抑える。(MToonの何かが変わったものと思われるけど把握できてない)
        private const float LightIntensityConstFactor = 0.85f;
        private const BindingFlags InstanceBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const string ScreenSpaceAmbientOcclusionFeatureTypeName = "ScreenSpaceAmbientOcclusion";
        // NOTE: 見た感じ効きが弱いのでちょっと強くしたものを当てる
        private const float AmbientOcclusionIntensityConstFactor = 2f;

        [SerializeField] private Light mainLight = null;
        [SerializeField] private Vector3 mainLightLocalEulerAngle = default;
        
        [SerializeField] private VmmAvatarDropShadowController avatarDropShadowController = null;
        [SerializeField] private DesktopLightEstimator desktopLightEstimator = null;

        private Color _color = Color.white;
        private Volume _globalVolume;
        private Bloom _bloom;
        private Camera _mainCamera;
        private ScriptableRendererFeature _screenSpaceAmbientOcclusionFeature;
        private object _screenSpaceAmbientOcclusionSettings;
        private FieldInfo _screenSpaceAmbientOcclusionIntensityField;
        private FieldInfo _screenSpaceAmbientOcclusionAfterOpaqueField;
        private bool _ambientOcclusionEnabled = false;
        private float _ambientOcclusionIntensity = 0.15f * AmbientOcclusionIntensityConstFactor;
        private bool _handTrackingEnabled = false;
        //NOTE: この値自体はビルドバージョンによらずfalseがデフォルトで良いことに注意。
        // 制限版でGUI側にtrue相当の値が表示されるが、これはGUI側が別途決め打ちしてくれてる。
        // ハンドトラッキング以外の条件 (VMCP, サブキャラの一部機能)についても同様
        private bool _showEffectDuringTracking = false;

        private bool _vmcpSendEnabled = false;
        private bool _showEffectDuringVmcpSendEnabled = false;
        private bool _buddyInteractionApiEnabled = false;

        private readonly ReactiveProperty<bool> _bloomEnabled = new(true);
        private readonly ReactiveProperty<float> _bloomIntensity = new(0.5f);
        
        private void Awake()
        {
            if (mainLight != null)
            {
                RenderSettings.sun = mainLight;
            }

            VmmVolumeComponentAccessor.SetVmmRetroActive(false);
            VmmVolumeComponentAccessor.SetVmmColoredSsaoActive(false);

            EnsureVolumeOverrides();
        }

        [Inject]
        public void Initialize(
            Camera mainCamera,
            IMessageReceiver receiver,
            FixedShadowController fixedShadowController,
            LateUpdateSourceAfterFinalIK lateUpdateSource)
        {
            _mainCamera = mainCamera;
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

            var shadowEnabled = new ReactiveProperty<bool>(true);
            receiver.BindBoolProperty(VmmCommands.ShadowEnable, shadowEnabled);
            
            // - 背面シャドウと固定シャドウは原則として排他になる
            // - ただしセルフ落影を有効にしている場合、背面シャドウと使っていてもlightのshadowを有効にする
            shadowEnabled.CombineLatest(
                fixedShadowController.FixedShadowEnabled,
                (dropShadowEnabled, fixedShadowEnabled) => (
                    dropShadowEnabled: dropShadowEnabled && !fixedShadowEnabled,
                    lightingShadowEnabled: fixedShadowEnabled
                ))
                .DistinctUntilChanged()
                .Subscribe(v => SetEnableShadow(v.dropShadowEnabled, v.lightingShadowEnabled))
                .AddTo(this);

            receiver.AssignCommandHandler(
                VmmCommands.ShadowColor,
                message =>
                {
                    var shadowRgb = message.ToColorFloats();
                    SetShadowColor(shadowRgb[0], shadowRgb[1], shadowRgb[2]);
                });
            receiver.AssignCommandHandler(
                VmmCommands.ShadowBlur,
                message => SetShadowBlur(message.ToInt())
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

            receiver.BindBoolProperty(VmmCommands.BloomEnable, _bloomEnabled);
            receiver.BindPercentageProperty(VmmCommands.BloomIntensity, _bloomIntensity);
            _bloomEnabled.CombineLatest(
                _bloomIntensity,
                (enableBloom, intensity) => enableBloom ? intensity : 0f
                )
                .DistinctUntilChanged()
                .Subscribe(SetBloomIntensity)
                .AddTo(this);

            receiver.AssignCommandHandler(
                VmmCommands.BloomThreshold,
                message => SetBloomThreshold(message.ParseAsPercentage())
                );
            receiver.AssignCommandHandler(
                VmmCommands.BloomColor,
                message =>
                {
                    var bloomRgb = message.ToColorFloats();
                    SetBloomColor(bloomRgb[0], bloomRgb[1], bloomRgb[2]);
                });

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
                VmmCommands.AmbientOcclusionEnable,
                message =>
                {
                    EnsureVolumeOverrides();
                    _ambientOcclusionEnabled = message.ToBoolean();
                    UpdateAmbientOcclusionActive();
                });

            receiver.AssignCommandHandler(
                VmmCommands.AmbientOcclusionIntensity,
                message =>
                {
                    EnsureVolumeOverrides();
                    _ambientOcclusionIntensity =
                        Mathf.Max(0f, message.ParseAsPercentage()) * AmbientOcclusionIntensityConstFactor;
                    if (_screenSpaceAmbientOcclusionSettings != null &&
                        _screenSpaceAmbientOcclusionIntensityField != null)
                    {
                        _screenSpaceAmbientOcclusionIntensityField.SetValue(
                            _screenSpaceAmbientOcclusionSettings,
                            _ambientOcclusionIntensity);
                    }
                    UpdateAmbientOcclusionActive();
                });

            receiver.AssignCommandHandler(
                VmmCommands.AmbientOcclusionColor,
                message =>
                {
                    var rgb = message.ToColorFloats();
                    SetAmbientOcclusionColor(rgb[0], rgb[1], rgb[2]);
                });

            receiver.AssignCommandHandler(
                VmmCommands.EnableVMCPSend,
                message =>
                {
                    _vmcpSendEnabled = message.ToBoolean();
                    UpdateRetroEffectStatus();
                });
            receiver.AssignCommandHandler(
                VmmCommands.ShowEffectDuringVMCPSendEnabled,
                message =>
                {
                    _showEffectDuringVmcpSendEnabled = message.ToBoolean();
                    UpdateRetroEffectStatus();
                });
            
            receiver.AssignCommandHandler(
                VmmCommands.BuddySetInteractionApiEnabled,
                message =>
                {
                    _buddyInteractionApiEnabled = message.ToBoolean();
                    UpdateRetroEffectStatus();
                });
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

            //ライトの色を弱めにして環境光にも入れる
            Color.RGBToHSV(color, out var h, out var s, out var v);
            RenderSettings.ambientSkyColor = Color.HSVToRGB(h, s, v * 0.5f);
            RenderSettings.ambientEquatorColor = Color.HSVToRGB(h, s, v * 0.5f);
            RenderSettings.ambientGroundColor = Color.HSVToRGB(h, s, v * 0.1f);
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

        private void SetEnableShadow(bool dropShadowEnabled, bool lightingShadowEnabled)
        {
            avatarDropShadowController.SetEnabled(dropShadowEnabled);
            // NOTE: セルフ落影のオンオフを動的に変えられるようにする場合、セルフ影がオンの場合にもSoftに倒す必要がある
            mainLight.shadows = lightingShadowEnabled ? LightShadows.Soft : LightShadows.None;
        }

        private void SetShadowColor(float r, float g, float b)
            => avatarDropShadowController.SetShadowColor(r, g, b);

        private void SetShadowBlur(int blur)
            => avatarDropShadowController.SetShadowBlur(blur);

        private void SetShadowIntensity(float shadowStrength)
            => avatarDropShadowController.SetShadowIntensity(shadowStrength);

        private void SetShadowYaw(int yawDeg)
            => avatarDropShadowController.SetShadowYaw(yawDeg);

        private void SetShadowPitch(int pitchDeg)
            => avatarDropShadowController.SetShadowPitch(pitchDeg);

        private void SetShadowDepthOffset(float depthOffset)
            => avatarDropShadowController.SetDepthOffset(depthOffset);
        
        private void SetBloomColor(float r, float g, float b)
        {
            EnsureVolumeOverrides();
            if (_bloom != null)
            {
                _bloom.tint.Override(new Color(r, g, b));
            }
        }

        private void SetBloomIntensity(float intensity)
        {
            EnsureVolumeOverrides();
            if (_bloom != null)
            {
                _bloom.intensity.Override(intensity);
            }
        }

        private void SetBloomThreshold(float threshold)
        {
            EnsureVolumeOverrides();
            if (_bloom != null)
            {
                _bloom.threshold.Override(threshold);
            }
        }

        private void SetAmbientOcclusionColor(float r, float g, float b)
        {
            VmmVolumeComponentAccessor.UpdateColoredSsao(
                component => component.color.Override(new Color(r, g, b)));
        }

        private void UpdateAmbientOcclusionActive()
        {
            var active = _ambientOcclusionEnabled && _ambientOcclusionIntensity > 0f;
            if (_screenSpaceAmbientOcclusionFeature != null)
            {
                _screenSpaceAmbientOcclusionFeature.SetActive(active);
            }
            VmmVolumeComponentAccessor.SetVmmColoredSsaoActive(active);
        }

        private void EnsureVolumeOverrides()
        {
            if (_globalVolume == null)
            {
                var volumes = Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var volume in volumes)
                {
                    if (!volume.isGlobal)
                    {
                        continue;
                    }

                    var sourceProfile = volume.profile != null ? volume.profile : volume.sharedProfile;
                    if (sourceProfile == null)
                    {
                        continue;
                    }

                    if (!sourceProfile.TryGet(out Bloom bloom) ||
                        sourceProfile == null)
                    {
                        continue;
                    }

                    if (volume.profile == null && volume.sharedProfile != null)
                    {
                        volume.profile = Instantiate(volume.sharedProfile);
                        sourceProfile = volume.profile;
                        sourceProfile.TryGet(out bloom);
                    }

                    _globalVolume = volume;
                    _bloom = bloom;
                    break;
                }
            }

            if (_screenSpaceAmbientOcclusionFeature == null && _mainCamera != null)
            {
                var additionalCameraData = _mainCamera.GetUniversalAdditionalCameraData();
                if (UniversalRenderPipeline.asset == null)
                {
                    return;
                }

                var rendererIndex = -1;
                if (additionalCameraData != null)
                {
                    var rendererIndexField = additionalCameraData.GetType().GetField("m_RendererIndex", InstanceBindingFlags);
                    if (rendererIndexField?.GetValue(additionalCameraData) is int indexValue)
                    {
                        rendererIndex = indexValue;
                    }
                }

                var rendererDataList = UniversalRenderPipeline.asset.rendererDataList;
                if (rendererDataList.IsEmpty)
                {
                    return;
                }
                
                if (rendererIndex < 0 || rendererIndex >= rendererDataList.Length || rendererDataList[rendererIndex] == null)
                {
                    var defaultRendererIndexField = UniversalRenderPipeline.asset.GetType()
                        .GetField("m_DefaultRendererIndex", InstanceBindingFlags);
                    if (defaultRendererIndexField?.GetValue(UniversalRenderPipeline.asset) is int defaultRendererIndex)
                    {
                        rendererIndex = defaultRendererIndex;
                    }
                    else
                    {
                        rendererIndex = 0;
                    }
                }

                if (rendererIndex < 0 || rendererIndex >= rendererDataList.Length)
                {
                    return;
                }

                var rendererData = rendererDataList[rendererIndex];
                if (rendererData == null)
                {
                    return;
                }

                foreach (var feature in rendererData.rendererFeatures)
                {
                    if (feature == null || feature.GetType().Name != ScreenSpaceAmbientOcclusionFeatureTypeName)
                    {
                        continue;
                    }

                    _screenSpaceAmbientOcclusionFeature = feature;
                    var settingsField = feature.GetType().GetField("m_Settings", InstanceBindingFlags);
                    _screenSpaceAmbientOcclusionSettings = settingsField?.GetValue(feature);
                    if (_screenSpaceAmbientOcclusionSettings != null)
                    {
                        var settingsType = _screenSpaceAmbientOcclusionSettings.GetType();
                        _screenSpaceAmbientOcclusionIntensityField = settingsType.GetField("Intensity", InstanceBindingFlags);
                        if (_screenSpaceAmbientOcclusionIntensityField?.GetValue(_screenSpaceAmbientOcclusionSettings) is float intensity)
                        {
                            _ambientOcclusionIntensity = Mathf.Max(0f, intensity);
                        }
                        _screenSpaceAmbientOcclusionAfterOpaqueField = settingsType.GetField("AfterOpaque", InstanceBindingFlags);
                        _screenSpaceAmbientOcclusionAfterOpaqueField?.SetValue(
                            _screenSpaceAmbientOcclusionSettings,
                            false);
                    }
                    break;
                }
            }
        }


        private void UpdateRetroEffectStatus()
        {
            // サブキャラは他2つと違って「わざとエフェクトを表示する」のオプションはない
            // NOTE: 常時エフェクトを利かす独立なオプションを「エフェクト」タブに増設したほうが建て付けが良いかも…
            VmmVolumeComponentAccessor.SetVmmRetroActive(
                (_handTrackingEnabled && (FeatureLocker.IsFeatureLocked || _showEffectDuringTracking)) ||
                (_vmcpSendEnabled && (FeatureLocker.IsFeatureLocked || _showEffectDuringVmcpSendEnabled)) ||
                (_buddyInteractionApiEnabled && FeatureLocker.IsFeatureLocked));
        }
    }
}
