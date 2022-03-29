using System;
using System.Globalization;
using System.Windows.Data;

namespace Baku.VMagicMirrorConfig.View
{
    public class BooleanToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b)
                ? (b ? 1.0f : 0.5f)
                : 1.0f;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
