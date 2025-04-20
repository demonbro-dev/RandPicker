using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace RandPicker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            var config = demonbro.UniLibs.ConfigurationManager.LoadConfig(configPath);

            // 检查加密状态与密钥是否匹配
            if (config.UseRSAEncryption)
            {
                string privateKeyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "private.pem");
                if (!File.Exists(privateKeyPath))
                {
                    MessageBox.Show("加密已启用但缺少私钥，已强制关闭加密功能");
                    config.UseRSAEncryption = false;
                    demonbro.UniLibs.ConfigurationManager.SaveConfig(config, configPath);
                }
            }
        }
    }
}
