using System.Threading.Tasks;
using System.Windows.Controls;

namespace Baku.VMagicMirrorConfig.View
{
    public partial class StreamingPanel : UserControl
    {
        public StreamingPanel() => InitializeComponent();

        private async void OnCameraHowToButtonClicked(object? sender, System.Windows.RoutedEventArgs e)
        {
            //何も仕掛けないとボタンクリック時にツールチップが閉じてしまうので、それを開くための措置
            if (sender is Button { ToolTip: ToolTip tooltip })
            {
                await Task.Delay(16);
                tooltip.PlacementTarget = sender as Button;
                tooltip.IsOpen = true;
            }
        }

        private void OnCameraHowToButtonMouseLeave(object? sender, System.Windows.Input.MouseEventArgs e)
        {
            //明確に切らないと出っぱなしになりがちなので、マウスが動いたらさっさと消してしまう
            if (sender is Button { ToolTip: ToolTip tooltip })
            {
                tooltip.IsOpen = false;
            }
        }
    }
}
