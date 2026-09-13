using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;
using StackExchange.Redis;

namespace DatabaseFinder
{
    public enum LockedFileHandling
    {
        /// <summary>کپی فایل قفل‌شده از Shadow Copy (VSS) بدون توقف سرویس.</summary>
        Vss,
        /// <summary>توقف خودکار سرویس‌های دیتابیس، کپی، و راه‌اندازی مجدد.</summary>
        StopServices,
        /// <summary>فقط گزارش خطا.</summary>
        ReportOnly
    }

    public class FileCopyItem
    {
        public string DisplayName { get; set; } = "";
        public string SourcePath { get; set; } = "";
        public string RelativePath { get; set; } = "";
        public long Size { get; set; }
        public bool Locked { get; set; }
        public string? Error { get; set; }
        /// <summary>آخرین تغییر فایل (سرنخ قدمت دیتا).</summary>
        public DateTime Modified { get; set; }
        /// <summary>فایل همین حالا توسط پروسسی قفل است (سرنخ زنده بودن).</summary>
        public bool IsInUse { get; set; }
        public string? InUseBy { get; set; }
        /// <summary>هش مبدأ/مقصد و مسیر نهایی برای اثبات یکسان بودن برداشت.</summary>
        public string? SourceHash { get; set; }
        public string? DestHash { get; set; }
        public string? CopiedPath { get; set; }
        public bool Verified { get; set; }
    }

    public class DatabaseCopyItem
    {
        public DatabaseInfo Server { get; set; } = new();
        public string DatabaseName { get; set; } = "";
        public string FolderName { get; set; } = "";
        public List<FileCopyItem> Files { get; set; } = new();
        public long TotalSize => Files.Sum(f => f.Size);
        public string? Error { get; set; }
        public bool UseManualPath { get; set; }
        public string ManualPath { get; set; } = "";
    }

    public static class DatabaseFileLocator
    {
        public static List<DatabaseCopyItem> BuildPlan(List<DatabaseInfo> servers, Action<string>? log = null)
        {
            var items = new List<DatabaseCopyItem>();
            foreach (var server in servers)
            {
                log?.Invoke(L.Format("S116", server.TypeDisplayName));

                if (server.IsServiceStopped && server.Type == DatabaseType.SQLServer && IsLocalHost(server))
                {
                    items.AddRange(EnumerateStoppedSqlServer(server, log));
                    continue;
                }

                if (!server.IsOnline)
                {
                    items.Add(BuildOfflineItem(server, log));
                    continue;
                }

                if (!IsLocalHost(server))
                {
                    items.Add(ErrorItem(server, server.Name,
                        L.Text("S117")));
                    continue;
                }

                try
                {
                    switch (server.Type)
                    {
                        case DatabaseType.SQLServer:
                            // سرویس خاموش: فایل‌ها قفل نیستند؛ مستقیم از پوشه DATA برمی‌داریم
                            if (server.IsServiceStopped)
                                items.AddRange(EnumerateStoppedSqlServer(server, log));
                            else
                                items.AddRange(EnumerateSqlServer(server, log));
                            break;
                        case DatabaseType.MySQL:
                        case DatabaseType.MariaDB:
                            items.AddRange(EnumerateMySql(server, log));
                            break;
                        case DatabaseType.PostgreSQL:
                            items.AddRange(EnumeratePostgre(server, log));
                            break;
                        case DatabaseType.Redis:
                            items.Add(EnumerateRedis(server, log));
                            break;
                        case DatabaseType.MongoDB:
                            items.AddRange(EnumerateMongoDb(server, log));
                            break;
                        default:
                            items.Add(ErrorItem(server, server.Name,
                                L.Text("S118")));
                            break;
                    }
                }
                catch (Exception ex)
                {
                    items.Add(ErrorItem(server, server.Name,
                        L.Format("S119", ex.Message)));
                }
            }

            // حذف موارد تکراری (همان دیتابیس از چند روش تشخیص)
            return items
                .GroupBy(i => i.Server.IsOnline
                    ? $"{i.Server.Type}-{i.Server.Port}-{i.DatabaseName}"
                    : (i.Files.FirstOrDefault()?.SourcePath ?? Guid.NewGuid().ToString()))
                .Select(g => g.First())
                .ToList();
        }

        /// <summary>
        /// فایل‌های همراه SQLite (همان نام + ‎-wal / ‎-shm). بدون این‌ها، تراکنش‌های
        /// آخر (ثبت‌های امروز مودی) در کپی نیست و تصویر دیتا ناقص می‌ماند.
        /// </summary>
        public static List<string> GetSqliteCompanions(string dbPath)
        {
            var found = new List<string>();
            try
            {
                foreach (var suffix in new[] { "-wal", "-shm" })
                {
                    var p = dbPath + suffix;
                    if (File.Exists(p)) found.Add(p);
                }
            }
            catch { }
            return found;
        }

