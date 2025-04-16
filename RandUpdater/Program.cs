using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RandUpdater
{
    class Program
    {
        class DownloadProgress
        {
            public long TotalBytes;
            public long BytesDownloaded;
            public DateTime StartTime;
        }
        static readonly HttpClient client = CreateHttpClient();
        static readonly string versionFile = "version.txt";
        const string TARGET_ZIP_NAME = "RandPicker.zip";

        enum SourcePlatform { GitHub, Gitee }

        static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                MaxConnectionsPerServer = 4
            };
            return new HttpClient(handler);
        }

        static async Task Main(string[] args)
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; WOW64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.6788.76 Safari/537.36");
            client.Timeout = TimeSpan.FromSeconds(30);

            try
            {
                Console.WriteLine("##################################\n#\n# RandUpdater v2\n# Copyright 2025 demonbro. All Rights Reserved.\n#\n##################################\n");

                var defaultRepoUrl = "https://gitee.com/demonbro-dev/RandPicker";
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

                        SourcePlatform selectedSource = SourcePlatform.Gitee;
                        var key = Console.ReadKey();
                        if (key.KeyChar == '2')
                            selectedSource = SourcePlatform.Gitee;
                        else if (key.KeyChar != '1')
                            Console.WriteLine("\n无效输入，默认使用Gitee源");

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
            string tempFile = null;
            var progress = new DownloadProgress { StartTime = DateTime.Now };
            try
            {
                bool supportsRange = await CheckRangeSupport(downloadUrl);
                tempFile = supportsRange
                    ? await DownloadWithMultithread(downloadUrl, progress)
                    : await DownloadWithSingleThread(downloadUrl, progress);
            }
            catch
            {
                if (tempFile != null && File.Exists(tempFile)) File.Delete(tempFile);
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
                if (File.Exists(tempFile)) File.Delete(tempFile);
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

        static async Task<bool> CheckRangeSupport(string url)
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            var response = await client.SendAsync(request);
            return response.Headers.AcceptRanges.Contains("bytes");
        }

        static async Task<string> DownloadWithMultithread(string downloadUrl, DownloadProgress progress)
        {
            const int CHUNKS = 4;
            var tempFile = Path.GetTempFileName();
            long totalSize;

            try
            {
                using (var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    totalSize = response.Content.Headers.ContentLength ?? 0;
                }

                if (totalSize == 0) throw new Exception("无法获取文件大小");
                progress.TotalBytes = totalSize;

                using (var file = File.OpenWrite(tempFile))
                {
                    file.SetLength(totalSize);
                }

                var chunkSize = totalSize / CHUNKS;
                var tasks = new List<Task>();

                for (int i = 0; i < CHUNKS; i++)
                {
                    long start = i * chunkSize;
                    long end = (i == CHUNKS - 1) ? totalSize - 1 : start + chunkSize - 1;
                    tasks.Add(DownloadChunk(downloadUrl, tempFile, start, end, progress));
                }

                // 启动进度显示
                var progressTask = DisplayProgress(progress);
                await Task.WhenAll(tasks);
                await progressTask; // 等待进度条完成

                return tempFile;
            }
            catch
            {
                File.Delete(tempFile);
                throw;
            }
        }
        static async Task DownloadChunk(string url, string filePath, long start, long end, DownloadProgress progress)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.Write)
            {
                Position = start
            };

            var buffer = new byte[512 * 1024];
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                Interlocked.Add(ref progress.BytesDownloaded, bytesRead);
            }
        }
        static async Task<string> DownloadWithSingleThread(string downloadUrl, DownloadProgress progress)
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                progress.TotalBytes = response.Content.Headers.ContentLength ?? 0;
                if (progress.TotalBytes == 0) throw new Exception("无法获取文件大小");

                using var stream = await response.Content.ReadAsStreamAsync();
                using var fileStream = File.Create(tempFile);

                // 启动进度显示
                var progressTask = DisplayProgress(progress);

                var buffer = new byte[512 * 1024];
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    Interlocked.Add(ref progress.BytesDownloaded, bytesRead);
                }

                await progressTask; // 等待进度条完成
                return tempFile;
            }
            catch
            {
                File.Delete(tempFile);
                throw;
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

        static async Task DisplayProgress(DownloadProgress progress)
        {
            const int PROGRESS_WIDTH = 50;
            var startLeft = Console.CursorLeft;
            var startTop = Console.CursorTop;

            try
            {
                while (progress.BytesDownloaded < progress.TotalBytes)
                {
                    var elapsed = DateTime.Now - progress.StartTime;
                    var speed = progress.BytesDownloaded / elapsed.TotalSeconds;

                    // 计算进度百分比
                    var percent = (double)progress.BytesDownloaded / progress.TotalBytes;
                    var progressBar = new StringBuilder("[");

                    // 构建进度条
                    int pos = (int)(percent * PROGRESS_WIDTH);
                    progressBar.Append('#', pos);
                    progressBar.Append('-', PROGRESS_WIDTH - pos);
                    progressBar.Append($"] {percent:P1}");

                    // 构建速度信息
                    var speedInfo = $"{FormatBytes(speed)}/s | {FormatBytes(progress.TotalBytes)}";

                    // 组合输出
                    var totalWidth = Console.WindowWidth - 1;
                    var progressLine = $"{progressBar} {speedInfo}";
                    if (progressLine.Length > totalWidth)
                        progressLine = progressLine.Substring(0, totalWidth);

                    Console.SetCursorPosition(startLeft, startTop);
                    Console.Write(progressLine.PadRight(totalWidth));

                    await Task.Delay(100);
                }

                // 下载完成后显示完整进度条
                var finalLine = $"[{new string('#', PROGRESS_WIDTH)}] 100% | {FormatBytes(progress.TotalBytes)}";
                Console.SetCursorPosition(startLeft, startTop);
                Console.Write(finalLine.PadRight(Console.WindowWidth - 1));
                Console.WriteLine();
            }
            catch
            {
                // 防止控制台异常中断程序
            }
        }

        // 字节格式化方法
        static string FormatBytes(double bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            int unitIndex = 0;

            while (bytes >= 1024 && unitIndex < units.Length - 1)
            {
                bytes /= 1024;
                unitIndex++;
            }

            return $"{bytes:0.##} {units[unitIndex]}";
        }
        record ReleaseInfo(string TagName);
    }
}