using System;

namespace Baku.VMagicMirrorConfig
{
    public class BuddySaveData
    {
        public BuddySaveDataSingleBuddy[] Buddies { get; set; } = Array.Empty<BuddySaveDataSingleBuddy>();

        public static BuddySaveData Empty { get; } = new BuddySaveData();
    }

    public class BuddySaveDataSingleBuddy
    {
        // note: このidは内部でBuddyIdと呼んでいるフォルダ名が入る。manifest.jsonに載ってるidも後から併設するかもしれないが、そっちは別物なので注意
        public string Id { get; set; } = "";
        public bool IsActive { get; set; }
        public BuddySaveDataProperty[] Properties { get; set; } = Array.Empty<BuddySaveDataProperty>();
    }

    public class BuddySaveDataProperty
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";

        //NOTE: 「同じキーにbool/number/stringどれかが入る」というのも渋いので、それを避けている
        //TODOかも: これもTypeがあるんだからBool以降はDefaultValueでIgnoreすべきでは
        public bool BoolValue { get; set; }
        public int IntValue { get; set; }
        public float FloatValue { get; set; }
        public string StringValue { get; set; } = "";
        public BuddyVector2 Vector2Value { get; set; }
        public BuddyVector3 Vector3Value { get; set; }
        public BuddyTransform2D Transform2DValue { get; set; }
        public BuddyTransform3D Transform3DValue { get; set; }

        public BuddyPropertyValue ToValue() => Type switch
        {
            nameof(BuddyPropertyType.Bool) => BuddyPropertyValue.Bool(BoolValue),
            nameof(BuddyPropertyType.Int) => BuddyPropertyValue.Int(IntValue),
            nameof(BuddyPropertyType.Float) => BuddyPropertyValue.Float(FloatValue),
            nameof(BuddyPropertyType.String) => BuddyPropertyValue.String(StringValue),
            nameof(BuddyPropertyType.Vector2) => BuddyPropertyValue.Vector2(Vector2Value),
            nameof(BuddyPropertyType.Vector3) => BuddyPropertyValue.Vector3(Vector3Value),
            nameof(BuddyPropertyType.Quaternion) => BuddyPropertyValue.Quaternion(Vector3Value),
            nameof(BuddyPropertyType.Transform2D) => BuddyPropertyValue.Transform2D(Transform2DValue),
            nameof(BuddyPropertyType.Transform3D) => BuddyPropertyValue.Transform3D(Transform3DValue),
            // NOTE: そもそもActionは保存されない状態がto-beではあるが、入ってても無害であって欲しいのでswitchの対象にはしている
            nameof(BuddyPropertyType.Action) => BuddyPropertyValue.Action(),
            _ => throw new NotSupportedException(),
        };
    }
}
