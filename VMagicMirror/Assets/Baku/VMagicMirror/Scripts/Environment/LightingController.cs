using System;
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

        private void SetBloomColor(float r, float g, float b)
            => _bloom.color.value = new Color(r, g, b);

        private void SetBloomIntensity(float intensity)
            => _bloom.intensity.value = intensity;

        private void SetBloomThreshold(float threshold)
            => _bloom.threshold.value = threshold;
    }
}
