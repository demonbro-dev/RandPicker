using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace RandUpdater
{
    class Program
    {
        static readonly HttpClient client = new HttpClient();
        static readonly string versionFile = "version.txt";
        const string TARGET_ZIP_NAME = "RandPicker.zip";

        enum SourcePlatform { GitHub, Gitee }

        static async Task Main(string[] args)
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; WOW64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.6788.76 Safari/537.36");
            client.Timeout = TimeSpan.FromSeconds(30);

            try
            {
                Console.WriteLine("##################################\n#\n# RandUpdater V1\n# Copyright 2025 demonbro. All Rights Reserved.\n#\n##################################\n");

                var defaultRepoUrl = "https://github.com/demonbro-dev/RandPicker";
                var (owner, repo, source) = ParseRepositoryUrl(defaultRepoUrl);

                var latestRelease = await GetLatestRelease(owner, repo, source);
                Version currentVersion = GetCurrentVersion();
                Version latestVersion = new Version(latestRelease.TagName.TrimStart('v', 'V'));

                Console.WriteLine($"当前版本: {currentVersion}");
                Console.WriteLine($"{source} 最新版本: {latestVersion}");

                if (latestVersion > currentVersion)
                {
                    Console.Write("\n发现新版本，是否要更新？(Y/N): ");
                    if (Console.ReadKey().Key == ConsoleKey.Y)
                    {
                        Console.WriteLine("\n\n请选择下载源：");
                        Console.WriteLine("1. GitHub（国际源，可能较慢）");
                        Console.WriteLine("2. Gitee（国内镜像，速度更快）");
                        Console.Write("请输入选项（1/2）: ");

                        SourcePlatform selectedSource = SourcePlatform.GitHub;
                        var key = Console.ReadKey();
                        if (key.KeyChar == '2')
                            selectedSource = SourcePlatform.Gitee;
                        else if (key.KeyChar != '1')
                            Console.WriteLine("\n无效输入，默认使用GitHub源");

                        string newRepoUrl = selectedSource == SourcePlatform.GitHub
                            ? "https://github.com/demonbro-dev/RandPicker"
                            : "https://gitee.com/demonbro-dev/RandPicker";

                        var (newOwner, newRepo, newSource) = ParseRepositoryUrl(newRepoUrl);

                        var newRelease = await GetLatestRelease(newOwner, newRepo, newSource);
                        Version newLatestVersion = new Version(newRelease.TagName.TrimStart('v', 'V'));

                        string downloadUrl = newSource switch
                        {
                            SourcePlatform.GitHub => $"https://github.com/{newOwner}/{newRepo}/releases/download/{newRelease.TagName}/{TARGET_ZIP_NAME}",
                            SourcePlatform.Gitee => $"https://gitee.com/{newOwner}/{newRepo}/releases/download/{newRelease.TagName}/{TARGET_ZIP_NAME}",
                            _ => throw new NotSupportedException("不支持的平台")
                        };

                        Console.WriteLine($"\n开始从{newSource}下载更新...");
                        await DownloadAndUpdate(downloadUrl);
                        UpdateVersionFile(newLatestVersion);
                        Console.WriteLine("更新完成！");
                    }
                }
                else
                {
                    Console.WriteLine("\n已经是最新版本！");
                }

                Console.Write("\n按任意键退出程序...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n发生错误: {ex.Message}");
                Console.Write("\n按任意键退出程序...");
                Console.ReadKey();
            }
        }

        static (string owner, string repo, SourcePlatform source) ParseRepositoryUrl(string url)
        {
            var uri = new Uri(url.TrimEnd('/'));
            if (uri.Segments.Length < 3)
                throw new ArgumentException("无效的仓库地址格式");

            var source = uri.Host switch
            {
                "github.com" => SourcePlatform.GitHub,
                "gitee.com" => SourcePlatform.Gitee,
                _ => throw new ArgumentException("不支持的仓库平台")
            };

            return (
                owner: uri.Segments[1].Trim('/'),
                repo: uri.Segments[2].Trim('/'),
                source: source
            );
        }

        static async Task<ReleaseInfo> GetLatestRelease(string owner, string repo, SourcePlatform source)
        {
            string apiUrl = source switch
            {
                SourcePlatform.GitHub => $"https://api.github.com/repos/{owner}/{repo}/releases/latest",
                SourcePlatform.Gitee => $"https://gitee.com/api/v5/repos/{owner}/{repo}/releases/latest",
                _ => throw new ArgumentException("无效的软件源")
            };

            var response = await client.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();

            using var jsonDoc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var root = jsonDoc.RootElement;
            return new ReleaseInfo(
                TagName: root.GetProperty("tag_name").GetString()
            );
        }

        static async Task DownloadAndUpdate(string downloadUrl)
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                using (var stream = await client.GetStreamAsync(downloadUrl))
                using (var fileStream = File.Create(tempFile))
                    await stream.CopyToAsync(fileStream);
            }
            catch
            {
                File.Delete(tempFile);
                throw;
            }

            var parentDir = Directory.GetParent(Environment.CurrentDirectory).FullName;
            var namelistPath = Path.Combine(parentDir, "RandPicker", "namelist.json");
            string tempBackupPath = null;

            if (File.Exists(namelistPath))
            {
                tempBackupPath = Path.GetTempFileName();
                File.Copy(namelistPath, tempBackupPath, overwrite: true);
            }

            bool extractSuccess = false;
            try
            {
                ZipFile.ExtractToDirectory(tempFile, parentDir, overwriteFiles: true);
                extractSuccess = true;
            }
            finally
            {
                File.Delete(tempFile);
            }

            if (extractSuccess && tempBackupPath != null)
            {
                try
                {
                    File.Copy(tempBackupPath, namelistPath, overwrite: true);
                }
                finally
                {
                    File.Delete(tempBackupPath);
                }
            }
        }

        static void UpdateVersionFile(Version version)
        {
            string versionString = version.ToString();
            if (version.Revision != -1)
                versionString = version.ToString(4);
            else if (version.Build != -1)
                versionString = version.ToString(3);

            File.WriteAllText(versionFile, versionString);
        }

        static Version GetCurrentVersion()
        {
            try
            {
                return File.Exists(versionFile)
                    ? Version.Parse(File.ReadAllText(versionFile))
                    : new Version(0, 0);
            }
            catch
            {
                return new Version(0, 0);
            }
        }
    }

    record ReleaseInfo(string TagName);
}