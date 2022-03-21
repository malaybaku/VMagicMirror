using System.Windows;
using System.Windows.Controls;

namespace Baku.VMagicMirrorConfig.View
{
    public partial class AboutPanel : UserControl
    {
        public AboutPanel() => InitializeComponent();

        private void OnClickHyperLinkToGitHub(object sender, RoutedEventArgs e)
            => UrlNavigate.Open("https://github.com/malaybaku/VMagicMirror");

        private void OnClickHyperLinkToModelData(object sender, RoutedEventArgs e)
            => UrlNavigate.Open("https://github.com/malaybaku/VMagicMirror/pull/616");
    }
}
