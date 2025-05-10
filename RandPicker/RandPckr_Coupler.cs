using System.Windows.Media;
using RandPicker.SubModules;

namespace RandPicker
{
    public static class RandPckrCoupler
    {
        public static Color FromHex(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }

        public static string ToHex(Color color)
        {
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        public static AppConfig WithWpfAdaptations(this AppConfig config)
        {
            // 添加其他可能需要转换的属性
            return config;
        }
    }
}