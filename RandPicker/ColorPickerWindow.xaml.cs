// ColorPickerWindow.xaml.cs
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RandPicker
{
    public partial class ColorPickerWindow : Window
    {
        public string SelectedColor { get; private set; }
        private Point _lastMousePos;

        public ColorPickerWindow(string initialColor)
        {
            InitializeComponent();
            colorPreview.Fill = new SolidColorBrush(ParseColor(initialColor));
            CreateColorPalette();
        }
        public static Color HsvToRgb(double hue, double saturation, double value)
        {
            int hi = (int)(hue / 60) % 6;
            double f = hue / 60 - (int)(hue / 60);

            double v = value * 255;
            double p = v * (1 - saturation);
            double q = v * (1 - f * saturation);
            double t = v * (1 - (1 - f) * saturation);

            return hi switch
            {
                0 => Color.FromRgb((byte)v, (byte)t, (byte)p),
                1 => Color.FromRgb((byte)q, (byte)v, (byte)p),
                2 => Color.FromRgb((byte)p, (byte)v, (byte)t),
                3 => Color.FromRgb((byte)p, (byte)q, (byte)v),
                4 => Color.FromRgb((byte)t, (byte)p, (byte)v),
                _ => Color.FromRgb((byte)v, (byte)p, (byte)q)
            };
        }
        private void CreateColorPalette()
        {
            // 清除原有调色板
            paletteCanvas.Children.Clear();

            // 生成360度色相渐变
            for (int hue = 0; hue < 360; hue++)
            {
                Color color = HsvToRgb(hue, saturation: 1.0, value: 1.0);

                var rect = new Rectangle
                {
                    Width = 1,
                    Height = 30,
                    Fill = new SolidColorBrush(color),
                    Tag = color,
                    Cursor = Cursors.Hand
                };
                rect.MouseLeftButtonDown += ColorRect_Click;

                paletteCanvas.Children.Add(rect);
                Canvas.SetLeft(rect, hue);
            }

            // 调整画布尺寸
            paletteCanvas.Width = 360;
            this.Width = 400; // 调整窗口宽度适应新调色板
        }
        private void ColorRect_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Rectangle rect && rect.Tag is Color color)
            {
                SelectedColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                colorPreview.Fill = new SolidColorBrush(color);
                hexTextBox.Text = SelectedColor;
            }
        }

        private Color ParseColor(string hex)
        {
            try
            {
                return (Color)ColorConverter.ConvertFromString(hex);
            }
            catch
            {
                return Colors.Black;
            }
        }
        private void PaletteCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var pos = e.GetPosition(paletteCanvas);
                if (pos.X < 0 || pos.X >= 360) return;

                int hue = (int)pos.X;
                Color color = HsvToRgb(hue, 1.0, 1.0);
                UpdateColorPreview(color);
            }
        }

        private void UpdateColorPreview(Color color)
        {
            colorPreview.Fill = new SolidColorBrush(color);
            hexTextBox.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            SelectedColor = hexTextBox.Text;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}