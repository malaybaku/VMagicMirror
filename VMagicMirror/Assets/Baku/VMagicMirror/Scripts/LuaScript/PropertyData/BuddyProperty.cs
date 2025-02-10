using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    // NOTE: WPF側で定義したPropertyTypeの部分集合になっている。型の実態だけを見るので、例えばIntとRangeIntとEnumは区別されない
    // 文字列ベースでシリアライズしてWPFから送られるため、Unity側ではintの実数値やバージョン更新による互換性はケア不要
    public enum BuddyPropertyType
    {
        // NOTE: Propertyの通し方をゴニョゴニョするために、内部で便宜的に「不明」扱いすることがある
        Unknown,
        Bool,
        Int,
        Float,
        String,
        Vector2,
        Vector3,
        Quaternion,
        Transform2D,
        Transform3D,
    }

    // TODO: たぶんAPIに引っ越す…ような気がする
    public class BuddyProperty
    {
        private BuddyProperty(string name, BuddyPropertyType type, object value)
        {
            Name = name;
            PropertyType = type;
            Type = (int)type;
            Value = value;
        }

        public static BuddyProperty Bool(string name, bool value) => new(name, BuddyPropertyType.Bool, value);
        public static BuddyProperty Int(string name, int value) => new(name, BuddyPropertyType.Int, value);
        public static BuddyProperty Float(string name, float value) => new(name, BuddyPropertyType.Float, value);
        public static BuddyProperty String(string name, string value) => new(name, BuddyPropertyType.String, value);
        public static BuddyProperty Vector2(string name, Vector2 value) => new(name, BuddyPropertyType.Vector2, value);
        public static BuddyProperty Vector3(string name, Vector3 value) => new(name, BuddyPropertyType.Vector3, value);
        public static BuddyProperty Quaternion(string name, Quaternion value) => new(name, BuddyPropertyType.Quaternion, value);
        
        internal BuddyPropertyType PropertyType { get; }
        
        internal string Name { get; }

        // NOTE: APIとしては文字列が公開されるほうが良い可能性もある
        public int Type { get; }
        
        public object Value { get; }
    }
}

