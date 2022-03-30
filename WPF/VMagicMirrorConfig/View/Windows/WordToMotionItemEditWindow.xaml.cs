using MahApps.Metro.Controls;
using System;
using System.ComponentModel;
using System.Windows;

namespace Baku.VMagicMirrorConfig.View
{
    public partial class WordToMotionItemEditWindow : MetroWindow
    {
        public static MetroWindow? CurrentWindow { get; private set; } = null;

        public WordToMotionItemEditWindow()
        {
            InitializeComponent();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            CurrentWindow = this;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            //ダイアログ形式で出るので、他ウィンドウの参照を消す心配はないです
            CurrentWindow = null;
        }

    }
}
