using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Baku.VMagicMirror
{
    //Thanks To: https://qiita.com/3yen/items/c549ff26848dbb906635
    [Serializable]
    [PostProcess(typeof(VmmVhsRenderer), PostProcessEvent.AfterStack, "Vmm/VHS", allowInSceneView: false)]
    public sealed class VmmVhs : PostProcessEffectSettings
    {
        [SerializeField, Range(0, 1)] 
        public FloatParameter bleeding = new FloatParameter(){value = 0.8f};
        [SerializeField, Range(0, 1)] 
        public FloatParameter fringing = new FloatParameter(){value = 1.0f};
        [SerializeField, Range(0, 1)]
        public FloatParameter scanline = new FloatParameter(){value = 0.125f};
    }

    public sealed class VmmVhsRenderer : PostProcessEffectRenderer<VmmVhs>
    {
        private float _time;
        private static readonly int Src = Shader.PropertyToID("_src");
        private static readonly int Width = Shader.PropertyToID("_Width");
        private static readonly int Height = Shader.PropertyToID("_Height");
        private static readonly int BleedTaps = Shader.PropertyToID("_BleedTaps");
        private static readonly int BleedDelta = Shader.PropertyToID("_BleedDelta");
        private static readonly int FringeDelta = Shader.PropertyToID("_FringeDelta");
        private static readonly int Scanline = Shader.PropertyToID("_Scanline");
        private static readonly int NoiseY = Shader.PropertyToID("_NoiseY");

        public override void Render(PostProcessRenderContext context)
        {
            _time += Time.deltaTime;
            if (_time > 1.0f)
            {
                if (_time > UnityEngine.Random.Range(3f, 8f))
                {
                    _time = 0f;
                }
            }

            var sheet = context.propertySheets.Get(Shader.Find("Hidden/Vmm/VHS"));
            
            var bleedWidth = 0.04f * settings.bleeding.value;
            var bleedStep = 2.5f / context.width;
            var bleedTaps = Mathf.CeilToInt(bleedWidth / bleedStep);
            var bleedDelta = bleedWidth / bleedTaps;
            var fringeWidth = 0.0025f * settings.fringing.value;
            
            sheet.properties.SetFloat(Src, 0.5f);
            sheet.properties.SetInt(Width, context.width);
            sheet.properties.SetInt(Height, context.height);
            sheet.properties.SetInt(BleedTaps, bleedTaps);
            sheet.properties.SetFloat(BleedDelta, bleedDelta);
            sheet.properties.SetFloat(FringeDelta, fringeWidth);
            sheet.properties.SetFloat(Scanline, settings.scanline.value);
            sheet.properties.SetFloat(NoiseY, 1.0f - _time);
            
            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        }
    }
}