        private static bool IsSqliteFile(DatabaseInfo server, string path)
        {
            if (server.Type == DatabaseType.SQLite) return true;
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext is ".db" or ".sqlite" or ".sqlite3" or ".sqlitedb";
        }

        private static DatabaseCopyItem BuildOfflineItem(DatabaseInfo server, Action<string>? log = null)
        {
            if (!string.IsNullOrEmpty(server.LocalPath) && File.Exists(server.LocalPath))
            {
                var fi = new FileInfo(server.LocalPath);
                var format = string.IsNullOrEmpty(server.FormatName) ? server.TypeDisplayName : server.FormatName;
                var item = new DatabaseCopyItem
                {
                    Server = server,
                    DatabaseName = server.Name,
                    FolderName = SafeFolder($"{format}_{server.Name}")
                };
                item.Files.Add(new FileCopyItem
                {
                    DisplayName = fi.Name,
                    SourcePath = fi.FullName,
                    RelativePath = fi.Name,
                    Size = fi.Length,
                    Modified = fi.LastWriteTime
                });
                if (IsSqliteFile(server, fi.FullName))
                {
                    var companions = GetSqliteCompanions(fi.FullName);
                    foreach (var c in companions)
                    {
                        try
                        {
                            var cfi = new FileInfo(c);
                            item.Files.Add(new FileCopyItem
                            {
                                DisplayName = cfi.Name,
                                SourcePath = cfi.FullName,
                                RelativePath = cfi.Name,
                                Size = cfi.Length,
                                Modified = cfi.LastWriteTime
                            });
                        }
                        catch { }
                    }
                    if (companions.Count > 0)
                        log?.Invoke(L.Format("S385", string.Join(", ", companions.Select(Path.GetFileName))));
                }
                return item;
            }

            return ErrorItem(server, server.Name,
                L.Text("S120"));
        }

        private static DatabaseCopyItem ErrorItem(DatabaseInfo server, string dbName, string error)
        {
            return new DatabaseCopyItem
            {
                Server = server,
                DatabaseName = string.IsNullOrEmpty(dbName) ? server.Name : dbName,
                FolderName = SafeFolder($"{server.TypeDisplayName}_{dbName}"),
                Error = error
            };
        }

        public static string SafeFolder(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            name = name.Trim().TrimEnd('.');
            return string.IsNullOrEmpty(name) ? "Database" : name;
        }

