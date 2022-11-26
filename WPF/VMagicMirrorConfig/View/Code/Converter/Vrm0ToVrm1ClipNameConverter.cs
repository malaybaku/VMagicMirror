using System;
using System.Globalization;
using System.Windows.Data;

namespace Baku.VMagicMirrorConfig.View
{
    public class Vrm0ToVrm1ClipNameConverter : IValueConverter
    {
        //VRM0.xのクリップ名 -> VRM1.0のクリップ名 
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string s)
            {
                return Binding.DoNothing;
            }

            return DefaultBlendShapeNameStore.GetVrm10KeyName(s);
        }

        //VRM1.0のクリップ名 -> VRM0.xのクリップ名 
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string s)
            {
                return Binding.DoNothing;
            }

            return DefaultBlendShapeNameStore.GetVrm0KeyName(s);
        }
    }
}
