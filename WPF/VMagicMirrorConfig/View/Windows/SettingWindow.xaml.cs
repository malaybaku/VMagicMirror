using MahApps.Metro.Controls;
using System;
using System.Windows;

namespace Baku.VMagicMirrorConfig.View
{
    public partial class SettingWindow : MetroWindow
    {
        public SettingWindow() => InitializeComponent();

        /// <summary>現在設定ウィンドウがあればそれを取得し、なければnullを取得します。</summary>
        public static SettingWindow? CurrentWindow { get; private set; } = null;

        public static void OpenOrActivateExistingWindow()
        {
            if (CurrentWindow == null)
            {
                CurrentWindow = new SettingWindow()
                {
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };

                CurrentWindow.Closed += OnSettingWindowClosed;
                CurrentWindow.Show();
            }
            else
            {
                CurrentWindow.Activate();
            }
        }

        private static void OnSettingWindowClosed(object? sender, EventArgs e)
        {
            if (CurrentWindow != null)
            {
                CurrentWindow.Closed -= OnSettingWindowClosed;
                CurrentWindow = null;
            }
        }
    }
}
