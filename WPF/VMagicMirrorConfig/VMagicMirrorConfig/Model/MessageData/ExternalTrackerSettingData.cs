using Newtonsoft.Json.Linq;

namespace Baku.VMagicMirrorConfig
{
    public class ExternalTrackerSettingData
    {
        public string? AppName { get; set; }
        public string? ParamName { get; set; }
        public string? Value { get; set; }

        public string ToJsonString()
        {
            return new JObject()
            {
                [nameof(AppName)] = AppName ?? "",
                [nameof(ParamName)] = ParamName ?? "",
                [nameof(Value)] = Value ?? "",
            }.ToString(Newtonsoft.Json.Formatting.None);
        }
    }
}
