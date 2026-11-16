using Microsoft.Win32;

namespace DatabaseFinder
{
    public enum RootCategory
    {
        ExternalDrive,      // 🔌 فلش مموری و هارد اکسترنال
        NetworkDrive,       // 🌐 درایو شبکه و اشتراکی
        FixedDrive,         // 💽 درایو داخلی سیستم
        AccountingSoftware, // 🎯 نرم‌افزارهای مالی و حسابداری
        DatabaseData,       // 💾 مسیرهای داده و پشتیبان SQL Server
        UserDocuments,      // 📁 دسکتاپ، اسناد و دانلودها
        CustomFolder,       // 📂 پوشه دستی افزوده شده
        ShadowCopy          // 🕰️ نسخه قبلی ویندوز (Shadow Copy)
    }

    public enum ScanMode
    {
        Smart,          // اسکن هوشمند (مسیرهای مالی + دیتابیس + درایوهای اکسترنال + پوشه‌های کاربران)
        Full,           // اسکن کامل (تمام درایوهای داخلی + اکسترنال + شبکه)
        ExternalOnly,   // فقط حافظه‌های جانبی و فلش مموری‌های اکسترنال
        ShadowCopies    // 🕰️ فقط نسخه‌های قبلی ویندوز (Shadow Copies)
    }

