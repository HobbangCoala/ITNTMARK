using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ITNTMARK
{
    public class LogLevelToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var level = value as LogLevel?;
            if (level == null) return Brushes.Black;

            switch (level)
            {
                case LogLevel.Info: return Brushes.Black;
                case LogLevel.Warning: return Brushes.Orange;
                case LogLevel.Error: return Brushes.Red;
                default: return Brushes.Gray;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
