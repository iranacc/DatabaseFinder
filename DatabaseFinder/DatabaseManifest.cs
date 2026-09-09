using System.Runtime.InteropServices;
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

    public class ManifestDatabaseFile
    {
        public string Path { get; set; } = "";
        public string Sha256 { get; set; } = "";
        public long Size { get; set; }
        public DateTime ModifiedUtc { get; set; }
    }

    public class ManifestDatabase
    {
        public string Name { get; set; } = "";
        public string Engine { get; set; } = "";
        public string Host { get; set; } = "";
        public int? Port { get; set; }
        public string Instance { get; set; } = "";
        public string ServerVersion { get; set; } = "";
        public string Method { get; set; } = "";
        public string Chain { get; set; } = "full";
        public bool Compress { get; set; }
        public bool Verify { get; set; }
        public bool Checksum { get; set; }
        public DateTime StartedUtc { get; set; }
        public DateTime EndedUtc { get; set; }
        public long DurationMs { get; set; }
        public long Bytes { get; set; }
        public long SourceBytes { get; set; }
        public string Status { get; set; } = "ok";
        public string? Error { get; set; }
        public List<ManifestDatabaseFile> Files { get; set; } = new();
    }

    public class ManifestServer
    {
        public string Engine { get; set; } = "";
        public string Host { get; set; } = "";
        public int? Port { get; set; }
        public string Instance { get; set; } = "";
        public string Version { get; set; } = "";
    }

    public static class ManifestGenerator
    {
        public const string ToolVersion = "1.8.5";

        public static string Sha256File(string path)
        {
            using var fs = File.OpenRead(path);
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(fs);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static string Sha256Text(string text)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static string BuildTaxStamp181()
        {
            var glyphs = new[]
            {
                "    ███      ███████     ███ ",
                "    ███    ███   ███     ███ ",
                "    ███     ███████      ███ ",
                "    ███    ███   ███     ███ ",
                "  ███████   ███████    ███████ "
            };
            var art = new List<string>();
            foreach (var line in glyphs)
            {
                var dbl = string.Concat(line.Select(ch => ch == '█' ? "██" : "  "));
                art.Add(dbl);
                art.Add(dbl);
            }
            var logoWidth = art.Max(l => l.TrimEnd().Length);
            var caption = "TAX 181 ARTICLE . MSAM Group";
            var pad = Math.Max(0, (logoWidth - caption.Length) / 2);
            art.Add("");
            art.Add(new string(' ', pad) + caption);
            var inner = Math.Max(logoWidth, art.Max(l => l.TrimEnd().Length));
            var sb = new StringBuilder();
            sb.Append('╔').Append('═', inner).Append('╗').Append('\n');
            foreach (var line in art)
                sb.Append('║').Append(line.TrimEnd().PadRight(inner)).Append('║').Append('\n');
            sb.Append('╚').Append('═', inner).Append('╝');
            return sb.ToString();
        }

        /// <summary>
        /// تمام فایل‌های پوشه root را اسکن کرده و سه فایل مانیفست می‌سازد:
        /// <c>manifest.txt</c> (صورت‌جلسهٔ قاب‌بندی‌شده با بلوک sha256sum سازگار با sha256sum --check)،
        /// <c>manifest.md</c> (قابل رندر برای GitHub/مشاهده‌کننده‌ها) و <c>manifest.json</c> (ساختاریافته با schema).
        /// اگر <paramref name="items"/> پر شده باشد مشخصات هر دیتابیس، خلاصهٔ اجرا، نسخهٔ سرور و
        /// نسبت فشرده‌سازی نیز ثبت می‌شود.
        /// خروجی: مسیر فایل متنی.
        /// </summary>
        public static string Generate(string rootFolder, IReadOnlyList<DatabaseBackupItem>? items = null, string? outputBase = null)
        {
            outputBase ??= Path.Combine(rootFolder, "manifest");
            var rootFull = Path.GetFullPath(rootFolder);

            var entries = new List<ManifestEntry>();
            if (Directory.Exists(rootFolder))
            {
                foreach (var file in Directory.EnumerateFiles(rootFolder, "*", SearchOption.AllDirectories))
                {
                    string rel;
                    try { rel = ToRel(rootFull, file); }
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
            var byPath = entries.ToDictionary(e => e.Path, StringComparer.OrdinalIgnoreCase);

            List<ManifestDatabase>? databases = null;
            List<ManifestServer>? servers = null;
            if (items != null)
            {
                databases = new List<ManifestDatabase>();
                servers = new List<ManifestServer>();
                var seen = new HashSet<string>();

                foreach (var item in items)
                {
                    var s = item.Server;
                    var key = $"{s.Type}|{s.Host}|{s.Port}";
                    if (!seen.Contains(key))
                    {
                        seen.Add(key);
                        servers.Add(new ManifestServer
                        {
                            Engine = s.TypeDisplayName,
                            Host = s.Host,
                            Port = s.Port,
                            Instance = s.ServiceName ?? "",
                            Version = DatabaseBackuper.ProbeServerVersion(s)
                        });
                    }

                    var dbBytes = item.BytesProduced;
                    var srcBytes = dbBytes > 0 ? DatabaseBackuper.ProbeDatabaseSizeBytes(s, item.DatabaseName) : 0;

                    var files = new List<ManifestDatabaseFile>();
                    foreach (var f in item.OutputFiles)
                    {
                        string rel;
                        try { rel = ToRel(rootFull, f); }
                        catch { rel = f; }

                        if (byPath.TryGetValue(rel, out var known))
                        {
                            files.Add(new ManifestDatabaseFile
                            {
                                Path = rel,
                                Sha256 = known.Sha256,
                                Size = known.Size,
                                ModifiedUtc = known.ModifiedUtc
                            });
                        }
                        else
                        {
                            string sha256;
                            try { sha256 = Sha256File(f); }
                            catch { sha256 = ""; }
                            var fi = new FileInfo(f);
                            files.Add(new ManifestDatabaseFile
                            {
                                Path = rel,
                                Sha256 = sha256,
                                Size = fi.Length,
                                ModifiedUtc = fi.LastWriteTimeUtc
                            });
                        }
                    }

                    databases.Add(new ManifestDatabase
                    {
                        Name = item.DatabaseName,
                        Engine = s.TypeDisplayName,
                        Host = s.Host,
                        Port = s.Port,
                        Instance = s.ServiceName ?? "",
                        ServerVersion = servers.Find(x => x.Host == s.Host && x.Port == s.Port && x.Engine == s.TypeDisplayName)?.Version ?? "",
                        Method = item.Method,
                        Chain = item.Chain,
                        Compress = item.Compress,
                        Verify = item.Verify,
                        Checksum = item.Checksum,
                        StartedUtc = item.StartedUtc,
                        EndedUtc = item.EndedUtc,
                        DurationMs = (long)Math.Round((item.EndedUtc - item.StartedUtc).TotalMilliseconds),
                        Bytes = dbBytes,
                        SourceBytes = srcBytes,
                        Status = item.Failed ? "failed" : item.Done ? "ok" : "skipped",
                        Error = item.Error,
                        Files = files
                    });
                }
            }

            var started = databases?.Select(d => d.StartedUtc).Where(t => t != default).DefaultIfEmpty().Min() ?? default;
            var ended = databases?.Select(d => d.EndedUtc).Where(t => t != default).DefaultIfEmpty().Max() ?? default;
            var durationMs = ended != default && started != default ? (long)Math.Round((ended - started).TotalMilliseconds) : 0;
            var totalBackupBytes = entries.Sum(e => e.Size);
            var totalSourceBytes = databases?.Sum(d => d.SourceBytes) ?? 0;

            var summary = new
            {
                databases = databases?.Count ?? 0,
                succeeded = items?.Count(i => i.Done && !i.Failed) ?? 0,
                failed = items?.Count(i => i.Failed) ?? 0,
                skipped = items?.Count(i => !i.Done && !i.Failed) ?? 0,
                files = entries.Count,
                totalBytes = totalBackupBytes,
                totalSourceBytes,
                durationMs,
                os = RuntimeInformation.OSDescription + " (" + RuntimeInformation.OSArchitecture + ")",
                machine = Environment.MachineName,
                user = Environment.UserName
            };

            var appVersion = typeof(ManifestGenerator).Assembly.GetName().Version?.ToString() ?? "";
            var osText = RuntimeInformation.OSDescription + " (" + RuntimeInformation.OSArchitecture + ")";
            var nowUtc = DateTime.UtcNow;

            // ---------- بدنهٔ متنی (قاب‌بندی‌شده) ----------
            var body = new List<string>();
            body.Add("Database Finder - Backup Manifest (SHA-256)");
            body.Add($"Tool version : {ToolVersion}    App version : {appVersion}");
            body.Add($"Machine      : {Environment.MachineName}    User : {Environment.UserName}");
            body.Add($"OS           : {osText}");
            body.Add($"Created (UTC): {nowUtc:yyyy-MM-dd HH:mm:ss}");
            body.Add($"Root         : {rootFull}");
            body.Add("");
            body.Add("Summary: " +
                $"Databases {summary.databases}    Succeeded {summary.succeeded}    Failed {summary.failed}    " +
                $"Skipped {summary.skipped}    Files {summary.files}");
            body.Add($"Total {FormatBytes(summary.totalBytes)}    " +
                (summary.totalSourceBytes > 0 ? $"Source {FormatBytes(summary.totalSourceBytes)}    " : "") +
                $"Duration {FormatDuration(summary.durationMs)}    " +
                (RatioText(summary.totalSourceBytes, summary.totalBytes).Length > 0 ? $"Compressed {RatioText(summary.totalSourceBytes, summary.totalBytes)}" : ""));

            if (servers is { Count: > 0 })
            {
                body.Add("");
                body.Add("Servers:");
                foreach (var srv in servers)
                {
                    body.Add("  " + srv.Engine + " @ " + srv.Host +
                        (srv.Port.HasValue ? $":{srv.Port}" : "") +
                        (string.IsNullOrEmpty(srv.Instance) ? "" : $"  [{srv.Instance}]") +
                        (string.IsNullOrEmpty(srv.Version) ? "" : $"  {srv.Version}"));
                }
            }

            if (databases is { Count: > 0 })
            {
                var totalForBar = Math.Max(1L, totalBackupBytes);
                body.Add("");
                body.Add($"Databases ({databases.Count}):");
                foreach (var db in databases)
                {
                    var ratio = db.SourceBytes > 0 ? db.SourceBytes / (double)Math.Max(1, db.Bytes) : 0;
                    var ratioTxt = db.SourceBytes > 0 && ratio >= 1.05 ? $"  (compressed {ratio:0.#}x)" : "";
                    body.Add($"  {db.Name} ({db.Engine})");
                    body.Add($"    Method   : {db.Method}    Chain: {db.Chain}    Options: {OptionsText(db)}");
                    body.Add($"    Source   : {db.Host}" + (db.Port.HasValue ? $":{db.Port}" : "") +
                        (string.IsNullOrEmpty(db.Instance) ? "" : $"  [{db.Instance}]") +
                        (string.IsNullOrEmpty(db.ServerVersion) ? "" : $"  {db.ServerVersion}"));
                    body.Add($"    Timing   : {T(db.StartedUtc)} -> {T(db.EndedUtc)}  ({FormatDuration(db.DurationMs)})");
                    body.Add($"    Status   : {db.Status}" +
                        (string.IsNullOrEmpty(db.Error) ? "" : $"    Error: {db.Error}") +
                        $"    Backup {FormatBytes(db.Bytes)}" +
                        (db.SourceBytes > 0 ? $"  of ~{FormatBytes(db.SourceBytes)}{ratioTxt}" : ""));
                    if (db.Bytes > 0)
                        body.Add("    Proportion: " + Bar(db.Bytes, totalForBar, 20) +
                            $"  ({(100.0 * db.Bytes / totalForBar):0.#}% of total)");
                    foreach (var f in db.Files)
                        body.Add($"    File     : {f.Path}  {FormatBytes(f.Size)}  {f.Sha256}");
                }
            }

            if (entries.Count > 0)
            {
                body.Add("");
                body.Add("File hashes:");
                body.Add($"{"SHA256",-64} {"Size",8}  Path");
                body.Add("--------------------------------------------------------------------------------------------");
                foreach (var e in entries)
                    body.Add($"{e.Sha256}  {FormatBytes(e.Size).PadLeft(8)}  {e.Path}");
            }

            body.Add("");
            body.Add("sha256sum block (verify with: sha256sum -c manifest.txt):");
            foreach (var e in entries)
                body.Add($"{e.Sha256}  {e.Path.Replace('\\', '/')}");

            var framed = Frame(body);
            var banner = BuildTaxStamp181();
            var bodySource = banner + "\n" + framed;
            var bodyHash = Sha256Text(bodySource);
            var selfLine = $"Manifest SHA-256: {bodyHash}";
            var txtText = bodySource + selfLine + "\n";
            var txtPath = outputBase + ".txt";
            File.WriteAllText(txtPath, txtText, new UTF8Encoding(false));

            // ---------- Markdown ----------
            var md = new StringBuilder();
            md.Append("# Database Finder - Backup Manifest\n\n");
            md.Append("<pre>\n").Append(banner).Append('\n').Append("</pre>\n\n");
            md.Append($"**Tool** `{ToolVersion}` | **App** `{appVersion}` | **Machine** `{Environment.MachineName}` | **OS** `{osText}`\n\n");
            md.Append($"- Created (UTC): `{nowUtc:yyyy-MM-dd HH:mm:ss}`\n");
            md.Append($"- Root: `{rootFull}`\n\n");
            md.Append("## Summary\n\n");
            md.Append("| Databases | Succeeded | Failed | Skipped | Files | Total | Source | Duration |\n");
            md.Append("|--:|--:|--:|--:|--:|--:|--:|--:|\n");
            md.Append($"| {summary.databases} | {summary.succeeded} | {summary.failed} | {summary.skipped} | {summary.files} | {FormatBytes(summary.totalBytes)} | " +
                (summary.totalSourceBytes > 0 ? FormatBytes(summary.totalSourceBytes) : "-") + $" | {FormatDuration(summary.durationMs)} |\n\n");
            if (servers is { Count: > 0 })
            {
                md.Append("## Servers\n\n| Engine | Host | Port | Instance | Version |\n|---|---:|---|---|---|\n");
                foreach (var srv in servers)
                    md.Append($"| {srv.Engine} | {srv.Host} | {(srv.Port.HasValue ? srv.Port.ToString() : "-")} | {EmptyDash(srv.Instance)} | {EmptyDash(srv.Version)} |\n");
                md.Append("\n");
            }
            if (databases is { Count: > 0 })
            {
                md.Append("## Databases\n\n");
                foreach (var db in databases)
                {
                    var ratio = db.SourceBytes > 0 ? db.SourceBytes / (double)Math.Max(1, db.Bytes) : 0;
                    md.Append($"### {EscapeMd(db.Name)} ({EscapeMd(db.Engine)})\n\n");
                    md.Append($"- Method: `{EscapeMd(db.Method)}`, Chain: {db.Chain}, Options: {OptionsText(db)}\n");
                    md.Append($"- Source: `{db.Host}{(db.Port.HasValue ? ":" + db.Port : "")}{(string.IsNullOrEmpty(db.Instance) ? "" : " [" + EscapeMd(db.Instance) + "]")}`{EmptyColon(db.ServerVersion)}\n");
                    md.Append($"- Timing (UTC): {T(db.StartedUtc)} → {T(db.EndedUtc)}  ({FormatDuration(db.DurationMs)})\n");
                    md.Append($"- Status: **{db.Status}**{(string.IsNullOrEmpty(db.Error) ? "" : " — " + EscapeMd(db.Error))}\n");
                    md.Append($"- Backup: `{FormatBytes(db.Bytes)}`" + (db.SourceBytes > 0 ? $", source ~{FormatBytes(db.SourceBytes)} (compressed {ratio:0.#}x)" : "") + "\n\n");
                    if (db.Files.Count > 0)
                    {
                        md.Append("| SHA-256 | File | Size |\n|---|---|---:|\n");
                        foreach (var f in db.Files)
                            md.Append($"| `{f.Sha256}` | `{f.Path}` | {FormatBytes(f.Size)} |\n");
                        md.Append("\n");
                    }
                }
            }
            if (entries.Count > 0)
            {
                md.Append("## File hashes\n\n");
                md.Append("| SHA-256 | Size | Path |\n|---|---:|---|\n");
                foreach (var e in entries)
                    md.Append($"| `{e.Sha256}` | {FormatBytes(e.Size)} | `{e.Path}` |\n");
                md.Append("\n");
            }
            md.Append($"_Manifest SHA-256 (of `manifest.txt`): `{bodyHash}`_\n");
            var mdPath = outputBase + ".md";
            File.WriteAllText(mdPath, md.ToString(), new UTF8Encoding(false));

            // ---------- JSON ----------
            var json = new
            {
                schema = ToolVersion,
                taxNotice = "TAX 181 ARTICLE . MSAM Group",
                tool = "Database Finder",
                version = ToolVersion,
                appVersion,
                machine = Environment.MachineName,
                user = Environment.UserName,
                os = osText,
                root = rootFull,
                createdUtc = nowUtc,
                summary,
                servers,
                databases,
                files = entries,
                totalFiles = entries.Count,
                totalBytes = totalBackupBytes,
                selfSha256 = bodyHash
            };
            var jsonPath = outputBase + ".json";
            File.WriteAllText(jsonPath,
                JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true }),
                new UTF8Encoding(false));

            return txtPath;
        }

        private static string ToRel(string rootFull, string file)
        {
            return Path.GetRelativePath(rootFull, Path.GetFullPath(file));
        }

        private static string Frame(List<string> body)
        {
            var width = body.Count == 0 ? 0 : body.Max(l => l.Length);
            var sb = new StringBuilder();
            sb.Append('┌').Append('─', width + 2).Append('┐').Append('\n');
            foreach (var line in body)
                sb.Append('│').Append(' ').Append(line.PadRight(width)).Append(' ').Append('│').Append('\n');
            sb.Append('└').Append('─', width + 2).Append('┘').Append('\n');
            return sb.ToString();
        }

        private static string Bar(long part, long total, int width)
        {
            var filled = (int)Math.Round(width * part / (double)total);
            filled = Math.Clamp(filled, 0, width);
            return new string('█', filled) + new string('░', width - filled);
        }

        private static string RatioText(long source, long backup)
        {
            if (source <= 0 || backup <= 0) return "";
            var ratio = source / (double)backup;
            return ratio >= 1.05 ? $"{ratio:0.#}x" : "";
        }

        private static string OptionsText(ManifestDatabase db)
        {
            var parts = new List<string>();
            if (db.Compress) parts.Add("compression");
            if (db.Checksum) parts.Add("checksum");
            if (db.Verify) parts.Add("verify");
            return parts.Count == 0 ? "none" : string.Join(", ", parts);
        }

        private static string T(DateTime utc) => utc == default ? "-" : utc.ToString("yyyy-MM-dd HH:mm:ss");

        private static string EmptyDash(string v) => string.IsNullOrEmpty(v) ? "-" : v;

        private static string EmptyColon(string v) => string.IsNullOrEmpty(v) ? "" : " — " + v;

        private static string EscapeMd(string v) => v.Replace("|", "\\|").Replace("\n", " ");

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

        public static string FormatDuration(long ms)
        {
            var t = TimeSpan.FromMilliseconds(Math.Max(0, ms));
            if (t.TotalHours >= 1) return $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}";
            if (t.TotalMinutes >= 1) return $"{t.Minutes}:{t.Seconds:00}";
            return $"{t.TotalSeconds:0.#} s";
        }
    }
}