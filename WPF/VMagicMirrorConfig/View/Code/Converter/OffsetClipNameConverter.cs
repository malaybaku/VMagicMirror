using System;
using System.Globalization;
using System.Windows.Data;

namespace Baku.VMagicMirrorConfig.View
{
    public class OffsetClipNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string s)
            {
                return Binding.DoNothing;
            }

            if (string.IsNullOrEmpty(s))
            {
                return LocalizedString.GetString("CommonUi_None");
            }

            return s.Replace("\t", ", ");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
