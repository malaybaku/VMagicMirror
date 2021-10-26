using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// 直近で更新通知ダイアログを出した時刻、および特定バージョンをスキップする場合の対象バージョン。
    /// 一度もダイアログを出した事がない場合、DateTime.MinValueが便宜的に入る
    /// </summary>
    public class UpdatePreference
    {
        public DateTime LastDialogShownTime { get; set; } = DateTime.MinValue;
        public string LastShownVersion { get; set; } = "";
        //「このバージョンはスキップ」をした場合、そのバージョンの次のアップデートまでは通知を出さない
        public bool SkipLastShownVersion { get; set; }

        public static UpdatePreference Empty => new UpdatePreference();
    }

    public static class UpdatePreferenceRepository
    {
        public static UpdatePreference Load()
        {
            if (!File.Exists(SpecialFilePath.UpdateCheckFilePath))
            {
                return UpdatePreference.Empty;
            }

            try
            {
                //NOTE: DateTimeが問題なくシリアライズできるか、という点だけ要注意
                var data = File.ReadAllText(SpecialFilePath.UpdateCheckFilePath);
                var serializer = new JsonSerializer();
                using (var reader = new StringReader(data))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    return serializer.Deserialize<UpdatePreference>(jsonReader) ?? UpdatePreference.Empty;
                }
            }
            catch(Exception ex)
            {
                LogOutput.Instance.Write(ex);
                return UpdatePreference.Empty;
            }
        }

        public static void Save(UpdatePreference preference)
        {
            var serializer = new JsonSerializer();
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                serializer.Serialize(jsonWriter, preference);
            }

            File.WriteAllText(SpecialFilePath.UpdateCheckFilePath, sb.ToString());
        }
    }
}
