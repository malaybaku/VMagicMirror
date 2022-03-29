using System;
using System.Globalization;
using System.Windows.Data;

namespace Baku.VMagicMirrorConfig.View
{
    public class FileIdIndicationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string s)
            {
                return Binding.DoNothing;
            }

            // "HogeFolder>"みたいな記法だとしっくり来ないので、"HogeFolder/"に直す
            if (s.EndsWith(AccessoryItemSetting.FolderIdSuffixChar))
            {
                return s.TrimEnd(AccessoryItemSetting.FolderIdSuffixChar) + "/";
            }
            else
            {
                return s;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
