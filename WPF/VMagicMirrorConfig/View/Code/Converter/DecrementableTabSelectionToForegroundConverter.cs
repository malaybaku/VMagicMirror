using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Baku.VMagicMirrorConfig.View
{
    public class DecrementableTabSelectionToForegroundConverter : IMultiValueConverter
    {
        //「自身より手前のタブが1つ隠れる事がある」…というのを踏まえてindex調整しつつハイライト表示してくれるやつ。
        //
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not int selectedIndex || values[1] is not bool tabShown)
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

            //タブが隠れる == 比較すべきタブIndexが減る
            if (!tabShown)
            {
                param--;
            }

            //NOTE: 呼び出し回数が多すぎるような…
            var key = (selectedIndex == param) ? "SelectedTabItemForeground" : "UnselectedTabItemForeground";
            return (Application.Current.Resources[key] as SolidColorBrush) ?? Binding.DoNothing;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => Array.Empty<object>();
    }
}
