using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// VMCPの送信設定をIPCでUnityに送るときに使うやつ
    /// </summary>
    public class SerializedVMCPSendSettings
    {
        public VMCPSendSetting ToSetting() => new()
        {
            SendAddress = SendAddress,
            SendPort = SendPort,
            SendBonePose = SendBonePose,
            SendFingerBonePose = SendFingerBonePose,
            SendFacial = SendFacial,
            SendNonStandardFacial = SendNonStandardFacial,
            UseVrm0Facial = UseVrm0Facial,
            Prefer30Fps = Prefer30Fps,
        };

        public string ToJson()
        {
            var sb = new StringBuilder();
            using var writer = new StringWriter(sb);
            using var jsonWriter = new JsonTextWriter(writer);
            var serializer = new JsonSerializer();
            serializer.Serialize(jsonWriter, this);
            return sb.ToString();
        }

        public static SerializedVMCPSendSettings FromSetting(VMCPSendSetting setting) => new()
        {
            SendAddress = setting.SendAddress,
            SendPort = setting.SendPort,
            SendBonePose = setting.SendBonePose,
            SendFingerBonePose = setting.SendFingerBonePose,
            SendFacial = setting.SendFacial,
            SendNonStandardFacial = setting.SendNonStandardFacial,
            UseVrm0Facial = setting.UseVrm0Facial,
            Prefer30Fps = setting.Prefer30Fps,
        };

        public static SerializedVMCPSendSettings FromJson(string json)
        {
            try
            {
                using var reader = new StringReader(json);
                using var jsonReader = new JsonTextReader(reader);
                var serializer = new JsonSerializer();
                return serializer.Deserialize<SerializedVMCPSendSettings>(jsonReader) ?? CreateDefault();
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
                return CreateDefault();
            }
        }


        public string SendAddress { get; set; } = "";
        public int SendPort { get; set; }

        public bool SendBonePose { get; set; }
        public bool SendFingerBonePose { get; set; }
        public bool SendFacial { get; set; }
        public bool SendNonStandardFacial { get; set; }
        public bool UseVrm0Facial { get; set; }
        public bool Prefer30Fps { get; set; }

        public static SerializedVMCPSendSettings CreateDefault() => FromSetting(VMCPSendSetting.Default());
    }
}
