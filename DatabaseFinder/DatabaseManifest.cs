using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DatabaseFinder
{
    public class ManifestEntry
    {
        public string Path { get; set; } = "";
        public string Sha256 { get; set; } = "";
        public long Size { get; set; }
        public DateTime ModifiedUtc { get; set; }
    }

    public static class ManifestGenerator
    {
        public const string ToolVersion = "1.8.1";

        public static string Sha256File(string path)
        {
            using var fs = File.OpenRead(path);
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(fs);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>
        /// تمام فایل‌های پوشه root را اسکن کرده و دو فایل مانیفست می‌سازد:
        /// <c>manifest.txt</c> (hash + مسیر — قابل چاپ برای صورت‌جلسه) و <c>manifest.json</c> (ساختاریافته).
        /// خروجی: مسیر فایل متنی.
        /// </summary>
        public static string Generate(string rootFolder, string? outputBase = null)
        {
            outputBase ??= Path.Combine(rootFolder, "manifest");

            var entries = new List<ManifestEntry>();
            if (Directory.Exists(rootFolder))
            {
                foreach (var file in Directory.EnumerateFiles(rootFolder, "*", SearchOption.AllDirectories))
                {
                    string rel;
                    try { rel = Path.GetRelativePath(rootFolder, file); }
                    catch { rel = file; }

                    long size = 0;
                    DateTime modified = DateTime.MinValue;
                    try
                    {
                        var fi = new FileInfo(file);
                        size = fi.Length;
                        modified = fi.LastWriteTimeUtc;
                    }
                    catch (Exception ex) { AppLog.Write("Manifest.FileInfo: " + file, ex); }

                    string sha256;
                    try
                    {
                        sha256 = Sha256File(file);
                    }
                    catch (Exception ex)
                    {
                        AppLog.Write("Manifest.Hash: " + file, ex);
                        throw;
                    }

                    entries.Add(new ManifestEntry
                    {
                        Path = rel,
                        Sha256 = sha256,
                        Size = size,
                        ModifiedUtc = modified
                    });
                }
            }

            entries = entries.OrderBy(e => e.Path, StringComparer.OrdinalIgnoreCase).ToList();

            var txtPath = outputBase + ".txt";
            var sb = new StringBuilder();
            sb.AppendLine("Database Finder - Manifest (SHA-256)");
            sb.AppendLine($"Version: {ToolVersion}");
            sb.AppendLine($"Machine: {Environment.MachineName}");
            sb.AppendLine($"Created (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Root: {Path.GetFullPath(rootFolder)}");
            sb.AppendLine($"Files: {entries.Count} | Total: {FormatBytes(entries.Sum(e => e.Size))}");
            sb.AppendLine();
            sb.AppendLine("SHA256                                                          Path");
            sb.AppendLine("----------------------------------------------------------------------------------------------------------------------------");
            foreach (var e in entries)
            {
                sb.AppendLine($"{e.Sha256}  {e.Path}");
            }
            File.WriteAllText(txtPath, sb.ToString(), new UTF8Encoding(false));

            var json = new
            {
                tool = "Database Finder",
                version = ToolVersion,
                machine = Environment.MachineName,
                user = Environment.UserName,
                root = Path.GetFullPath(rootFolder),
                createdUtc = DateTime.UtcNow,
                files = entries,
                totalFiles = entries.Count,
                totalBytes = entries.Sum(e => e.Size)
            };
            var jsonPath = outputBase + ".json";
            File.WriteAllText(jsonPath,
                JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true }),
                new UTF8Encoding(false));

            return txtPath;
        }

        public static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            var unit = 0;
            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }
            return $"{size:0.#} {units[unit]}";
        }
    }
}
