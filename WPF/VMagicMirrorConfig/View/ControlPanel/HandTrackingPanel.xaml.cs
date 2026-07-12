using System.Windows.Controls;

namespace Baku.VMagicMirrorConfig.View
{
    public partial class HandTrackingPanel : UserControl
    {
        public HandTrackingPanel()
        {
            InitializeComponent();
        }

        private async void OnCrashPreventionHelpButtonClicked(object? sender, System.Windows.RoutedEventArgs e)
        {
            // ボタンクリック時に閉じるToolTipを、クリック操作でも確認できるよう開き直す
            if (sender is Button { ToolTip: ToolTip tooltip })
            {
                await System.Threading.Tasks.Task.Delay(16);
                tooltip.PlacementTarget = sender as Button;
                tooltip.IsOpen = true;
            }
        }

        private void OnCrashPreventionHelpButtonMouseLeave(object? sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Button { ToolTip: ToolTip tooltip })
            {
                tooltip.IsOpen = false;
            }
        }
    }
}
