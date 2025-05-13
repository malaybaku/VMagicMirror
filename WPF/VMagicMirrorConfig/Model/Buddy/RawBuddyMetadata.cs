using Newtonsoft.Json;
using System;

namespace Baku.VMagicMirrorConfig.RawData
{
    // JSONのデシリアライズ用に定義したBuddyのメタ情報。
    // NOTE: メタ情報の決めとして、commentというキーは定義しないことになっている (必ず無視する)。

    public class RawBuddyMetadata
    {
        [JsonProperty("id")] public string Id { get; set; } = "";
        [JsonProperty("displayName")] public RawBuddyLocalizedText DisplayName { get; set; }
        [JsonProperty("creator")] public string Creator { get; set; } = "";
        [JsonProperty("creatorUrl")] public string CreatorUrl { get; set; } = "";
        [JsonProperty("version")] public string Version { get; set; } = "";

        [JsonProperty("property")] public RawBuddyPropertyMetadata[] Properties { get; set; } = Array.Empty<RawBuddyPropertyMetadata>();
    }

    public class RawBuddyPropertyMetadata
    {
        [JsonProperty("name")] public string Name { get; set; } = "";
        // NOTE: DisplayNameが無い場合、ローカリゼーションと無関係にNameをDisplayNameとしても用いる
        [JsonProperty("displayName")] public RawBuddyLocalizedText? DisplayName { get; set; }
        // NOTE: Descriptionがない場合はUI上にキャプション用のUI部分がそもそも表示されないのが期待値
        [JsonProperty("description")] public RawBuddyLocalizedText? Description { get; set; }

        // NOTE: "bool", "int" など、特定の文字列だけが想定されている
        [JsonProperty("type")] public string Type { get; set; } = "";

        [JsonProperty("boolData")] public RawBuddyBoolPropertyMetadata? BoolData { get; set; }
        [JsonProperty("intData")] public RawBuddyIntPropertyMetadata? IntData { get; set; }
        [JsonProperty("floatData")] public RawBuddyFloatPropertyMetadata? FloatData { get; set; }
        [JsonProperty("stringData")] public RawBuddyStringPropertyMetadata? StringData { get; set; }
        [JsonProperty("vector2Data")] public RawBuddyVector2PropertyMetadata? Vector2Data { get; set; }
        [JsonProperty("vector3Data")] public RawBuddyVector3PropertyMetadata? Vector3Data { get; set; }
        [JsonProperty("quaternionData")] public RawBuddyQuaternionPropertyMetadata? QuaternionData { get; set; }
        [JsonProperty("transform2DData")] public RawBuddyTransform2DPropertyMetadata? Transform2DData { get; set; }
        [JsonProperty("transform3DData")] public RawBuddyTransform3DPropertyMetadata? Transform3DData { get; set; }
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

    public class RawBuddyTransform2DPropertyMetadata
    {
        [JsonProperty("defaultValue")]
        public RawBuddyTransform2D DefaultValue { get; set; }
    }

    public class RawBuddyTransform3DPropertyMetadata
    {
        [JsonProperty("defaultValue")]
        public RawBuddyTransform3D DefaultValue { get; set; }
    }

    public struct RawBuddyVector2
    {
        public RawBuddyVector2(float x, float y) : this()
        {
            X = x;
            Y = y;
        }

        [JsonProperty("x")] public float X { get; set; }
        [JsonProperty("y")] public float Y { get; set; }

        public BuddyVector2 ToBuddyVector2() => new(X, Y);
    }

    public struct RawBuddyVector3
    {
        public RawBuddyVector3(float x, float y, float z) : this()
        {
            X = x;
            Y = y; 
            Z = z;
        }

        [JsonProperty("x")] public float X { get; set; }
        [JsonProperty("y")] public float Y { get; set; }
        [JsonProperty("z")] public float Z { get; set; }

        public BuddyVector3 ToBuddyVector3() => new(X, Y, Z);
    }

    public struct RawBuddyTransform2D
    {
        [JsonProperty("position")]
        public RawBuddyVector2 Position { get; set; }
        [JsonProperty("rotation")]
        public RawBuddyVector3 Rotation { get; set; }

        [JsonProperty("scale")]
        public float Scale { get; set; }

        public BuddyTransform2D ToTransform2D() => new(Position.ToBuddyVector2(), Rotation.ToBuddyVector3(), Scale);

        public static RawBuddyTransform2D CreateDefaultValue() => new()
        {
            Position = new RawBuddyVector2(0.3f, 0.5f),
            Scale = 0.1f,
        };
    }

    public struct RawBuddyTransform3D
    {
        [JsonProperty("position")]
        public RawBuddyVector3 Position { get; set; }

        [JsonProperty("rotation")]
        public RawBuddyVector3 Rotation { get; set; }

        [JsonProperty("scale")]
        public float Scale { get; set; }

        [JsonProperty("parentBone")]
        public string ParentBone { get; set; }

        public BuddyTransform3D ToTransform3D() => new(
            Position.ToBuddyVector3(),
            Rotation.ToBuddyVector3(), 
            Scale, 
            Enum.TryParse<BuddyParentBone>(ParentBone, out var bone) ? bone : BuddyParentBone.None
            );

        public static RawBuddyTransform3D CreateDefaultValue() => new()
        {
            Position = new RawBuddyVector3(1f, 0f, 0f),
            Scale = 1f,
            ParentBone = "",
        };
    }

    public struct RawBuddyLocalizedText
    {
        [JsonProperty("ja")] public string Ja { get; set; }
        [JsonProperty("en")] public string En { get; set; }
    }
}
