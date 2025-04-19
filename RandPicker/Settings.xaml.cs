using HandyControl.Controls;
using System.Windows;
using System.Windows.Controls;
using ComboBox = System.Windows.Controls.ComboBox;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;
using Window = System.Windows.Window;

namespace RandPicker
{
    using demonbro.UniLibs; // 添加命名空间引用
    using System.Windows.Media;

    public partial class Settings : Window
    {
        private string _configPath;
        private AppConfig _config;
        private TextBox _borderColorTextBox;

        public Settings(string configPath)
        {
            InitializeComponent();
            _configPath = configPath;
            _config = ConfigurationManager.LoadConfig(_configPath);
            categoryList.SelectedIndex = 0;
        }

        private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (categoryList.SelectedItem is string selectedCategory)
            {
                settingsContent.Children.Clear();

                switch (selectedCategory)
                {
                    case "界面设置":
                        var colorPanel = new StackPanel { Orientation = Orientation.Horizontal };
                        _borderColorTextBox = new TextBox { Text = _config.BorderColor, Width = 100 };
                        var colorPreview = new Button
                        {
                            Content = "■",
                            Foreground = new SolidColorBrush(UniLibsAdapter.FromHex(_config.BorderColor)),
                            Width = 50,
                            FontSize = 14,
                            Margin = new Thickness(5, 0, 0, 0)
                        };
                        colorPreview.Click += ColorPreview_Click;

                        colorPanel.Children.Add(_borderColorTextBox);
                        colorPanel.Children.Add(colorPreview);

                        AddSettingItem("边框颜色", colorPanel);
                        break;
                    case "抽选设置":
                        break;
                    case "RandPicker Labs":
                        break;
                }
            }
        }
        private void ColorPreview_Click(object sender, RoutedEventArgs e)
        {
            var picker = new ColorPickerWindow(_borderColorTextBox.Text)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            if (picker.ShowDialog() == true)
            {
                _borderColorTextBox.Text = picker.SelectedColor;
                if (sender is Button btn)
                {
                    btn.Foreground = new SolidColorBrush(ParseColor(picker.SelectedColor));
                }
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

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                var color = ParseColor(_borderColorTextBox.Text);
                _config.BorderColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            }
            catch
            {
                MessageBox.Show("颜色格式无效，请使用#RRGGBB格式");
                e.Cancel = true;
                return;
            }
            // 保存配置
            _config.BorderColor = _borderColorTextBox.Text;
            ConfigurationManager.SaveConfig(_config, _configPath);

            // 更新主窗口边框颜色
            if (Owner is MainWindow mainWindow)
            {
                try
                {
                    var color = UniLibsAdapter.FromHex(_config.BorderColor);
                    mainWindow.BorderColor = new SolidColorBrush(color);
                }
                catch { /* 忽略无效颜色值 */ }
            }

            base.OnClosing(e);
        }
        private void AddSettingItem(string title, FrameworkElement control)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
            panel.Children.Add(new TextBlock
            {
                Text = title,
                Width = 410,
                MinWidth = 410,
                VerticalAlignment = VerticalAlignment.Center,
                Style = Application.Current.FindResource("SettingTitleStyle") as Style
            });
            panel.Children.Add(control);
            settingsContent.Children.Add(panel);
        }
    }
}