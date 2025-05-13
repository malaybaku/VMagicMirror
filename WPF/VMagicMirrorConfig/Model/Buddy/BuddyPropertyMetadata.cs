using System;
using System.Collections.Generic;

namespace Baku.VMagicMirrorConfig
{
    public enum BuddyPropertyType
    {
        Bool,
        Int,
        Float,
        RangeInt,
        RangeFloat,
        String,
        Enum,
        Vector2,
        Vector3,
        Quaternion,
        Transform2D,
        Transform3D,
    }

    public class BuddyPropertyMetadata
    {
        private BuddyPropertyMetadata(
            string name, 
            BuddyLocalizedText displayName, 
            BuddyLocalizedText description,
            BuddyPropertyType visualType)
        {
            Name = name;
            DisplayName = displayName;
            VisualType = visualType;
            ValueType = visualType.ToValueType();
        }

        public string Name { get; }
        public BuddyLocalizedText DisplayName { get; }
        public BuddyLocalizedText Description { get; }

        public BuddyPropertyType ValueType { get; }
        public BuddyPropertyType VisualType { get; }

        public bool DefaultBoolValue { get; private init; }
        public int DefaultIntValue { get; private init; }
        public float DefaultFloatValue { get; private init; }
        public string DefaultStringValue { get; private init; } = "";
        public BuddyVector2 DefaultVector2Value { get; private init; }
        public BuddyVector3 DefaultVector3Value { get; private init; }
        public BuddyTransform2D DefaultTransform2DValue { get; private init; }
        public BuddyTransform3D DefaultTransform3DValue { get; private init; }

        public int IntRangeMin { get; private init; }
        public int IntRangeMax { get; private init; }
        public float FloatRangeMin { get; private init; }
        public float FloatRangeMax { get; private init; }
        public IReadOnlyList<string> EnumOptions { get; private init; } = Array.Empty<string>();

        // TODO: VisualTypeの種類と同じだけ生成メソッドを用意したい
        public static BuddyPropertyMetadata Bool(string name, BuddyLocalizedText displayName, BuddyLocalizedText description, bool defaultValue)
        {
            return new BuddyPropertyMetadata(name, displayName, description, BuddyPropertyType.Bool)
            {
                DefaultBoolValue = defaultValue,
            };
        }

        public static BuddyPropertyMetadata Int(string name, BuddyLocalizedText displayName, BuddyLocalizedText description, int defaultValue)
        {
            return new BuddyPropertyMetadata(name, displayName, description, BuddyPropertyType.Int)
            {
                DefaultIntValue = defaultValue,
            };
        }

        public static BuddyPropertyMetadata RangeInt(string name, BuddyLocalizedText displayName, BuddyLocalizedText description, int defaultValue, int min, int max)
        {
            return new BuddyPropertyMetadata(name, displayName, description, BuddyPropertyType.RangeInt)
            {
                DefaultIntValue = defaultValue,
                IntRangeMin = min,
                IntRangeMax = max,
            };
        }

        public static BuddyPropertyMetadata Float(string name, BuddyLocalizedText displayName, BuddyLocalizedText description, float defaultValue)
        {
            return new BuddyPropertyMetadata(name, displayName, description, BuddyPropertyType.Float)
            {
                DefaultFloatValue = defaultValue,
            };
        }

        public static BuddyPropertyMetadata RangeFloat(string name, BuddyLocalizedText displayName, BuddyLocalizedText description, float defaultValue, float min, float max)
        {
            return new BuddyPropertyMetadata(name, displayName, description, BuddyPropertyType.RangeFloat)
            {
                DefaultFloatValue = defaultValue,
                FloatRangeMin = min,
                FloatRangeMax = max,
            };
        }

        public static BuddyPropertyMetadata Enum(string name, BuddyLocalizedText displayName, BuddyLocalizedText description, int defaultValue, string[] options)
        {
            //NOTE: enumOptionsは配列コピーはしない(デシリアライズされた値が渡ってきてるはずで、それは再利用しないので)
            return new BuddyPropertyMetadata(name, displayName, description, BuddyPropertyType.Enum)
            {
                DefaultIntValue = defaultValue,
                EnumOptions = options,
            };
        }

        public static BuddyPropertyMetadata String(string name, BuddyLocalizedText displayName, BuddyLocalizedText description, string defaultValue)
        {
            return new BuddyPropertyMetadata(name, displayName, description, BuddyPropertyType.String)
            {
                DefaultStringValue = defaultValue,
            };
        }

