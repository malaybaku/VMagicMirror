using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Baku.VMagicMirrorConfig
{
    public class BooleanToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool b))
            {
                return Binding.DoNothing;
            }

            return b ? Brushes.Black : Brushes.LightGray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
