namespace Baku.VMagicMirrorConfig
{
    // ユーザーが編集したBuddyの設定項目の値
    public class BuddyPropertyValue
    {
        private BuddyPropertyValue(BuddyPropertyType type)
        {
            Type = type;
        }

        public static BuddyPropertyValue Bool(bool value) => new(BuddyPropertyType.Bool)
        {
            BoolValue = value,
        };
        public static BuddyPropertyValue Int(int value) => new(BuddyPropertyType.Int)
        {
            IntValue = value,
        };
        public static BuddyPropertyValue Float(float value) => new(BuddyPropertyType.Float)
        {
            FloatValue = value,
        };
        public static BuddyPropertyValue String(string value) => new(BuddyPropertyType.String)
        {
            StringValue = value,
        };
        public static BuddyPropertyValue Vector2(BuddyVector2 value) => new(BuddyPropertyType.Vector2)
        {
            Vector2Value = value,
        };
        public static BuddyPropertyValue Vector3(BuddyVector3 value) => new(BuddyPropertyType.Vector3)
        {
            Vector3Value = value,
        };
        public static BuddyPropertyValue Quaternion(BuddyVector3 value) => new(BuddyPropertyType.Quaternion)
        {
            Vector3Value = value,
        };
        public static BuddyPropertyValue Transform2D(BuddyTransform2D value) => new(BuddyPropertyType.Transform2D)
        {
            Transform2DValue = value,
        };
        public static BuddyPropertyValue Transform3D(BuddyTransform3D value) => new(BuddyPropertyType.Transform3D)
        {
            Transform3DValue = value,
        };

        // NOTE: Actionは値がないので、逐一インスタンスを生成しないようにするのもアリ
        public static BuddyPropertyValue Action() => new(BuddyPropertyType.Action);

        public bool BoolValue { get; set; }
        public int IntValue { get; set; }
        public float FloatValue { get; set; }
        public string StringValue { get; set; } = "";
        public BuddyVector2 Vector2Value { get; set; }
        public BuddyVector3 Vector3Value { get; set; }
        public BuddyTransform2D Transform2DValue { get; set; }
        public BuddyTransform3D Transform3DValue { get; set; }
        // NOTE: ActionはValueを特に保存しない

        public BuddyPropertyType Type { get; }
    }

    public static class BuddyPropertyValueExtension
    {
        /// <summary>
        /// プロパティの値について、その値がメタデータの定義に沿った有効な値かどうかを取得する。
        /// - 型が合っている
        /// - Rangeの範囲内に収まっている
        /// などを検証する
        /// </summary>
        /// <param name="value"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        public static bool IsValidFor(this BuddyPropertyValue value, BuddyPropertyMetadata metadata)
        {
            if (value.Type != metadata.ValueType)
            {
                return false;
            }

            if (metadata.VisualType is BuddyPropertyType.RangeInt &&
                (value.IntValue < metadata.IntRangeMin || value.IntValue > metadata.IntRangeMax))
            {
                return false;
            }

            if (metadata.VisualType is BuddyPropertyType.RangeFloat &&
                (value.FloatValue < metadata.FloatRangeMin || value.FloatValue > metadata.FloatRangeMax))
            {
                return false;
            }

            if (metadata.VisualType is BuddyPropertyType.Enum && 
                (value.IntValue < 0 || value.IntValue >= metadata.EnumOptions.Count))
            {
                return false;
            }

            return true;
        }

    }
}
