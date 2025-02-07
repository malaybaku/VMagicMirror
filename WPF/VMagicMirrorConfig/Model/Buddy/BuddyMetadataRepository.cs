using Baku.VMagicMirrorConfig.RawData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Baku.VMagicMirrorConfig
{
    public static class BuddyMetadataRepository
    {
        /// <summary>
        /// 全てのBuddyのメタデータを取得する
        /// </summary>
        /// <returns></returns>
        public static BuddyMetadata[] LoadAllBuddyMetadata()
        {
            if (!Directory.Exists(SpecialFilePath.BuddyDir))
            {
                return Array.Empty<BuddyMetadata>();
            }

            var dirs = Directory.GetDirectories(SpecialFilePath.BuddyDir);
            var result = new List<BuddyMetadata>();
            foreach (var dir in dirs)
            {
                if (TryGetBuddyMetadata(dir, out var buddyMetadata))
                {
                    result.Add(buddyMetadata);
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// フォルダパスを指定して、そのフォルダに定義されたBuddyのメタデータ取得を試みる
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryGetBuddyMetadata(string folderPath, [NotNullWhen(true)] out BuddyMetadata? result)
        {
            // 書いてる通りだが、GUI側で以下までは検証する
            // - エントリポイントっぽいファイルがある
            // - メタデータの定義ファイルがある
            // - メタデータがJSONとして読めそうである

            var entryScriptPath = Path.Combine(folderPath, "main.lua");
            if (!File.Exists(entryScriptPath))
            {
                LogOutput.Instance.Write("entry script file (main.lua) does not exist at path: " + entryScriptPath);
                result = null;
                return false;
            }

            var manifestFilePath = Path.Combine(folderPath, "manifest.json");
            if (!File.Exists(manifestFilePath))
            {
                LogOutput.Instance.Write("manifest file does not exist at path: " + manifestFilePath);
                result = null;
                return false;
            }

            // ファイルがJSONとしてパースできない場合はNG、キーがちょっと抜けてるとかは基本的に許容する
            try
            {
                result = LoadBuddyMetadata(folderPath, manifestFilePath);
                if (result.Properties.Select(p => p.Name).ToHashSet().Count < result.Properties.Length)
                {
                    throw new ArgumentException("manifest has properties with same name: " + manifestFilePath);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
                result = null;
                return false;
            }
        }

        private static BuddyMetadata LoadBuddyMetadata(string folderPath, string filePath)
        {
            // NOTE: StreamWriterでもよいが、読み込みがさっさと終わることを好んでFile.ReadAllTextしている
            var json = File.ReadAllText(filePath);
            using var tr = new StringReader(json);
            using var jr = new JsonTextReader(tr);
            var serializer = new JsonSerializer();
            var rawMetadata = serializer.Deserialize<RawBuddyMetadata>(jr);

            if (rawMetadata == null)
            {
                throw new ArgumentException("manifest.json's format is invalid");
            }

            var properties = rawMetadata.Properties
                .Select(Convert)
                .ToArray();

            return new BuddyMetadata(
                folderPath,
                rawMetadata.Id,
                rawMetadata.DisplayName,
                rawMetadata.Creator,
                rawMetadata.CreatorUrl,
                rawMetadata.Version,
                properties.ToArray()
                );
        }

        private static BuddyPropertyMetadata Convert(RawBuddyPropertyMetadata src)
        {
            var name = src.Name;
            var rawType = src.Type;
            var displayName = string.IsNullOrEmpty(src.DisplayName) ? name : src.DisplayName;

            switch (rawType)
            {
                case "bool":
                    return BuddyPropertyMetadata.Bool(
                        name, 
                        displayName,
                        src.BoolData?.DefaultValue ?? false
                        );
                case "int":
                    if (src.IntData == null)
                    {
                        return BuddyPropertyMetadata.Int(name, displayName, 0);
                    }

                    var intData = src.IntData;
                    if (intData.Options is not null && intData.Options.Length > 0)
                    {
                        return BuddyPropertyMetadata.Enum(
                            name,
                            displayName,
                            intData.DefaultValue,
                            intData.Options
                            );
                    }
                    else if (intData.Min.HasValue && intData.Max.HasValue)
                    {
                        return BuddyPropertyMetadata.RangeInt(
                            name, displayName, intData.DefaultValue, intData.Min.Value, intData.Max.Value);
                    }
                    else
                    {
                        return BuddyPropertyMetadata.Int(name, displayName, intData.DefaultValue);
                    }
                case "float":
                    if (src.FloatData == null)
                    {
                        return BuddyPropertyMetadata.Float(name, displayName, 0);
                    }

                    var floatData = src.FloatData;
                    if (floatData.Min.HasValue && floatData.Max.HasValue)
                    {
                        return BuddyPropertyMetadata.RangeFloat(
                            name, displayName, floatData.DefaultValue, floatData.Min.Value, floatData.Max.Value);
                    }
                    else
                    {
                        return BuddyPropertyMetadata.Float(name, displayName, floatData.DefaultValue);
                    }
                case "string":
                    return BuddyPropertyMetadata.String(
                        name,
                        displayName,
                        src.StringData?.DefaultValue ?? ""
                        );
                case "vector2":
                    var vector2DefaultValue = new BuddyVector2();
                    if (src.Vector2Data != null)
                    {
                        vector2DefaultValue = new BuddyVector2(src.Vector2Data.DefaultValue.X, src.Vector2Data.DefaultValue.Y);
                    }

                    return BuddyPropertyMetadata.Vector2(
                        name,
                        displayName,
                        vector2DefaultValue
                        );
                case "vector3":
                    var vector3DefaultValue = new BuddyVector3();
                    if (src.Vector3Data != null)
                    {
                        var v3value = src.Vector3Data.DefaultValue;
                        vector3DefaultValue = new BuddyVector3(v3value.X, v3value.Y, v3value.Z);
                    }
                    return BuddyPropertyMetadata.Vector3(
                        name,
                        displayName,
                        vector3DefaultValue
                        );
                case "quaternion":
                    // NOTE: メモリ上での表現はオイラー角=Vector3なので、Vector3との区別はかなり少ない
                    var eulerAngleDefault = new BuddyVector3();
                    if (src.QuaternionData != null)
                    {
                        var eValue = src.QuaternionData.DefaultValue;
                        eulerAngleDefault = new BuddyVector3(eValue.X, eValue.Y, eValue.Z);
                    }
                    return BuddyPropertyMetadata.Quaternion(
                        name,
                        displayName,
                        eulerAngleDefault
                        );
                case "transform2D":
                    var transform2DDefault = src.Transform2DData?.DefaultValue ?? new RawBuddyTransform2D();
                    return BuddyPropertyMetadata.Transform2D(
                        name, displayName, transform2DDefault.ToTransform2D()
                        );
                case "transform3D":
                    var transform3DDefault = src.Transform3DData?.DefaultValue ?? new RawBuddyTransform3D();
                    return BuddyPropertyMetadata.Transform3D(
                        name, displayName, transform3DDefault.ToTransform3D()
                        );
                default:
                    throw new ArgumentException("Unsupported type is specified for property");
            }
        }
    }
}