        public static BuddyPropertyMetadata Vector2(string name, BuddyLocalizedText displayName, BuddyLocalizedText description, BuddyVector2 defaultValue)
        {
            return new BuddyPropertyMetadata(name, displayName, description, BuddyPropertyType.Vector2)
            {
                DefaultVector2Value = defaultValue,
            };
        }

        public static BuddyPropertyMetadata Vector3(string name, BuddyLocalizedText displayName, BuddyLocalizedText description, BuddyVector3 defaultValue)
        {
            return new BuddyPropertyMetadata(name, displayName, description, BuddyPropertyType.Vector3)
            {
                DefaultVector3Value = defaultValue,
            };
        }

        public static BuddyPropertyMetadata Quaternion(string name, BuddyLocalizedText displayName, BuddyLocalizedText description, BuddyVector3 defaultValue)
        {
            return new BuddyPropertyMetadata(name, displayName, description, BuddyPropertyType.Quaternion)
            {
                DefaultVector3Value = defaultValue,
            };
        }

        public static BuddyPropertyMetadata Transform2D(string name, BuddyLocalizedText displayName, BuddyLocalizedText description, BuddyTransform2D defaultValue)
        {
            return new BuddyPropertyMetadata(name, displayName, description, BuddyPropertyType.Transform2D)
            {
                DefaultTransform2DValue = defaultValue,
            };
        }

        public static BuddyPropertyMetadata Transform3D(string name, BuddyLocalizedText displayName, BuddyLocalizedText description, BuddyTransform3D defaultValue)
        {
            return new BuddyPropertyMetadata(name, displayName, description, BuddyPropertyType.Transform3D)
            {
                DefaultTransform3DValue = defaultValue,
            };
        }
    }

    public static class BuddyPropertyTypeExtension
    {
        public static BuddyPropertyType ToValueType(this BuddyPropertyType type)
        {
            return type switch
            {
                BuddyPropertyType.Bool => BuddyPropertyType.Bool,
                BuddyPropertyType.Int => BuddyPropertyType.Int,
                BuddyPropertyType.Float => BuddyPropertyType.Float,
                BuddyPropertyType.RangeInt => BuddyPropertyType.Int,
                BuddyPropertyType.RangeFloat => BuddyPropertyType.Float,
                BuddyPropertyType.String => BuddyPropertyType.String,
                BuddyPropertyType.Enum => BuddyPropertyType.Int,
                BuddyPropertyType.Vector2 => BuddyPropertyType.Vector2,
                BuddyPropertyType.Vector3 => BuddyPropertyType.Vector3,
                BuddyPropertyType.Quaternion => BuddyPropertyType.Quaternion,
                BuddyPropertyType.Transform2D => BuddyPropertyType.Transform2D,
                BuddyPropertyType.Transform3D => BuddyPropertyType.Transform3D,
                _ => throw new NotSupportedException(),
            };
        }
    }

    public static class BuddyPropertyMetadataExtension
    {
        public static BuddyPropertyValue CreateDefaultValue(this BuddyPropertyMetadata metadata)
        {
            return metadata.ValueType switch
            {
                BuddyPropertyType.Bool => BuddyPropertyValue.Bool(metadata.DefaultBoolValue),
                BuddyPropertyType.Int => BuddyPropertyValue.Int(metadata.DefaultIntValue),
                BuddyPropertyType.Float => BuddyPropertyValue.Float(metadata.DefaultFloatValue),
                BuddyPropertyType.String => BuddyPropertyValue.String(metadata.DefaultStringValue),
                BuddyPropertyType.Vector2 => BuddyPropertyValue.Vector2(metadata.DefaultVector2Value),
                BuddyPropertyType.Vector3 => BuddyPropertyValue.Vector3(metadata.DefaultVector3Value),
                BuddyPropertyType.Quaternion => BuddyPropertyValue.Quaternion(metadata.DefaultVector3Value),
                BuddyPropertyType.Transform2D => BuddyPropertyValue.Transform2D(metadata.DefaultTransform2DValue),
                BuddyPropertyType.Transform3D => BuddyPropertyValue.Transform3D(metadata.DefaultTransform3DValue),
                _ => throw new NotSupportedException(),
            };
        }
    }
}
