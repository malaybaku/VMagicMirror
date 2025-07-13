using Newtonsoft.Json;
using System;
using System.Linq;

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

        public string ToJson() => JsonConvert.SerializeObject(this, Formatting.None);

        public VMCPSources ToSetting() => new(Sources?.Select(s => s.ToSource()) ?? []);

        public static SerializedVMCPSources FromJson(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<SerializedVMCPSources>(json) ?? Empty;
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
        public bool ReceiveLowerBodyPose { get; set; }

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
            ReceiveLowerBodyPose = source.ReceiveLowerBodyPose,
        };

        public VMCPSource ToSource() => new VMCPSource()
        {
            Name = Name,
            Port = Port,
            ReceiveHeadPose = ReceiveHeadPose,
            ReceiveFacial = ReceiveFacial,
            ReceiveHandPose = ReceiveHandPose,
            ReceiveLowerBodyPose = ReceiveLowerBodyPose,
        };
    }
}