        private static bool IsLocalHost(DatabaseInfo server)
        {
            var h = (server.Host ?? "").Trim();
            if (string.IsNullOrEmpty(h)) return true;
            if (h.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;
            return h == "127.0.0.1" || h == "::1" || h == "[::1]" || h == "0.0.0.0";
        }

        // ---------- SQL Server ----------
        private static List<DatabaseCopyItem> EnumerateSqlServer(DatabaseInfo server, Action<string>? log)
        {
            try
            {
                return EnumerateSqlServerViaQuery(server, log);
            }
            catch (Exception ex) when (IsLoginFailure(ex))
            {
                // بدون پسورد sa: فایل‌های فیزیکی از روی رجیستری/سرویس پیدا می‌شوند؛ کپی با VSS.
                log?.Invoke(L.Text("S347"));
                AppLog.Write("SqlCopy.Fallback", ex);
                return EnumerateSqlServerFilesWithoutAuth(server, log);
            }
        }

        private static bool IsLoginFailure(Exception ex)
        {
            var msg = ex.Message ?? "";
            if (msg.IndexOf("login failed", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (msg.IndexOf("18456", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (msg.IndexOf("18452", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (msg.IndexOf("not associated with a trusted", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            // هر خطای اتصال SQL را هم به fallback ببر تا ماموریت متوقف نشود
            return ex is Microsoft.Data.SqlClient.SqlException || ex is InvalidOperationException;
        }

        /// <summary>
        /// سرویس SQL خاموش است: فایل‌ها قفل نیستند، پس همان برداشت بدون لاگین
        /// کافی است (کپی مستقیم، بدون نیاز به VSS).
        /// </summary>
        private static List<DatabaseCopyItem> EnumerateStoppedSqlServer(DatabaseInfo server, Action<string>? log)
        {
            var dirs = SqlServerPathResolver.GetDataDirectories(server);
            if (dirs.Count == 0)
            {
                return new List<DatabaseCopyItem> { ErrorItem(server, server.Name, L.Text("S348")) };
            }
            var built = BuildNoAuthItems(server, SqlServerPathResolver.EnumerateDataFiles(dirs));
            if (built.Count == 0)
            {
                return new List<DatabaseCopyItem> { ErrorItem(server, server.Name, L.Text("S348")) };
            }
            log?.Invoke(L.Format("S353", built.Count, string.Join("; ", dirs)));
            return built;
        }

        private static List<DatabaseCopyItem> EnumerateSqlServerFilesWithoutAuth(DatabaseInfo server, Action<string>? log)
        {
            var result = new List<DatabaseCopyItem>();
            var dirs = SqlServerPathResolver.GetDataDirectories(server);
            if (dirs.Count == 0)
            {
                result.Add(ErrorItem(server, server.Name, L.Text("S348")));
                return result;
            }

            var files = SqlServerPathResolver.EnumerateDataFiles(dirs);
            var built = BuildNoAuthItems(server, files);
            if (built.Count == 0)
            {
                result.Add(ErrorItem(server, server.Name, L.Text("S348")));
                return result;
            }

            result.AddRange(built);
            log?.Invoke(L.Format("S353", result.Count, string.Join("; ", dirs)));
            return result;
        }

        /// <summary>
        /// فایل‌های mdf/ldf/ndf را بر اساس (پوشه + نام) گروه‌بندی و به آیتم کپی تبدیل می‌کند؛
        /// نام پوشه مقصد یکتا می‌شود تا فایل‌های هم‌نامِ پوشه‌های مختلف روی هم نروند.
        /// </summary>
        private static List<DatabaseCopyItem> BuildNoAuthItems(DatabaseInfo server, List<string> files)
        {
            var result = new List<DatabaseCopyItem>();
            var usedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var groups = files
                .Where(f => !string.IsNullOrEmpty(Path.GetDirectoryName(f)))
                .GroupBy(f => (Path.GetDirectoryName(f) ?? "").ToLowerInvariant() + "|" +
                              NormalizeSqlFileStem(Path.GetFileNameWithoutExtension(f)))
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var g in groups)
            {
                // نام‌گذاری رایج SQL Server: Database.mdf، Database_log.ldf و Database_1.ndf
                if (!g.Any(f => f.EndsWith(".mdf", StringComparison.OrdinalIgnoreCase))) continue;
                var baseFile = g.First(f => f.EndsWith(".mdf", StringComparison.OrdinalIgnoreCase));
                var baseName = Path.GetFileNameWithoutExtension(baseFile);
                var instance = SqlInstanceLabel(server);
                var folderPrefix = string.IsNullOrEmpty(instance)
                    ? server.TypeDisplayName
                    : $"{server.TypeDisplayName}_{instance}";
                var folder = SafeFolder($"{folderPrefix}_{baseName}");
                for (var i = 2; usedFolders.Contains(folder); i++)
                    folder = SafeFolder($"{server.TypeDisplayName}_{baseName}_{i}");
                usedFolders.Add(folder);

                var item = new DatabaseCopyItem
                {
                    Server = server,
                    DatabaseName = baseName + L.Text("S349"),
                    FolderName = folder
                };
                foreach (var path in g)
                {
                    try
                    {
                        var fi = new FileInfo(path);
                        if (!fi.Exists) continue;
                        var cf = new FileCopyItem
                        {
                            DisplayName = fi.Name,
                            SourcePath = fi.FullName,
                            RelativePath = fi.Name,
                            Size = fi.Length,
                            Modified = fi.LastWriteTime
                        };
                        FileLockProbe.Apply(cf);
                        item.Files.Add(cf);
                    }
                    catch { }
                }
                if (item.Files.Count > 0) result.Add(item);
            }

            return result;
        }

        private static string SqlInstanceLabel(DatabaseInfo server)
        {
            var service = server.ServiceName?.Trim() ?? "";
            if (service.Equals("MSSQLSERVER", StringComparison.OrdinalIgnoreCase))
                return "MSSQLSERVER";
            if (service.StartsWith("MSSQL$", StringComparison.OrdinalIgnoreCase))
                return service.Substring("MSSQL$".Length);
            return service;
        }

        private static string NormalizeSqlFileStem(string stem)
        {
            var normalized = stem.Trim().ToLowerInvariant();
            string previous;
            do
            {
                previous = normalized;
                normalized = Regex.Replace(normalized, @"(?:_log|_data|_\d+)$", "", RegexOptions.IgnoreCase);
            }
            while (!string.Equals(previous, normalized, StringComparison.Ordinal));

            return normalized;
        }

        /// <summary>
        /// سرورهای SQL Server آنلاینی که هیچ فایلی برایشان پیدا نشده و
        /// کاندیدای جست‌وجوی عمیق درایوها هستند.
        /// </summary>
        public static List<DatabaseInfo> SqlServersNeedingSweep(List<DatabaseInfo> servers, List<DatabaseCopyItem> items)
        {
            var need = new List<DatabaseInfo>();
            foreach (var s in servers)
            {
                if (s.Type != DatabaseType.SQLServer || !s.IsOnline || !IsLocalHost(s)) continue;
                var mine = items.Where(i => i.Server.Type == DatabaseType.SQLServer
                    && string.Equals(i.Server.Host, s.Host, StringComparison.OrdinalIgnoreCase)
                    && (i.Server.Port ?? 0) == (s.Port ?? 0));
                if (mine.Any() && mine.All(i => i.Files.Count == 0))
                    need.Add(s);
            }
            return need;
        }

        /// <summary>
        /// جست‌وجوی عمیق درایوها برای فایل‌های دیتای SQL؛ فقط پس از اجازه صریح کاربر.
        /// </summary>
        public static List<DatabaseCopyItem> SweepSqlServerFiles(DatabaseInfo server, Action<string>? log)
        {
            var files = SqlServerPathResolver.DeepSweepForDataFiles(log);
            var built = BuildNoAuthItems(server, files);
            if (built.Count > 0)
                log?.Invoke(L.Format("S353", built.Count, L.Text("S364")));
            return built;
        }

        private static List<DatabaseCopyItem> EnumerateSqlServerViaQuery(DatabaseInfo server, Action<string>? log)
        {
            var result = new List<DatabaseCopyItem>();
            var profile = ProfileManager.Load()
                .FirstOrDefault(p => p.Type == server.Type && p.Host == server.Host && p.Port == (server.Port ?? 0));

            var serverName = server.Port.HasValue && server.Port.Value > 0
                ? $"{server.Host},{server.Port.Value}"
                : server.Host;

            string cs;
            if (profile != null && !string.IsNullOrEmpty(profile.Username))
            {
                cs = $"Data Source={serverName};User ID={profile.Username};Password={profile.Password};TrustServerCertificate=True;Connect Timeout=5;";
            }
            else
            {
                cs = $"Data Source={serverName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5;";
            }

            using var conn = new SqlConnection(cs);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandTimeout = 10;
            cmd.CommandText =
                "SELECT db.name AS dbname, mf.physical_name AS path " +
                "FROM sys.databases db INNER JOIN sys.master_files mf ON db.database_id = mf.database_id " +
                "WHERE db.database_id > 4 ORDER BY db.name;";

            var groups = new Dictionary<string, List<string>>();
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var db = reader.GetString(0);
                    var path = reader.GetString(1);
                    if (!groups.TryGetValue(db, out var list)) groups[db] = list = new List<string>();
                    list.Add(path);
                }
            }

            foreach (var kv in groups)
            {
                var item = new DatabaseCopyItem
                {
                    Server = server,
                    DatabaseName = kv.Key,
                    FolderName = SafeFolder($"{server.TypeDisplayName}_{kv.Key}")
                };

                foreach (var path in kv.Value)
                {
                    var fileName = Path.GetFileName(path);
                    if (File.Exists(path))
                    {
                        var fi = new FileInfo(path);
                        var cf = new FileCopyItem
                        {
                            DisplayName = fileName,
                            SourcePath = path,
                            RelativePath = fileName,
                            Size = fi.Length,
                            Modified = fi.LastWriteTime
                        };
                        FileLockProbe.Apply(cf);
                        item.Files.Add(cf);
                    }
                    else
                    {
                        var fileItem = new FileCopyItem
                        {
                            DisplayName = fileName,
                            SourcePath = path,
                            RelativePath = fileName
                        };
                        fileItem.Error = L.Text("S121");
                        item.Files.Add(fileItem);
                    }
                }

                if (item.Files.Count == 0 && string.IsNullOrEmpty(item.Error))
                {
                    item.Error = L.Text("S122");
                }

                result.Add(item);
            }

            log?.Invoke(L.Format("S123", result.Count));
            return result;
        }

        // ---------- MySQL / MariaDB ----------
        private static List<DatabaseCopyItem> EnumerateMySql(DatabaseInfo server, Action<string>? log)
        {
            var port = server.Port ?? (server.Type == DatabaseType.MariaDB ? 3307 : 3306);
            var profile = ProfileManager.Load()
                .FirstOrDefault(p => p.Type == server.Type && p.Host == server.Host && p.Port == port);

            var csb = new MySqlConnectionStringBuilder
            {
                Server = server.Host,
                Port = (uint)port,
                UserID = profile?.Username ?? "root",
                Password = profile?.Password ?? "",
                ConnectionTimeout = 5
            };

            using var conn = new MySqlConnection(csb.ConnectionString);
            conn.Open();

            string datadir;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT @@datadir;";
                datadir = (string?)cmd.ExecuteScalar() ?? "";
            }

            var result = new List<DatabaseCopyItem>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT schema_name FROM information_schema.schemata " +
                                  "WHERE schema_name NOT IN ('information_schema','performance_schema','mysql','sys','ndbinfo') " +
                                  "ORDER BY schema_name;";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var db = reader.GetString(0);
                    var dbPath = Path.Combine(datadir, db);
                    var item = new DatabaseCopyItem
                    {
                        Server = server,
                        DatabaseName = db,
                        FolderName = SafeFolder($"{server.TypeDisplayName}_{db}")
                    };

                    if (Directory.Exists(dbPath))
                    {
                        foreach (var f in Directory.EnumerateFiles(dbPath, "*", SearchOption.AllDirectories))
                        {
                            var fi = new FileInfo(f);
                            var rel = Path.GetRelativePath(dbPath, f);
                            item.Files.Add(new FileCopyItem
                            {
                                DisplayName = Path.GetFileName(f),
                                SourcePath = f,
                                RelativePath = rel,
                                Size = fi.Length,
                            Modified = fi.LastWriteTime
                            });
                        }
                    }
                    else
                    {
                        item.Error = L.Format("S124", dbPath);
                    }

                    result.Add(item);
                }
            }

            log?.Invoke(L.Format("S125", server.TypeDisplayName, datadir, result.Count));
            return result;
        }

        // ---------- PostgreSQL ----------
        private static List<DatabaseCopyItem> EnumeratePostgre(DatabaseInfo server, Action<string>? log)
        {
            var port = server.Port ?? 5432;
            var profile = ProfileManager.Load()
                .FirstOrDefault(p => p.Type == server.Type && p.Host == server.Host && p.Port == port);

            var csb = new NpgsqlConnectionStringBuilder
            {
                Host = server.Host,
                Port = port,
                Username = profile?.Username ?? "postgres",
                Password = profile?.Password ?? "postgres",
                Database = "postgres",
                Timeout = 5
            };

            using var conn = new NpgsqlConnection(csb.ConnectionString);
            conn.Open();

            string dataDir;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SHOW data_directory;";
                dataDir = (string?)cmd.ExecuteScalar() ?? "";
            }

            var result = new List<DatabaseCopyItem>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT datname, oid::text FROM pg_database " +
                                  "WHERE datistemplate = false AND datname <> 'postgres';";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var db = reader.GetString(0);
                    var oid = reader.GetString(1);
                    var dbPath = Path.Combine(dataDir, "base", oid);

                    var item = new DatabaseCopyItem
                    {
                        Server = server,
                        DatabaseName = db,
                        FolderName = SafeFolder($"{server.TypeDisplayName}_{db}")
                    };

                    if (Directory.Exists(dbPath))
                    {
                        foreach (var f in Directory.EnumerateFiles(dbPath, "*", SearchOption.AllDirectories))
                        {
                            var fi = new FileInfo(f);
                            var rel = Path.GetRelativePath(dbPath, f);
                            item.Files.Add(new FileCopyItem
                            {
                                DisplayName = Path.GetFileName(f),
                                SourcePath = f,
                                RelativePath = rel,
                                Size = fi.Length,
                            Modified = fi.LastWriteTime
                            });
                        }
                    }
                    else
                    {
                        item.Error = L.Format("S124", dbPath);
                    }

                    result.Add(item);
                }
            }

            log?.Invoke(L.Format("S126", dataDir, result.Count));
            return result;
        }

