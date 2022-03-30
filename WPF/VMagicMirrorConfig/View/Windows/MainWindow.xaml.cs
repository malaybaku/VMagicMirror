using Baku.VMagicMirrorConfig.ViewModel;
using MahApps.Metro.Controls;
using System;

namespace Baku.VMagicMirrorConfig.View
{
    public partial class MainWindow : MetroWindow
    {
        public MainWindow() => InitializeComponent();

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            (DataContext as IWindowViewModel)?.Initialize();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            (DataContext as IDisposable)?.Dispose();
        }
    }
}
