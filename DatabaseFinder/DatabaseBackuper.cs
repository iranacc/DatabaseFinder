using System.Diagnostics;
using System.Text;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;
using StackExchange.Redis;

namespace DatabaseFinder
{
    public class DatabaseBackupItem
    {
        public DatabaseInfo Server { get; set; } = new();
        public string DatabaseName { get; set; } = "";
        public string FolderName { get; set; } = "";
        public string BackupDir { get; set; } = "";
        public string DestFile { get; set; } = "";
        public string Method { get; set; } = "";
        public string ToolPath { get; set; } = "";
        public bool Done { get; set; }
        public bool Failed { get; set; }
        public string? Error { get; set; }
        public List<string> OutputFiles { get; set; } = new();
        public long BytesProduced { get; set; }
        public bool Compress { get; set; } = true;
        public bool Verify { get; set; } = true;
        public bool Checksum { get; set; }
        public string Chain { get; set; } = "full";
        public DateTime StartedUtc { get; set; }
        public DateTime EndedUtc { get; set; }
    }

    public static class DatabaseBackuper
    {
        // ---------- ساخت برنامه (پلن) ----------
        public static List<DatabaseBackupItem> BuildBackupPlan(
            List<DatabaseInfo> servers, string destRoot, Action<string>? log = null)
        {
            var items = new List<DatabaseBackupItem>();

            foreach (var server in servers)
            {
                log?.Invoke(L.Format("S001", server.TypeDisplayName));

                if (!server.IsOnline)
                {
                    items.Add(ErrorItem(server, server.Name,
                        L.Text("S002")));
                    continue;
                }

                if (!IsLocalHost(server))
                {
                    items.Add(ErrorItem(server, server.Name,
                        L.Text("S003")));
                    continue;
                }

                try
                {
                    switch (server.Type)
                    {
                        case DatabaseType.SQLServer:
                            items.AddRange(PlanSqlServer(server, destRoot, log));
                            break;
                        case DatabaseType.MySQL:
                        case DatabaseType.MariaDB:
                            items.AddRange(PlanMySql(server, destRoot, log));
                            break;
                        case DatabaseType.PostgreSQL:
                            items.AddRange(PlanPostgres(server, destRoot, log));
                            break;
                        case DatabaseType.Redis:
                            items.Add(PlanRedis(server, destRoot, log));
                            break;
                        case DatabaseType.MongoDB:
                            items.Add(PlanMongo(server, destRoot, log));
                            break;
                        default:
                            items.Add(ErrorItem(server, server.Name,
                                L.Text("S004")));
                            break;
                    }
                }
                catch (Exception ex)
                {
                    items.Add(ErrorItem(server, server.Name,
                        L.Format("S005", ex.Message)));
                }
            }

            return items;
        }

        private static DatabaseBackupItem ErrorItem(DatabaseInfo server, string dbName, string error)
        {
            var name = string.IsNullOrEmpty(dbName) ? server.Name : dbName;
            return new DatabaseBackupItem
            {
                Server = server,
                DatabaseName = name,
                FolderName = DatabaseFileLocator.SafeFolder($"{server.TypeDisplayName}_{name}"),
                Error = error
            };
        }

        private static DatabaseBackupItem NewItem(DatabaseInfo server, string dbName, string destRoot, string method)
        {
            var folder = DatabaseFileLocator.SafeFolder($"{server.TypeDisplayName}_{dbName}");
            return new DatabaseBackupItem
            {
                Server = server,
                DatabaseName = dbName,
                FolderName = folder,
                BackupDir = Path.Combine(destRoot, folder),
                Method = method
            };
        }

        // ---------- SQL Server ----------
        private static List<DatabaseBackupItem> PlanSqlServer(DatabaseInfo server, string destRoot, Action<string>? log)
        {
            var cs = BuildSqlServerCs(server);
            var names = new List<string>();
            using (var conn = new SqlConnection(cs))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandTimeout = 10;
                cmd.CommandText = "SELECT name FROM sys.databases WHERE database_id > 4 ORDER BY name;";
                using var reader = cmd.ExecuteReader();
                while (reader.Read()) names.Add(reader.GetString(0));
            }

