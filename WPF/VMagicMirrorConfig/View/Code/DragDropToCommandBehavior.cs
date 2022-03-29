using Microsoft.Xaml.Behaviors;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Baku.VMagicMirrorConfig.View
{
    /// <summary>
    /// VRMファイルのドラッグ&ドロップをコマンドとしてVMに送信するためのビヘイビア
    /// あんまり清潔に書くモチベが無いのでロジックも入れます
    /// </summary>
    public class DragDropToCommandBehavior : Behavior<FrameworkElement>
    {
        /// <summary>
        /// ドロップ処理時に呼ばれるコマンドを取得、設定します。
        /// </summary>
        public ICommand DropCommand
        {
            get => (ICommand)GetValue(DropCommandProperty);
            set => SetValue(DropCommandProperty, value);
        }

        public Visibility InstructionVisibility
        {
            get => (Visibility)GetValue(InstructionVisibilityProperty);
            set
            {
                LogOutput.Instance.Write($"Set DragDrop InstrcutionVisibility, value = {value}");
                SetValue(InstructionVisibilityProperty, value);
            }
        }

        /// <summary>
        /// <see cref="DropCommand"/>の依存関係プロパティです。
        /// </summary>
        public static readonly DependencyProperty DropCommandProperty
            = DependencyProperty.RegisterAttached(
                nameof(DropCommand),
                typeof(ICommand),
                typeof(DragDropToCommandBehavior)
                );

        public static readonly DependencyProperty InstructionVisibilityProperty
            = DependencyProperty.RegisterAttached(
                nameof(InstructionVisibility),
                typeof(Visibility),
                typeof(DragDropToCommandBehavior),
                new PropertyMetadata(Visibility.Collapsed)
                );

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.DragEnter += OnDragEnter;
            AssociatedObject.Drop += OnDrop;
            AssociatedObject.DragLeave += OnDragLeave;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.DragEnter -= OnDragEnter;
            AssociatedObject.Drop -= OnDrop;
            AssociatedObject.DragLeave -= OnDragLeave;
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            e.Effects =
                (e.Data.GetData(DataFormats.FileDrop) is string[] fileNames &&
                    fileNames.Length == 1 &&
                    Path.GetExtension(fileNames[0]) == ".vrm")
                ? DragDropEffects.Copy
                : DragDropEffects.None;

            InstructionVisibility = (e.Effects == DragDropEffects.Copy) ? Visibility.Visible : Visibility.Collapsed;

            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            var command = DropCommand;
            if (command != null &&
                command.CanExecute(null))
            {
                command.Execute(
                    (e.Data.GetData(DataFormats.FileDrop) as string[])?[0]
                    );
            }

            InstructionVisibility = Visibility.Collapsed;
        }

        private void OnDragLeave(object sender, DragEventArgs e) => InstructionVisibility = Visibility.Collapsed;
    }
}