        // ---------- Redis ----------
        private static DatabaseCopyItem EnumerateRedis(DatabaseInfo server, Action<string>? log)
        {
            var port = server.Port ?? 6379;
            var profile = ProfileManager.Load()
                .FirstOrDefault(p => p.Type == server.Type && p.Host == server.Host && p.Port == port);

            var options = new ConfigurationOptions
            {
                EndPoints = { $"{server.Host}:{port}" },
                AbortOnConnectFail = false,
                ConnectTimeout = 5000,
                SyncTimeout = 5000
            };
            if (!string.IsNullOrEmpty(profile?.Password))
                options.Password = profile.Password;

            var item = new DatabaseCopyItem
            {
                Server = server,
                DatabaseName = server.Name,
                FolderName = SafeFolder($"Redis_{server.Name}")
            };

            using var redis = ConnectionMultiplexer.Connect(options);
            var db = redis.GetDatabase();

            string? dir = null;
            try
            {
                var raw = db.Execute("CONFIG", "GET", "dir");
                if (raw.IsNull) throw new InvalidOperationException(L.Text("S127"));
                if (raw.Length >= 2)
                {
                    dir = raw[1].ToString();
                }
                else
                {
                    dir = raw.ToString();
                }
            }
            catch { }

            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                item.Error = L.Text("S128");
            }
            else
            {
                foreach (var pattern in new[] { "dump.rdb", "appendonly*", "nodes.conf" })
                {
                    foreach (var f in Directory.EnumerateFiles(dir, pattern))
                    {
                        var fi = new FileInfo(f);
                        item.Files.Add(new FileCopyItem
                        {
                            DisplayName = fi.Name,
                            SourcePath = f,
                            RelativePath = fi.Name,
                            Size = fi.Length,
                            Modified = fi.LastWriteTime
                        });
                    }
                }

                if (item.Files.Count == 0)
                    item.Error = L.Format("S129", dir);
            }

