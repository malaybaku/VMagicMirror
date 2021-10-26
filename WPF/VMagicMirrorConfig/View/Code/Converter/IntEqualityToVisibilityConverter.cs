using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Baku.VMagicMirrorConfig
{
    public class IntEqualityToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int x &&
                parameter is int y
                )
            {
                return (x == y) ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                return Binding.DoNothing;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
