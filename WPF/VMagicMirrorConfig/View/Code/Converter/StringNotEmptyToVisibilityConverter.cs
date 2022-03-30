using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Baku.VMagicMirrorConfig.View
{
    /// <summary> 空でない文字列をVisible、それ以外をCollapsedにするコンバータ </summary>
    public class StringNotEmptyToVisibilityConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value switch
            {
                string s => string.IsNullOrWhiteSpace(s) ? Visibility.Collapsed : Visibility.Visible,
                _ => Visibility.Collapsed,
            };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
