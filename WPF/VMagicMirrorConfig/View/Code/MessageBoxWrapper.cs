using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Baku.VMagicMirrorConfig
{
    class MessageBoxWrapper
    {
        private MessageBoxWrapper() { }
        private static MessageBoxWrapper? _instance;
        public static MessageBoxWrapper Instance
            => _instance ??= new MessageBoxWrapper();

        private ProgressDialogController? _mainProgress = null;
        private ProgressDialogController? _settingProgress = null;
        private TaskCompletionSource<bool>? _tcsProgress = null;

        /// <summary>
        /// メインウィンドウ、および表示されている場合は設定ウィンドウに、
        /// 同時にダイアログを表示します。
        /// 片方のダイアログを閉じた時点でもう片方を閉じて結果を返します。
        /// MessageBox.Showの代わりに呼び出して使います。
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ShowAsync(string title, string content, MessageBoxStyle style)
        {
            if (Application.Current.MainWindow is not MetroWindow window)
            {
                //ウィンドウが何かおかしい: 無視
                return true;
            }

            var settingWindow = SettingWindow.CurrentWindow;

            if (style == MessageBoxStyle.OK)
            {
                var cts = new CancellationTokenSource();
                var mainWindowTask = window.ShowMessageAsync(
                    title,
                    content,
                    settings: SettingsForOkDialog(cts.Token)
                    );

                if (settingWindow != null)
                {
                    await Task.WhenAny(
                        mainWindowTask,
                        settingWindow.ShowMessageAsync(
                            title,
                            content,
                            settings: SettingsForOkDialog(cts.Token)
                        )
                    );
                }
                else
                {
                    await mainWindowTask;
                }
                //どっちかのダイアログが閉じたらもう片方も閉じる
                cts.Cancel();
                return true;
            }
            else if (style == MessageBoxStyle.OKCancel)
            {
                var cts = new CancellationTokenSource();
                var mainWindowTask = window.ShowMessageAsync(
                    title,
                    content,
                    MessageDialogStyle.AffirmativeAndNegative,
                    SettingsForOkCancel(cts.Token)
                    );

                if (settingWindow != null)
                {
                    var firstTask = await Task.WhenAny(
                        mainWindowTask,
                        settingWindow.ShowMessageAsync(
                            title,
                            content,
                            MessageDialogStyle.AffirmativeAndNegative,
                            SettingsForOkCancel(cts.Token)
                        )
                    );
                    //どっちかのダイアログが閉じたらもう片方も閉じる
                    cts.Cancel();
                    return firstTask.Result == MessageDialogResult.Affirmative;
                }
                else
                {
                    var result = await mainWindowTask;
                    return result == MessageDialogResult.Affirmative;
                }
            }
            else if (style == MessageBoxStyle.None)
            {
                //Progress方式で出す
                _mainProgress = await window.ShowProgressAsync(
                   title,
                   content,
                   settings: SettingsForProgress()
                   );

                if (settingWindow != null)
                {
                    _settingProgress = await settingWindow.ShowProgressAsync(
                            title,
                            content,
                            settings: SettingsForProgress()
                        );
                }

                _tcsProgress = new TaskCompletionSource<bool>();
                return await _tcsProgress.Task;
            }

            //ふつう到達しない
            return true;
        }

        /// <summary>
        /// ダイアログの結果を設定します。UIから実行したり、プログラム的にダイアログを閉じたいときに呼び出します。
        /// </summary>
        /// <param name="result"></param>
        public async void SetDialogResult(bool result)
        {
            //NOTE: 名前に反して、ほぼダイアログを閉じるためだけに使う。
            //これは現状の用途が"UI上ではキャンセル不可のダイアログをコードからキャンセル扱いで閉じる"という特殊な目的に限定されているため。
            if (_mainProgress != null)
            {
                await _mainProgress.CloseAsync();
            }

            if (_settingProgress != null)
            {
                await _settingProgress.CloseAsync();
            }

            _tcsProgress?.SetResult(true);

            _mainProgress = null;
            _settingProgress = null;
            _tcsProgress = null;
        }

        /// <summary>
        /// <see cref="ShowAsync(string, string, MessageBoxStyle)"/>と大体同じだが、Word To Motionのアイテム編集ウィンドウにだけ表示する。
        /// 以下の点でかなり特殊なメソッドです。
        /// - 対象ウィンドウが異なる
        /// - OK / Cancelのケースだけ配慮すればいい
        /// - 外部からのキャンセルは無視してよい
        /// </summary>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task<bool> ShowAsyncOnWordToMotionItemEdit(string title, string content)
        {
            if (WordToMotionItemEditWindow.CurrentWindow is not MetroWindow metroWindow)
            {
                return false;
            }

            var emptyToken = new CancellationToken();
            var result = await metroWindow.ShowMessageAsync(
                title,
                content,
                MessageDialogStyle.AffirmativeAndNegative,
                settings: SettingsForOkCancel(emptyToken)
                );
            return result == MessageDialogResult.Affirmative;
        }

        private MetroDialogSettings SettingsForOkDialog(CancellationToken token)
        {
            return new MetroDialogSettings()
            {
                CancellationToken = token,
                AnimateShow = true,
                AnimateHide = false,
                OwnerCanCloseWithDialog = true,
            };
        }

        private MetroDialogSettings SettingsForOkCancel(CancellationToken token)
        {
            return new MetroDialogSettings()
            {
                CancellationToken = token,
                AffirmativeButtonText = "OK",
                NegativeButtonText = "Cancel",
                AnimateShow = true,
                AnimateHide = false,
                DialogResultOnCancel = MessageDialogResult.Negative,
                OwnerCanCloseWithDialog = true,
            };
        }

        private MetroDialogSettings SettingsForProgress()
        {
            return new MetroDialogSettings()
            {
                DialogResultOnCancel = MessageDialogResult.Negative,
                AnimateShow = true,
                AnimateHide = false,
                OwnerCanCloseWithDialog = true,
            };
        }

        public enum MessageBoxStyle
        {
            OK,
            OKCancel,
            //NOTE: NoneはUnityの特定操作が終わるまでUIをガードしたいときに使う。
            //表示したダイアログはSetDialogResultで閉じる必要がある。
            None,
        }
    }
}
