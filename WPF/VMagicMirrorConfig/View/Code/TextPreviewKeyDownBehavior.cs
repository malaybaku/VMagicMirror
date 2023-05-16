using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Baku.VMagicMirrorConfig.View
{
    public class TextPreviewKeyDownBehavior : Behavior<TextBox>
    {
        public ICommand KeyDownCommand
        {
            get => (ICommand)GetValue(KeyDownCommandProperty);
            set => SetValue(KeyDownCommandProperty, value);
        }

        public static readonly DependencyProperty KeyDownCommandProperty
            = DependencyProperty.RegisterAttached(
                nameof(KeyDownCommand),
                typeof(ICommand),
                typeof(TextPreviewKeyDownBehavior)
                );

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewKeyDown += OnKeyDown;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewKeyDown -= OnKeyDown;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            TextKeyDownBehaviorUtil.OnKeyDown(e, key =>
            {
                if (KeyDownCommand?.CanExecute(key) == true)
                {
                    KeyDownCommand.Execute(key);
                }
            }, true);
        }
    }
}
