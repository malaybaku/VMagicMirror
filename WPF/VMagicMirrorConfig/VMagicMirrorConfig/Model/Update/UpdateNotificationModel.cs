using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using System.Net.Http;

namespace Baku.VMagicMirrorConfig
{
    public class UpdateNotificationModel
    {
        const string LatestReleaseApiEndPoint = "https://api.github.com/repos/malaybaku/VMagicMirror/releases/latest";
        const double ApiTimeout = 3.0;

        //NOTE: 確かコレ系のClientは使い回すと不幸になるので、単一インスタンスにしておく
        private static readonly HttpClient _httpClient = new HttpClient();
      
        static UpdateNotificationModel()
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(ApiTimeout);
            //GitHub君がUser-Agentを欲しているのでEdgeであるということにします
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Microsoft Edge");
        }

        public async Task<UpdateCheckResult> CheckUpdateAvailable()
        {
            try
            {                
                var responseJson = await _httpClient.GetStringAsync(LatestReleaseApiEndPoint);
                var jobj = JObject.Parse(responseJson);
                var releaseName = jobj["name"] is JValue jvName ? ((string?)jvName) : null;
                if (!VmmAppVersion.TryParse(releaseName, out var version))
                {
                    return UpdateCheckResult.NoUpdateNeeded();
                }

                var rawReleaseNote = jobj["body"] is JValue jvBody ? ((string?)jvBody) : null;
                var releaseNote = ReleaseNote.FromRawString(rawReleaseNote);

                return new UpdateCheckResult(
                    version.IsNewerThan(AppConsts.AppVersion),
                    version,
                    releaseNote
                    );
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
                return UpdateCheckResult.NoUpdateNeeded();
            }
        }
    }
}
