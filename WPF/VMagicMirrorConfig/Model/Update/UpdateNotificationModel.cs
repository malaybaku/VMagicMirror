using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using System.Net.Http;

namespace Baku.VMagicMirrorConfig
{
    public class UpdateNotificationModel
    {
        private const string LatestReleaseApiEndPoint = "https://api.github.com/repos/malaybaku/VMagicMirror/releases/latest";
        private const double ApiTimeout = 3.0;

        //NOTE: 確かコレ系のClientは使い回すと不幸になるので、単一インスタンスにしておく
        private static readonly HttpClient _httpClient = new HttpClient();
      
        //TODO: このstatic ctor内で何か例外が出てるらしいので要調査 (dev限定の可能性もあるが)
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
                // DEBUG: 検証用に画像URLを強制適用する
                var releaseNote = ReleaseNote.FromRawString(rawReleaseNote)
                with
                {
                    ImageUrl = new Uri("https://github.com/user-attachments/assets/3b3972e2-110d-4d72-ace6-4637519c339f"),
                }
                ;

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
