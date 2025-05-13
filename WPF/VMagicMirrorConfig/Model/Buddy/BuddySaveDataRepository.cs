using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Baku.VMagicMirrorConfig
{
    public static class BuddySaveDataRepository
    {
        public static BuddySaveDataSingleBuddy ExportSetting(BuddyData buddy)
        {
            return new BuddySaveDataSingleBuddy()
            {
                Id = buddy.Metadata.BuddyId,
                IsActive = buddy.IsActive.Value,
                Properties = buddy.Properties
                    .Where(p => p.Metadata.ValueType != BuddyPropertyType.Action)
                    .Select(p => new BuddySaveDataProperty()
                {
                    Name = p.Metadata.Name,
                    Type = p.Metadata.ValueType.ToString(),
                    BoolValue = p.Value.BoolValue,
                    IntValue = p.Value.IntValue,
                    FloatValue = p.Value.FloatValue,
                    StringValue = p.Value.StringValue,
                    Vector2Value = p.Value.Vector2Value,
                    Vector3Value = p.Value.Vector3Value,
                    Transform2DValue = p.Value.Transform2DValue,
                    Transform3DValue = p.Value.Transform3DValue,
                }).ToArray(),
            };
        }

        public static BuddySaveData ExportSetting(bool mainAvatarOutputActive, IEnumerable<BuddyData> buddies)
        {
            // TODO: FeatureLocker絡みのプロパティもここで保存対象にしたい(し、ロード時にも同様にケアしたい)
            return new BuddySaveData()
            {
                MainAvatarOutputActive = mainAvatarOutputActive,
                Buddies = buddies.Select(ExportSetting).ToArray(),
            };
        }

        public static void SaveSetting(bool mainAvatarOutputActive, IEnumerable<BuddyData> buddies, string path)
        {
            var saveData = ExportSetting(mainAvatarOutputActive, buddies);
            using var sw = new StringWriter();
            new JsonSerializer().Serialize(sw, saveData);
            File.WriteAllText(path, sw.ToString());
        }

        // NOTE: Saveと非対称であることには注意。
        // メタデータも突き合わせないとBuddyDataが完成しないので対称性がない
        public static BuddySaveData LoadSetting(string path)
        {
            if (!File.Exists(path))
            {
                return BuddySaveData.Empty;
            }

            try
            {
                return JsonConvert.DeserializeObject<BuddySaveData>(File.ReadAllText(path)) ?? BuddySaveData.Empty;
            }
            catch (Exception ex) 
            {
                LogOutput.Instance.Write(ex);
                return BuddySaveData.Empty;
            }
        }
    }
}
