using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace DatabaseFinder
{
    public class ArchiveEntryInfo
    {
        public string EntryPath { get; set; } = "";
        public string FileName { get; set; } = "";
        public string Extension { get; set; } = "";
        public long UncompressedSize { get; set; }
        public long CompressedSize { get; set; }
        public DateTime Modified { get; set; }
        public DatabaseType Type { get; set; } = DatabaseType.Unknown;
        public string FormatName { get; set; } = "";
        public bool IsBackup { get; set; }
        public string? RealDatabaseName { get; set; }
        public string? OriginServerName { get; set; }
        public string? BackupTypeDisplay { get; set; }
        public string ContainerArchivePath { get; set; } = "";
        public string ContainerType { get; set; } = ""; // ZIP, RAR, TAR, 7Z
    }

    /// <summary>
    /// موتور پویش درون فایل‌های فشرده (Archive Peeking) بدون نیاز به اکسترکت کردن روی دیسک.
    /// قادر به بازرسی ساختار داخلی ZIP, TAR, GZ, RAR و 7Z در چند میلی‌ثانیه.
    /// </summary>
    public static class ArchivePeeker
    {
        private static readonly HashSet<string> ArchiveExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".zip", ".rar", ".tar", ".gz", ".tgz", ".7z", ".jar", ".apk"
        };

        private static readonly HashSet<string> DatabaseExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            // SQL Server
            ".bak", ".mdf", ".ndf", ".ldf", ".trn",
            // SQLite
            ".sqlite", ".sqlite3", ".db", ".db3", ".sqlitedb",
            // Microsoft Access
            ".accdb", ".mdb",
            // MySQL & MariaDB
            ".ibd", ".frm", ".myd", ".myi",
            // PostgreSQL & Dumps
            ".dump", ".sql",
            // FoxPro & dBase
            ".dbc", ".dbf",
            // Firebird
            ".fdb", ".gdb",
            // Oracle
            ".dmp"
        };

        private static readonly string[] AccountingKeywords =
        {
            "sepidar", "holoo", "tadbir", "parsian", "mahak", "nosa", "ghias", "payvast",
            "paya", "kowsar", "rahkaran", "systemgroup", "arian", "saniran", "hesabgar",
            "backup", "bak", "databackup", "بکاپ", "پشتیبان", "مالی", "حسابداری"
        };

        public static bool IsSupportedArchive(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return false;
            var ext = Path.GetExtension(filePath);
            if (!ArchiveExtensions.Contains(ext)) return false;

            try
            {
                if (!File.Exists(filePath)) return false;
                var fi = new FileInfo(filePath);
                if (fi.Length < 16) return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static List<ArchiveEntryInfo> PeekArchive(string archivePath, long minSizeBytes = 0)
        {
            var results = new List<ArchiveEntryInfo>();
            if (!File.Exists(archivePath)) return results;

            var ext = Path.GetExtension(archivePath).ToLowerInvariant();

            try
            {
                if (ext is ".zip" or ".jar" or ".apk" || FileSignatures.IsZip(archivePath))
                {
                    results.AddRange(PeekZip(archivePath, minSizeBytes));
                }
                else if (ext is ".rar" || FileSignatures.IsRar(archivePath))
                {
                    results.AddRange(PeekRar(archivePath, minSizeBytes));
                }
                else if (ext is ".tar" || ext is ".tgz" || ext.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) || FileSignatures.IsTar(archivePath))
                {
                    results.AddRange(PeekTar(archivePath, minSizeBytes));
                }
                else if (ext is ".gz" || FileSignatures.IsGzip(archivePath))
                {
                    results.AddRange(PeekGz(archivePath, minSizeBytes));
                }
                else if (ext is ".7z" || FileSignatures.Is7z(archivePath))
                {
                    results.AddRange(Peek7z(archivePath, minSizeBytes));
                }
            }
            catch { }

            return results;
        }

        // =====================================================================
        // 1. پویش درایور ZIP
        // =====================================================================
        public static List<ArchiveEntryInfo> PeekZip(string archivePath, long minSizeBytes)
        {
            var list = new List<ArchiveEntryInfo>();
            using var fs = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var za = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: false);

            foreach (var entry in za.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name) || entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'))
                    continue;

                if (IsCandidateEntry(entry.Name, out var type, out var formatName, out var isBackup))
                {
                    long unpSize = entry.Length;
                    if (unpSize >= minSizeBytes)
                    {
                        var info = new ArchiveEntryInfo
                        {
                            EntryPath = entry.FullName,
                            FileName = entry.Name,
                            Extension = Path.GetExtension(entry.Name).ToLowerInvariant(),
                            UncompressedSize = unpSize,
                            CompressedSize = entry.CompressedLength,
                            Modified = entry.LastWriteTime.LocalDateTime,
                            Type = type,
                            FormatName = formatName,
                            IsBackup = isBackup,
                            ContainerArchivePath = archivePath,
                            ContainerType = "ZIP"
                        };

                        // در صورت امکان، خواندن سرآیند ۶۴ کیلوبایتی بافر فایل .bak از درون زیپ جهت کشف نام واقعی دیتابیس
                        if (entry.Name.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                using var s = entry.Open();
                                byte[] buf = new byte[65536];
                                int read = s.Read(buf, 0, buf.Length);
                                if (read >= 512)
                                {
                                    var hdr = SqlBackupInspector.ParseOfflineBytes(buf, read);
                                    if (hdr.IsValid)
                                    {
                                        info.RealDatabaseName = hdr.DatabaseName;
                                        info.OriginServerName = hdr.ServerName;
                                        info.BackupTypeDisplay = hdr.BackupType;
                                    }
                                }
                            }
                            catch { }
                        }

                        list.Add(info);
                    }
                }
            }

            return list;
        }

        // =====================================================================
        // 2. پویش درایور TAR و TAR.GZ
        // =====================================================================
        public static List<ArchiveEntryInfo> PeekTar(string archivePath, long minSizeBytes)
        {
            var list = new List<ArchiveEntryInfo>();
            using var fs = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

            Stream tarStream = fs;
            bool isGz = archivePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) ||
                        archivePath.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase) ||
                        FileSignatures.IsGzip(archivePath);

            GZipStream? gzStream = null;
            if (isGz)
            {
                gzStream = new GZipStream(fs, CompressionMode.Decompress, leaveOpen: true);
                tarStream = gzStream;
            }

            try
            {
                byte[] header = new byte[512];
                while (true)
                {
                    int bytesRead = ReadFullBlock(tarStream, header, 512);
                    if (bytesRead < 512) break;

                    // دو بلاک صفر نشانگر پایان آرشیو است
                    if (IsAllZero(header)) break;

                    // نام فایل در بایت‌های 0..99
                    int nameLen = 0;
                    while (nameLen < 100 && header[nameLen] != 0) nameLen++;
                    if (nameLen == 0) continue;
                    string name = Encoding.ASCII.GetString(header, 0, nameLen).Trim();

                    // حجم در مبنای ۸ (اکتال) در بایت‌های 124..135
                    long size = ParseOctal(header, 124, 12);
                    // تاریخ mtime در بایت‌های 136..147
                    long mtime = ParseOctal(header, 136, 12);
                    DateTime modDate = DateTimeOffset.FromUnixTimeSeconds(mtime > 0 ? mtime : 0).LocalDateTime;

                    byte typeFlag = header[156];
                    bool isRegularFile = typeFlag == '0' || typeFlag == 0;

                    if (isRegularFile && IsCandidateEntry(name, out var type, out var formatName, out var isBackup))
                    {
                        if (size >= minSizeBytes)
                        {
                            list.Add(new ArchiveEntryInfo
                            {
                                EntryPath = name,
                                FileName = Path.GetFileName(name),
                                Extension = Path.GetExtension(name).ToLowerInvariant(),
                                UncompressedSize = size,
                                CompressedSize = size,
                                Modified = modDate,
                                Type = type,
                                FormatName = formatName,
                                IsBackup = isBackup,
                                ContainerArchivePath = archivePath,
                                ContainerType = isGz ? "TAR.GZ" : "TAR"
                            });
                        }
                    }

                    // پرش از بایت‌های محتوای فایل به اندازه مضرب ۵۱۲
                    long dataBlocks = (size + 511) / 512;
                    long bytesToSkip = dataBlocks * 512;
                    if (tarStream.CanSeek)
                    {
                        tarStream.Seek(bytesToSkip, SeekOrigin.Current);
                    }
                    else
                    {
                        SkipStreamBytes(tarStream, bytesToSkip);
                    }
                }
            }
            finally
            {
                gzStream?.Dispose();
            }

            return list;
        }

        // =====================================================================
        // 3. پویش درایور GZ تکی (مانند database.bak.gz)
        // =====================================================================
        public static List<ArchiveEntryInfo> PeekGz(string archivePath, long minSizeBytes)
        {
            var list = new List<ArchiveEntryInfo>();
            using var fs = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            if (fs.Length < 18) return list;

            byte[] header = new byte[10];
            if (fs.Read(header, 0, 10) != 10) return list;
            if (header[0] != 0x1F || header[1] != 0x8B) return list; // GZIP magic

            byte flg = header[3];
            long mtime = BitConverter.ToUInt32(header, 4);
            DateTime modDate = mtime > 0 ? DateTimeOffset.FromUnixTimeSeconds(mtime).LocalDateTime : File.GetLastWriteTime(archivePath);

            // استخراج حجم غیرفشرده از ۴ بایت انتهای فایل GZ
            fs.Seek(-4, SeekOrigin.End);
            byte[] isizeBuf = new byte[4];
            fs.Read(isizeBuf, 0, 4);
            long unpSize = BitConverter.ToUInt32(isizeBuf, 0);

            // خواندن نام فایل اصلی از هدر (در صورت وجود فلگ FNAME: bit 3)
            string innerName = "";
            fs.Seek(10, SeekOrigin.Begin);
            if ((flg & 0x04) != 0) // FEXTRA
            {
                int xlen = fs.ReadByte() | (fs.ReadByte() << 8);
                fs.Seek(xlen, SeekOrigin.Current);
            }
            if ((flg & 0x08) != 0) // FNAME (Zero-terminated)
            {
                var sb = new StringBuilder();
                int b;
                while ((b = fs.ReadByte()) > 0) sb.Append((char)b);
                innerName = sb.ToString();
            }

            if (string.IsNullOrEmpty(innerName))
            {
                // حدس نام فایل با حذف پسوند .gz
                innerName = Path.GetFileNameWithoutExtension(archivePath);
            }

            if (IsCandidateEntry(innerName, out var type, out var formatName, out var isBackup))
            {
                if (unpSize >= minSizeBytes || unpSize == 0)
                {
                    list.Add(new ArchiveEntryInfo
                    {
                        EntryPath = innerName,
                        FileName = innerName,
                        Extension = Path.GetExtension(innerName).ToLowerInvariant(),
                        UncompressedSize = unpSize > 0 ? unpSize : fs.Length,
                        CompressedSize = fs.Length,
                        Modified = modDate,
                        Type = type,
                        FormatName = formatName,
                        IsBackup = isBackup,
                        ContainerArchivePath = archivePath,
                        ContainerType = "GZ"
                    });
                }
            }

            return list;
        }

        // =====================================================================
        // 4. پویش درایور RAR (پشتیبانی باینری نیتیو از سربرگ‌های RAR 4.x و RAR 5.x)
        // =====================================================================
        public static List<ArchiveEntryInfo> PeekRar(string archivePath, long minSizeBytes)
        {
            var list = new List<ArchiveEntryInfo>();
            using var fs = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            if (fs.Length < 16) return list;

            byte[] sig = new byte[8];
            int read = fs.Read(sig, 0, 8);
            if (read < 7) return list;

            // RAR 4.x Signature: 52 61 72 21 1A 07 00
            if (sig[0] == 0x52 && sig[1] == 0x61 && sig[2] == 0x72 && sig[3] == 0x21 && sig[4] == 0x1A && sig[5] == 0x07 && sig[6] == 0x00)
            {
                fs.Seek(7, SeekOrigin.Begin);
                return PeekRar4(fs, archivePath, minSizeBytes);
            }
            // RAR 5.x Signature: 52 61 72 21 1A 07 01 00
            if (read >= 8 && sig[0] == 0x52 && sig[1] == 0x61 && sig[2] == 0x72 && sig[3] == 0x21 && sig[4] == 0x1A && sig[5] == 0x07 && sig[6] == 0x01 && sig[7] == 0x00)
            {
                fs.Seek(8, SeekOrigin.Begin);
                return PeekRar5(fs, archivePath, minSizeBytes);
            }

            return list;
        }

        private static List<ArchiveEntryInfo> PeekRar4(FileStream fs, string archivePath, long minSizeBytes)
        {
            var list = new List<ArchiveEntryInfo>();
            byte[] buf = new byte[32];

            while (fs.Position + 7 <= fs.Length)
            {
                long blockStart = fs.Position;
                if (fs.Read(buf, 0, 7) != 7) break;

                ushort headCrc = BitConverter.ToUInt16(buf, 0);
                byte headType = buf[2];
                ushort headFlags = BitConverter.ToUInt16(buf, 3);
                ushort headSize = BitConverter.ToUInt16(buf, 5);
                if (headSize < 7) break;

                // 0x7B = پایان آرشیو
                if (headType == 0x7B) break;

                // 0x74 = بلوک فایل (FILE_HEAD)
                if (headType == 0x74)
                {
                    byte[] fileHead = new byte[headSize - 7];
                    if (fs.Read(fileHead, 0, fileHead.Length) != fileHead.Length) break;

                    long packSize = BitConverter.ToUInt32(fileHead, 0);
                    long unpSize = BitConverter.ToUInt32(fileHead, 4);
                    ushort nameSize = BitConverter.ToUInt16(fileHead, 19);

                    // فلگ 0x0100 = فایل‌های بزرگ ۶۴ بیتی (LARGE_FILE)
                    int nameOffset = 25;
                    if ((headFlags & 0x0100) != 0 && fileHead.Length >= 33)
                    {
                        uint highPack = BitConverter.ToUInt32(fileHead, 25);
                        uint highUnp = BitConverter.ToUInt32(fileHead, 29);
                        packSize |= ((long)highPack << 32);
                        unpSize |= ((long)highUnp << 32);
                        nameOffset = 33;
                    }

                    string name = "";
                    if (nameOffset + nameSize <= fileHead.Length)
                    {
                        name = Encoding.UTF8.GetString(fileHead, nameOffset, nameSize);
                    }

                    if (!string.IsNullOrEmpty(name) && IsCandidateEntry(name, out var type, out var formatName, out var isBackup))
                    {
                        if (unpSize >= minSizeBytes)
                        {
                            list.Add(new ArchiveEntryInfo
                            {
                                EntryPath = name,
                                FileName = Path.GetFileName(name),
                                Extension = Path.GetExtension(name).ToLowerInvariant(),
                                UncompressedSize = unpSize,
                                CompressedSize = packSize,
                                Modified = File.GetLastWriteTime(archivePath),
                                Type = type,
                                FormatName = formatName,
                                IsBackup = isBackup,
                                ContainerArchivePath = archivePath,
                                ContainerType = "RAR"
                            });
                        }
                    }

                    // پرش فوری به سربرگ بلوک بعدی با عبور از داده‌های فشرده
                    fs.Seek(blockStart + headSize + packSize, SeekOrigin.Begin);
                }
                else
                {
                    // برای سایر بلاک‌ها، بررسی فلگ 0x8000 برای ADD_SIZE
                    long addSize = 0;
                    if ((headFlags & 0x8000) != 0 && fs.Length >= fs.Position + 4)
                    {
                        byte[] addBuf = new byte[4];
                        fs.Read(addBuf, 0, 4);
                        addSize = BitConverter.ToUInt32(addBuf, 0);
                    }
                    fs.Seek(blockStart + headSize + addSize, SeekOrigin.Begin);
                }
            }

            return list;
        }

        private static List<ArchiveEntryInfo> PeekRar5(FileStream fs, string archivePath, long minSizeBytes)
        {
            var list = new List<ArchiveEntryInfo>();

            while (fs.Position + 4 < fs.Length)
            {
                // خواندن CRC32 (۴ بایت)
                if (fs.ReadByte() < 0) break;
                fs.Seek(3, SeekOrigin.Current);

                ulong headerSize = ReadVint(fs);
                if (headerSize == 0 || (long)headerSize > fs.Length - fs.Position) break;

                long blockDataStart = fs.Position;
                ulong headerType = ReadVint(fs);
                ulong headerFlags = ReadVint(fs);

                ulong extraSize = 0;
                if ((headerFlags & 1) != 0) extraSize = ReadVint(fs);

                ulong dataSize = 0;
                if ((headerFlags & 2) != 0) dataSize = ReadVint(fs);

                // نوع ۲ = بلوک فایل یا سرویس (File / Service)
                if (headerType == 2)
                {
                    ulong fileFlags = ReadVint(fs);
                    ulong unpSize = ReadVint(fs);
                    ulong attr = ReadVint(fs);

                    // در صورت وجود فلگ mtime
                    if ((fileFlags & 2) != 0)
                    {
                        fs.Seek(4, SeekOrigin.Current);
                    }

                    ulong nameLen = ReadVint(fs);
                    string name = "";
                    if (nameLen > 0 && nameLen < 2048)
                    {
                        byte[] nameBuf = new byte[(int)nameLen];
                        if (fs.Read(nameBuf, 0, nameBuf.Length) == nameBuf.Length)
                        {
                            name = Encoding.UTF8.GetString(nameBuf);
                        }
                    }

                    if (!string.IsNullOrEmpty(name) && IsCandidateEntry(name, out var type, out var formatName, out var isBackup))
                    {
                        if ((long)unpSize >= minSizeBytes)
                        {
                            list.Add(new ArchiveEntryInfo
                            {
                                EntryPath = name,
                                FileName = Path.GetFileName(name),
                                Extension = Path.GetExtension(name).ToLowerInvariant(),
                                UncompressedSize = (long)unpSize,
                                CompressedSize = (long)dataSize,
                                Modified = File.GetLastWriteTime(archivePath),
                                Type = type,
                                FormatName = formatName,
                                IsBackup = isBackup,
                                ContainerArchivePath = archivePath,
                                ContainerType = "RAR5"
                            });
                        }
                    }
                }

                // پرش به بلوک بعدی
                fs.Seek(blockDataStart + (long)headerSize + (long)dataSize, SeekOrigin.Begin);
            }

            return list;
        }

        private static ulong ReadVint(Stream stream)
        {
            ulong result = 0;
            int shift = 0;
            while (true)
            {
                int b = stream.ReadByte();
                if (b < 0) break;
                result |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0) break;
                shift += 7;
                if (shift >= 64) break;
            }
            return result;
        }

        // =====================================================================
        // 5. پویش درایور 7Z (از طریق ابزار رسمی سیستم tar.exe یا 7z.exe)
        // =====================================================================
        public static List<ArchiveEntryInfo> Peek7z(string archivePath, long minSizeBytes)
        {
            var list = new List<ArchiveEntryInfo>();

            // بررسی امکان فهرست‌گیری از طریق ابزار bsdtar تعبیه شده در ویندوز (System32\tar.exe)
            var tarExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "tar.exe");
            if (File.Exists(tarExe))
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = tarExe,
                        Arguments = $"-tf \"{archivePath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        string output = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit(3000);
                        if (proc.ExitCode == 0)
                        {
                            foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                var trimmed = line.Trim();
                                if (IsCandidateEntry(trimmed, out var type, out var formatName, out var isBackup))
                                {
                                    list.Add(new ArchiveEntryInfo
                                    {
                                        EntryPath = trimmed,
                                        FileName = Path.GetFileName(trimmed),
                                        Extension = Path.GetExtension(trimmed).ToLowerInvariant(),
                                        UncompressedSize = 0, // tar -tf سایز را بدون پارامتر verbose برنمی‌گرداند
                                        CompressedSize = 0,
                                        Modified = File.GetLastWriteTime(archivePath),
                                        Type = type,
                                        FormatName = formatName,
                                        IsBackup = isBackup,
                                        ContainerArchivePath = archivePath,
                                        ContainerType = "7Z"
                                    });
                                }
                            }
                            return list;
                        }
                    }
                }
                catch { }
            }

            return list;
        }

        // =====================================================================
        // 6. استخراج تکی بدون نیاز به اکسترکت کل آرشیو (On-Demand Single File Extraction)
        // =====================================================================
        public static bool ExtractEntryWithProgress(string archivePath, string entryPath, string destFilePath, Action<long, long>? progress = null)
        {
            if (!File.Exists(archivePath)) return false;
            var ext = Path.GetExtension(archivePath).ToLowerInvariant();

            Directory.CreateDirectory(Path.GetDirectoryName(destFilePath) ?? ".");

            // الف: استخراج نیتیو از ZIP
            if (ext is ".zip" or ".jar" or ".apk" || FileSignatures.IsZip(archivePath))
            {
                using var fs = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var za = new ZipArchive(fs, ZipArchiveMode.Read);
                var entry = za.GetEntry(entryPath) ?? za.Entries.FirstOrDefault(e => string.Equals(e.FullName, entryPath, StringComparison.OrdinalIgnoreCase) || string.Equals(e.Name, entryPath, StringComparison.OrdinalIgnoreCase));
                if (entry == null) return false;

                using var inStream = entry.Open();
                using var outStream = new FileStream(destFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                byte[] buffer = new byte[128 * 1024];
                long total = entry.Length;
                long copied = 0;
                int read;
                while ((read = inStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    outStream.Write(buffer, 0, read);
                    copied += read;
                    progress?.Invoke(copied, total);
                }
                return true;
            }

            // ب: استخراج نیتیو از GZ تکی
            if (ext is ".gz" && !archivePath.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
            {
                using var fs = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var gz = new GZipStream(fs, CompressionMode.Decompress);
                using var outStream = new FileStream(destFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                byte[] buffer = new byte[128 * 1024];
                long total = fs.Length * 2; // تخمین حجم
                long copied = 0;
                int read;
                while ((read = gz.Read(buffer, 0, buffer.Length)) > 0)
                {
                    outStream.Write(buffer, 0, read);
                    copied += read;
                    progress?.Invoke(copied, Math.Max(total, copied));
                }
                return true;
            }

            // ج: استخراج از RAR, 7Z یا TAR از طریق bsdtar ویندوز (موجود در System32 تمامی ویندوزهای ۱۰ و ۱۱)
            var tarExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "tar.exe");
            if (File.Exists(tarExe))
            {
                try
                {
                    var destDir = Path.GetDirectoryName(destFilePath) ?? ".";
                    var psi = new ProcessStartInfo
                    {
                        FileName = tarExe,
                        Arguments = $"-xf \"{archivePath}\" -C \"{destDir}\" \"{entryPath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        proc.WaitForExit();
                        var expected = Path.Combine(destDir, entryPath);
                        if (File.Exists(expected) && !string.Equals(expected, destFilePath, StringComparison.OrdinalIgnoreCase))
                        {
                            if (File.Exists(destFilePath)) File.Delete(destFilePath);
                            File.Move(expected, destFilePath);
                        }
                        return File.Exists(destFilePath);
                    }
                }
                catch { }
            }

            return false;
        }

        // =====================================================================
        // متدهای کمکی
        // =====================================================================
        public static bool IsCandidateEntry(string entryPath, out DatabaseType type, out string formatName, out bool isBackup)
        {
            type = DatabaseType.Unknown;
            formatName = "";
            isBackup = false;

            var ext = Path.GetExtension(entryPath).ToLowerInvariant();
            var fileName = Path.GetFileName(entryPath).ToLowerInvariant();

            if (ext is ".bak" or ".trn")
            {
                type = DatabaseType.SQLServer;
                formatName = L.Pick("پشتیبان SQL Server", "SQL Server Backup");
                isBackup = true;
                return true;
            }
            if (ext is ".mdf" or ".ndf")
            {
                type = DatabaseType.SQLServer;
                formatName = L.Pick("پایگاه داده اصلی SQL Server", "SQL Server Data");
                isBackup = false;
                return true;
            }
            if (ext is ".ldf")
            {
                type = DatabaseType.SQLServer;
                formatName = L.Pick("لاگ تراکنش SQL Server", "SQL Server Log");
                isBackup = false;
                return true;
            }
            if (ext is ".sqlite" or ".sqlite3" or ".db" or ".db3" or ".sqlitedb")
            {
                type = DatabaseType.SQLite;
                formatName = L.Pick("پایگاه داده SQLite", "SQLite Database");
                isBackup = false;
                return true;
            }
            if (ext is ".accdb" or ".mdb")
            {
                type = DatabaseType.Unknown;
                formatName = L.Pick("دیتابیس اکسس (Access)", "Access Database");
                isBackup = false;
                return true;
            }
            if (ext is ".ibd" or ".frm" or ".myd" or ".myi")
            {
                type = DatabaseType.MySQL;
                formatName = L.Pick("دیتابیس MySQL/MariaDB", "MySQL Database");
                isBackup = false;
                return true;
            }
            if (ext is ".dump")
            {
                type = DatabaseType.PostgreSQL;
                formatName = L.Pick("پشتیبان دامپ PostgreSQL", "PostgreSQL Dump");
                isBackup = true;
                return true;
            }
            if (ext is ".sql")
            {
                type = DatabaseType.Unknown;
                formatName = L.Pick("اسکریپت دیتابیس (SQL Dump)", "SQL Script / Dump");
                isBackup = true;
                return true;
            }
            if (ext is ".dbc" or ".dbf")
            {
                type = DatabaseType.Unknown;
                formatName = L.Pick("پایگاه داده فاکس‌پرو (FoxPro)", "FoxPro Database");
                isBackup = false;
                return true;
            }
            if (ext is ".fdb" or ".gdb")
            {
                type = DatabaseType.Unknown;
                formatName = L.Pick("پایگاه داده فایربیرد (Firebird)", "Firebird Database");
                isBackup = false;
                return true;
            }

            // بررسی الگوهای اسامی نرم‌افزارهای حسابداری
            foreach (var kw in AccountingKeywords)
            {
                if (fileName.Contains(kw))
                {
                    type = DatabaseType.SQLServer;
                    formatName = L.Pick("پشتیبان حسابداری / مالی", "Accounting Backup");
                    isBackup = true;
                    return true;
                }
            }

            return false;
        }

        private static long ParseOctal(byte[] buf, int offset, int length)
        {
            long val = 0;
            for (int i = offset; i < offset + length; i++)
            {
                byte b = buf[i];
                if (b == 0 || b == ' ' || b == '\t') continue;
                if (b >= '0' && b <= '7')
                {
                    val = (val << 3) + (b - '0');
                }
            }
            return val;
        }

        private static bool IsAllZero(byte[] buf)
        {
            for (int i = 0; i < buf.Length; i++)
            {
                if (buf[i] != 0) return false;
            }
            return true;
        }

        private static int ReadFullBlock(Stream stream, byte[] buffer, int count)
        {
            int total = 0;
            while (total < count)
            {
                int read = stream.Read(buffer, total, count - total);
                if (read <= 0) break;
                total += read;
            }
            return total;
        }

        private static void SkipStreamBytes(Stream stream, long count)
        {
            byte[] discard = new byte[8192];
            long remaining = count;
            while (remaining > 0)
            {
                int toRead = (int)Math.Min(discard.Length, remaining);
                int read = stream.Read(discard, 0, toRead);
                if (read <= 0) break;
                remaining -= read;
            }
        }
    }
}
