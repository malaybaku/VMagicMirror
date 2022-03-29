using Baku.VMagicMirrorConfig.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Baku.VMagicMirrorConfig.View
{
    public partial class MidiNoteAssignEditorWindow : Window
    {
        public MidiNoteAssignEditorWindow()
        {
            InitializeComponent();
        }

        //NOTE: コレがほしいのは「MIDIイベントが来たら現在フォーカスしているテキストボックスにノート番号を入れる」という親切処理をしてあげたいからです
        private MidiNoteReceiver? _midiNoteReceiver;

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

            if (DataContext is MidiNoteToMotionEditorViewModel vm)
            {
                _midiNoteReceiver = vm.MidiNoteReceiver;
                if (_midiNoteReceiver != null)
                {
                    _midiNoteReceiver.MidiNoteOn += ReceiveMidiNote;
                }
            }

            var textbox = afterKeys.ItemContainerGenerator.ContainerFromIndex(0).FindChildByType<TextBox>();
            FocusManager.SetFocusedElement(this, textbox);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (_midiNoteReceiver != null)
            {
                _midiNoteReceiver.MidiNoteOn -= ReceiveMidiNote;
            }
        }

        private void ReceiveMidiNote(object? sender, MidiNoteEventArgs e)
        {
            if (Keyboard.FocusedElement is TextBox tb)
            {
                tb.Text = e.Note.ToString();
                tb.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

    }

    public static class VisualTreeExtensions
    {
        public static T? FindChildByType<T>(this DependencyObject? depObj) where T : DependencyObject
        {
            if (depObj == null)
            {
                return null;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                if (child != null && child is T)
                {
                    return (T)child;
                }

                T? childItem = child?.FindChildByType<T>();
                if (childItem != null)
                {
                    return childItem;
                }
            }
            return null;
        }
    }

}
