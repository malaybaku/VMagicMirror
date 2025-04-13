using Baku.VMagicMirrorConfig.ViewModel;
using MahApps.Metro.Controls;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Baku.VMagicMirrorConfig.View
{
    //NOTE: 構造は詳細設定のウィンドウ = SettingWindowと同じで、1アプリに1ウィンドウまでの単独なウィンドウを想定している
    public partial class FaceTrackingEyeCalibrationWindow : MetroWindow
    {
        public FaceTrackingEyeCalibrationWindow() => InitializeComponent();

        /// <summary>現在ウィンドウが開いていればそれを取得し、なければnullを取得します。</summary>
        public static FaceTrackingEyeCalibrationWindow? CurrentWindow { get; private set; } = null;

        public static void OpenOrActivateExistingWindow()
        {
            if (CurrentWindow == null)
            {
                CurrentWindow = new FaceTrackingEyeCalibrationWindow()
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

                if (CurrentWindow.DataContext is FaceTrackerEyeCalibrationViewModel viewModel)
                {
                    // プレビューのデータを送信しっぱなしだとIPCがムダに残るので、それを避けている
                    viewModel.EnableEyeBlendShapeValuePreview.Value = false;
                }

                CurrentWindow = null;

                //NOTE: ウィンドウを閉じたあとはGC可能なリソースがそこそこある(WindowとかViewModelとか)ので、明示的にやってしまう
                await Task.Delay(1000);
                GC.Collect();
            }
        }
    }
}
