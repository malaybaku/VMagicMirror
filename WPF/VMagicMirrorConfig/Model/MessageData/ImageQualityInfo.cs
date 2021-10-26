using Newtonsoft.Json;
using System;
using System.IO;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// Unity側の画質設定を教えてもらうときのデータ構造
    /// </summary>
    public class ImageQualityInfo
    {
        public string[]? ImageQualityNames { get; set; }
        public int CurrentQualityIndex { get; set; }

        public static ImageQualityInfo ParseFromJson(string json)
        {
            try
            {
                using (var sReader = new StringReader(json))
                using (var jReader = new JsonTextReader(sReader))
                {
                    return
                        new JsonSerializer().Deserialize<ImageQualityInfo>(jReader) ??
                        new ImageQualityInfo();
                }
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
                return new ImageQualityInfo();
            }
        }
    }
}
