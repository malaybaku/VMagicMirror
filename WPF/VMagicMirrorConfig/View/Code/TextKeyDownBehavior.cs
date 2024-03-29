﻿using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Baku.VMagicMirrorConfig.View
{
    /// <summary>
    /// Hot Keyの編集をするためのテキストボックスからKeyDown情報をコマンド的に送信するやつ
    /// </summary>
    public class TextKeyDownBehavior : Behavior<TextBox>
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
                typeof(TextKeyDownBehavior)
                );

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.KeyDown += OnKeyDown;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.KeyDown -= OnKeyDown;
        }

        //NOTE: 必要ならpreviewにするのもあり
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            TextKeyDownBehaviorUtil.OnKeyDown(e, key =>
            {
                if (KeyDownCommand?.CanExecute(key) == true)
                {
                    KeyDownCommand.Execute(key);
                }
            });
        }
    }
}
