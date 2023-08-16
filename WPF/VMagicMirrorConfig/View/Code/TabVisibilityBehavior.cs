using Dragablz;
using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
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
                new PropertyMetadata(true, OnTabVisibilityChanged)
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
            new PropertyMetadata(null)
            );


        private static void OnTabVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TabVisibilityBehavior behavior)
            {
                return;
            }

            var tabControl = behavior.AssociatedObject;
            var shouldShow = (bool)e.NewValue;
            var tabItem = behavior.VmcpTab;
            var currentVisible = tabControl.Items.Contains(tabItem);

            if (shouldShow == currentVisible)
            {
                return;
            }

            var index = tabControl.SelectedIndex;
            var items = tabControl.Items.Cast<TabItem>().ToList();

            // - VMCPより後方のタブを選択していた場合、タブが増えても選択状態が維持される
            // - VMCPより後方のタブを選択していた場合、タブが消えても選択タブを維持
            //   - もしVMCPタブが選択中だったら、ひとつ手前のタブが選択された状態にする
            // わざわざindexを保持するのは「いったん全部削除して入れ直す」というシーケンスになっているため
            if (shouldShow && index >= TargetIndex)
            {
                index++;
            }
            else if(!shouldShow && index >= TargetIndex)
            {
                index--;
            }

            tabControl.Items.Clear();
            tabControl.IsHeaderPanelVisible = false;

            if (shouldShow)
            {
                items.Insert(TargetIndex, tabItem);
            }
            else
            {
                items.Remove(tabItem);
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