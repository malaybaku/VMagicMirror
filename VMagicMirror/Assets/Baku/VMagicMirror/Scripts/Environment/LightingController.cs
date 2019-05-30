using UniRx;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Baku.VMagicMirror
{
    public class LightingController : MonoBehaviour
    {
        [SerializeField]
        private Light mainLight = null;

        [SerializeField]
        private Vector3 mainLightLocalEulerAngle;

        [SerializeField]
        private Light shadowLight = null;

        [SerializeField]
        private Vector3 shadowLightLocalEulerAngle;

        [SerializeField]
        private ShadowBoardMotion shadowBoardMotion = null;

        [SerializeField]
        private PostProcessVolume postProcess = null;

        [SerializeField]
        private ReceivedMessageHandler handler = null;


        private Bloom _bloom;

        private void Start()
        {

            _bloom = postProcess.profile.GetSetting<Bloom>();

            handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.LightIntensity:
                        SetLightIntensity(message.ParseAsPercentage());
                        break;
                    case MessageCommandNames.LightColor:
                        float[] lightRgb = message.ToColorFloats();
                        SetLightColor(lightRgb[0], lightRgb[1], lightRgb[2]);
                        break;
                    case MessageCommandNames.LightYaw:
                        SetLightYaw(message.ToInt());
                        break;
                    case MessageCommandNames.LightPitch:
                        SetLightPitch(message.ToInt());
                        break;
                    case MessageCommandNames.ShadowEnable:
                        EnableShadow(message.ToBoolean());
                        break;
                    case MessageCommandNames.ShadowIntensity:
                        SetShadowIntensity(message.ParseAsPercentage());
                        break;
                    case MessageCommandNames.ShadowYaw:
                        SetShadowYaw(message.ToInt());
                        break;
                    case MessageCommandNames.ShadowPitch:
                        SetShadowPitch(message.ToInt());
                        break;
                    case MessageCommandNames.ShadowDepthOffset:
                        SetShadowDepthOffset(message.ParseAsCentimeter());
                        break;
                    case MessageCommandNames.BloomIntensity:
                        SetBloomIntensity(message.ParseAsPercentage());
                        break;
                    case MessageCommandNames.BloomThreshold:
                        SetBloomThreshold(message.ParseAsPercentage());
                        break;
                    case MessageCommandNames.BloomColor:
                        float[] bloomRgb = message.ToColorFloats();
                        SetBloomColor(bloomRgb[0], bloomRgb[1], bloomRgb[2]);
                        break;
                    default:
                        break;
                }
            });
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
            => shadowLight.enabled = enable;

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
