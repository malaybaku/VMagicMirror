using System;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// 設定リセット処理の共通処理
    /// </summary>
    static class SettingResetUtils
    {
        /// <summary>
        /// 確認ダイアログを出したうえで、個別カテゴリの設定をリセットします。
        /// </summary>
        /// <param name="resetAction"></param>
        public static async void ResetSingleCategoryAsync(Action resetAction)
        {
            var indication = MessageIndication.ResetSingleCategoryConfirmation();
            bool res = await MessageBoxWrapper.Instance.ShowAsync(
                indication.Title,
                indication.Content,
                MessageBoxWrapper.MessageBoxStyle.OKCancel
                );

            if (res)
            {
                resetAction();
            }
        }
    }
}
