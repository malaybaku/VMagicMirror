using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Baku.VMagicMirrorConfig
{
    public class TabSelectionToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.WriteLine($"value type == {value.GetType()}, {value}, param type == {parameter.GetType()}, {parameter}");
            if (value is not int v)
            {
                return Binding.DoNothing;
            }

            int param;
            if (parameter is int x)
            {
                param = x;
            }
            else if (parameter is string s && int.TryParse(s, out var y))
            {
                param = y;
            }
            else
            {
                return Binding.DoNothing;
            }

            //NOTE: 呼び出し回数が多すぎるような…
            var key = (v == param) ? "SelectedTabItemBackground" : "UnselectedTabItemBackground";
            return (Application.Current.Resources[key] as SolidColorBrush) ?? Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
