using System;
using System.Globalization;
using System.Windows.Data;

namespace Baku.VMagicMirrorConfig.View
{
    public class AntiAliasStyleToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not AntiAliasStyles style)
            {
                return Binding.DoNothing;
            }

            return style switch
            {
                AntiAliasStyles.None => LocalizedString.GetString("ImageQuality_AntiAlias_None"),
                AntiAliasStyles.Low => LocalizedString.GetString("ImageQuality_AntiAlias_Low"),
                AntiAliasStyles.Mid => LocalizedString.GetString("ImageQuality_AntiAlias_Mid"),
                AntiAliasStyles.High => LocalizedString.GetString("ImageQuality_AntiAlias_High"),
                _ => "",
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
