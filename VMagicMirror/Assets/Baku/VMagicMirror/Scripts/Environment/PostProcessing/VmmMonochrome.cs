using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Baku.VMagicMirror
{
    [Serializable]
    [PostProcess(typeof(VmmMonochromeRenderer), PostProcessEvent.AfterStack, "Vmm/Monochrome", allowInSceneView: false)]
    public sealed class VmmMonochrome : PostProcessEffectSettings
    {
        public BoolParameter useBlock = new BoolParameter() {value = false};
        [Range(2, 30)]
        public IntParameter blockSize = new IntParameter() {value = 4};

        public BoolParameter useMonochrome = new BoolParameter() {value = true};
        public ColorParameter black = new ColorParameter() {value = Color.black};
        public ColorParameter white = new ColorParameter() {value = Color.white};

        public BoolParameter useLevel = new BoolParameter() {value = false};
        [Range(2, 20)] 
        public IntParameter levelDivision = new IntParameter() {value = 8};
        [Range(1, 20)]
        public IntParameter whitenThreshold = new IntParameter() {value = 4};
        
        public BoolParameter useColorReduction = new BoolParameter() {value = false};
        [Range(4, 64)]
        public IntParameter colorDivision = new IntParameter() {value = 16};
        
    }

    public sealed class VmmMonochromeRenderer : PostProcessEffectRenderer<VmmMonochrome>
    {
        private static readonly int Width = Shader.PropertyToID("_Width");
        private static readonly int Height = Shader.PropertyToID("_Height");

        private static readonly int BlockSize = Shader.PropertyToID("_BlockSize");
        
        private static readonly int UseMonochrome = Shader.PropertyToID("_UseMonochrome");
        private static readonly int BlackColor = Shader.PropertyToID("_BlackColor");
        private static readonly int WhiteColor = Shader.PropertyToID("_WhiteColor");
        private static readonly int UseLevel = Shader.PropertyToID("_UseLevel");
        private static readonly int Division = Shader.PropertyToID("_Division");
        private static readonly int WhiteThreshold = Shader.PropertyToID("_WhiteThreshold");
        
        private static readonly int UseColorReduction = Shader.PropertyToID("_UseColorReduction");
        private static readonly int ColorDivision = Shader.PropertyToID("_ColorDivision");

        public override void Render(PostProcessRenderContext context)
        {
            var sheet = context.propertySheets.Get(Shader.Find("Hidden/Vmm/Monochrome"));
            
            sheet.properties.SetFloat(Width, context.width);
            sheet.properties.SetFloat(Height, context.height);
            sheet.properties.SetFloat(BlockSize, settings.useBlock.value ? settings.blockSize.value : 0f);

            sheet.properties.SetFloat(UseMonochrome, settings.useMonochrome ? 1f : 0f);
            sheet.properties.SetColor(BlackColor, settings.black);
            sheet.properties.SetColor(WhiteColor, settings.white);

            sheet.properties.SetFloat(UseLevel, settings.useLevel ? 1.0f : 0.0f);
            sheet.properties.SetFloat(Division, settings.levelDivision);
            sheet.properties.SetFloat(WhiteThreshold, settings.whitenThreshold);
            
            sheet.properties.SetFloat(UseColorReduction, settings.useColorReduction ? 1f : 0f);
            sheet.properties.SetFloat(ColorDivision, settings.colorDivision);
            
            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        }
    }
}
