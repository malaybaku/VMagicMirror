using System;
using System.Threading.Tasks;
using System.Windows;

namespace Baku.VMagicMirrorConfig
{
    public enum UpdateDialogResult
    {
        GetLatestVersion,
        AskMeLater,
        SkipThisVersion,
    }

    /// <summary>
    /// アプリ起動時に適当に走らせるとアプリ更新チェックができるすごいやつだよ
    /// </summary>
    class UpdateChecker
    {
        private const string StandardEditionUrl = "https://baku-dreameater.booth.pm/items/1272298";
        private const string FullEditionUrl = "https://baku-dreameater.booth.pm/items/3064040";

        //「後で通知」が具体的にどのくらい後なのか、という値[day]
        private const double DialogAppearMinimumIntervalDay = 5.0;

        /// <summary>
        /// アプリ起動時か、あるいはそれ以外で明示的に要求された場合に更新を確認します。
        /// </summary>
        /// <param name="startupCheck"></param>
        /// <returns></returns>
        public async Task RunAsync(bool startupCheck)
        {
            var model = new UpdateNotificationModel();
            var preference = UpdatePreferenceRepository.Load();

            var checkResult = await model.CheckUpdateAvailable();
            if (!checkResult.UpdateNeeded)
            {
                //有効なバージョン情報が降ってきた場合、アップデートが不要だとしても保存はしておく
                if (checkResult.Version.IsValid)
                {
                    UpdatePreferenceRepository.Save(new UpdatePreference()
                    {
                        LastDialogShownTime = DateTime.Now,
                        LastShownVersion = checkResult.Version.ToString(),
                        //現時点で最新版がないので、このフラグはどっちになっていても挙動に影響しない
                        SkipLastShownVersion = false,
                    });
                    LogOutput.Instance.Write("Update is not needed: this version seems latest.");
                }
                else
                {
                    LogOutput.Instance.Write("Update check seems failed...");
                }

                if (!startupCheck)
                {
                    var indication = MessageIndication.AlreadyLatestVersion();
                    _ = await MessageBoxWrapper.Instance.ShowAsync(
                        indication.Title,
                        string.Format(indication.Content, AppConsts.AppVersion.ToString()),
                        MessageBoxWrapper.MessageBoxStyle.OK
                        );
                }

                return;
            }

            var lastShownVersion = VmmAppVersion.TryParse(preference.LastShownVersion, out var version)
                ? version
                : VmmAppVersion.LoadInvalid();

            //コードの通りだが、ダイアログを出せるのは以下のケース。
            // - 前回表示したのより新しいバージョンである
            // - アプリ起動時のものではなく、明示的にチェック操作をしている
            // - 前回表示したのとバージョンで、かつユーザーが「そのバージョンはスキップする」を指定しておらず、かつ前回から十分時間があいている
            var shouldShowDialog =
                checkResult.Version.IsNewerThan(lastShownVersion) ||
                !startupCheck ||
                (!preference.SkipLastShownVersion &&
                    (DateTime.Now - preference.LastDialogShownTime).TotalDays > DialogAppearMinimumIntervalDay);

            if (!shouldShowDialog)
            {
                return;
            }

            var vm = new UpdateNotificationViewModel(checkResult);
            var dialog = new UpdateNotificationWindow()
            {
                DataContext = vm,
                Owner = Application.Current.MainWindow,
            };
            dialog.ShowDialog();

            var openStorePage = false;
            var skipLastShownVersion = false;
            switch (vm.Result)
            {
                case UpdateDialogResult.GetLatestVersion:
                    openStorePage = true;
                    break;
                case UpdateDialogResult.AskMeLater:
                    //何もしない
                    break;
                case UpdateDialogResult.SkipThisVersion:
                    skipLastShownVersion = true;
                    break;
            }

            UpdatePreferenceRepository.Save(new UpdatePreference()
            {
                LastDialogShownTime = DateTime.Now,
                LastShownVersion = checkResult.Version.ToString(),
                SkipLastShownVersion = skipLastShownVersion,
            });

            if (openStorePage)
            {
                UrlNavigate.Open(FeatureLocker.FeatureLocked ? StandardEditionUrl : FullEditionUrl);
            }
        }
    }
}
