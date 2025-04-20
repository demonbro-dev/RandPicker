using System.Windows;
using System.Windows.Controls;

namespace RandPicker.Management
{
    using demonbro.UniLibs;
    using demonbro.UniLibs.Cryptography;
    using RandPicker.Input;
    using RandPicker;
    using System.IO;
    using System.Windows.Media;
    using static demonbro.UniLibs.AppConfig;
    using Newtonsoft.Json;

    public partial class Settings : Window
    {
        private string _configPath;
        private AppConfig _config;
        private TextBox _borderColorTextBox;
        private ComboBox _defaultPageComboBox;
        private CheckBox _rsaEncryptCheckBox;
        public PickerLogic PickerLogic { get; set; }

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
                            Margin = new Thickness(5, 0, 0, 0),
                            ToolTip = "点击打开颜色选单"
                        };
                        colorPreview.Click += ColorPreview_Click;

                        colorPanel.Children.Add(_borderColorTextBox);
                        colorPanel.Children.Add(colorPreview);

                        AddSettingItem("边框颜色", colorPanel);

                        _defaultPageComboBox = new ComboBox
                        {
                            ItemsSource = new[] { "主页面", "多抽选模式" },
                            SelectedIndex = (int)_config.DefaultPage,
                            Width = 150
                        };
                        AddSettingItem("启动时默认页面", _defaultPageComboBox);
                        break;
                    case "抽选设置":
                        break;
                    case "RandPicker Labs":
                        _rsaEncryptCheckBox = new CheckBox
                        {
                            IsChecked = _config.UseRSAEncryption,
                            Content = "启用RSA4096加密",
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        AddSettingItem("名单加密", _rsaEncryptCheckBox);

                        // 添加密钥生成按钮
                        var keyGenButton = new Button
                        {
                            Content = "生成新密钥",
                            Margin = new Thickness(5, 0, 0, 0)
                        };
                        if (_rsaEncryptCheckBox.IsChecked == true)
                        {
                            keyGenButton.IsEnabled = false;
                        }
                        else 
                        {
                            keyGenButton.IsEnabled = true;
                        }
                            keyGenButton.Click += KeyGenButton_Click;
                        AddSettingItem("密钥管理", keyGenButton);
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
        private void KeyGenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 生成新密钥对
                var (publicKey, privateKey) = GenerateRSAKey.Generate(4096);
                File.WriteAllText("private.pem", privateKey);
                File.WriteAllText("public.pem", publicKey);
                MessageBox.Show("已生成新的RSA密钥对");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作失败：{ex.Message}");
            }
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            bool originalEncryptionState = _config.UseRSAEncryption;
            bool newEncryptionState = _rsaEncryptCheckBox.IsChecked ?? false;

            try
            {
                // 保存配置（包含加密状态）
                _config.UseRSAEncryption = newEncryptionState;
                _config.DefaultPage = (DefaultPageMode)_defaultPageComboBox.SelectedIndex;
                ConfigurationManager.SaveConfig(_config, _configPath);

                // 如果加密状态发生改变，触发文件格式转换
                if (originalEncryptionState != newEncryptionState)
                {
                    string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "namelist.json");
                    string backupPath = jsonPath + ".bak";

                    // 备份原文件
                    File.Copy(jsonPath, backupPath, true);

                    try
                    {
                        string data;
                        if (File.Exists(jsonPath))
                        {
                            // 根据旧状态读取数据
                            if (originalEncryptionState)
                            {
                                var privateKey = File.ReadAllText("private.pem");
                                data = RSAKeyProcessor.Decrypt(File.ReadAllText(jsonPath), privateKey);
                            }
                            else
                            {
                                data = File.ReadAllText(jsonPath);
                            }

                            // 根据新状态写入数据
                            if (newEncryptionState)
                            {
                                var publicKey = File.ReadAllText("public.pem");
                                string encrypted = RSAKeyProcessor.Encrypt(data, publicKey);
                                File.WriteAllText(jsonPath, encrypted);
                            }
                            else
                            {
                                File.WriteAllText(jsonPath, data);
                            }

                            File.Delete(backupPath); // 成功后删除备份
                        }
                    }
                    catch (Exception ex)
                    {
                        // 恢复备份并提示错误
                        File.Copy(backupPath, jsonPath, true);
                        MessageBox.Show($"加密状态切换失败：{ex.Message}\n已恢复原文件", "错误");
                        e.Cancel = true;
                        return;
                    }
                }

                // 更新主窗口边框颜色（原有逻辑）
                if (Owner is MainWindow mainWindow)
                {
                    try
                    {
                        var color = UniLibsAdapter.FromHex(_config.BorderColor);
                        mainWindow.BorderColor = new SolidColorBrush(color);
                    }
                    catch { /* 忽略无效颜色值 */ }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存配置失败：{ex.Message}");
                e.Cancel = true;
                return;
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