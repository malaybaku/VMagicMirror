using System;
using System.Globalization;
using System.Windows.Data;

namespace Baku.VMagicMirrorConfig.View
{
    public class FloatValueScaleConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not float or double)
            {
                return Binding.DoNothing;
            }

            var scale = parameter switch
            {
                float f => f,
                double d => (float)d,
                string s => float.TryParse(s, out var strScale) ? strScale : 1f,
                _ => 1f,
            };

            return (float)value * scale;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
