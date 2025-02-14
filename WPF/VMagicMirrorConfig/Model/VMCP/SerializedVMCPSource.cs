using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// VMCPの受信設定の一覧
    /// </summary>
    /// <remarks>
    /// 順序に意味があることに注意。
    /// 複数ソースで同じ情報を受信しようとした場合、Indexが0寄りのソースのほうが優先的にて適用される
    /// </remarks>
    public class SerializedVMCPSources
    {
        public SerializedVMCPSource[]? Sources { get; set; }

        public string ToJson()
        {
            var sb = new StringBuilder();
            using var writer = new StringWriter(sb);
            using var jsonWriter = new JsonTextWriter(writer);
            var serializer = new JsonSerializer();
            serializer.Serialize(jsonWriter, this);
            return sb.ToString();
        }

        public VMCPSources ToSetting() => new VMCPSources(
            Sources?.Select(s => s.ToSource()) ?? Enumerable.Empty<VMCPSource>()
            );

        public static SerializedVMCPSources FromJson(string json)
        {
            try
            {
                using var reader = new StringReader(json);
                using var jsonReader = new JsonTextReader(reader);
                var serializer = new JsonSerializer();
                return serializer.Deserialize<SerializedVMCPSources>(jsonReader) ?? Empty;
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
                return Empty;
            }
        }

        public static SerializedVMCPSources FromSetting(VMCPSources sources)
        {
            return new SerializedVMCPSources()
            {
                Sources = sources.Sources
                    .Select(SerializedVMCPSource.FromSource)
                    .ToArray(),
            };
        }


        public static SerializedVMCPSources Empty => FromSetting(VMCPSources.Default);
    }

    /// <summary>
    /// VMCPSourceと等価でシリアライズ可能なことが保証されてるデータ
    /// </summary>
    public class SerializedVMCPSource
    {
        public string Name { get; set; } = "";
        public int Port { get; set; } = 0;
        public bool ReceiveHeadPose { get; set; }
        public bool ReceiveFacial { get; set; }
        public bool ReceiveHandPose { get; set; }

        public SerializedVMCPSource()
        {
        }

        public static SerializedVMCPSource FromSource(VMCPSource source) => new()
        {
            Name = source.Name,
            Port = source.Port,
            ReceiveHeadPose = source.ReceiveHeadPose,
            ReceiveFacial = source.ReceiveFacial,
            ReceiveHandPose = source.ReceiveHandPose,
        };

        public VMCPSource ToSource() => new VMCPSource()
        {
            Name = Name,
            Port = Port,
            ReceiveHeadPose = ReceiveHeadPose,
            ReceiveFacial = ReceiveFacial,
            ReceiveHandPose = ReceiveHandPose,
        };
    }
}
