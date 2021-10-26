using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// マイクまたはカメラの名称一覧のシリアライズに関するデータ。
    /// </summary>
    /// <remarks>
    /// 配列1個分でもシリアライザ挟んだ方が安全そう(文字回りの心配が減る)、という判断によりクラスを挟んでます。
    /// </remarks>
    public class DeviceNames
    {
        public DeviceNames(string[] names)
        {
            Names = names;
        }

        public string[] Names { get; set; }
       
        //NOTE: 第2引数は失敗時のログ収集用で実際にはあんまり意味がない
        public static DeviceNames FromJson(string data, string usageName)
        {
            try
            {
                var serializer = new JsonSerializer();
                using (var reader = new StringReader(data))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    return serializer.Deserialize<DeviceNames>(jsonReader) ?? new DeviceNames(new string[0]);
                }
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write("Failed to get DeviceNames of " + usageName);
                LogOutput.Instance.Write(ex);
                return new DeviceNames(new string[0]);
            }
        }
    }
}
