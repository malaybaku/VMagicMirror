using System;
using System.Globalization;
using System.Windows.Data;

namespace Baku.VMagicMirrorConfig.View
{
    public class AccessoryResolutionLimitToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not AccessoryImageResolutionLimit limit)
            {
                return Binding.DoNothing;
            }

            return limit switch
            {
                AccessoryImageResolutionLimit.None => LocalizedString.GetString("Accessory_Item_MaxTextureSize_Unlimited"),
                AccessoryImageResolutionLimit.Max1024 => "1024px",
                AccessoryImageResolutionLimit.Max512 => "512px",
                AccessoryImageResolutionLimit.Max256 => "256px",
                AccessoryImageResolutionLimit.Max128 => "128px",
                _ => "(unknown)",
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
