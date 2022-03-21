using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Baku.VMagicMirrorConfig.View
{
    public class SelectOnFocusBehavior : Behavior<TextBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            //二重購読したくないので、念のため。
            AssociatedObject.GotFocus -= OnGotFocus;
            AssociatedObject.PreviewMouseLeftButtonDown -= OnMouseLeftButtonDown;
            AssociatedObject.GotFocus += OnGotFocus;
            AssociatedObject.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.GotFocus -= OnGotFocus;
            AssociatedObject.PreviewMouseLeftButtonDown -= OnMouseLeftButtonDown;
            base.OnDetaching();
        }


        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            AssociatedObject.SelectAll();
        }

        //↓をやっておかないとマウスクリックでフォーカスした場合に全選択が外れてしまう
        //ref: https://threeshark3.com/gotfocus-selectall/
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (AssociatedObject.IsFocused)
            {
                return;
            }

            AssociatedObject.Focus();
            e.Handled = true;
        }
    }
}