//
//
// About.xaml.cs : RandPicker 关于页面
//
//
using System.Windows;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Navigation;

namespace RandPicker
{
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();

            // 动态设置版本号信息，不用在xaml里手动改
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            versionTextBlock.Text = $"RandPicker v{version.Major}.{version.Minor}";
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}