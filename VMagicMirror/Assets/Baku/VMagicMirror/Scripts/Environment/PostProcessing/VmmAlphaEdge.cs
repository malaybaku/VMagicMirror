using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Baku.VMagicMirror
{
    
    [Serializable]
    [PostProcess(typeof(VmmAlphaEdgeRenderer), PostProcessEvent.AfterStack, "Vmm/AlphaEdge", allowInSceneView: false)]
    public sealed class VmmAlphaEdge : PostProcessEffectSettings
    {
        public FloatParameter thickness = new() { value = 20f };
        public FloatParameter threshold = new() { value = 1f };
        public ColorParameter edgeColor = new() { value = Color.white };
        public FloatParameter outlineOverwriteAlpha = new() { value = 0.02f };
        public BoolParameter highQualityMode = new() { value = false };
    }

    public sealed class VmmAlphaEdgeRenderer : PostProcessEffectRenderer<VmmAlphaEdge>
    {
        private static readonly int Thickness = Shader.PropertyToID("_Thickness");
        private static readonly int Threshold = Shader.PropertyToID("_Threshold");
        private static readonly int EdgeColor = Shader.PropertyToID("_EdgeColor");
        private static readonly int OutlineOverwriteAlpha = Shader.PropertyToID("_OutlineOverwriteAlpha");
        private static readonly int HighQualityMode = Shader.PropertyToID("_HighQualityMode");

        public override void Render(PostProcessRenderContext context)
        {
            var sheet = context.propertySheets.Get(Shader.Find("Hidden/Vmm/AlphaEdge"));
            sheet.properties.SetFloat(Thickness, settings.thickness);
            sheet.properties.SetFloat(Threshold, settings.threshold);
            sheet.properties.SetColor(EdgeColor, settings.edgeColor);
            sheet.properties.SetFloat(OutlineOverwriteAlpha, settings.outlineOverwriteAlpha);
            sheet.properties.SetFloat(HighQualityMode, settings.highQualityMode ? 1f : 0f);

            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        }
    }
}
