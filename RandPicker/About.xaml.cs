using System.Windows;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Navigation;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.IO;

namespace RandPicker
{
    public partial class About : Window
    {
        private bool _isLicenseShown = false;
        public About()
        {
            InitializeComponent();

            // 动态设置版本号信息，不用在xaml里手动改
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            versionTextBlock.Text = $"RandPicker v{version.Major}.{version.Minor}.{version.Build}";

            LoadLicenseText();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
        private void LicenseButton_Click(object sender, RoutedEventArgs e)
        {
            var transform = LicensePanel.RenderTransform as TranslateTransform;
            if (transform == null) return;

            // 显示时先设置可见性
            if (!_isLicenseShown)
            {
                LicensePanel.Visibility = Visibility.Visible;
            }

            DoubleAnimation animation = new DoubleAnimation
            {
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            if (!_isLicenseShown)
            {
                animation.From = 150;
                animation.To = 0;
            }
            else
            {
                animation.From = 0;
                animation.To = 150;
                // 动画完成后隐藏
                animation.Completed += (s, _) =>
                {
                    LicensePanel.Visibility = Visibility.Collapsed;
                };
            }

            transform.BeginAnimation(TranslateTransform.YProperty, animation);
            _isLicenseShown = !_isLicenseShown;
        }
        private void LoadLicenseText()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = $"{assembly.GetName().Name}.Resources.ThirdPartyLicense.txt";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            LicenseTextBlock.Text = reader.ReadToEnd();
                        }
                    }
                    else
                    {
                        LicenseTextBlock.Text = "未找到许可证文件";
                    }
                }
            }
            catch (Exception ex)
            {
                LicenseTextBlock.Text = $"加载失败: {ex.Message}";
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}