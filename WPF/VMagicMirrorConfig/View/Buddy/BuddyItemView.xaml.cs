using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Baku.VMagicMirrorConfig.View
{
    public partial class BuddyItemView : UserControl
    {
        public BuddyItemView() => InitializeComponent();

        private bool _triedToFindParentScrollViewer;
        private ScrollViewer? _parentScrollViewer;

        // 内側のScrollViewerのPreviewMouseWheelイベントハンドラ
        private void LogMessageScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not ScrollViewer scrollViewer)
            {
                return;
            }

            if (!_triedToFindParentScrollViewer)
            {
                _triedToFindParentScrollViewer = true;
                _parentScrollViewer = FindParentScrollViewer(scrollViewer);
            }

            if (_parentScrollViewer == null)
            {
                return;
            }

            // 下方向（e.Delta < 0) か上方向（e.Delta > 0）かでスクロールが端に到達してることの判定は変わる。
            // どっちの場合も親スクロールへのイベント転送に帰着できる
            if ((e.Delta < 0 && scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight) ||
                (e.Delta > 0 && scrollViewer.VerticalOffset <= 0))
            {
                e.Handled = true;

                var e2 = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = MouseWheelEvent,
                };
                _parentScrollViewer.RaiseEvent(e2);
            }
        }

        // 指定した要素の親要素を辿って最初に見つかったScrollViewerを返す
        private static ScrollViewer? FindParentScrollViewer(DependencyObject child)
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null && parent is not ScrollViewer)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as ScrollViewer;
        }
    }
}
