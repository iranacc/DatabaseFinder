using System.Text;

namespace DatabaseFinder
{
    public class CarvedDatabaseResult
    {
        public bool Success { get; set; }
        public DatabaseType Type { get; set; } = DatabaseType.Unknown;
        public string FormatName { get; set; } = "";
        public string TrueExtension { get; set; } = "";
        public bool IsBackup { get; set; }
        public bool IsDisguised { get; set; }
        public string DisguisedReason { get; set; } = "";
        public string? RealDatabaseName { get; set; }
        public string? OriginServerName { get; set; }
        public string? BackupTypeDisplay { get; set; }
        public SqlBackupHeaderInfo? BackupHeader { get; set; }
    }

    /// <summary>
    /// موتور بازرسی بایت‌های جادویی و کشف فایل‌های دیتابیس تغییرنام‌یافته، پنهان یا بدون پسوند (Signature Carving).
    /// </summary>
    public static class DatabaseCarver
    {
        private static readonly HashSet<string> StandardDbExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mdf", ".ndf", ".ldf", ".bak",
            ".ibd", ".ibt", ".myd", ".myi", ".frm",
            ".db", ".sqlite", ".sqlite3", ".sqlitedb",
            ".accdb", ".mdb",
            ".dbf", ".dbt",
            ".fdb", ".gdb",
            ".rdb", ".aof",
            ".wt",
            ".sql", ".dump",
            ".zip", ".7z", ".rar", ".tar", ".gz"
        };