            var items = names.Select(db =>
            {
                var it = NewItem(server, db, destRoot, "BACKUP DATABASE");
                it.DestFile = Path.Combine(it.BackupDir, DatabaseFileLocator.SafeFolder(db) + ".bak");
                return it;
            }).ToList();

            log?.Invoke(L.Format("S006", names.Count));
            return items;
        }

        private static string BuildSqlServerCs(DatabaseInfo server)
        {
            var profile = ProfileManager.Load()
                .FirstOrDefault(p => p.Type == server.Type && p.Host == server.Host && p.Port == (server.Port ?? 0));

            var serverName = server.Port.HasValue && server.Port.Value > 0
                ? $"{server.Host},{server.Port.Value}"
                : server.Host;

            return profile != null && !string.IsNullOrEmpty(profile.Username)
                ? $"Data Source={serverName};User ID={profile.Username};Password={profile.Password};TrustServerCertificate=True;Connect Timeout=5;"
                : $"Data Source={serverName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5;";
        }

        private static void RunSqlBackup(DatabaseBackupItem item, Action<string>? log)
        {
            var cs = BuildSqlServerCs(item.Server);
            using var conn = new SqlConnection(cs);
            conn.Open();
            var dbName = item.DatabaseName.Replace("]", "]]");
            var dest = item.DestFile.Replace("'", "''");

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = 0;
                var opts = item.Compress ? "COMPRESSION" : "NO_COMPRESSION";
                if (item.Checksum) opts += ", CHECKSUM";
                cmd.CommandText = $"BACKUP DATABASE [{dbName}] TO DISK = N'{dest}' WITH INIT, {opts};";
                log?.Invoke(L.Format("S007", item.DatabaseName));
                cmd.ExecuteNonQuery();
            }

