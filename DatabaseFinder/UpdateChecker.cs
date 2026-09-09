using System.Text;
using System.Reflection;
using System.Text.Json;
using System.Diagnostics;
using System.Security.Cryptography;

namespace DatabaseFinder;

public sealed record UpdateInfo(Version Latest, string Tag, string AssetName, string AssetUrl, string Body)
{
    public bool IsNewer => Latest > UpdateChecker.CurrentVersion;
}

public static class UpdateChecker
{
    public const string Repo = "iranacc/DatabaseFinder";
    public static bool Enabled { get; set; } = true;

    public static Version CurrentVersion { get; } =
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

    public static string VersionString(Version version) => $"{version.Major}.{version.Minor}.{version.Build}";

    public static UpdateInfo? Parse(string json, string runningExeName)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("tag_name", out var tagElement)) return null;
            var tag = tagElement.GetString() ?? "";
            if (!Version.TryParse(tag.TrimStart('v', 'V'), out var version)) return null;
            Asset? chosen = null;
            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                var exeAssets = new List<Asset>();
                foreach (var asset in assets.EnumerateArray())
                {
                    if (!asset.TryGetProperty("name", out var nameElement)) continue;
                    var name = nameElement.GetString() ?? "";
                    if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) continue;
                    var url = asset.TryGetProperty("browser_download_url", out var urlElement)
                        ? urlElement.GetString() ?? ""
                        : "";
                    exeAssets.Add(new Asset(name, url));
                }
                chosen = exeAssets.FirstOrDefault(a => string.Equals(a.Name, runningExeName, StringComparison.OrdinalIgnoreCase))
                    ?? exeAssets.FirstOrDefault(a => string.Equals(a.Name, "DatabaseFinder.exe", StringComparison.OrdinalIgnoreCase))
                    ?? exeAssets.FirstOrDefault();
            }
            if (chosen == null) return null;
            var body = root.TryGetProperty("body", out var bodyElement) ? bodyElement.GetString() ?? "" : "";
            return new UpdateInfo(version, tag, chosen.Name, chosen.Url, body);
        }
        catch
        {
            return null;
        }
    }

    public static UpdateInfo? FetchLatest(string runningExeName, int timeoutMs = 8000)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"DatabaseFinder/{VersionString(CurrentVersion)}");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        var json = client.GetStringAsync($"https://api.github.com/repos/{Repo}/releases/latest").GetAwaiter().GetResult();
        return Parse(json, runningExeName);
    }

    public static async Task DownloadAsync(string url, string destination,
        IProgress<(long Downloaded, long Total)>? progress, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        client.Timeout = Timeout.InfiniteTimeSpan;
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"DatabaseFinder/{VersionString(CurrentVersion)}");
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength ?? -1;
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var file = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None,
            81920, useAsync: true);
        var buffer = new byte[81920];
        long downloaded = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            downloaded += read;
            progress?.Report((downloaded, total));
        }
    }

    public static string? ParseChecksum(string sha256sumsText, string assetName)
    {
        foreach (var raw in sha256sumsText.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length < 66) continue;
            var hash = line.Substring(0, 64);
            if (!hash.All(c => Uri.IsHexDigit(c))) continue;
            var name = line.Substring(64).TrimStart().TrimStart('*').Trim();
            if (name.StartsWith("./", StringComparison.Ordinal)) name = name.Substring(2);
            if (string.Equals(name, assetName, StringComparison.OrdinalIgnoreCase)) return hash;
        }
        return null;
    }

    public static async Task VerifyChecksumAsync(string file, string tag, string assetName, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(20);
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"DatabaseFinder/{VersionString(CurrentVersion)}");
        string text;
        try
        {
            text = await client.GetStringAsync($"https://github.com/{Repo}/releases/download/{tag}/SHA256SUMS.txt", cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            throw new InvalidOperationException(L.Text("S346"));
        }
        var expected = ParseChecksum(text, assetName);
        if (expected == null) throw new InvalidOperationException(L.Text("S346"));
        var actual = await Task.Run(() =>
        {
            using var stream = File.OpenRead(file);
            return Convert.ToHexString(SHA256.HashData(stream));
        }, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(L.Text("S345"));
    }

    public static void Apply(string downloadedExe, string currentExe)
    {
        var directory = Path.GetDirectoryName(currentExe);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) directory = Path.GetTempPath();
        var target = Path.Combine(directory, Path.GetFileName(currentExe));
        var script = Path.Combine(directory, "df_update.cmd");
        var lines = new[]
        {
            "@echo off",
            "ping -n 3 127.0.0.1 >nul",
            $"copy /y \"{downloadedExe}\" \"{target}\" >nul 2>nul",
            "if errorlevel 1 goto :done",
            $"start \"\" \"{target}\"",
            ":done",
            $"del /q \"{downloadedExe}\" \"{script}\" >nul 2>nul",
        };
        File.WriteAllLines(script, lines, new UTF8Encoding(false));
        Process.Start(new ProcessStartInfo
        {
            FileName = script,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        });
    }

    private sealed record Asset(string Name, string Url);
}