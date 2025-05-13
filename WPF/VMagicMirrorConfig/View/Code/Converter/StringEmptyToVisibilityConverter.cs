using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Baku.VMagicMirrorConfig.View
{
    /// <summary> 空文字列ならVisible、そうでなければCollapsedにするコンバータ </summary>
    public class StringEmptyToVisibilityConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value switch
            {
                string s => string.IsNullOrWhiteSpace(s) ? Visibility.Visible : Visibility.Collapsed,
                _ => Visibility.Collapsed,
            };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
