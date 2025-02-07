using Newtonsoft.Json;
using System;

namespace Baku.VMagicMirrorConfig.BuddySettingsMessages
{
    //NOTE:
    // - MessageFactory経由でUnityに投げるデータだけ分けて定義してる
    // - 使うクラスが少ない(BuddySettingsSenderのみの想定)ので、他のクラスよりnamespaceが一つ深い
    [Serializable]
    public class BuddySettingsMessage
    {
        public string BuddyId { get; set; } = "";
        public BuddySettingsPropertyMessage[] Properties { get; set; } = Array.Empty<BuddySettingsPropertyMessage>();
    }

    // NOTE: 雑にデータ量を削る目的で、JSONはなるべく略記する。
    // とくに ~~Value は既定値のとき、Typeと合致するValueも省略する。
    //
    // 例: BuddySettingsMessage.Property の要素として「boolのfalse」を指定する場合、
    // NameとType(="Bool")だけがJSONのキーとして入るのが想定挙動
    [Serializable]
    public class BuddySettingsPropertyMessage
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? BuddyId { get; set; }

        public string Name { get; set; } = "";

        public string Type { get; set; } = "";

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool BoolValue { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int IntValue { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public float FloatValue { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? StringValue { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public BuddyVector2 Vector2Value { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public BuddyVector3 Vector3Value { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public BuddyTransform2D Transform2DValue { get; set; }

        //TODO?: ParentBoneのシリアライズがintになりそうなのはちょっと性質は悪い。最悪諦めてもUnity側で手に負えるけど
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public BuddyTransform3D Transform3DValue { get; set; }
    }
}
