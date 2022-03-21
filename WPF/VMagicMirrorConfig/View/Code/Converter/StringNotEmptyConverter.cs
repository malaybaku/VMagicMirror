using System;
using System.Globalization;
using System.Windows.Data;

namespace Baku.VMagicMirrorConfig.View
{
    /// <summary> 空でない文字列のみをtrue、それ以外はfalseにするコンバータ </summary>
    public class StringNotEmptyConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value switch
            {
                string s => !string.IsNullOrWhiteSpace(s),
                _ => false,
            };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
