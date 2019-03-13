using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Baku.VMagicMirror
{
    public class LightingController : MonoBehaviour
    {
        [SerializeField]
        private Light mainLight;

        [SerializeField]
        private PostProcessVolume postProcess;

        private Bloom _bloom;

        private void Start()
        {
            _bloom = postProcess.profile.GetSetting<Bloom>();
        }

        public void SetLightColor(float r, float g, float b)
            => mainLight.color = new Color(r, g, b);

        public void SetLightIntensity(float intensity)
            => mainLight.intensity = intensity;

        public void SetBloomColor(float r, float g, float b)
            => _bloom.color.value = new Color(r, g, b);

        public void SetBloomIntensity(float intensity)
            => _bloom.intensity.value = intensity;

        public void SetBloomThreshold(float threshold)
            => _bloom.threshold.value = threshold;
    }
}
