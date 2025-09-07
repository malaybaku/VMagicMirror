using System.Windows;
using System.Windows.Controls;

namespace Baku.VMagicMirrorConfig.View
{
    public sealed class ComboBoxSelectProhibitBehavior
    {
        public static readonly DependencyProperty SuppressSelectionProperty = DependencyProperty.RegisterAttached(
          "SuppressSelection",
          typeof(bool),
          typeof(ComboBoxSelectProhibitBehavior),
          new PropertyMetadata(false, OnSuppressSelectionChanged)
        );

        public static void SetSuppressSelection(DependencyObject obj, bool value)
            => obj.SetValue(SuppressSelectionProperty, value);

        public static bool GetSuppressSelection(DependencyObject obj)
            => (bool)obj.GetValue(SuppressSelectionProperty);

        static void OnSuppressSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ComboBox cb)
            {
                if ((bool)e.NewValue)
                {
                    // 二重登録が怖いので一応こっちのケースでも一回外しておく
                    cb.SelectionChanged -= OnCheckboxSelectionChanged;
                    cb.SelectionChanged += OnCheckboxSelectionChanged;
                }
                else
                {
                    cb.SelectionChanged -= OnCheckboxSelectionChanged;
                }
            }
        }

        static void OnCheckboxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // もし選択状態になったらすぐカラに戻す
            if (sender is ComboBox cb && cb.SelectedIndex != -1)
            {
                cb.SelectedIndex = -1;
                // ドロップダウンの閉じ対策: PaddingつきでComboBoxの要素を出してると欲しくなる実装だが、
                // 暴発が怖いのでやらないでおく
                //if (cb.IsDropDownOpen == false) cb.IsDropDownOpen = true;
            }
        }
    }
}
