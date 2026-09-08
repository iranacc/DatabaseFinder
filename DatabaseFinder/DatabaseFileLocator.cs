using System.Diagnostics;
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

                if (!server.IsOnline)
                {
                    items.Add(BuildOfflineItem(server));
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

        private static DatabaseCopyItem BuildOfflineItem(DatabaseInfo server)
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
                    Size = fi.Length
                });
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
                        item.Files.Add(new FileCopyItem
                        {
                            DisplayName = fileName,
                            SourcePath = path,
                            RelativePath = fileName,
                            Size = fi.Length
                        });
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
                                Size = fi.Length
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
                                Size = fi.Length
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
                            Size = fi.Length
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
                        Size = fi.Length
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
        public static (int FilesCopied, long BytesCopied, int Failed, string Errors) ExecuteCopy(
            List<DatabaseCopyItem> items, string destRoot, LockedFileHandling lockedHandling,
            Action<string>? log = null)
        {
            var filesCopied = 0;
            long bytesCopied = 0;
            var failed = 0;
            var errorLines = new List<string>();
            var shadowCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var stoppedServices = new List<string>();

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
                        if (!DbServiceHelper.StopServices(serviceNames.ToList(), log, out var stopErr))
                        {
                            log?.Invoke(L.Format("S132", stopErr));
                            errorLines.Add(L.Format("S133", stopErr));
                            foreach (var item in items.Where(i => i.Server.IsOnline && IsLocalHost(i.Server)))
                            {
                                if (string.IsNullOrEmpty(item.Error))
                                    item.Error = L.Text("S134");
                                failed++;
                            }
                            return (0, 0, failed, string.Join(Environment.NewLine, errorLines));
                        }
                        stoppedServices.AddRange(serviceNames);
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
                            continue;
                        }

                        if (sourceRoot != null && Directory.Exists(sourceRoot))
                        {
                            // کپی کل پوشه دستی
                            CopyDirectory(sourceRoot, destDir, ref filesCopied, ref bytesCopied, ref failed, errorLines, log);
                            continue;
                        }

                        foreach (var file in item.Files)
                        {
                            var dest = Path.Combine(destDir, file.RelativePath);
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

                                File.Copy(src, dest, overwrite: true);
                                filesCopied++;
                                bytesCopied += info.Length;
                                log?.Invoke(L.Format("S137", item.FolderName, file.DisplayName, FormatSize(info.Length)));
                            }
                            catch (IOException ex)
                            {
                                file.Locked = true;
                                if (lockedHandling == LockedFileHandling.Vss)
                                {
                                    var vol = Path.GetPathRoot(file.SourcePath);
                                    if (TryCopyViaShadow(file.SourcePath, dest, vol, shadowCache, out var shadowErr))
                                    {
                                        var info2 = new FileInfo(dest);
                                        file.Locked = false;
                                        file.Error = null;
                                        filesCopied++;
                                        bytesCopied += info2.Length;
                                        log?.Invoke(L.Format("S138", item.FolderName, file.DisplayName, FormatSize(info2.Length)));
                                    }
                                    else
                                    {
                                        file.Error = L.Text("S139") + shadowErr;
                                        errorLines.Add($"{item.FolderName}\\{file.DisplayName}: {file.Error}");
                                        failed++;
                                    }
                                }
                                else
                                {
                                    file.Error = L.Text("S140");
                                    errorLines.Add(L.Format("S141", item.FolderName, file.DisplayName, ex.Message));
                                    failed++;
                                }
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
                        }
                    }
                    catch (Exception ex)
                    {
                        errorLines.Add($"{item.FolderName}: {ex.Message}");
                        failed++;
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

            return (filesCopied, bytesCopied, failed, string.Join(Environment.NewLine, errorLines));
        }

        private static void CopyDirectory(string sourceRoot, string destRoot,
            ref int filesCopied, ref long bytesCopied, ref int failed,
            List<string> errorLines, Action<string>? log)
        {
            foreach (var file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(sourceRoot, file);
                var dest = Path.Combine(destRoot, rel);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(dest) ?? destRoot);
                    var info = new FileInfo(file);
                    File.Copy(file, dest, overwrite: true);
                    filesCopied++;
                    bytesCopied += info.Length;
                    log?.Invoke(L.Format("S137", Path.GetFileName(destRoot), rel, FormatSize(info.Length)));
                }
                catch (Exception ex)
                {
                    errorLines.Add($"{Path.GetFileName(destRoot)}\\{rel}: {ex.Message}");
                    failed++;
                }
            }
        }

        private static bool TryCopyViaShadow(
            string src, string dest, string? volumeRoot,
            Dictionary<string, string> shadowCache, out string? error)
        {
            error = null;
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

            var shadowSrc = VolumeShadowCopy.MapToShadow(shadowId, src, volumeRoot);
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