using System;
using System.Globalization;
using System.Windows.Data;

namespace Baku.VMagicMirrorConfig.View
{
    public class WhiteSpaceStringToNullConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value switch
            {
                //""をnullにするのがポイント
                string s when string.IsNullOrEmpty(s) => null,
                string s => s,
                _ => Binding.DoNothing,
            };

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value switch
            {
                string s => s,
                null => null,
                _ => Binding.DoNothing,
            };
    }
}
