using System;
using System.Globalization;
using System.Windows.Data;

namespace Baku.VMagicMirrorConfig.View
{
    /// <summary>
    /// ノート番号を編集不可なテキストに変換するときのコンバータ
    /// </summary>
    public class NoteNumberToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i)
            {
                return (i >= 0) ? i.ToString() : "??";
            }
            else
            {
                return Binding.DoNothing;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
