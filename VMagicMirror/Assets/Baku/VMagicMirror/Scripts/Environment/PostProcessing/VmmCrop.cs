using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Baku.VMagicMirror
{
    [Serializable]
    [PostProcess(typeof(VmmCropRenderer), PostProcessEvent.AfterStack, "Vmm/Crop", allowInSceneView: false)]
    public sealed class VmmCrop : PostProcessEffectSettings
    {
        public FloatParameter margin = new() { value = 0.02f };
        public FloatParameter squareRate = new() { value = 0.0f };
        public FloatParameter borderWidth = new() { value = 0.01f };
        public ColorParameter borderColor = new() { value = Color.white };
    }

    public sealed class VmmCropRenderer : PostProcessEffectRenderer<VmmCrop>
    {
        private static readonly int Margin = Shader.PropertyToID("_Margin");
        private static readonly int SquareRate = Shader.PropertyToID("_SquareRate");
        private static readonly int BorderWidth = Shader.PropertyToID("_BorderWidth");
        private static readonly int BorderColor = Shader.PropertyToID("_BorderColor");

        public override void Render(PostProcessRenderContext context)
        {
            var sheet = context.propertySheets.Get(Shader.Find("Hidden/Vmm/Crop"));
            sheet.properties.SetFloat(Margin, settings.margin);
            sheet.properties.SetFloat(SquareRate, settings.squareRate);
            sheet.properties.SetFloat(BorderWidth, settings.borderWidth);
            sheet.properties.SetColor(BorderColor, settings.borderColor);
            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        }
    }
}
