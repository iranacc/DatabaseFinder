using System.Collections.Concurrent;

namespace DatabaseFinder
{
    public class DiskFormat
    {
        public string Name { get; set; } = "";
        public DatabaseType Type { get; set; }
        public string[] Extensions { get; set; } = Array.Empty<string>();
        public bool IsBackup { get; set; }
        public string Description { get; set; } = "";
    }

    public static class DiskFormatRegistry
    {
        public static List<DiskFormat> All { get; } = new()
        {
            new DiskFormat
            {
                Name = "SQL Server",
                Type = DatabaseType.SQLServer,
                Extensions = new[] { ".mdf", ".ldf", ".ndf" },
                Description = "فایل‌های دیتابیس SQL Server (mdf/ldf/ndf)"
            },
            new DiskFormat
            {
                Name = "SQL Server (بکاپ)",
                Type = DatabaseType.SQLServer,
                Extensions = new[] { ".bak" },
                IsBackup = true,
                Description = "بکاپ‌های SQL Server (.bak)"
            },
            new DiskFormat
            {
                Name = "MySQL InnoDB",
                Type = DatabaseType.MySQL,
                Extensions = new[] { ".ibd", ".ibt" },
                Description = "فایل‌های جدول InnoDB (.ibd)"
            },
            new DiskFormat
            {
                Name = "MySQL MyISAM",
                Type = DatabaseType.MySQL,
                Extensions = new[] { ".myd", ".myi", ".frm" },
                Description = "فایل‌های MyISAM/جدول‌های قدیمی (.myd/.myi/.frm)"
            },
            new DiskFormat
            {
                Name = "SQLite",
                Type = DatabaseType.SQLite,
                Extensions = new[] { ".db", ".sqlite", ".sqlite3", ".sqlitedb" },
                Description = "دیتابیس‌های SQLite (نرم‌افزارهای سبک و حسابداری)"
            },
            new DiskFormat
            {
                Name = "Access",
                Type = DatabaseType.Unknown,
                Extensions = new[] { ".accdb", ".mdb" },
                Description = "دیتابیس‌های Microsoft Access"
            },
            new DiskFormat
            {
                Name = "FoxPro / dBase",
                Type = DatabaseType.Unknown,
                Extensions = new[] { ".dbf", ".dbt" },
                Description = "فایل‌های FoxPro/dBase - رایج در نرم‌افزارهای حسابداری ایرانی"
            },
            new DiskFormat
            {
                Name = "Firebird",
                Type = DatabaseType.Unknown,
                Extensions = new[] { ".fdb", ".gdb" },
                Description = "دیتابیس‌های Firebird (نرم‌افزارهای ایرانی)"
            },
            new DiskFormat
            {
                Name = "MongoDB (WiredTiger)",
                Type = DatabaseType.MongoDB,
                Extensions = new[] { ".wt" },
                Description = "فایل‌های داده MongoDB WiredTiger"
            },
            new DiskFormat
            {
                Name = "Redis",
                Type = DatabaseType.Redis,
                Extensions = new[] { ".rdb", ".aof" },
                Description = "فایل‌های داده و appendonly Redis"
            },
            new DiskFormat
            {
                Name = "بکاپ/آرشیو عمومی",
                Type = DatabaseType.Unknown,
                Extensions = new[] { ".zip", ".7z", ".rar", ".tar", ".tar.gz", ".gz", ".bkf" },
                IsBackup = true,
                Description = "آرشیو/بکاپ‌های احتمالی حاوی داده مالی"
            }
        };
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
            foreach (var f in formats)
            {
                foreach (var e in f.Extensions)
                {
                    extMap[e] = f;
                }
            }

            var p = new ScanProgress();
            foreach (var root in roots)
            {
                if (cancel.IsCancellationRequested) break;
                if (!Directory.Exists(root)) continue;
                WalkDir(root, extMap, minSizeBytes, found, p, progress, cancel);
            }
            p.IsFinished = true;
            progress?.Invoke(p);
            return found;
        }

        private void WalkDir(
            string dir,
            Dictionary<string, DiskFormat> extMap,
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
                    if (extMap.TryGetValue(ext, out var format))
                    {
                        try
                        {
                            var fi = new FileInfo(file);
                            if (fi.Length == 0) continue;
                            if (fi.Length < minSizeBytes) continue;

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
                    WalkDir(sub, extMap, minSizeBytes, found, p, progress, cancel);
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch
            {
            }
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
                    foreach (var sub in new[] { "data", "database", "databases", "backup", "backups", "accounting", "financial", "حسابداری", "مالی", "بکاپ", "پشتیبان" })
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