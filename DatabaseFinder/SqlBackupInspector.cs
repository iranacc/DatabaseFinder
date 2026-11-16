using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace DatabaseFinder
{
    public class SqlBackupFileEntry
    {
        public string LogicalName { get; set; } = "";
        public string PhysicalName { get; set; } = "";
        public string Type { get; set; } = ""; // Data (MDF/NDF) or Log (LDF)
        public long Size { get; set; }
    }

    public class SqlBackupHeaderInfo
    {
        public string DatabaseName { get; set; } = "";
        public string ServerName { get; set; } = "";
        public string MachineName { get; set; } = "";
        public string BackupType { get; set; } = ""; // Full Database, Differential, Transaction Log
        public int BackupTypeCode { get; set; } = 1;
        public DateTime? BackupStartDate { get; set; }
        public DateTime? BackupFinishDate { get; set; }
        public string SoftwareName { get; set; } = "";
        public string Collation { get; set; } = "";
        public List<SqlBackupFileEntry> Files { get; set; } = new();
        public bool IsCompressed { get; set; }
        public bool IsValid { get; set; }
        public string Source { get; set; } = ""; // "Offline Header" or "SQL Server Engine"

        public bool IsFullBackup => BackupTypeCode == 1;
        public bool IsDifferential => BackupTypeCode == 5;
        public bool IsTransactionLog => BackupTypeCode == 2;

        public string SummaryText
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine(L.Pick($"نام واقعی پایگاه داده: {(string.IsNullOrEmpty(DatabaseName) ? "-" : DatabaseName)}", $"Real Database Name: {(string.IsNullOrEmpty(DatabaseName) ? "-" : DatabaseName)}"));
                sb.AppendLine(L.Pick($"نوع پشتیبان: {BackupType}", $"Backup Type: {BackupType}"));
                if (!string.IsNullOrEmpty(ServerName))
                    sb.AppendLine(L.Pick($"سرور مبدأ: {ServerName}", $"Origin Server: {ServerName}"));
                if (BackupStartDate.HasValue)
                {
                    var dt = BackupStartDate.Value;
                    var dateStr = L.IsFa
                        ? $"{JalaliDateHelper.FormatShort(dt)} {dt:HH:mm:ss}"
                        : dt.ToString("yyyy-MM-dd HH:mm:ss");
                    sb.AppendLine(L.Pick($"تاریخ ایجاد پشتیبان: {dateStr}", $"Backup Created: {dateStr}"));
                }
                if (Files.Count > 0)
                {
                    sb.AppendLine(L.Pick("فایل‌های داده و لاگ مبدأ:", "Original Files:"));
                    foreach (var f in Files)
                    {
                        var sizeStr = DatabaseFileLocator.FormatSize(f.Size);
                        sb.AppendLine($"  • [{f.Type}] {f.LogicalName}: {f.PhysicalName} ({sizeStr})");
                    }
                }
                return sb.ToString().TrimEnd();
            }
        }
    }

    /// <summary>
    /// موتور بازرسی عمیق متادیتای داخلی فایل‌های پشتیبان (Deep Header Inspection).
    /// قادر به بازرسی آفلاین ساختار MTF باینری بدون نیاز به سرویس و بازرسی آنلاین از طریق موتور SQL Server.
    /// </summary>
    public static class SqlBackupInspector
    {
        private static readonly Dictionary<string, SqlBackupHeaderInfo> _cache = new(StringComparer.OrdinalIgnoreCase);

        public static SqlBackupHeaderInfo Inspect(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return new SqlBackupHeaderInfo { IsValid = false };

            lock (_cache)
            {
                if (_cache.TryGetValue(filePath, out var cached))
                    return cached;
            }

            // 1. تلاش اولیه با بازرسی باینری آفلاین (سریع، بدون نیاز به سرویس یا مجوز)
            var info = ParseOffline(filePath);

            // 2. در صورت امکان، اعتبارسنجی تکمیلی از طریق موتور محلی SQL Server
            if (TryInspectViaSqlServer(filePath, out var sqlInfo))
            {
                // ادغام اطلاعات دریافتی از سرور محلی
                if (!string.IsNullOrWhiteSpace(sqlInfo.DatabaseName))
                    info.DatabaseName = sqlInfo.DatabaseName;
                if (!string.IsNullOrWhiteSpace(sqlInfo.ServerName))
                    info.ServerName = sqlInfo.ServerName;
                if (!string.IsNullOrWhiteSpace(sqlInfo.MachineName))
                    info.MachineName = sqlInfo.MachineName;
                if (sqlInfo.BackupStartDate.HasValue)
                    info.BackupStartDate = sqlInfo.BackupStartDate;
                if (sqlInfo.BackupFinishDate.HasValue)
                    info.BackupFinishDate = sqlInfo.BackupFinishDate;
                if (!string.IsNullOrWhiteSpace(sqlInfo.BackupType))
                {
                    info.BackupType = sqlInfo.BackupType;
                    info.BackupTypeCode = sqlInfo.BackupTypeCode;
                }
                if (sqlInfo.Files.Count > 0)
                    info.Files = sqlInfo.Files;

                info.Source = "SQL Server Engine (RESTORE HEADERONLY)";
                info.IsValid = true;
            }

            lock (_cache)
            {
                _cache[filePath] = info;
            }

            return info;
        }

        /// <summary>
        /// بازرسی باینری آفلاین هدرهای MTF و بلاک‌های SFGI/SFIN بدون نیاز به اتصال دیتابیس.
        /// </summary>
        public static SqlBackupHeaderInfo ParseOffline(string filePath)
        {
            var info = new SqlBackupHeaderInfo { IsValid = false, Source = "Offline Binary Parser" };
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                if (fs.Length < 512) return info;

                int readSize = (int)Math.Min(262144L, fs.Length);
                byte[] buf = new byte[readSize];
                int bytesRead = fs.Read(buf, 0, readSize);
                return ParseOfflineBytes(buf, bytesRead);
            }
            catch
            {
                return info;
            }
        }

        public static SqlBackupHeaderInfo ParseOfflineBytes(byte[] buf, int bytesRead)
        {
            var info = new SqlBackupHeaderInfo { IsValid = false, Source = "Offline Binary Parser" };
            if (bytesRead < 512) return info;
            try
            {

                // الف: بررسی امضای فایل فشرده SQL Server (MSSQLBAK)
                if (bytesRead >= 8 && Encoding.ASCII.GetString(buf, 0, 8) == "MSSQLBAK")
                {
                    info.IsCompressed = true;
                    info.IsValid = true;
                    info.BackupType = L.Pick("پشتیبان کامل فشرده (Compressed Full)", "Full Database (Compressed)");
                    info.BackupTypeCode = 1;

                    // استخراج مسیرها و نام‌ها از بافر
                    ExtractStringsFromCompressedBuffer(buf, bytesRead, info);
                    return info;
                }

                // ب: بررسی ساختار نوار مایکروسافت (MTF) با شناسه TAPE
                int tapePos = -1;
                if (StartsWithAscii(buf, 0, "TAPE")) tapePos = 0;
                else if (bytesRead >= 516 && StartsWithAscii(buf, 512, "TAPE")) tapePos = 512;
                else tapePos = IndexOfAscii(buf, 0, Math.Min(bytesRead, 4096), "TAPE");

                if (tapePos < 0) return info;
                info.IsValid = true;

                // استخراج فایل‌های فیزیکی از بلاک‌های SFIN
                ExtractFilesFromSfin(buf, bytesRead, info);

                // استخراج نام دیتابیس، سرور و نوع بکاپ از بلاک SFGI
                int sfgiPos = IndexOfAscii(buf, 0, bytesRead, "SFGI");
                if (sfgiPos > 0)
                {
                    if (sfgiPos + 16 <= bytesRead)
                    {
                        int typeCode = BitConverter.ToInt32(buf, sfgiPos + 12);
                        info.BackupTypeCode = typeCode;
                        info.BackupType = typeCode switch
                        {
                            1 => L.Pick("پشتیبان کامل (Full Database)", "Full Database Backup"),
                            2 => L.Pick("لاگ تراکنش (Transaction Log)", "Transaction Log Backup"),
                            5 => L.Pick("پشتیبان تفاضلی (Differential)", "Differential Database Backup"),
                            6 => L.Pick("فایل تفاضلی (Differential File)", "Differential File Backup"),
                            7 => L.Pick("گروه فایل (Filegroup Full)", "Filegroup Backup"),
                            _ => L.Pick($"پشتیبان نوع {typeCode}", $"Type {typeCode}")
                        };
                    }

                    ExtractDbAndServerBeforeSfgi(buf, sfgiPos, info);
                }

                // اگر نام دیتابیس استخراج نشد، از نام فایل MDF استنباط می‌شود
                if (string.IsNullOrWhiteSpace(info.DatabaseName))
                {
                    var mdf = info.Files.FirstOrDefault(f => f.Type.StartsWith("Data") || f.PhysicalName.EndsWith(".mdf", StringComparison.OrdinalIgnoreCase));
                    if (mdf != null)
                    {
                        info.DatabaseName = Path.GetFileNameWithoutExtension(mdf.PhysicalName);
                    }
                }

                if (string.IsNullOrWhiteSpace(info.BackupType))
                {
                    info.BackupType = L.Pick("پشتیبان کامل (Full)", "Full Backup");
                    info.BackupTypeCode = 1;
                }
            }
            catch
            {
                // نادیده گرفتن خطاهای خواندن فایل و بازگشت مقادیر پیش‌فرض
            }

            return info;
        }

        private static void ExtractDbAndServerBeforeSfgi(byte[] buf, int sfgiPos, SqlBackupHeaderInfo info)
        {
            // رشته‌های یونیکد قبل از SFGI ذخیره شده‌اند
            int p = sfgiPos - 2;
            while (p > 100 && buf[p] == 0 && buf[p + 1] == 0) p -= 2;
            int end = p + 2;

            while (p > 100 && (buf[p] != 0 || buf[p + 1] != 0)) p -= 2;
            int start = p + 2;

            if (end > start && (end - start) >= 4)
            {
                string combined = Encoding.Unicode.GetString(buf, start, end - start).Trim();
                if (!string.IsNullOrEmpty(combined))
                {
                    // اگر شامل نام دیتابیس شناخته شده از قبل است:
                    if (!string.IsNullOrEmpty(info.DatabaseName) && combined.StartsWith(info.DatabaseName, StringComparison.OrdinalIgnoreCase))
                    {
                        info.ServerName = combined[info.DatabaseName.Length..].Trim();
                    }
                    else if (combined.Contains('\\'))
                    {
                        // سرورهای نمونه دارای اسلش هستند مانند: HESABDARI-PC\TNC
                        int slash = combined.IndexOf('\\');
                        // جستجو برای شروع نام سرور به سمت چپ
                        int spaceOrSep = combined.LastIndexOfAny(new[] { ' ', '\0' }, slash);
                        if (spaceOrSep >= 0 && spaceOrSep < slash)
                        {
                            info.DatabaseName = combined[..spaceOrSep].Trim();
                            info.ServerName = combined[spaceOrSep..].Trim();
                        }
                        else
                        {
                            info.ServerName = combined;
                        }
                    }
                    else
                    {
                        // نام فایل MDF کمکی
                        var mdf = info.Files.FirstOrDefault(f => f.PhysicalName.EndsWith(".mdf", StringComparison.OrdinalIgnoreCase));
                        string mdfBase = mdf != null ? Path.GetFileNameWithoutExtension(mdf.PhysicalName) : "";

                        if (!string.IsNullOrEmpty(mdfBase) && combined.StartsWith(mdfBase, StringComparison.OrdinalIgnoreCase))
                        {
                            info.DatabaseName = mdfBase;
                            info.ServerName = combined[mdfBase.Length..].Trim();
                        }
                        else
                        {
                            // تقسیم بندی بر اساس تغییر حروف کوچک به بزرگ یا اعداد
                            info.DatabaseName = combined;
                        }
                    }
                }
            }
        }

        private static void ExtractFilesFromSfin(byte[] buf, int bytesRead, SqlBackupHeaderInfo info)
        {
            int idx = 0;
            while ((idx = IndexOfAscii(buf, idx, bytesRead - idx, "SFIN")) >= 0)
            {
                int chunkLen = Math.Min(1024, bytesRead - idx);
                string chunkUnicode = Encoding.Unicode.GetString(buf, idx, chunkLen);

                // جستجو برای مسیرهای فایل ویندوزی: C:\...
                var match = Regex.Match(chunkUnicode, @"([A-Za-z]:\\[A-Za-z0-9_\\\-\.: ]+\.(?:mdf|ldf|ndf))", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string path = match.Groups[1].Value.Trim();
                    string ext = Path.GetExtension(path).ToLowerInvariant();
                    string type = (ext == ".ldf") ? "Log" : "Data";

                    if (!info.Files.Any(f => string.Equals(f.PhysicalName, path, StringComparison.OrdinalIgnoreCase)))
                    {
                        // استخراج Logical Name
                        string logical = Path.GetFileNameWithoutExtension(path);
                        int pathIdx = chunkUnicode.IndexOf(path);
                        if (pathIdx > 0)
                        {
                            var prev = chunkUnicode[..pathIdx].Trim();
                            var tokens = Regex.Matches(prev, @"[A-Za-z0-9_]{2,50}");
                            if (tokens.Count > 0)
                            {
                                logical = tokens[^1].Value;
                            }
                        }

                        info.Files.Add(new SqlBackupFileEntry
                        {
                            PhysicalName = path,
                            LogicalName = logical,
                            Type = type
                        });

                        if (type == "Data" && string.IsNullOrEmpty(info.DatabaseName))
                        {
                            info.DatabaseName = Path.GetFileNameWithoutExtension(path);
                        }
                    }
                }
                idx += 4;
            }
        }

        private static void ExtractStringsFromCompressedBuffer(byte[] buf, int bytesRead, SqlBackupHeaderInfo info)
        {
            string unicode = Encoding.Unicode.GetString(buf, 0, bytesRead);
            var mdfMatch = Regex.Match(unicode, @"([A-Za-z]:\\[A-Za-z0-9_\\\-\.: ]+\.(?:mdf|ldf|ndf))", RegexOptions.IgnoreCase);
            if (mdfMatch.Success)
            {
                string path = mdfMatch.Groups[1].Value.Trim();
                info.Files.Add(new SqlBackupFileEntry
                {
                    PhysicalName = path,
                    LogicalName = Path.GetFileNameWithoutExtension(path),
                    Type = path.EndsWith(".ldf", StringComparison.OrdinalIgnoreCase) ? "Log" : "Data"
                });
                info.DatabaseName = Path.GetFileNameWithoutExtension(path);
            }
        }

        /// <summary>
        /// تلاش جهت اجرای RESTORE HEADERONLY با استفاده از اینستنس فعال محلی SQL Server.
        /// </summary>
        private static bool TryInspectViaSqlServer(string filePath, out SqlBackupHeaderInfo headerInfo)
        {
            headerInfo = new SqlBackupHeaderInfo();

            // بررسی اتصال محلی به SQL Server
            string[] connStrings =
            {
                "Server=localhost;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=2;",
                "Server=localhost,1433;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=2;",
                "Server=.;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=2;"
            };

            foreach (var cs in connStrings)
            {
                try
                {
                    using var conn = new SqlConnection(cs);
                    conn.Open();

                    using var cmd = conn.CreateCommand();
                    cmd.CommandTimeout = 5;
                    cmd.CommandText = $"RESTORE HEADERONLY FROM DISK = N'{filePath.Replace("'", "''")}'";

                    using var adapter = new SqlDataAdapter(cmd);
                    var dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        var row = dt.Rows[0];
                        headerInfo.DatabaseName = row["DatabaseName"]?.ToString() ?? "";
                        headerInfo.ServerName = row["ServerName"]?.ToString() ?? "";
                        headerInfo.MachineName = row["MachineName"]?.ToString() ?? "";
                        if (row["BackupStartDate"] is DateTime bsd) headerInfo.BackupStartDate = bsd;
                        if (row["BackupFinishDate"] is DateTime bfd) headerInfo.BackupFinishDate = bfd;

                        if (int.TryParse(row["BackupType"]?.ToString(), out int btc))
                        {
                            headerInfo.BackupTypeCode = btc;
                            headerInfo.BackupType = btc switch
                            {
                                1 => L.Pick("پشتیبان کامل (Full Database)", "Full Database Backup"),
                                2 => L.Pick("لاگ تراکنش (Transaction Log)", "Transaction Log Backup"),
                                5 => L.Pick("پشتیبان تفاضلی (Differential)", "Differential Database Backup"),
                                6 => L.Pick("فایل تفاضلی (Differential File)", "Differential File Backup"),
                                7 => L.Pick("گروه فایل (Filegroup Full)", "Filegroup Backup"),
                                _ => $"Type {btc}"
                            };
                        }

                        // دریافت لیست فایل‌ها
                        try
                        {
                            using var cmdFiles = conn.CreateCommand();
                            cmdFiles.CommandTimeout = 5;
                            cmdFiles.CommandText = $"RESTORE FILELISTONLY FROM DISK = N'{filePath.Replace("'", "''")}'";
                            using var adapterFiles = new SqlDataAdapter(cmdFiles);
                            var dtFiles = new DataTable();
                            adapterFiles.Fill(dtFiles);

                            foreach (DataRow fr in dtFiles.Rows)
                            {
                                string fType = fr["Type"]?.ToString() == "L" ? "Log" : "Data";
                                long fSize = 0;
                                if (long.TryParse(fr["Size"]?.ToString(), out long sz)) fSize = sz;

                                headerInfo.Files.Add(new SqlBackupFileEntry
                                {
                                    LogicalName = fr["LogicalName"]?.ToString() ?? "",
                                    PhysicalName = fr["PhysicalName"]?.ToString() ?? "",
                                    Type = fType,
                                    Size = fSize
                                });
                            }
                        }
                        catch { }

                        headerInfo.IsValid = true;
                        return true;
                    }
                }
                catch
                {
                    // ادامه بررسی با کانکشن استرینگ بعدی
                }
            }

            return false;
        }

        private static bool StartsWithAscii(byte[] buf, int offset, string str)
        {
            if (offset + str.Length > buf.Length) return false;
            for (int i = 0; i < str.Length; i++)
            {
                if (buf[offset + i] != (byte)str[i]) return false;
            }
            return true;
        }

        private static int IndexOfAscii(byte[] buf, int start, int count, string str)
        {
            byte[] target = Encoding.ASCII.GetBytes(str);
            int end = Math.Min(buf.Length, start + count) - target.Length;
            for (int i = start; i <= end; i++)
            {
                bool match = true;
                for (int j = 0; j < target.Length; j++)
                {
                    if (buf[i + j] != target[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }
            return -1;
        }
    }
}
