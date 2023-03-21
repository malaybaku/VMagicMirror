using MahApps.Metro.Controls;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Baku.VMagicMirrorConfig.View
{
    //NOTE: 構造は詳細設定のウィンドウ = SettingWindowと同じで、1アプリに1ウィンドウまでの単独なウィンドウを想定している
    public partial class GameInputKeyAssignWindow : MetroWindow
    {
        public GameInputKeyAssignWindow() => InitializeComponent();

        /// <summary>現在設定ウィンドウがあればそれを取得し、なければnullを取得します。</summary>
        public static GameInputKeyAssignWindow? CurrentWindow { get; private set; } = null;

        public static void OpenOrActivateExistingWindow()
        {
            if (CurrentWindow == null)
            {
                CurrentWindow = new GameInputKeyAssignWindow()
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

        private static async void OnSettingWindowClosed(object? sender, EventArgs e)
        {
            if (CurrentWindow != null)
            {
                CurrentWindow.Closed -= OnSettingWindowClosed;
                CurrentWindow = null;

                //NOTE: ウィンドウを閉じたあとはGC可能なリソースがそこそこある(WindowとかViewModelとか)ので、明示的にやってしまう
                await Task.Delay(1000);
                GC.Collect();
            }
        }
    }
}
