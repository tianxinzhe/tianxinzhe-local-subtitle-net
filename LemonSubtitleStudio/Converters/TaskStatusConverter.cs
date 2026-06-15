
using LemonSubtitleStudio.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace LemonSubtitleStudio.Converters
{
    public class TaskStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskStatus status)
            {
                return status switch
                {
                    TaskStatus.Waiting => "等待",
                    TaskStatus.Processing => "处理中",
                    TaskStatus.Completed => "完成",
                    TaskStatus.Failed => "失败",
                    _ => string.Empty
                };
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
