using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace DatabaseFinder
{
    public class DiskFormat
    {
        public string Name { get; set; } = "";
        public DatabaseType Type { get; set; }
        public string[] Extensions { get; set; } = Array.Empty<string>();
        public bool IsBackup { get; set; }
        public string Description { get; set; } = "";

        /// <summary>
        /// بررسی محتوای فایل پس از تطبیق پسوند؛ اگر null باشد فقط پسوند ملاک است.
        /// </summary>
        public Func<string, bool>? ContentValidator { get; set; }

        /// <summary>الگوهای نام فایل (regex) که به صورت نادقیق با نام فایل مقایسه می‌شوند.</summary>
        public string[] NamePatterns { get; set; } = Array.Empty<string>();

        /// <summary>نام پوشه‌هایی که فایل‌های داخل آن‌ها به این نرم‌افزار نسبت داده می‌شوند.</summary>
        public string[] FolderNames { get; set; } = Array.Empty<string>();
    }

    public static class DiskFormatRegistry
    {
        public static List<DiskFormat> All => new()
        {
            new DiskFormat
            {
                Name = "SQL Server",
                Type = DatabaseType.SQLServer,
                Extensions = new[] { ".mdf", ".ldf", ".ndf" },
                ContentValidator = FileSignatures.IsSqlServerFile,
                Description = L.Text("S199")
            },
            new DiskFormat
            {
                Name = L.Text("S200"),
                Type = DatabaseType.SQLServer,
                Extensions = new[] { ".bak" },
                IsBackup = true,
                ContentValidator = FileSignatures.IsSqlServerFile,
                Description = L.Text("S201")
            },
            new DiskFormat
            {
                Name = "MySQL InnoDB",
                Type = DatabaseType.MySQL,
                Extensions = new[] { ".ibd", ".ibt" },
                ContentValidator = FileSignatures.IsInnoDbFile,
                Description = L.Text("S202")
            },
            new DiskFormat
            {
                Name = "MySQL MyISAM",
                Type = DatabaseType.MySQL,
                Extensions = new[] { ".myd", ".myi", ".frm" },
                Description = L.Text("S203")
            },
            new DiskFormat
            {
                Name = "SQLite",
                Type = DatabaseType.SQLite,
                Extensions = new[] { ".db", ".sqlite", ".sqlite3", ".sqlitedb" },
                ContentValidator = FileSignatures.IsSqliteFile,
                Description = L.Text("S204")
            },
            new DiskFormat
            {
                Name = "Access",
                Type = DatabaseType.Unknown,
                Extensions = new[] { ".accdb", ".mdb" },
                ContentValidator = FileSignatures.IsAccessFile,
                Description = L.Text("S205")
            },
            new DiskFormat
            {
                Name = "FoxPro / dBase",
                Type = DatabaseType.Unknown,
                Extensions = new[] { ".dbf", ".dbt" },
                ContentValidator = FileSignatures.IsDBaseFile,
                Description = L.Text("S206")
            },
            new DiskFormat
            {
                Name = "Firebird",
                Type = DatabaseType.Unknown,
                Extensions = new[] { ".fdb", ".gdb" },
                ContentValidator = FileSignatures.IsFirebirdFile,
                Description = L.Text("S207")
            },
            new DiskFormat
            {
                Name = "MongoDB (WiredTiger)",
                Type = DatabaseType.MongoDB,
                Extensions = new[] { ".wt" },
                ContentValidator = FileSignatures.IsWtfFile,
                Description = L.Text("S208")
            },
            new DiskFormat
            {
                Name = "Redis",
                Type = DatabaseType.Redis,
                Extensions = new[] { ".rdb", ".aof" },
                ContentValidator = FileSignatures.IsRedisFile,
                Description = L.Text("S209")
            },
            new DiskFormat
            {
                Name = L.Text("S212"),
                Type = DatabaseType.SQLServer,
                Extensions = new[] { ".bak", ".zip" },
                IsBackup = true,
                NamePatterns = new[] { "mahak", @"(^|[^a-z0-9])ver\d+", "BeforeUpdate", "FullBackup" },
                ContentValidator = ValidateDbBackupOrArchive,
                Description = L.Text("S313")
            },
            new DiskFormat
            {
                Name = L.Text("S213"),
                Type = DatabaseType.SQLServer,
                Extensions = new[] { ".bak" },
                IsBackup = true,
                FolderNames = new[] { "parsian.back" },
                ContentValidator = FileSignatures.IsSqlServerFile,
                Description = L.Text("S314")
            },
            new DiskFormat
            {
                Name = L.Text("S214"),
                Type = DatabaseType.SQLServer,
                Extensions = new[] { ".zip", ".bak" },
                IsBackup = true,
                FolderNames = new[] { "holoo.bak" },
                ContentValidator = ValidateDbBackupOrArchive,
                Description = L.Text("S315")
            },
            new DiskFormat
            {
Name = L.Text("S210"),
                Type = DatabaseType.Unknown,
                Extensions = new[] { ".zip", ".7z", ".rar", ".tar", ".gz", ".bkf" },
                IsBackup = true,
                ContentValidator = p => ValidateArchive(p),
                Description = L.Text("S211")
            }
        };

        private static bool ValidateArchive(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            switch (ext)
            {
                case ".zip": return FileSignatures.IsZip(path);
                case ".7z": return FileSignatures.Is7z(path);
                case ".rar": return FileSignatures.IsRar(path);
                case ".tar": return FileSignatures.IsTar(path);
                case ".gz": return FileSignatures.IsGzip(path);
                default: return true;
            }
        }

        private static bool ValidateDbBackupOrArchive(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".bak") return FileSignatures.IsSqlServerFile(path);
            return ValidateArchive(path);
        }
    }

    public class DiskScanner
    {
        public class ScanProgress
        {
            public int DirectoriesScanned { get; set; }
            public int FilesScanned { get; set; }
            public int FilesFound { get; set; }
            public string CurrentDirectory { get; set; } = "";
            public bool IsFinished { get; set; }
        }

        private static readonly string[] SkipDirectories =
        {
            "windows", "system volume information", "$recycle.bin",
            "programdata\\microsoft", "appdata\\local\\temp", "windows.old",
            "node_modules", "\\system32", "\\syswow64", "intel"
        };

        public List<DatabaseInfo> Scan(
            IReadOnlyList<string> roots,
            IReadOnlyList<DiskFormat> formats,
            long minSizeBytes,
            Action<ScanProgress>? progress,
            CancellationToken cancel)
        {
            var found = new List<DatabaseInfo>();
            var extMap = new Dictionary<string, DiskFormat>(StringComparer.OrdinalIgnoreCase);
            var nameRules = new List<(Regex Regex, DiskFormat Format)>();
            var dirMap = new Dictionary<string, DiskFormat>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in formats)
            {
                foreach (var e in f.Extensions)
                {
                    extMap[e] = f;
                }

                if (f.NamePatterns.Length > 0)
                {
                    nameRules.Add((new Regex(string.Join("|", f.NamePatterns), RegexOptions.IgnoreCase | RegexOptions.Compiled), f));
                }

                foreach (var fn in f.FolderNames)
                {
                    if (!string.IsNullOrWhiteSpace(fn)) dirMap[fn] = f;
                }
            }

            var p = new ScanProgress();
            foreach (var root in roots)
            {
                if (cancel.IsCancellationRequested) break;
                if (!Directory.Exists(root)) continue;
                WalkDir(root, extMap, nameRules, dirMap, null, minSizeBytes, found, p, progress, cancel);
            }
            p.IsFinished = true;
            progress?.Invoke(p);
            return found;
        }

        private void WalkDir(
            string dir,
            Dictionary<string, DiskFormat> extMap,
            List<(Regex Regex, DiskFormat Format)> nameRules,
            Dictionary<string, DiskFormat> dirMap,
            DiskFormat? folderFormat,
            long minSizeBytes,
            List<DatabaseInfo> found,
            ScanProgress p,
            Action<ScanProgress>? progress,
            CancellationToken cancel)
        {
            if (cancel.IsCancellationRequested) return;

            try
            {
                foreach (var file in Directory.EnumerateFiles(dir))
                {
                    if (cancel.IsCancellationRequested) return;

                    string ext;
                    try
                    {
                        ext = Path.GetExtension(file);
                    }
                    catch
                    {
                        continue;
                    }

                    p.FilesScanned++;
                    var format = MatchFormat(file, Path.GetFileName(file), ext, extMap, nameRules, folderFormat);
                    if (format != null)
                    {
                        try
                        {
                            var fi = new FileInfo(file);
                            if (fi.Length == 0) continue;
                            if (fi.Length < minSizeBytes) continue;
                            if (format.ContentValidator != null && !format.ContentValidator(file)) continue;

                            var candidate = GuessDatabaseName(file, fi.Name);
                            found.Add(new DatabaseInfo
                            {
                                Type = format.Type,
                                Name = candidate,
                                IsOnline = false,
                                LocalPath = file,
                                FileSize = fi.Length,
                                FileModified = fi.LastWriteTime,
                                IsBackup = format.IsBackup,
                                FormatName = format.Name,
                                Host = ""
                            });
                            p.FilesFound++;
                        }
                        catch
                        {
                            // فایل قفل یا حذف شده
                        }
                    }

                    if (p.FilesScanned % 2000 == 0)
                    {
                        progress?.Invoke(p);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                progress?.Invoke(p);
            }
            catch
            {
                progress?.Invoke(p);
            }

            try
            {
                foreach (var sub in Directory.EnumerateDirectories(dir))
                {
                    if (cancel.IsCancellationRequested) return;
                    p.DirectoriesScanned++;
                    p.CurrentDirectory = sub;
                    if (p.DirectoriesScanned % 50 == 0) progress?.Invoke(p);
                    if (ShouldSkip(sub)) continue;
                    dirMap.TryGetValue(Path.GetFileName(sub), out var marked);
                    WalkDir(sub, extMap, nameRules, dirMap, marked ?? folderFormat, minSizeBytes, found, p, progress, cancel);
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch
            {
            }
        }

        private static DiskFormat? MatchFormat(
            string file,
            string fileName,
            string ext,
            Dictionary<string, DiskFormat> extMap,
            List<(Regex Regex, DiskFormat Format)> nameRules,
            DiskFormat? folderFormat)
        {
            if (folderFormat != null && MatchesExtension(folderFormat, ext))
            {
                return folderFormat;
            }

            foreach (var (rx, nf) in nameRules)
            {
                if (!MatchesExtension(nf, ext)) continue;
                if (!rx.IsMatch(fileName)) continue;
                return nf;
            }

            extMap.TryGetValue(ext, out var ef);
            return ef;
        }

        private static bool MatchesExtension(DiskFormat format, string ext)
        {
            if (format.Extensions.Length == 0) return true;
            foreach (var e in format.Extensions)
            {
                if (string.Equals(e, ext, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static bool ShouldSkip(string dir)
        {
            var lower = dir.ToLowerInvariant();
            foreach (var s in SkipDirectories)
            {
                if (lower.Contains(s, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        public static string GuessDatabaseName(string filePath, string fileName)
        {
            var name = Path.GetFileNameWithoutExtension(filePath);
            if (!string.IsNullOrWhiteSpace(name))
            {
                if (name.Length <= 40 && !name.Equals("database", StringComparison.OrdinalIgnoreCase)
                    && !name.Equals("db", StringComparison.OrdinalIgnoreCase)
                    && !name.Equals("backup", StringComparison.OrdinalIgnoreCase)
                    && !name.StartsWith("data_", StringComparison.OrdinalIgnoreCase))
                {
                    return name;
                }
            }

            var parent = Path.GetFileName(Path.GetDirectoryName(filePath) ?? "");
            if (!string.IsNullOrWhiteSpace(parent))
            {
                return parent;
            }

            return fileName;
        }

        public static List<string> GetDefaultRoots(bool quickOnly)
        {
            var roots = new List<string>();

            if (quickOnly)
            {
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed))
                {
                    var baseDir = drive.RootDirectory.FullName;
                    AddIfExists(roots, Path.Combine(baseDir, "Program Files"));
                    AddIfExists(roots, Path.Combine(baseDir, "Program Files (x86)"));
                    AddIfExists(roots, Path.Combine(baseDir, "ProgramData"));

                    // پوشه‌های کاربران
                    var users = Path.Combine(baseDir, "Users");
                    if (Directory.Exists(users))
                    {
                        foreach (var profile in Directory.EnumerateDirectories(users))
                        {
                            AddIfExists(roots, Path.Combine(profile, "Documents"));
                            AddIfExists(roots, Path.Combine(profile, "Desktop"));
                            AddIfExists(roots, Path.Combine(profile, "AppData"));
                        }
                    }

                    // پوشه‌های ریشه با نام‌های مرتبط با حسابداری/دیتابیس/بکاپ
                    foreach (var sub in new[] { "data", "database", "databases", "backup", "backups", "accounting", "financial", "حسابداری", "مالی", "بکاپ", "پشتیبان", "parsian.back", "holoo.bak" })
                    {
                        AddIfExists(roots, Path.Combine(baseDir, sub));
                    }
                }
            }
            else
            {
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed))
                {
                    roots.Add(drive.RootDirectory.FullName);
                }
            }

            return roots.Distinct().OrderBy(r => r).ToList();
        }

        private static void AddIfExists(List<string> roots, string path)
        {
            try
            {
                if (Directory.Exists(path)) roots.Add(path);
            }
            catch { }
        }
    }
}
