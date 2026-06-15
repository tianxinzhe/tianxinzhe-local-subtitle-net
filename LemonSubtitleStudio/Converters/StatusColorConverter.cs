
using LemonSubtitleStudio.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LemonSubtitleStudio.Converters
{
    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskStatus status)
            {
                return status switch
                {
                    TaskStatus.Waiting => new SolidColorBrush(Color.FromRgb(160, 160, 176)),
                    TaskStatus.Processing => new SolidColorBrush(Color.FromRgb(255, 165, 0)),
                    TaskStatus.Completed => new SolidColorBrush(Color.FromRgb(0, 200, 83)),
                    TaskStatus.Failed => new SolidColorBrush(Color.FromRgb(255, 100, 100)),
                    _ => new SolidColorBrush(Color.FromRgb(160, 160, 176))
                };
            }
            return new SolidColorBrush(Color.FromRgb(160, 160, 176));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
