// NegativeConverter.cs
using System;
using System.Windows.Data;

namespace RandPicker
{
    public class NegativeValueConverter : IValueConverter
    {
        // 将输入值转换为负数
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double width)
            {
                return -width; // 例如，800 → -800
            }
            return 0;
        }

        // 不需要反向转换，此处直接抛出异常
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}