            return item;
        }

        // ---------- MongoDB ----------
        private static List<DatabaseCopyItem> EnumerateMongoDb(DatabaseInfo server, Action<string>? log)
        {
            var candidates = new List<string>();

            // C:\Program Files\MongoDB\Server\*\data\db
            var progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var mongoRoot = Path.Combine(progFiles, "MongoDB", "Server");
            if (Directory.Exists(mongoRoot))
            {
                foreach (var ver in Directory.EnumerateDirectories(mongoRoot, "*"))
                {
                    var dbDir = Path.Combine(ver, "data", "db");
                    if (Directory.Exists(dbDir)) candidates.Add(dbDir);
                }
            }

            // \data\db در هر درایو ثابت
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed))
            {
                var dbDir = Path.Combine(drive.RootDirectory.FullName, "data", "db");
                if (Directory.Exists(dbDir) && !candidates.Contains(dbDir)) candidates.Add(dbDir);
            }

            var result = new List<DatabaseCopyItem>();
            foreach (var dir in candidates.Distinct())
            {
                var item = new DatabaseCopyItem
                {
                    Server = server,
                    DatabaseName = server.Name,
                    FolderName = SafeFolder($"MongoDB_{server.Name}")
                };

                foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
                {
                    var fi = new FileInfo(f);
                    if (fi.Length == 0) continue; // placeholder files
                    item.Files.Add(new FileCopyItem
                    {
                        DisplayName = fi.Name,
                        SourcePath = f,
                        RelativePath = fi.Name,
                        Size = fi.Length,
                            Modified = fi.LastWriteTime
                    });
                }

                if (item.Files.Count > 0)
                {
                    result.Add(item);
                }
            }

            if (result.Count == 0)
            {
                result.Add(ErrorItem(server, server.Name,
                    L.Text("S130")));
            }

            return result;
        }

        // ---------- اجرای کپی ----------
        public static (int FilesCopied, long BytesCopied, int Failed, int Mismatched, string Errors, List<CopyAcquisition> Acquisitions) ExecuteCopy(
            List<DatabaseCopyItem> items, string destRoot, LockedFileHandling lockedHandling,
            Action<string>? log = null, bool verifyHash = true, Action<int, int>? progress = null)
        {
            var filesCopied = 0;
            long bytesCopied = 0;
            var failed = 0;
            var mismatched = 0;
            var errorLines = new List<string>();
            var acquisitions = new List<CopyAcquisition>();
            var shadowCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var stoppedServices = new List<string>();
            var completedWork = 0;
            var totalWork = Math.Max(1, items.Sum(i => Math.Max(1, i.Files.Count)));

            try
            {
                if (lockedHandling == LockedFileHandling.StopServices)
                {
                    var serviceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var item in items.Where(i => i.Server.IsOnline && IsLocalHost(i.Server)))
                    {
                        foreach (var n in DbServiceHelper.GetDatabaseServices(item.Server.Type, item.Server))
                            serviceNames.Add(n);
                    }

                    if (serviceNames.Count > 0)
                    {
                        log?.Invoke(L.Text("S131"));
                        if (!DbServiceHelper.StopServices(serviceNames.ToList(), log,
                                out var stoppedByOperation, out var stopErr))
                        {
                            stoppedServices.AddRange(stoppedByOperation);
                            log?.Invoke(L.Format("S132", stopErr));
                            errorLines.Add(L.Format("S133", stopErr));
                            foreach (var item in items.Where(i => i.Server.IsOnline && IsLocalHost(i.Server)))
                            {
                                if (string.IsNullOrEmpty(item.Error))
                                    item.Error = L.Text("S134");
                                failed++;
                            }
                            return (0, 0, failed, mismatched, string.Join(Environment.NewLine, errorLines), acquisitions);
                        }
                        stoppedServices.AddRange(stoppedByOperation);
                    }
                }

                foreach (var item in items)
                {
                    var destDir = Path.Combine(destRoot, item.FolderName);
                    var sourceRoot = item.UseManualPath && !string.IsNullOrEmpty(item.ManualPath)
                        ? item.ManualPath
                        : null;

                    try
                    {
                        if (!string.IsNullOrEmpty(item.Error) && sourceRoot == null)
                        {
                            errorLines.Add($"{item.FolderName}: {item.Error}");
                            failed++;
                            progress?.Invoke(++completedWork, totalWork);
                            continue;
                        }

                        if (sourceRoot != null && Directory.Exists(sourceRoot))
                        {
                            if (IsSameOrDescendantPath(sourceRoot, destDir))
                                throw new InvalidOperationException(L.Text("S393"));

                            // کپی کل پوشه دستی
                            CopyDirectory(sourceRoot, destDir, item.FolderName, ref filesCopied, ref bytesCopied, ref failed, ref mismatched, acquisitions, errorLines, verifyHash, log);
                            progress?.Invoke(completedWork += Math.Max(1, item.Files.Count), totalWork);
                            continue;
                        }

                        foreach (var file in item.Files)
                        {
                            var dest = Path.Combine(destDir, file.RelativePath);
                            var relToRoot = Path.Combine(item.FolderName, file.RelativePath);
                            try
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(dest) ?? destDir);
                                var src = file.SourcePath;
                                var info = new FileInfo(src);
                                if (!info.Exists)
                                {
                                    file.Error = L.Text("S135");
                                    errorLines.Add(L.Format("S136", item.FolderName, file.DisplayName));
                                    failed++;
                                    continue;
                                }

                                string effectiveSrc = src;
                                bool viaShadow = false;
                                try
                                {
                                    File.Copy(src, dest, overwrite: true);
                                }
                                catch (IOException ex)
                                {
                                    // فایل قفل است؛ تلاش از روی Shadow Copy
                                    file.Locked = true;
                                    if (lockedHandling != LockedFileHandling.Vss)
                                    {
                                        file.Error = L.Text("S140");
                                        errorLines.Add(L.Format("S141", item.FolderName, file.DisplayName, ex.Message));
                                        failed++;
                                        continue;
                                    }
                                    var vol = Path.GetPathRoot(file.SourcePath);
                                    if (!TryCopyViaShadow(file.SourcePath, dest, vol, shadowCache, out var shadowErr, out var shadowSrc))
                                    {
                                        file.Error = L.Text("S139") + shadowErr;
                                        errorLines.Add($"{item.FolderName}\\{file.DisplayName}: {file.Error}");
                                        failed++;
                                        continue;
                                    }
                                    effectiveSrc = shadowSrc;
                                    viaShadow = true;
                                    file.Locked = false;
                                    file.Error = null;
                                }

                                var destInfo = new FileInfo(dest);
                                RecordCopy(file, relToRoot, effectiveSrc, dest, destInfo.Length,
                                    ref filesCopied, ref bytesCopied, ref failed, ref mismatched,
                                    acquisitions, errorLines, verifyHash, log,
                                    viaShadow ? L.Format("S138", item.FolderName, file.DisplayName, FormatSize(destInfo.Length))
                                              : L.Format("S137", item.FolderName, file.DisplayName, FormatSize(destInfo.Length)));
                            }
                            catch (UnauthorizedAccessException ex)
                            {
                                file.Error = L.Text("S142");
                                errorLines.Add(L.Format("S143", item.FolderName, file.DisplayName, ex.Message));
                                failed++;
                            }
                            catch (Exception ex)
                            {
                                file.Error = ex.Message;
                                errorLines.Add($"{item.FolderName}\\{file.DisplayName}: {ex.Message}");
                                failed++;
                            }
                            finally
                            {
                                progress?.Invoke(++completedWork, totalWork);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errorLines.Add($"{item.FolderName}: {ex.Message}");
                        failed++;
                        progress?.Invoke(++completedWork, totalWork);
                    }

                }
            }
            finally
            {
                if (stoppedServices.Count > 0)
                {
                    log?.Invoke(L.Text("S144"));
                    if (!DbServiceHelper.StartServices(stoppedServices, log, out var startErr))
                        errorLines.Add(L.Text("S145") + startErr);
                }

                foreach (var shadow in shadowCache.Values)
                    VolumeShadowCopy.DeleteShadow(shadow);
            }

            return (filesCopied, bytesCopied, failed, mismatched, string.Join(Environment.NewLine, errorLines), acquisitions);
        }

        /// <summary>هش SHA-256 فایل؛ null یعنی خواندن ممکن نشد.</summary>
        public static string? TryHashFile(string path)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite, 1024 * 1024);
                using var sha = System.Security.Cryptography.SHA256.Create();
                return Convert.ToHexString(sha.ComputeHash(fs)).ToLowerInvariant();
            }
            catch (Exception ex)
            {
                AppLog.Write("Copy.Hash: " + path, ex);
                return null;
            }
        }

        /// <summary>
        /// ثبت یک فایل کپی‌شده: هش مبدأ از همان بایت‌هایی که کپی شد، هش مقصد،
        /// مقایسه و ثبت در مانیفست. عدم تطابق = failed (فایل نگه داشته می‌شود تا دیده شود).
        /// </summary>
        private static void RecordCopy(FileCopyItem? file, string relToRoot, string effectiveSrc, string dest,
            long bytes, ref int filesCopied, ref long bytesCopied, ref int failed, ref int mismatched,
            List<CopyAcquisition> acquisitions, List<string> errorLines, bool verifyHash, Action<string>? log,
            string doneMessage)
        {
            string? srcHash = null, dstHash = null;
            var verified = false;
            if (verifyHash)
            {
                srcHash = TryHashFile(effectiveSrc);
                dstHash = TryHashFile(dest);
                verified = srcHash != null && dstHash != null && srcHash == dstHash;
            }

            if (file != null)
            {
                file.SourceHash = srcHash;
                file.DestHash = dstHash;
                file.CopiedPath = dest;
                file.Verified = verified;
                file.Error = null;
            }

            acquisitions.Add(new CopyAcquisition
            {
                RelPath = relToRoot,
                SourcePath = effectiveSrc,
                SourceHash = srcHash ?? "",
                DestHash = dstHash ?? "",
                Verified = verified,
                Bytes = bytes
            });

            if (verifyHash && srcHash != null && dstHash != null && !verified)
            {
                mismatched++;
                failed++;
                var msg = L.Format("S375", relToRoot);
                errorLines.Add(msg);
                log?.Invoke(msg);
                return;
            }

            filesCopied++;
            bytesCopied += bytes;
            log?.Invoke(doneMessage + (verifyHash && verified ? "  ✓" : ""));
        }

        private static void CopyDirectory(string sourceRoot, string destRoot, string folderName,
            ref int filesCopied, ref long bytesCopied, ref int failed, ref int mismatched,
            List<CopyAcquisition> acquisitions, List<string> errorLines, bool verifyHash, Action<string>? log)
        {
            foreach (var file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(sourceRoot, file);
                var dest = Path.Combine(destRoot, rel);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(dest) ?? destRoot);
                    File.Copy(file, dest, overwrite: true);
                    var destInfo = new FileInfo(dest);
                    RecordCopy(null, Path.Combine(folderName, rel), file, dest, destInfo.Length,
                        ref filesCopied, ref bytesCopied, ref failed, ref mismatched,
                        acquisitions, errorLines, verifyHash, log,
                        L.Format("S137", Path.GetFileName(destRoot), rel, FormatSize(destInfo.Length)));
                }
                catch (Exception ex)
                {
                    errorLines.Add($"{Path.GetFileName(destRoot)}\\{rel}: {ex.Message}");
                    failed++;
                }
            }
        }

        private static bool IsSameOrDescendantPath(string parent, string candidate)
        {
            var parentFull = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var candidateFull = Path.GetFullPath(candidate).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return candidateFull.Equals(parentFull, StringComparison.OrdinalIgnoreCase) ||
                   candidateFull.StartsWith(parentFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                   candidateFull.StartsWith(parentFull + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryCopyViaShadow(
            string src, string dest, string? volumeRoot,
            Dictionary<string, string> shadowCache, out string? error, out string shadowSrc)
        {
            error = null;
            shadowSrc = src;
            if (string.IsNullOrEmpty(volumeRoot))
            {
                error = L.Text("S146");
                return false;
            }

            if (!shadowCache.TryGetValue(volumeRoot, out var shadowId))
            {
                shadowId = VolumeShadowCopy.TryCreateShadow(volumeRoot, out var createErr);
                if (shadowId == null)
                {
                    error = createErr ?? L.Text("S147");
                    return false;
                }
                shadowCache[volumeRoot] = shadowId;
            }

            shadowSrc = VolumeShadowCopy.MapToShadow(shadowId, src, volumeRoot);
            try
            {
                if (!File.Exists(shadowSrc))
                {
                    error = L.Text("S148");
                    return false;
                }
                File.Copy(shadowSrc, dest, overwrite: true);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static string FormatSize(long bytes)
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
