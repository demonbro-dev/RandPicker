using HandyControl.Controls;
using System.Windows;
using System.Windows.Controls;
using ComboBox = System.Windows.Controls.ComboBox;
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
                        AddSettingItem("主题颜色", new ComboBox { ItemsSource = new[] { "浅色", "深色" } });
                        AddSettingItem("字体大小", new Slider { Minimum = 12, Maximum = 24, Value = 14 });
                        // 添加边框颜色设置项
                        _borderColorTextBox = new TextBox { Text = _config.BorderColor };
                        AddSettingItem("边框颜色", _borderColorTextBox);
                        break;
                    case "抽选设置":
                        break;
                    case "RandPicker Labs":
                        break;
                }
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
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
                Width = 490,
                MinWidth = 490,
                VerticalAlignment = VerticalAlignment.Center,
                Style = Application.Current.FindResource("SettingTitleStyle") as Style
            });
            panel.Children.Add(control);
            settingsContent.Children.Add(panel);
        }
    }
}