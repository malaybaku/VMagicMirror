using System.Linq;
using UniGLTF.Extensions.VRMC_vrm;
using UniVRM10;

namespace Baku.VMagicMirror
{
    public class BlendShapeExclusivenessChecker
    {
        public BlendShapeExclusivenessChecker(IVRMLoadable vrmLoadable)
        {
            vrmLoadable.VrmLoaded += CheckBlendShape;
        }

        private void CheckBlendShape(VrmLoadedInfo info)
        {
            //表情のセットアップで排他系の設定がちゃんとあると検出するクラス
            var expr = info.instance.Vrm.Expression;
            
            var clipAboutOverride = expr.Clips.FirstOrDefault(c =>
                c.Clip.OverrideBlink != ExpressionOverrideType.none ||
                c.Clip.OverrideMouth != ExpressionOverrideType.none ||
                c.Clip.OverrideLookAt != ExpressionOverrideType.none
                );

            if (clipAboutOverride.Preset != ExpressionPreset.custom ||
                !string.IsNullOrEmpty(clipAboutOverride.Clip.name)
               )
            {
                LogOutput.Instance.Write($"Avatar has some exclusive blendshape setup, {clipAboutOverride.Preset} / {clipAboutOverride.Clip.name}");
            }
        }
    }
}
