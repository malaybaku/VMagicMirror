using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Baku.VMagicMirrorConfig.View
{
    public class IntegerEqualityToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is int i))
            {
                return Binding.DoNothing;
            }

            if (parameter is string s &&
                int.TryParse(s, out int v1))
            {
                return (i == v1) ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (parameter is int v2)
            {
                return (i == v2) ? Visibility.Visible : Visibility.Collapsed;
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
