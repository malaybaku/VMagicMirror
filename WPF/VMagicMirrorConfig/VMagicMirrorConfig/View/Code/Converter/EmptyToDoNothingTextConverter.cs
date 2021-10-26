using System;
using System.Globalization;
using System.Windows.Data;

namespace Baku.VMagicMirrorConfig
{
    public class EmptyToDoNothingTextConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 || !(values[0] is string mainValue) || !(values[1] is string))
            {
                return Binding.DoNothing;
            }

            //NOTE: values[1]には言語名が入ってる想定だが、Localized.GetStringで間接的に使うので直接参照しないでよい
            return string.IsNullOrEmpty(mainValue)
                ? LocalizedString.GetString("CommonUi_DoNothing")
                : mainValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => new object[] { Binding.DoNothing };
    }
}
