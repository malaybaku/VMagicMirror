using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Baku.VMagicMirrorConfig
{
    public class BooleanReverseToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool b))
            {
                return Binding.DoNothing;
            }

            return b ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
