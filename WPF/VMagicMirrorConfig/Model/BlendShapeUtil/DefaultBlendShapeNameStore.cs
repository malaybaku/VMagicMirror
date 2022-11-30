using System.Collections.Generic;
using System.Linq;

namespace Baku.VMagicMirrorConfig
{
    internal static class DefaultBlendShapeNameStore
    {
        //内部的なデータや設定ファイル上ではVRM 0.xの名称を使ったままであり、
        //かつUI表示 + Unityに渡すときだけVRM 1.0用の名前に読み替えるようにする。これは設定ファイルの互換性のため。
        // Joy     -> Happy
        // Sorrow  -> Sad
        // Fun     -> Relaxed
        // Blink_L -> BlinkLeft
        // Blink_R -> BlinkRight
        private static readonly Dictionary<string, string> _oldKeyToNewKey = new Dictionary<string, string>()
        {
            ["Joy"] = "Happy",
            ["Sorrow"] = "Sad",
            ["Fun"] = "Relaxed",
            ["Blink_L"] = "BlinkLeft",
            ["Blink_R"] = "BlinkRight",
            ["A"] = "Aa",
            ["I"] = "Ih",
            ["U"] = "Ou",
            ["E"] = "Ee",
            ["O"] = "Oh",
        };

        private static readonly Dictionary<string, string> _newKeyToOldKey;

        static DefaultBlendShapeNameStore()
        {
            _newKeyToOldKey = _oldKeyToNewKey.ToDictionary(p => p.Value, p => p.Key);
        }


        public static string[] LoadDefaultNames()
        {
            return new string[]
            {
                //「なし」があるのが大事。これによって、条件に合致しても何のブレンドシェイプを起動しない！という事ができる。
                "",
                "Joy",
                "Angry",
                "Sorrow",
                "Fun",
                "Surprised",

                "A",
                "I",
                "U",
                "E",
                "O",

                "Neutral",
                "Blink",
                "Blink_L",
                "Blink_R",

                "LookUp",
                "LookDown",
                "LookLeft",
                "LookRight",
            };
        }

        public static string GetVrm10KeyName(string key)
        {
            return _oldKeyToNewKey.TryGetValue(key, out var newName) ? newName : key;
        }

        public static string GetVrm0KeyName(string key)
        {
            return _newKeyToOldKey.TryGetValue(key, out var oldName) ? oldName : key;
        }

        public static bool ShouldRemoveFromExtraBlendShapeKeyName(string? key)
            => key == "Surprised";
    }
}
