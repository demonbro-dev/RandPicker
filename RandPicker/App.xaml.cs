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
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            var config = demonbro.UniLibs.ConfigurationManager.LoadConfig(configPath);

            if (config.UseRSAEncryption)
            {
                var keyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "private.pem");
                if (!File.Exists(keyPath))
                {
                    MessageBox.Show("加密功能已启用但未找到密钥文件，请先生成密钥");
                    config.UseRSAEncryption = false;
                    demonbro.UniLibs.ConfigurationManager.SaveConfig(config, configPath);
                }
            }

            base.OnStartup(e);
        }
    }

}
