// NegativeConverter.cs
using System;
using System.Windows.Data;

namespace RandPicker
{
    public class NegativeValueConverter : IValueConverter
    {
        // ������ֵת��Ϊ����
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double width)
            {
                return -width; // ���磬800 �� -800
            }
            return 0;
        }

        // ����Ҫ����ת�����˴�ֱ���׳��쳣
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}