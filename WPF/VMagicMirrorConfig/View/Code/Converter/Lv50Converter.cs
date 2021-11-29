using System;
using System.Globalization;
using System.Windows.Data;

namespace Baku.VMagicMirrorConfig
{
    /// <summary> 0 ~ 50の整数値を0.0 ~ 1.0のdoubleに変換するコンバータ </summary>
    public class Lv50Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is int i))
            {
                return Binding.DoNothing;
            }

            return Math.Clamp(i * 0.02, 0, 1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
