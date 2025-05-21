using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Baku.VMagicMirrorConfig.View
{
    public class BuddyLogLevelToForegroundConverter : IValueConverter
    {
        private static readonly SolidColorBrush InfoColor = new(Color.FromRgb(0x1F, 0x29, 0x37));
        private static readonly SolidColorBrush WarningColor = new(Color.FromRgb(0xFF, 0x99, 0x00));
        private static readonly SolidColorBrush ErrorColor = new(Color.FromRgb(0xFF, 0x28, 0x00));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not BuddyLogMessage message)
            {
                return Binding.DoNothing;
            }

            switch (message.Level)
            {
                case BuddyLogLevel.Fatal:
                case BuddyLogLevel.Error:
                    return ErrorColor;
                case BuddyLogLevel.Warning:
                    return WarningColor;
                default:
                    return InfoColor;
            }            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
