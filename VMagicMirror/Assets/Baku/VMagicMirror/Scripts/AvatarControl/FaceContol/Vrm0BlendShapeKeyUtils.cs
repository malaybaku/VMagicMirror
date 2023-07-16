using System.Collections.Generic;
using System.Linq;
using UniVRM10;
using VRM;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// VRM0のキーをVRM1.0のキーに変換するやつ
    /// ToStringされたVRM 0.xのキーをVRM 1.0のキーにマップするために用いる
    /// </summary>
    public static class Vrm0BlendShapeKeyUtils
    {
        private static readonly BlendShapeKey[] _srcKeys = {
            BlendShapeKey.CreateFromPreset(BlendShapePreset.Neutral),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.A),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.I),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.U),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.E),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.O),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.Blink),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.Joy),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.Angry),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.Sorrow),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.Fun),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.LookUp),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.LookDown),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.LookLeft),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.LookRight),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.Blink_L),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.Blink_R),
            BlendShapeKey.CreateUnknown("Surprised"),
        };

        private static readonly ExpressionKey[] _destKeys = {
            ExpressionKey.Neutral,
            ExpressionKey.Aa,
            ExpressionKey.Ih,
            ExpressionKey.Ou,
            ExpressionKey.Ee,
            ExpressionKey.Oh,
            ExpressionKey.Blink,
            ExpressionKey.Happy,
            ExpressionKey.Angry,
            ExpressionKey.Sad,
            ExpressionKey.Relaxed,
            ExpressionKey.LookUp,
            ExpressionKey.LookDown,
            ExpressionKey.LookLeft,
            ExpressionKey.LookRight,
            ExpressionKey.BlinkLeft,
            ExpressionKey.BlinkRight,
            ExpressionKey.Surprised,
        };
        
        public static Dictionary<BlendShapeKey, ExpressionKey> CreateBlendShapeConvertMap()
        {
            var result = new Dictionary<BlendShapeKey, ExpressionKey>();
            for (var i = 0; i < _srcKeys.Length; i++)
            {
                result[_srcKeys[i]] = _destKeys[i];
            }
            return result;
        }

        public static Dictionary<string, ExpressionKey> CreateVrm0StringKeyToVrm1KeyMap()
        {
            return CreateBlendShapeConvertMap().ToDictionary(
                pair => pair.Key.ToString(),
                pair => pair.Value
            );
        }
    }
}
