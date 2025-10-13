using System;
using System.Globalization;
using System.Windows.Data;

namespace Baku.VMagicMirrorConfig.View
{
    public class AvailableFramerateToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not TargetFramerateStyles style)
            {
                return Binding.DoNothing;
            }

            return style switch
            {
                TargetFramerateStyles.Fixed60 => "60",
                TargetFramerateStyles.Fixed30 => "30",
                TargetFramerateStyles.UseVSync => LocalizedString.GetString("ImageQuality_Framerate_UseVSync"),
                _ => "",
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