        private static readonly HashSet<string> SuspiciousExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            "", ".tmp", ".dat", ".bin", ".old", ".001", ".cab", ".dmp",
            ".backup", ".bak_old", ".bak2", ".data", ".log", ".txt",
            ".iso", ".img", ".raw", ".enc", ".temp", ".saved", ".doc", ".xls"
        };

        /// <summary>
        /// آیا این پسوند مشکوک به پنهان‌سازی یا تغییرنام است؟
        /// </summary>
        public static bool IsSuspiciousOrUnknownExtension(string ext)
        {
            if (string.IsNullOrEmpty(ext)) return true;
            if (SuspiciousExtensions.Contains(ext)) return true;
            return !StandardDbExtensions.Contains(ext);
        }

        /// <summary>
        /// بررسی باینری سرآیند فایل جهت کشف ماهیت واقعی فایل بدون اتکا به پسوند.
        /// </summary>
        public static bool TryCarve(string filePath, long fileSize, out CarvedDatabaseResult result)
        {
            result = new CarvedDatabaseResult();
            if (string.IsNullOrWhiteSpace(filePath) || fileSize < 16 || !File.Exists(filePath))
                return false;

            var ext = Path.GetExtension(filePath).ToLowerInvariant();

            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                var header = new byte[Math.Min(fs.Length, 65536)];
                int read = fs.Read(header, 0, header.Length);
                if (read < 16) return false;

                // 1. SQLite: "SQLite format 3\0"
                if (StartsWithAscii(header, "SQLite format 3\0"))
                {
                    result.Success = true;
                    result.Type = DatabaseType.SQLite;
                    result.FormatName = "SQLite";
                    result.TrueExtension = ".sqlite";
                    result.IsBackup = false;
                    result.RealDatabaseName = Path.GetFileNameWithoutExtension(filePath);
                    CheckDisguise(result, ext, ".sqlite", "SQLite", filePath);
                    return true;
                }

                // 2. SQL Server Compressed Backup: "MSSQLBAK"
                if (StartsWithAscii(header, "MSSQLBAK"))
                {
                    result.Success = true;
                    result.Type = DatabaseType.SQLServer;
                    result.FormatName = L.Pick("پشتیبان SQL Server (فشرده)", "SQL Server Backup (Compressed)");
                    result.TrueExtension = ".bak";
                    result.IsBackup = true;
                    PopulateSqlBackupDetails(filePath, result);
                    CheckDisguise(result, ext, ".bak", "SQL Server Backup", filePath);
                    return true;
                }

                // 3. SQL Server MTF Backup: DBLK "TAPE"
                if (StartsWithAscii(header, "TAPE") ||
                    (read >= 516 && header[512] == 0x54 && header[513] == 0x41 && header[514] == 0x50 && header[515] == 0x45) ||
                    (IndexOfAscii(header, "TAPE", 1024) >= 0 && IndexOfAscii(header, "SSET", 2048) >= 0))
                {
                    result.Success = true;
                    result.Type = DatabaseType.SQLServer;
                    result.FormatName = L.Pick("پشتیبان SQL Server (MTF)", "SQL Server Backup (MTF)");
                    result.TrueExtension = ".bak";
                    result.IsBackup = true;
                    PopulateSqlBackupDetails(filePath, result);
                    CheckDisguise(result, ext, ".bak", "SQL Server Backup", filePath);
                    return true;
                }

                // 4. SQL Server Primary Data File (MDF/NDF): Page 0 Header 01 0F 00 00 (00|08) 02
                if (header[0] == 0x01 && header[1] == 0x0F && header[2] == 0x00 && header[3] == 0x00 &&
                    (header[4] == 0x00 || header[4] == 0x08) && header[5] == 0x02)
                {
                    result.Success = true;
                    result.Type = DatabaseType.SQLServer;
                    result.TrueExtension = ".mdf";
                    result.IsBackup = false;

                    // بررسی آیا MDF است یا NDF یا LDF
                    string logicalName = ExtractSqlLogicalNameFromPage0(header);
                    if (!string.IsNullOrWhiteSpace(logicalName))
                    {
                        result.RealDatabaseName = logicalName;
                    }
                    else
                    {
                        result.RealDatabaseName = Path.GetFileNameWithoutExtension(filePath);
                    }

                    result.FormatName = "SQL Server (MDF)";
                    CheckDisguise(result, ext, ".mdf", "SQL Server MDF", filePath);
                    return true;
                }

                // 5. Microsoft Access: "Standard Jet DB" یا "Standard ACE DB"
                if (IndexOfAscii(header, "Standard Jet DB", 64) >= 0 || IndexOfAscii(header, "Standard ACE DB", 64) >= 0)
                {
                    result.Success = true;
                    result.Type = DatabaseType.Unknown;
                    result.TrueExtension = IndexOfAscii(header, "Standard ACE DB", 64) >= 0 ? ".accdb" : ".mdb";
                    result.FormatName = "Microsoft Access";
                    result.IsBackup = false;
                    result.RealDatabaseName = Path.GetFileNameWithoutExtension(filePath);
                    CheckDisguise(result, ext, result.TrueExtension, "Microsoft Access", filePath);
                    return true;
                }

                // 6. MySQL InnoDB: Checksum 0xCE 0xF0 0xED 0xFE یا 0xC0 0x64 0xF8 0x9A
                if (ContainsBytes(header, new byte[] { 0xCE, 0xF0, 0xED, 0xFE }) ||
                    ContainsBytes(header, new byte[] { 0xC0, 0x64, 0xF8, 0x9A }))
                {
                    result.Success = true;
                    result.Type = DatabaseType.MySQL;
                    result.FormatName = "MySQL InnoDB";
                    result.TrueExtension = ".ibd";
                    result.IsBackup = false;
                    result.RealDatabaseName = Path.GetFileNameWithoutExtension(filePath);
                    CheckDisguise(result, ext, ".ibd", "MySQL InnoDB", filePath);
                    return true;
                }

                // 7. Redis RDB: "REDIS"
                if (StartsWithAscii(header, "REDIS"))
                {
                    result.Success = true;
                    result.Type = DatabaseType.Redis;
                    result.FormatName = "Redis RDB Dump";
                    result.TrueExtension = ".rdb";
                    result.IsBackup = true;
                    result.RealDatabaseName = Path.GetFileNameWithoutExtension(filePath);
                    CheckDisguise(result, ext, ".rdb", "Redis RDB", filePath);
                    return true;
                }

                // 8. Firebird / InterBase
                if ((header[0] == 0x15 || header[0] == 0x14 || header[0] == 0x13) &&
                    (IndexOfAscii(header, "Firebird", 4096) >= 0 || IndexOfAscii(header, "InterBase", 4096) >= 0))
                {
                    result.Success = true;
                    result.Type = DatabaseType.Unknown;
                    result.FormatName = "Firebird / InterBase";
                    result.TrueExtension = ".fdb";
                    result.IsBackup = false;
                    result.RealDatabaseName = Path.GetFileNameWithoutExtension(filePath);
                    CheckDisguise(result, ext, ".fdb", "Firebird", filePath);
                    return true;
                }

                // 9. FoxPro / dBase DBF: 0x03, 0x83, 0x8B, 0xCB, 0xF5, 0xFB, 0x30
                if (IsDbfHeader(header))
                {
                    result.Success = true;
                    result.Type = DatabaseType.Unknown;
                    result.FormatName = "FoxPro / dBase";
                    result.TrueExtension = ".dbf";
                    result.IsBackup = false;
                    result.RealDatabaseName = Path.GetFileNameWithoutExtension(filePath);
                    CheckDisguise(result, ext, ".dbf", "dBase / FoxPro", filePath);
                    return true;
                }

                // 10. آرشیوهای فشرده با پسوندهای ساختگی (مانند Zip/7z/Rar که بکاپ هستند ولی پسوند .dat یا .tmp دارند)
                if (fileSize >= 3L * 1024 * 1024)
                {
                    if (StartsWithBytes(header, new byte[] { 0x50, 0x4B, 0x03, 0x04 }))
                    {
                        if (IsSuspiciousOrUnknownExtension(ext))
                        {
                            result.Success = true;
                            result.Type = DatabaseType.Unknown;
                            result.FormatName = L.Pick("آرشیو پشتیبان فشرده (ZIP)", "Backup Archive (ZIP)");
                            result.TrueExtension = ".zip";
                            result.IsBackup = true;
                            result.RealDatabaseName = Path.GetFileNameWithoutExtension(filePath);
                            CheckDisguise(result, ext, ".zip", "Zip Archive", filePath);
                            return true;
                        }
                    }
                    else if (StartsWithBytes(header, new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C }))
                    {
                        if (IsSuspiciousOrUnknownExtension(ext))
                        {
                            result.Success = true;
                            result.Type = DatabaseType.Unknown;
                            result.FormatName = L.Pick("آرشیو پشتیبان فشرده (7-Zip)", "Backup Archive (7-Zip)");
                            result.TrueExtension = ".7z";
                            result.IsBackup = true;
                            result.RealDatabaseName = Path.GetFileNameWithoutExtension(filePath);
                            CheckDisguise(result, ext, ".7z", "7-Zip Archive", filePath);
                            return true;
                        }
                    }
                    else if (StartsWithBytes(header, new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07 }))
                    {
                        if (IsSuspiciousOrUnknownExtension(ext))
                        {
                            result.Success = true;
                            result.Type = DatabaseType.Unknown;
                            result.FormatName = L.Pick("آرشیو پشتیبان فشرده (RAR)", "Backup Archive (RAR)");
                            result.TrueExtension = ".rar";
                            result.IsBackup = true;
                            result.RealDatabaseName = Path.GetFileNameWithoutExtension(filePath);
                            CheckDisguise(result, ext, ".rar", "RAR Archive", filePath);
                            return true;
                        }
                    }
                }

                // 11. فایل‌های SQL Dump متنی که با پسوند .log یا .txt یا .dat پنهان شده‌اند
                if (IsSqlTextDump(header))
                {
                    if (IsSuspiciousOrUnknownExtension(ext))
                    {
                        result.Success = true;
                        result.Type = DatabaseType.Unknown;
                        result.FormatName = L.Pick("اسکریپت دامپ پایگاه داده (SQL)", "SQL Database Dump");
                        result.TrueExtension = ".sql";
                        result.IsBackup = true;
                        result.RealDatabaseName = Path.GetFileNameWithoutExtension(filePath);
                        CheckDisguise(result, ext, ".sql", "SQL Dump Script", filePath);
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static void CheckDisguise(CarvedDatabaseResult result, string currentExt, string trueExt, string formatLabel, string filePath)
        {
            if (string.Equals(currentExt, trueExt, StringComparison.OrdinalIgnoreCase))
            {
                result.IsDisguised = false;
                return;
            }

            // برخی پسوندهای مجاز دیتابیس (مانند .ndf برای SQL Server یا .accdb/.mdb برای اکسس)
            if (trueExt == ".mdf" && string.Equals(currentExt, ".ndf", StringComparison.OrdinalIgnoreCase))
            {
                result.IsDisguised = false;
                result.TrueExtension = ".ndf";
                return;
            }
            if (trueExt == ".sqlite" && (currentExt == ".db" || currentExt == ".sqlite3" || currentExt == ".sqlitedb"))
            {
                result.IsDisguised = false;
                return;
            }

            result.IsDisguised = true;
            var extDisplay = string.IsNullOrEmpty(currentExt) ? L.Pick("(بدون پسوند)", "(no extension)") : currentExt;
            result.DisguisedReason = L.Pick(
                $"⚠️ فایل پایگاه داده {formatLabel} که با پسوند «{extDisplay}» استتار یا تغییرنام یافته است (پسوند واقعی: {trueExt})!",
                $"⚠️ Disguised {formatLabel} file hidden with extension '{extDisplay}' (real format: {trueExt})!");
        }

        private static void PopulateSqlBackupDetails(string filePath, CarvedDatabaseResult result)
        {
            try
            {
                var header = SqlBackupInspector.Inspect(filePath);
                if (header != null && header.IsValid)
                {
                    result.BackupHeader = header;
                    if (!string.IsNullOrWhiteSpace(header.DatabaseName))
                        result.RealDatabaseName = header.DatabaseName;
                    result.OriginServerName = header.ServerName;
                    result.BackupTypeDisplay = header.BackupType;
                }
            }
            catch { }

            if (string.IsNullOrWhiteSpace(result.RealDatabaseName))
            {
                result.RealDatabaseName = Path.GetFileNameWithoutExtension(filePath);
            }
        }

        private static string ExtractSqlLogicalNameFromPage0(byte[] page0)
        {
            try
            {
                // در صفحه ۰ فایل‌های MDF اس‌کیو‌ال سرور، در آفست‌های 0x19C الی 0x240 رشته یونیکد نام منطقی فایل قرار دارد
                for (int offset = 0x180; offset <= Math.Min(page0.Length - 64, 0x400); offset += 2)
                {
                    int len = 0;
                    while (offset + len * 2 + 1 < page0.Length && len < 60)
                    {
                        byte b0 = page0[offset + len * 2];
                        byte b1 = page0[offset + len * 2 + 1];
                        if (b1 == 0 && (char.IsLetterOrDigit((char)b0) || b0 == '_' || b0 == '-'))
                        {
                            len++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (len >= 3)
                    {
                        var name = Encoding.Unicode.GetString(page0, offset, len * 2);
                        if (!name.Equals("PRIMARY", StringComparison.OrdinalIgnoreCase))
                        {
                            return name;
                        }
                    }
                }
            }
            catch { }
            return "";
        }

        private static bool IsDbfHeader(byte[] header)
        {
            if (header.Length < 32) return false;
            byte b0 = header[0];
            if (b0 != 0x03 && b0 != 0x83 && b0 != 0x8B && b0 != 0xCB && b0 != 0xF5 && b0 != 0xFB && b0 != 0x30)
                return false;

            byte mm = header[2];
            byte dd = header[3];
            if (mm < 1 || mm > 12 || dd < 1 || dd > 31) return false;

            ushort headerLen = (ushort)(header[8] | (header[9] << 8));
            ushort recordLen = (ushort)(header[10] | (header[11] << 8));
            return headerLen >= 32 && recordLen > 0;
        }

        private static bool IsSqlTextDump(byte[] header)
        {
            int checkLen = Math.Min(header.Length, 2048);
            var text = Encoding.UTF8.GetString(header, 0, checkLen);
            return text.Contains("-- MySQL dump", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("/*M!", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("CREATE DATABASE", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("CREATE TABLE", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("INSERT INTO `", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("PostgreSQL database dump", StringComparison.OrdinalIgnoreCase);
        }

        private static bool StartsWithAscii(byte[] data, string ascii)
        {
            if (data.Length < ascii.Length) return false;
            for (int i = 0; i < ascii.Length; i++)
            {
                if (data[i] != (byte)ascii[i]) return false;
            }
            return true;
        }

        private static bool StartsWithBytes(byte[] data, byte[] magic)
        {
            if (data.Length < magic.Length) return false;
            for (int i = 0; i < magic.Length; i++)
            {
                if (data[i] != magic[i]) return false;
            }
            return true;
        }

        private static int IndexOfAscii(byte[] data, string ascii, int maxSearch)
        {
            int limit = Math.Min(data.Length - ascii.Length, maxSearch);
            for (int i = 0; i <= limit; i++)
            {
                bool match = true;
                for (int j = 0; j < ascii.Length; j++)
                {
                    if (data[i + j] != (byte)ascii[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }
            return -1;
        }

        private static bool ContainsBytes(byte[] data, byte[] magic)
        {
            int limit = data.Length - magic.Length;
            for (int i = 0; i <= limit; i++)
            {
                bool match = true;
                for (int j = 0; j < magic.Length; j++)
                {
                    if (data[i + j] != magic[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return true;
            }
            return false;
        }
    }
}
