using Microsoft.Xaml.Behaviors;
using System.Windows;

namespace Baku.VMagicMirrorConfig
{
    public class DialogBehavior : Behavior<Window>
    {
        public bool? Result
        {
            get { return (bool?)GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
        }

        public static readonly DependencyProperty ResultProperty = DependencyProperty.Register(
                nameof(Result),
                typeof(bool?),
                typeof(DialogBehavior),
                new PropertyMetadata(null, OnResultChanged)
                );

        private static void OnResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is DialogBehavior behavior))
            {
                return;
            }
            behavior.AssociatedObject.DialogResult = (bool)e.NewValue;
        }
    }
}