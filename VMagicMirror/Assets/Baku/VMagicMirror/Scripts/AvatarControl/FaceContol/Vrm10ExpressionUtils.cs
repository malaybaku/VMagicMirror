using System.Collections.Generic;
using System.Linq;
using UniVRM10;

namespace Baku.VMagicMirror
{
    public class VRM10ExpressionMap
    {
        public VRM10ExpressionMap(IReadOnlyDictionary<ExpressionKey, VRM10Expression> map)
        {
            _map = map;
        }

        private readonly IReadOnlyDictionary<ExpressionKey, VRM10Expression> _map;

        //NOTE: 必要ならPairs的なのを出してもいい
        public IEnumerable<ExpressionKey> Keys => _map.Keys;
        public IEnumerable<VRM10Expression> Values => _map.Values;

        public VRM10Expression this[ExpressionKey key] => _map[key];
        
        public bool TryGet(ExpressionKey key, out VRM10Expression result)
        {
            return _map.TryGetValue(key, out result);
        }
    }

    public static class Vrm10ObjectExpressionExtensions
    {
        public static VRM10ExpressionMap LoadExpressionMap(this VRM10ObjectExpression expression)
        {
            var map = expression.Clips
                .ToDictionary(
                    v => expression.CreateKey(v.Clip),
                    v => v.Clip
                );
            return new VRM10ExpressionMap(map);
        }
    }

    public static class Vrm10ExpressionExtensions
    {
        public static bool HasValidBinds(this VRM10Expression expression)
        {
            return 
                expression.MaterialColorBindings.Length > 0 ||
                expression.MorphTargetBindings.Length > 0 ||
                expression.MaterialUVBindings.Length > 0;
        }
    }
    
    public static class ExpressionKeyUtils
    {
        //キーが名前だけで与えられた場合、それがプリセットと同名ならプリセット、そうでなければカスタムクリップ扱いします。
        public static ExpressionKey CreateKeyByName(string name) => _presets.ContainsKey(name)
            ? ExpressionKey.CreateFromPreset(_presets[name])
            : ExpressionKey.CreateCustom(name);
        
        private static readonly Dictionary<string, ExpressionPreset> _presets = new Dictionary<string, ExpressionPreset>()
        {
            //キーって大文字でもいい or むしろ大文字のほうがいいのでは
            ["Neutral"] = ExpressionPreset.neutral,
            ["Happy"] = ExpressionPreset.happy,
            ["Angry"] = ExpressionPreset.angry,
            ["Sad"] = ExpressionPreset.sad,
            ["Relaxed"] = ExpressionPreset.relaxed,
            ["Surprised"] = ExpressionPreset.surprised,

            ["Aa"] = ExpressionPreset.aa,
            ["Ih"] = ExpressionPreset.ih,
            ["Ou"] = ExpressionPreset.ou,
            ["Ee"] = ExpressionPreset.ee,
            ["Oh"] = ExpressionPreset.oh,
            
            ["Blink"] = ExpressionPreset.blink,
            ["BlinkLeft"] = ExpressionPreset.blinkLeft,
            ["BlinkRight"] = ExpressionPreset.blinkRight,
        
            ["LookLeft"] = ExpressionPreset.lookLeft,
            ["LookRight"] = ExpressionPreset.lookRight,
            ["LookUp"] = ExpressionPreset.lookUp,
            ["LookDown"] = ExpressionPreset.lookDown,
        };
    }
}
