using System.Windows;
using System.Windows.Controls;

namespace Baku.VMagicMirrorConfig.View
{
    public partial class AboutPanel : UserControl
    {
        public AboutPanel() => InitializeComponent();

        private void OnClickHyperLinkToGitHub(object sender, RoutedEventArgs e)
            => UrlNavigate.Open("https://github.com/malaybaku/VMagicMirror");

        private void OnClickHyperLinkToGamepadModelData(object sender, RoutedEventArgs e)
            => UrlNavigate.Open("https://github.com/malaybaku/VMagicMirror/pull/616");

        private void OnClickHyperLinkToCarSteeringModelData(object sender, RoutedEventArgs e)
            => UrlNavigate.Open("https://sketchfab.com/3d-models/steering-wheel-rally-car-c83da5e0c5ea4e6095295dec147b3cfe");
    }
}