            if (item.Verify)
            {
                log?.Invoke(L.Format("S336", item.DatabaseName));
                using var vcmd = conn.CreateCommand();
                vcmd.CommandTimeout = 0;
                vcmd.CommandText = $"RESTORE VERIFYONLY FROM DISK = N'{dest}';";
                using var reader = vcmd.ExecuteReader();
                while (reader.Read()) { }
            }
        }

        internal static string ProbeServerVersion(DatabaseInfo server)
        {
            try
            {
                switch (server.Type)
                {
                    case DatabaseType.SQLServer:
                        using (var conn = new SqlConnection(BuildSqlServerCs(server)))
                        {
                            conn.Open();
                            using var cmd = conn.CreateCommand();
                            cmd.CommandTimeout = 5;
                            cmd.CommandText = "SELECT CONCAT(CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(64)), ' ', " +
                                              "CAST(SERVERPROPERTY('Edition') AS nvarchar(128)));";
                            return (cmd.ExecuteScalar() as string ?? "").Trim();
                        }

                    case DatabaseType.MySQL:
                    case DatabaseType.MariaDB:
                        var port = server.Port ?? (server.Type == DatabaseType.MariaDB ? 3307 : 3306);
                        var creds = GetMySqlCreds(server, port);
                        using (var conn = new MySqlConnection(creds.Item1))
                        {
                            conn.Open();
                            using var cmd = conn.CreateCommand();
                            cmd.CommandTimeout = 5;
                            cmd.CommandText = "SELECT CONCAT(VERSION(), ' ', @@hostname);";
                            return (cmd.ExecuteScalar() as string ?? "").Trim();
                        }

                    case DatabaseType.PostgreSQL:
                        var pgPort = server.Port ?? 5432;
                        var pgCsb = BuildPostgresCsb(server, pgPort);
                        using (var conn = new NpgsqlConnection(pgCsb.ConnectionString))
                        {
                            conn.Open();
                            using var cmd = conn.CreateCommand();
                            cmd.CommandTimeout = 5;
                            cmd.CommandText = "SELECT current_setting('server_version');";
                            return (cmd.ExecuteScalar() as string ?? "").Trim();
                        }
                }
            }
            catch { }
            return "";
        }

        internal static long ProbeDatabaseSizeBytes(DatabaseInfo server, string dbName)
        {
            try
            {
                switch (server.Type)
                {
                    case DatabaseType.SQLServer:
                        var esc = dbName.Replace("]", "]]");
                        using (var conn = new SqlConnection(BuildSqlServerCs(server)))
                        {
                            conn.Open();
                            using var cmd = conn.CreateCommand();
                            cmd.CommandTimeout = 5;
                            cmd.CommandText = $"SELECT CAST(COALESCE(SUM(d.size) * 8192.0, 0) AS bigint) FROM [{esc}].sys.database_files AS d WHERE d.type_desc = 'ROWS';";
                            return Convert.ToInt64(cmd.ExecuteScalar() ?? 0L);
                        }

                    case DatabaseType.MySQL:
                    case DatabaseType.MariaDB:
                        var port = server.Port ?? (server.Type == DatabaseType.MariaDB ? 3307 : 3306);
                        var creds = GetMySqlCreds(server, port);
                        using (var conn = new MySqlConnection(creds.Item1))
                        {
                            conn.Open();
                            using var cmd = conn.CreateCommand();
                            cmd.CommandTimeout = 5;
                            cmd.CommandText = "SELECT CAST(COALESCE(SUM(data_length + index_length), 0) AS SIGNED) FROM information_schema.tables WHERE table_schema = @db;";
                            cmd.Parameters.AddWithValue("@db", dbName);
                            return Convert.ToInt64(cmd.ExecuteScalar() ?? 0L);
                        }

                    case DatabaseType.PostgreSQL:
                        var pgPort = server.Port ?? 5432;
                        var pgCsb = BuildPostgresCsb(server, pgPort);
                        using (var conn = new NpgsqlConnection(pgCsb.ConnectionString))
                        {
                            conn.Open();
                            using var cmd = conn.CreateCommand();
                            cmd.CommandTimeout = 5;
                            cmd.CommandText = "SELECT COALESCE(pg_database_size(@db), 0);";
                            cmd.Parameters.AddWithValue("@db", dbName);
                            return Convert.ToInt64(cmd.ExecuteScalar() ?? 0L);
                        }
                }
            }
            catch { }
            return 0;
        }

        // ---------- MySQL / MariaDB ----------
        private static List<DatabaseBackupItem> PlanMySql(DatabaseInfo server, string destRoot, Action<string>? log)
        {
            var port = server.Port ?? (server.Type == DatabaseType.MariaDB ? 3307 : 3306);
            var creds = GetMySqlCreds(server, port);

            string basedir = "";
            using (var conn = new MySqlConnection(creds.Item1))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT @@basedir;";
                basedir = (string?)cmd.ExecuteScalar() ?? "";
            }

            var mysqldump = FindMySqlDump(basedir);
            var dbNames = GetMySqlDatabases(creds.Item1);

            var items = dbNames.Select(db =>
            {
                var it = NewItem(server, db, destRoot, "mysqldump");
                it.DestFile = Path.Combine(it.BackupDir, DatabaseFileLocator.SafeFolder(db) + ".sql");
                if (mysqldump == null) it.Error = L.Text("S008");
                it.ToolPath = mysqldump ?? "";
                return it;
            }).ToList();

            log?.Invoke(L.Format("S009", server.TypeDisplayName, basedir, dbNames.Count));
            return items;
        }

        private static (string ConnectionString, string User, string Password) GetMySqlCreds(DatabaseInfo server, int port)
        {
            var profile = ProfileManager.Load()
                .FirstOrDefault(p => p.Type == server.Type && p.Host == server.Host && p.Port == port);
            var user = profile?.Username ?? "root";
            var pass = profile?.Password ?? "";

            var csb = new MySqlConnectionStringBuilder
            {
                Server = server.Host,
                Port = (uint)port,
                UserID = user,
                Password = pass,
                ConnectionTimeout = 5
            };
            return (csb.ConnectionString, user, pass);
        }

        private static List<string> GetMySqlDatabases(string cs)
        {
            var names = new List<string>();
            using (var conn = new MySqlConnection(cs))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT schema_name FROM information_schema.schemata " +
                                  "WHERE schema_name NOT IN ('information_schema','performance_schema','mysql','sys','ndbinfo') " +
                                  "ORDER BY schema_name;";
                using var reader = cmd.ExecuteReader();
                while (reader.Read()) names.Add(reader.GetString(0));
            }
            return names;
        }

        private static string? FindMySqlDump(string basedir)
        {
            var candidates = new List<string>();
            if (!string.IsNullOrEmpty(basedir))
                candidates.Add(Path.Combine(basedir, "bin", "mysqldump.exe"));

            foreach (var pf in new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            })
            {
                var root = Path.Combine(pf, "MySQL");
                if (Directory.Exists(root))
                    candidates.AddRange(Directory.EnumerateDirectories(root, "MySQL Server*")
                        .Select(v => Path.Combine(v, "bin", "mysqldump.exe")));
            }

            // از مسیر اجرایی خود mysqld (اگر دسترسی داشته باشد)
            try
            {
                foreach (var p in Process.GetProcessesByName("mysqld"))
                {
                    if (p.MainModule?.FileName is string path && File.Exists(path))
                        candidates.Add(Path.Combine(Path.GetDirectoryName(path)!, "mysqldump.exe"));
                }
            }
            catch { }

            return candidates.FirstOrDefault(File.Exists);
        }

        private static void RunMySqlDump(DatabaseBackupItem item, Action<string>? log)
        {
            if (string.IsNullOrEmpty(item.ToolPath) || !File.Exists(item.ToolPath))
                throw new InvalidOperationException(L.Text("S010"));

            var port = item.Server.Port ?? (item.Server.Type == DatabaseType.MariaDB ? 3307 : 3306);
            var (_, user, pass) = GetMySqlCreds(item.Server, port);

            Directory.CreateDirectory(item.BackupDir);
            var args = $"--host={item.Server.Host} --port={port} --user={user} " +
                       $"--single-transaction --routines --triggers --default-character-set=utf8mb4 " +
                       $"--result-file=\"{item.DestFile}\" \"{item.DatabaseName}\"";

            log?.Invoke(L.Format("S011", item.Server.TypeDisplayName, item.DatabaseName));
            RunProcess(item.ToolPath, args, new Dictionary<string, string> { ["MYSQL_PWD"] = pass }, log);
        }

        // ---------- PostgreSQL ----------
        private static List<DatabaseBackupItem> PlanPostgres(DatabaseInfo server, string destRoot, Action<string>? log)
        {
            var port = server.Port ?? 5432;
            var csb = BuildPostgresCsb(server, port);

            var dbNames = new List<string>();
            using (var conn = new NpgsqlConnection(csb.ConnectionString))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT datname FROM pg_database " +
                                  "WHERE datistemplate = false AND datname <> 'postgres' ORDER BY datname;";
                using var reader = cmd.ExecuteReader();
                while (reader.Read()) dbNames.Add(reader.GetString(0));
            }

            var pgDump = FindPgDump();
            var items = dbNames.Select(db =>
            {
                var it = NewItem(server, db, destRoot, "pg_dump");
                it.DestFile = Path.Combine(it.BackupDir, DatabaseFileLocator.SafeFolder(db) + ".dump");
                if (pgDump == null) it.Error = L.Text("S012");
                it.ToolPath = pgDump ?? "";
                return it;
            }).ToList();

            log?.Invoke(L.Format("S013", dbNames.Count));
            return items;
        }

        private static NpgsqlConnectionStringBuilder BuildPostgresCsb(DatabaseInfo server, int port)
        {
            var profile = ProfileManager.Load()
                .FirstOrDefault(p => p.Type == server.Type && p.Host == server.Host && p.Port == port);

            return new NpgsqlConnectionStringBuilder
            {
                Host = server.Host,
                Port = port,
                Username = profile?.Username ?? "postgres",
                Password = profile?.Password ?? "postgres",
                Database = "postgres",
                Timeout = 5
            };
        }

        private static string? FindPgDump()
        {
            var candidates = new List<string>();
            foreach (var pf in new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            })
            {
                var root = Path.Combine(pf, "PostgreSQL");
                if (Directory.Exists(root))
                    candidates.AddRange(Directory.EnumerateDirectories(root, "*")
                        .Select(v => Path.Combine(v, "bin", "pg_dump.exe")));
            }

            try
            {
                foreach (var p in Process.GetProcessesByName("postgres"))
                {
                    if (p.MainModule?.FileName is string path && File.Exists(path))
                        candidates.Add(Path.Combine(Path.GetDirectoryName(path)!, "pg_dump.exe"));
                    if (p.MainModule?.FileName is string path2 && File.Exists(path2))
                        candidates.Add(Path.Combine(Path.GetDirectoryName(path2)!, "pg_dump.exe"));
                }
            }
            catch { }

            return candidates.FirstOrDefault(File.Exists);
        }

        private static void RunPostgresDump(DatabaseBackupItem item, Action<string>? log)
        {
            if (string.IsNullOrEmpty(item.ToolPath) || !File.Exists(item.ToolPath))
                throw new InvalidOperationException(L.Text("S014"));

            var port = item.Server.Port ?? 5432;
            var profile = ProfileManager.Load()
                .FirstOrDefault(p => p.Type == item.Server.Type && p.Host == item.Server.Host && p.Port == port);
            var user = profile?.Username ?? "postgres";
            var pass = profile?.Password ?? "postgres";

            Directory.CreateDirectory(item.BackupDir);
            var args = $"--host={item.Server.Host} --port={port} --username={user} " +
                       $"--format=custom --file=\"{item.DestFile}\" \"{item.DatabaseName}\"";

            log?.Invoke(L.Format("S015", item.DatabaseName));
            RunProcess(item.ToolPath, args, new Dictionary<string, string> { ["PGPASSWORD"] = pass }, log);
        }

        // ---------- Redis ----------
        private static DatabaseBackupItem PlanRedis(DatabaseInfo server, string destRoot, Action<string>? log)
        {
            var it = NewItem(server, server.Name, destRoot, "SAVE + dump.rdb");
            it.DestFile = Path.Combine(it.BackupDir,
                $"dump_{DateTime.Now:yyyyMMdd_HHmmss}.rdb");
            return it;
        }

        private static void RunRedisSave(DatabaseBackupItem item, Action<string>? log)
        {
            var port = item.Server.Port ?? 6379;
            var profile = ProfileManager.Load()
                .FirstOrDefault(p => p.Type == item.Server.Type && p.Host == item.Server.Host && p.Port == port);

            var options = new ConfigurationOptions
            {
                EndPoints = { $"{item.Server.Host}:{port}" },
                AbortOnConnectFail = false,
                ConnectTimeout = 5000,
                SyncTimeout = 60000
            };
            if (!string.IsNullOrEmpty(profile?.Password))
                options.Password = profile.Password;

            using var redis = ConnectionMultiplexer.Connect(options);
            var db = redis.GetDatabase();
            log?.Invoke(L.Text("S016"));
            db.Execute("SAVE");

            string? dir = null;
            try
            {
                var raw = db.Execute("CONFIG", "GET", "dir");
                if (!raw.IsNull && raw.Length >= 2) dir = raw[1].ToString();
            }
            catch { }

            string? fileName = "dump.rdb";
            try
            {
                var raw = db.Execute("CONFIG", "GET", "dbfilename");
                if (!raw.IsNull && raw.Length >= 2) fileName = raw[1].ToString();
            }
            catch { }

            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                throw new InvalidOperationException(L.Text("S017"));

            var src = Path.Combine(dir, fileName ?? "dump.rdb");
            if (!File.Exists(src))
                throw new InvalidOperationException(L.Format("S018", src));

            Directory.CreateDirectory(item.BackupDir);
            File.Copy(src, item.DestFile, overwrite: true);
        }

        // ---------- MongoDB ----------
        private static DatabaseBackupItem PlanMongo(DatabaseInfo server, string destRoot, Action<string>? log)
        {
            var it = NewItem(server, server.Name, destRoot, "mongodump");
            it.ToolPath = FindMongoDump() ?? "";
            if (string.IsNullOrEmpty(it.ToolPath))
                it.Error = L.Text("S019");
            return it;
        }

        private static string? FindMongoDump()
        {
            var candidates = new List<string>();
            var progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var mongoRoot = Path.Combine(progFiles, "MongoDB", "Server");
            if (Directory.Exists(mongoRoot))
                candidates.AddRange(Directory.EnumerateDirectories(mongoRoot, "*")
                    .Select(v => Path.Combine(v, "bin", "mongodump.exe")));

            var tools = Path.Combine(progFiles, "MongoDB", "Tools");
            if (Directory.Exists(tools))
                candidates.AddRange(Directory.EnumerateDirectories(tools, "*", SearchOption.TopDirectoryOnly)
                    .Select(v => Path.Combine(v, "bin", "mongodump.exe")));

            return candidates.FirstOrDefault(File.Exists);
        }

        private static void RunMongoDump(DatabaseBackupItem item, Action<string>? log)
        {
            if (string.IsNullOrEmpty(item.ToolPath) || !File.Exists(item.ToolPath))
                throw new InvalidOperationException(L.Text("S020"));

            Directory.CreateDirectory(item.BackupDir);
            var port = item.Server.Port ?? 27017;
            var args = $"--host={item.Server.Host} --port={port} --out=\"{item.BackupDir}\"";

            log?.Invoke(L.Format("S021", item.BackupDir));
            RunProcess(item.ToolPath, args, null, log);
        }

        // ---------- اجرای بکاپ ها ----------
        public static void ExecuteBackup(List<DatabaseBackupItem> items, Action<string>? log = null)
        {
            foreach (var item in items)
            {
                item.OutputFiles.Clear();
                item.Done = false;
                item.Failed = false;

                if (!string.IsNullOrEmpty(item.Error))
                {
                    item.Failed = true;
                    log?.Invoke(L.Format("S022", item.FolderName, item.Error));
                    continue;
                }

                try
                {
                    item.StartedUtc = DateTime.UtcNow;
                    Directory.CreateDirectory(item.BackupDir);

                    switch (item.Server.Type)
                    {
                        case DatabaseType.SQLServer:
                            RunSqlBackup(item, log);
                            break;
                        case DatabaseType.MySQL:
                        case DatabaseType.MariaDB:
                            RunMySqlDump(item, log);
                            break;
                        case DatabaseType.PostgreSQL:
                            RunPostgresDump(item, log);
                            break;
                        case DatabaseType.Redis:
                            RunRedisSave(item, log);
                            break;
                        case DatabaseType.MongoDB:
                            RunMongoDump(item, log);
                            break;
                        default:
                            throw new InvalidOperationException(L.Format("S023", item.Server.TypeDisplayName));
                    }

                    item.OutputFiles = Directory.Exists(item.BackupDir)
                        ? Directory.EnumerateFiles(item.BackupDir, "*", SearchOption.AllDirectories).ToList()
                        : new List<string>();
                    item.BytesProduced = item.OutputFiles.Sum(f => new FileInfo(f).Length);
                    item.EndedUtc = DateTime.UtcNow;
                    item.Done = true;
                    log?.Invoke(L.Format("S024", item.FolderName, DatabaseFileLocator.FormatSize(item.BytesProduced)));
                }
                catch (Exception ex)
                {
                    item.EndedUtc = DateTime.UtcNow;
                    item.Failed = true;
                    item.Error = ex.Message;
                    log?.Invoke(L.Format("S025", item.FolderName, ex.Message));
                }
            }
        }

        // ---------- ابزار ----------
        private static bool IsLocalHost(DatabaseInfo server)
        {
            var h = (server.Host ?? "").Trim();
            if (string.IsNullOrEmpty(h)) return true;
            if (h.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;
            return h == "127.0.0.1" || h == "::1" || h == "[::1]" || h == "0.0.0.0";
        }

        private static string RunProcess(string exe, string args, Dictionary<string, string>? env, Action<string>? log)
        {
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };
            if (env != null)
                foreach (var kv in env)
                    psi.Environment[kv.Key] = kv.Value;

            using var p = Process.Start(psi);
            if (p == null) throw new InvalidOperationException(L.Format("S026", exe));
            var err = p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0)
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(err) ? L.Text("S027") : err.Trim());
            return err;
        }
    }
}