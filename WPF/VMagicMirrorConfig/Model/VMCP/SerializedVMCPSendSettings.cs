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
        public string SendAddress { get; set; } = "";
        public int SendPort { get; set; }

        public bool SendBonePose { get; set; }
        public bool SendFingerBonePose { get; set; }
        public bool SendFacial { get; set; }
        public bool SendNonStandardFacial { get; set; }
        public bool UseVrm0Facial { get; set; }
        public bool Prefer30Fps { get; set; }

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

        public string ToJson() => JsonConvert.SerializeObject(this);

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
                if (string.IsNullOrEmpty(json))
                {
                    return CreateDefault();
                }

                return JsonConvert.DeserializeObject<SerializedVMCPSendSettings>(json) ?? CreateDefault();
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
                return CreateDefault();
            }
        }

        public static SerializedVMCPSendSettings CreateDefault() => FromSetting(VMCPSendSetting.Default());
    }
}
