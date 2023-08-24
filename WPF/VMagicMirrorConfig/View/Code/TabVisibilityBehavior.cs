using Dragablz;
using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace Baku.VMagicMirrorConfig.View
{
    public class TabVisibilityBehavior : Behavior<TabablzControl>
    {
        //ほんとは可変なほうがきれいです
        private const int TargetIndex = 3;

        public bool ShowVmcpTab
        {
            get { return (bool)GetValue(ShowVmcpTabProperty); }
            set { SetValue(ShowVmcpTabProperty, value); }
        }

        public static readonly DependencyProperty ShowVmcpTabProperty = DependencyProperty.Register(
                nameof(ShowVmcpTab),
                typeof(bool),
                typeof(TabVisibilityBehavior),
                new PropertyMetadata(true, OnBehaviorPropertyChanged)
                );

        public TabItem VmcpTab
        {
            get { return (TabItem)GetValue(VmcpTabProperty); }
            set { SetValue(VmcpTabProperty, value); }
        }

        public static readonly DependencyProperty VmcpTabProperty = DependencyProperty.Register(
            nameof(VmcpTab),
            typeof(TabItem),
            typeof(TabVisibilityBehavior),
            new PropertyMetadata(null, OnBehaviorPropertyChanged)
            );


        private static void OnBehaviorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TabVisibilityBehavior behavior)
            {
                behavior.UpdateTargetTabVisibility();
            }
        }

        protected override void OnAttached() => UpdateTargetTabVisibility();

        private void UpdateTargetTabVisibility()
        {
            var tabControl = AssociatedObject;
            if (tabControl == null)
            {
                //NOTE: ウィンドウの生成直後だと通過しうる
                return;
            }

            var vmcpTab = VmcpTab;
            var visible = ShowVmcpTab;
            var currentVisible = tabControl.Items.Contains(vmcpTab);

            if (visible == currentVisible)
            {
                return;
            }

            var index = tabControl.SelectedIndex;
            var items = tabControl.Items.Cast<TabItem>().ToList();

            // - VMCPより後方のタブを選択していた場合、タブが増えても選択状態が維持される
            // - VMCPより後方のタブを選択していた場合、タブが消えても選択タブを維持
            //   - もしVMCPタブが選択中だったら、ひとつ手前のタブが選択された状態にする
            // わざわざindexを保持するのは「いったん全部削除して入れ直す」というシーケンスになっているため
            if (visible && index >= TargetIndex)
            {
                index++;
            }
            else if (!visible && index >= TargetIndex)
            {
                index--;
            }

            tabControl.Items.Clear();
            tabControl.IsHeaderPanelVisible = false;

            if (visible)
            {
                items.Insert(TargetIndex, vmcpTab);
            }
            else
            {
                items.Remove(vmcpTab);
            }

            foreach (var item in items)
            {
                tabControl.Items.Add(item);
            }
            tabControl.SelectedIndex = index;
            tabControl.IsHeaderPanelVisible = true;
        }
    }
}