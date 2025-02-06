using Newtonsoft.Json;
using System;

namespace Baku.VMagicMirrorConfig.RawData
{
    // JSONのデシリアライズ用に定義したBuddyのメタ情報。
    // NOTE: メタ情報の決めとして、commentというキーは定義しないことになっている (必ず無視する)。

    public class RawBuddyMetadata
    {
        [JsonProperty("id")] public string Id { get; set; } = "";
        [JsonProperty("displayName")] public string DisplayName { get; set; } = "";
        [JsonProperty("creator")] public string Creator { get; set; } = "";
        [JsonProperty("creatorUrl")] public string CreatorUrl { get; set; } = "";
        [JsonProperty("version")] public string Version { get; set; } = "";

        [JsonProperty("property")] public RawBuddyPropertyMetadata[] Properties { get; set; } = Array.Empty<RawBuddyPropertyMetadata>();
    }

    public class RawBuddyPropertyMetadata
    {
        [JsonProperty("name")] public string Name { get; set; } = "";
        // DisplayNameが定義されていない場合、NameをDisplayNameとしても用いる
        [JsonProperty("displayName")] public string? DisplayName { get; set; }
        [JsonProperty("type")] public string Type { get; set; } = "";

        [JsonProperty("boolData")] public RawBuddyBoolPropertyMetadata? BoolData { get; set; }
        [JsonProperty("intData")] public RawBuddyIntPropertyMetadata? IntData { get; set; }
        [JsonProperty("floatData")] public RawBuddyFloatPropertyMetadata? FloatData { get; set; }
        [JsonProperty("stringData")] public RawBuddyStringPropertyMetadata? StringData { get; set; }
        [JsonProperty("vector2Data")] public RawBuddyVector2PropertyMetadata? Vector2Data { get; set; }
        [JsonProperty("vector3Data")] public RawBuddyVector3PropertyMetadata? Vector3Data { get; set; }
        [JsonProperty("quaternionData")] public RawBuddyQuaternionPropertyMetadata? QuaternionData { get; set; }

    }

    // NOTE: 以下のうち、min/max/optionsはjson上で指定がなければnullになることが期待値
    public class RawBuddyBoolPropertyMetadata
    {
        [JsonProperty("defaultValue")]
        public bool DefaultValue { get; set; }
    }
    
    public class RawBuddyIntPropertyMetadata
    {
        [JsonProperty("defaultValue")]
        public int DefaultValue { get; set; }

        [JsonProperty("min")]
        public int? Min { get; set; }

        [JsonProperty("max")]
        public int? Max { get; set; }

        [JsonProperty("options")]
        public string[]? Options { get; set; }
    }

    public class RawBuddyFloatPropertyMetadata
    {
        [JsonProperty("defaultValue")]
        public float DefaultValue { get; set; }

        [JsonProperty("min")]
        public float? Min { get; set; }

        [JsonProperty("max")]
        public float? Max { get; set; }
    }

    public class RawBuddyStringPropertyMetadata
    {
        [JsonProperty("defaultValue")]
        public string DefaultValue { get; set; } = "";
    }

    public class RawBuddyVector2PropertyMetadata
    {
        [JsonProperty("defaultValue")]
        public RawBuddyVector2 DefaultValue { get; set; }
    }

    public class RawBuddyVector3PropertyMetadata
    {
        [JsonProperty("defaultValue")]
        public RawBuddyVector3 DefaultValue { get; set; }
    }

    public class RawBuddyQuaternionPropertyMetadata
    {
        [JsonProperty("defaultValue")]
        public RawBuddyVector3 DefaultValue { get; set; }
    }

    public struct RawBuddyVector2
    {
        [JsonProperty("x")] public float X { get; set; }
        [JsonProperty("y")] public float Y { get; set; }
    }

    public struct RawBuddyVector3
    {
        [JsonProperty("x")] public float X { get; set; }
        [JsonProperty("y")] public float Y { get; set; }
        [JsonProperty("z")] public float Z { get; set; }
    }
}