    public class DiscoveredRoot
    {
        public string Path { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public RootCategory Category { get; set; }
        public bool IsSelectedByDefault { get; set; } = true;
        public long TotalBytes { get; set; }
        public long FreeBytes { get; set; }
        public ShadowCopySnapshot? ShadowSnapshot { get; set; }

        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// پویشگر هوشمند مسیرهای حسابداری، پایگاه‌های داده و درایوهای اکسترنال و شبکه.
    /// </summary>
    public static class SmartPathLocator
    {
        private static readonly string[] AccountingDirNames =
        {
            // سپیدار و همکاران سیستم
            "Sepidar", "SepidarSystem", "Sepidar.bak", "SepidarBackup", "SystemGroup", "Rahkaran", "SG_Data",
            // هلو و طرفه‌نگار
            "Holoo", "Holoo_Data", "Holoo.bak", "Holoo_Backup", "TarafNegar",
            // پارسیان
            "Parsian", "Parsian.back", "Parsian_Data", "ParsianAccounting",
            // تدبیر
            "Tadbir", "Tadbir_Data", "Tadbir_Bak", "TadbirAccounting",
            // شایگان و حسابگر
            "Shaygan", "CyberSystem", "Hesabgar", "Cyber_Data", "Shaygan.bak",
            // محک
            "Mahak", "Mahak.bak", "MahakData", "MahakAccounting",
            // نوسا
            "Nosa", "AccXP", "Nosa_Backup", "NosaData",
            // قیاس، پیوست، پایا، کوثر
            "Ghias", "GhiasData", "Payvast", "Payvast_Data", "Paya", "PayaSystem", "Kowsar", "Kowsar_Backup",
            // آرین سیستم، سانیران
            "Arian", "ArianSystem", "Saniran",
            // پوشه‌های عمومی پشتیبان و حسابداری
            "Backups", "Backup", "AutoBackup", "DB_Backup", "SQL_Backup", "DatabaseBackup", "Databases", "Database",
            // اسامی فارسی
            "بکاپ", "پشتیبان", "بک آپ", "حسابداری", "مالی", "دفاتر", "اطلاعات مالی", "پایگاه داده"
        };

        public static List<DiscoveredRoot> GetDiscoveredRoots(ScanMode mode)
        {
            var results = new List<DiscoveredRoot>();
            var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var drives = GetAvailableDrives();

            // 1. بررسی درایوهای اکسترنال و فلش مموری‌ها (در همه حالت‌ها اضافه می‌شوند)
            foreach (var d in drives.Where(d => d.DriveType == DriveType.Removable))
            {
                var rootDir = d.RootDirectory.FullName;
                if (seenPaths.Add(rootDir))
                {
                    results.Add(new DiscoveredRoot
                    {
                        Path = rootDir,
                        DisplayName = FormatDriveDisplay(d, "🔌 " + L.Pick("[اکسترنال / USB]", "[External / USB]")),
                        Category = RootCategory.ExternalDrive,
                        IsSelectedByDefault = true,
                        TotalBytes = TryGetTotalSize(d),
                        FreeBytes = TryGetFreeSpace(d)
                    });
                }
            }

            if (mode == ScanMode.ExternalOnly)
            {
                // در حالت فقط اکسترنال، اگر فلش پیدا شد برمی‌گرداند؛ در غیر اینصورت کل فهرست را برمی‌گرداند
                if (results.Count > 0) return results;
                // اگر هیچ درایو Removable نبود، پیامی برمی‌گرداند یا ریشه‌ها را خالی می‌گذارد
                return results;
            }

            if (mode == ScanMode.ShadowCopies)
            {
                var shadowSnapshots = ShadowCopyFinder.GetAvailableShadowCopies();
                if (shadowSnapshots.Count == 0)
                {
                    bool isAdmin = SysadminGranter.IsAdministrator();
                    results.Add(new DiscoveredRoot
                    {
                        Path = "",
                        DisplayName = isAdmin
                            ? L.Pick("⚠️ هیچ Shadow Copy در این سیستم یافت نشد", "⚠️ No Shadow Copies found on this system")
                            : L.Pick("⚠️ برای مشاهده Shadow Copies اجرای برنامه با Run as Administrator لازم است", "⚠️ Running as Administrator required to view Shadow Copies"),
                        Category = RootCategory.ShadowCopy,
                        IsSelectedByDefault = false
                    });
                    return results;
                }

                foreach (var snap in shadowSnapshots)
                {
                    results.Add(new DiscoveredRoot
                    {
                        Path = snap.DeviceObject,
                        DisplayName = $"🕰️ {snap.DisplayName}",
                        Category = RootCategory.ShadowCopy,
                        IsSelectedByDefault = true,
                        ShadowSnapshot = snap
                    });
                }
                return results;
            }

            if (mode == ScanMode.Full)
            {
                // اسکن کامل: ریشه تمام درایوهای Fixed, Removable, Network
                foreach (var d in drives)
                {
                    var rootDir = d.RootDirectory.FullName;
                    if (seenPaths.Add(rootDir))
                    {
                        string prefix = d.DriveType switch
                        {
                            DriveType.Removable => "🔌 " + L.Pick("[اکسترنال / USB]", "[External / USB]"),
                            DriveType.Network => "🌐 " + L.Pick("[شبکه / Mapped]", "[Network Share]"),
                            _ => "💽 " + L.Pick("[درایو داخلی]", "[Fixed Drive]")
                        };

                        results.Add(new DiscoveredRoot
                        {
                            Path = rootDir,
                            DisplayName = FormatDriveDisplay(d, prefix),
                            Category = d.DriveType == DriveType.Removable ? RootCategory.ExternalDrive
                                      : d.DriveType == DriveType.Network ? RootCategory.NetworkDrive
                                      : RootCategory.FixedDrive,
                            IsSelectedByDefault = true,
                            TotalBytes = TryGetTotalSize(d),
                            FreeBytes = TryGetFreeSpace(d)
                        });
                    }
                }
                return results;
            }

            // ---- حالت اسکن هوشمند (ScanMode.Smart) ----

            // 2. مسیرهای داده و پشتیبان SQL Server شناسایی‌شده از رجیستری
            foreach (var sqlPath in GetSqlServerPathsFromRegistry())
            {
                if (Directory.Exists(sqlPath) && seenPaths.Add(sqlPath))
                {
                    results.Add(new DiscoveredRoot
                    {
                        Path = sqlPath,
                        DisplayName = "💾 " + L.Pick("[دیتابیس/بکاپ SQL Server]", "[SQL Server Data/Backup]") + " " + sqlPath,
                        Category = RootCategory.DatabaseData,
                        IsSelectedByDefault = true
                    });
                }
            }

            // 3. پویش هوشمند نرم‌افزارهای حسابداری در ریشه تمام درایوها
            foreach (var d in drives)
            {
                var baseDir = d.RootDirectory.FullName;

                // پوشه‌های اختصاصی SQL Server روی درایو
                ProbeSqlServerDirectories(baseDir, results, seenPaths);

                // بررسی پوشه‌های حسابداری در ریشه
                foreach (var name in AccountingDirNames)
                {
                    var target = Path.Combine(baseDir, name);
                    if (Directory.Exists(target) && seenPaths.Add(target))
                    {
                        results.Add(new DiscoveredRoot
                        {
                            Path = target,
                            DisplayName = "🎯 " + L.Pick("[نرم‌افزار مالی]", "[Accounting]") + " " + target,
                            Category = RootCategory.AccountingSoftware,
                            IsSelectedByDefault = true
                        });
                    }
                }

                // ProgramData (محل نگهداری دیتای سپیدار، هلو و سرویس‌ها)
                var progData = Path.Combine(baseDir, "ProgramData");
                if (Directory.Exists(progData))
                {
                    if (seenPaths.Add(progData))
                    {
                        results.Add(new DiscoveredRoot
                        {
                            Path = progData,
                            DisplayName = "📁 ProgramData (" + baseDir + ")",
                            Category = RootCategory.AccountingSoftware,
                            IsSelectedByDefault = true
                        });
                    }

                    // زیرپوشه‌های مشخص ProgramData
                    foreach (var sub in new[] { "Sepidar", "SystemGroup", "Holoo", "Parsian", "Tadbir" })
                    {
                        var subPath = Path.Combine(progData, sub);
                        if (Directory.Exists(subPath) && seenPaths.Add(subPath))
                        {
                            results.Add(new DiscoveredRoot
                            {
                                Path = subPath,
                                DisplayName = "🎯 " + subPath,
                                Category = RootCategory.AccountingSoftware,
                                IsSelectedByDefault = true
                            });
                        }
                    }
                }

                // Program Files و Program Files (x86)
                var pf = Path.Combine(baseDir, "Program Files");
                if (Directory.Exists(pf) && seenPaths.Add(pf))
                {
                    results.Add(new DiscoveredRoot
                    {
                        Path = pf,
                        DisplayName = "📁 Program Files (" + baseDir + ")",
                        Category = RootCategory.FixedDrive,
                        IsSelectedByDefault = true
                    });
                }
                var pf86 = Path.Combine(baseDir, "Program Files (x86)");
                if (Directory.Exists(pf86) && seenPaths.Add(pf86))
                {
                    results.Add(new DiscoveredRoot
                    {
                        Path = pf86,
                        DisplayName = "📁 Program Files (x86) (" + baseDir + ")",
                        Category = RootCategory.FixedDrive,
                        IsSelectedByDefault = true
                    });
                }

                // پوشه‌های کاربران (Desktop, Documents, Downloads)
                var users = Path.Combine(baseDir, "Users");
                if (Directory.Exists(users))
                {
                    try
                    {
                        foreach (var profile in Directory.EnumerateDirectories(users))
                        {
                            var profName = Path.GetFileName(profile);
                            if (profName.Equals("All Users", StringComparison.OrdinalIgnoreCase) ||
                                profName.Equals("Default", StringComparison.OrdinalIgnoreCase) ||
                                profName.Equals("Public", StringComparison.OrdinalIgnoreCase))
                                continue;

                            AddUserFolder(profile, "Desktop", results, seenPaths);
                            AddUserFolder(profile, "Documents", results, seenPaths);
                            AddUserFolder(profile, "Downloads", results, seenPaths);
                        }
                    }
                    catch { }
                }
            }

            // 4. درایوهای شبکه
            foreach (var d in drives.Where(d => d.DriveType == DriveType.Network))
            {
                var rootDir = d.RootDirectory.FullName;
                if (seenPaths.Add(rootDir))
                {
                    results.Add(new DiscoveredRoot
                    {
                        Path = rootDir,
                        DisplayName = FormatDriveDisplay(d, "🌐 " + L.Pick("[اشتراک شبکه]", "[Network Share]")),
                        Category = RootCategory.NetworkDrive,
                        IsSelectedByDefault = true,
                        TotalBytes = TryGetTotalSize(d),
                        FreeBytes = TryGetFreeSpace(d)
                    });
                }
            }

            return results;
        }

        private static void AddUserFolder(string profile, string sub, List<DiscoveredRoot> results, HashSet<string> seenPaths)
        {
            var p = Path.Combine(profile, sub);
            if (Directory.Exists(p) && seenPaths.Add(p))
            {
                results.Add(new DiscoveredRoot
                {
                    Path = p,
                    DisplayName = "📁 " + p,
                    Category = RootCategory.UserDocuments,
                    IsSelectedByDefault = true
                });
            }
        }

        private static void ProbeSqlServerDirectories(string baseDir, List<DiscoveredRoot> results, HashSet<string> seenPaths)
        {
            foreach (var prog in new[] { "Program Files", "Program Files (x86)" })
            {
                var sqlRoot = Path.Combine(baseDir, prog, "Microsoft SQL Server");
                if (!Directory.Exists(sqlRoot)) continue;

                try
                {
                    foreach (var inst in Directory.EnumerateDirectories(sqlRoot, "MSSQL*"))
                    {
                        var dataDir = Path.Combine(inst, "MSSQL", "DATA");
                        if (Directory.Exists(dataDir) && seenPaths.Add(dataDir))
                        {
                            results.Add(new DiscoveredRoot
                            {
                                Path = dataDir,
                                DisplayName = "💾 " + L.Pick("[پایگاه‌های داده SQL Server]", "[SQL Server DATA]") + " " + dataDir,
                                Category = RootCategory.DatabaseData,
                                IsSelectedByDefault = true
                            });
                        }

                        var backupDir = Path.Combine(inst, "MSSQL", "Backup");
                        if (Directory.Exists(backupDir) && seenPaths.Add(backupDir))
                        {
                            results.Add(new DiscoveredRoot
                            {
                                Path = backupDir,
                                DisplayName = "💾 " + L.Pick("[بکاپ‌های SQL Server]", "[SQL Server Backup]") + " " + backupDir,
                                Category = RootCategory.DatabaseData,
                                IsSelectedByDefault = true
                            });
                        }
                    }
                }
                catch { }
            }
        }

        private static List<string> GetSqlServerPathsFromRegistry()
        {
            var paths = new List<string>();

            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                    using var sqlKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL");
                    if (sqlKey != null)
                    {
                        foreach (var valName in sqlKey.GetValueNames())
                        {
                            var instKeyName = sqlKey.GetValue(valName)?.ToString();
                            if (!string.IsNullOrEmpty(instKeyName))
                            {
                                using var instKey = baseKey.OpenSubKey($@"SOFTWARE\Microsoft\Microsoft SQL Server\{instKeyName}\MSSQLServer");
                                if (instKey != null)
                                {
                                    AddRegPath(paths, instKey.GetValue("DefaultData")?.ToString());
                                    AddRegPath(paths, instKey.GetValue("DefaultLog")?.ToString());
                                    AddRegPath(paths, instKey.GetValue("BackupDirectory")?.ToString());
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            return paths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static void AddRegPath(List<string> paths, string? p)
        {
            if (!string.IsNullOrWhiteSpace(p) && Directory.Exists(p))
            {
                paths.Add(p);
            }
        }

        private static List<DriveInfo> GetAvailableDrives()
        {
            var list = new List<DriveInfo>();
            try
            {
                foreach (var d in DriveInfo.GetDrives())
                {
                    if (d.IsReady) list.Add(d);
                }
            }
            catch { }
            return list;
        }

        private static string FormatDriveDisplay(DriveInfo d, string prefix)
        {
            var label = "";
            try
            {
                if (!string.IsNullOrWhiteSpace(d.VolumeLabel))
                    label = $" «{d.VolumeLabel}»";
            }
            catch { }

            var sizeStr = "";
            try
            {
                sizeStr = $" - {DatabaseFileLocator.FormatSize(d.TotalSize)}";
            }
            catch { }

            return $"{prefix} {d.Name}{label}{sizeStr}";
        }

        private static long TryGetTotalSize(DriveInfo d)
        {
            try { return d.TotalSize; } catch { return 0; }
        }

        private static long TryGetFreeSpace(DriveInfo d)
        {
            try { return d.TotalFreeSpace; } catch { return 0; }
        }
    }
}
