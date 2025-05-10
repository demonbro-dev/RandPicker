using System;
using System.IO;
using System.Text.Json;

namespace RandPicker.SubModules
{
    public static class ConfigurationManager
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static AppConfig LoadConfig(string configPath)
        {
            try
            {
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    return JsonSerializer.Deserialize<AppConfig>(json, Options);
                }

                // 创建默认配置并保存
                var newConfig = new AppConfig();
                var directory = Path.GetDirectoryName(configPath);

                // 确保目录存在
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                SaveConfig(newConfig, configPath);
                return newConfig;
            }
            catch { /* 异常处理 */ }

            return new AppConfig();
        }

        public static void SaveConfig(AppConfig config, string configPath)
        {
            try
            {
                var json = JsonSerializer.Serialize(config, Options);
                File.WriteAllText(configPath, json);
            }
            catch { /* 异常处理 */ }
        }
    }
}