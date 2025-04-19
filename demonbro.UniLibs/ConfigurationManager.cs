using System;
using System.IO;
using System.Text.Json;

namespace demonbro.UniLibs
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