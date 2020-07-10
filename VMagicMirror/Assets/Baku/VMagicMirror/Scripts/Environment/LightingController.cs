using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Zenject;

namespace Baku.VMagicMirror
{
    public class LightingController : MonoBehaviour
    {
        [SerializeField] private Light mainLight = null;
        [SerializeField] private Vector3 mainLightLocalEulerAngle = default;
        
        [SerializeField] private Light shadowLight = null;
        [SerializeField] private Vector3 shadowLightLocalEulerAngle = default;
        [SerializeField] private ShadowBoardMotion shadowBoardMotion = null;

        [SerializeField] private PostProcessVolume postProcess = null;

        private Bloom _bloom;

        [Inject]
        public void Initialize(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                MessageCommandNames.LightIntensity,
                message => SetLightIntensity(message.ParseAsPercentage())
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.LightColor,
                message =>
                {
                    var lightRgb = message.ToColorFloats();
                    SetLightColor(lightRgb[0], lightRgb[1], lightRgb[2]);
                });
            receiver.AssignCommandHandler(
                MessageCommandNames.LightYaw,
                message => SetLightYaw(message.ToInt())
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.LightPitch,
                message=> SetLightPitch(message.ToInt())
                );

            receiver.AssignCommandHandler(
                MessageCommandNames.ShadowEnable,
                message => EnableShadow(message.ToBoolean())
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.ShadowIntensity,
                message => SetShadowIntensity(message.ParseAsPercentage())
            );
            receiver.AssignCommandHandler(
                MessageCommandNames.ShadowYaw,
                message => SetShadowYaw(message.ToInt())
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.ShadowPitch,
                message => SetShadowPitch(message.ToInt())
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.ShadowDepthOffset,
                message => SetShadowDepthOffset(message.ParseAsCentimeter())
               );

            receiver.AssignCommandHandler(
                MessageCommandNames.BloomIntensity,
                message => SetBloomIntensity(message.ParseAsPercentage())
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.BloomThreshold,
                message => SetBloomThreshold(message.ParseAsPercentage())
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.BloomColor,
                message =>
                {
                    float[] bloomRgb = message.ToColorFloats();
                    SetBloomColor(bloomRgb[0], bloomRgb[1], bloomRgb[2]);
                });
        }
        
        private void Start()
        {
            _bloom = postProcess.profile.GetSetting<Bloom>();
        }

        private void SetLightColor(float r, float g, float b)
            => mainLight.color = new Color(r, g, b);

        private void SetLightIntensity(float intensity)
            => mainLight.intensity = intensity;

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
    }
}
