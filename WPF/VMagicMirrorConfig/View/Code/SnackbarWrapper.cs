using System;
using MaterialDesignThemes.Wpf;

namespace Baku.VMagicMirrorConfig.View
{
    /// <summary>
    /// スナックバーのラッパークラスです。
    /// </summary>
    /// <remarks>
    /// シングルトンでもstaticでもいいような内容なので今回はstaticにしてます
    /// </remarks>
    public static class SnackbarWrapper
    {
        /// <summary>
        /// xamlから参照する、スナックバーのキューです。
        /// MainWindowだけが使う想定です。
        /// </summary>
        public static SnackbarMessageQueue SnackbarMessageQueue { get; } = new SnackbarMessageQueue(TimeSpan.FromSeconds(3.0));

        /// <summary>
        /// コードから呼び出してスナックバーにメッセージを追加します。
        /// </summary>
        /// <param name="message"></param>
        public static void Enqueue(string message) => SnackbarMessageQueue.Enqueue(message);

    }
}
