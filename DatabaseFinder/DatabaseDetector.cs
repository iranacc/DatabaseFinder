using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;

namespace DatabaseFinder
{
    public enum DatabaseType
    {
        SQLServer,
        MySQL,
        MariaDB,
        PostgreSQL,
        Oracle,
        MongoDB,
        Redis,
        Elasticsearch,
        SQLite,
        CouchDB,
        Unknown
    }

    public class DatabaseInfo
    {
        public DatabaseType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? Port { get; set; }
        public bool IsRunningAsService { get; set; }
        public bool IsRunningAsProcess { get; set; }
        public string? ServiceName { get; set; }
        public int ProcessId { get; set; }
        public string? ProcessName { get; set; }

        public string Version { get; set; } = "";
        public DateTime DetectedAt { get; set; } = DateTime.Now;
        public string Host { get; set; } = "localhost";
        public string ServerName { get; set; } = "";
        public int? DatabaseCount { get; set; }
        public List<string>? DatabaseNames { get; set; }

        // وضعیت آنلاین/آفلاین
        public bool IsOnline { get; set; } = true;
        public string? LocalPath { get; set; }
        public long FileSize { get; set; }
        public DateTime FileModified { get; set; }
        public bool IsBackup { get; set; }
        public string FormatName { get; set; } = "";

        public string TypeDisplayName
        {
            get
            {
                switch (Type)
                {
                    case DatabaseType.SQLServer: return "SQL Server";
                    case DatabaseType.MySQL: return "MySQL";
                    case DatabaseType.MariaDB: return "MariaDB";
                    case DatabaseType.PostgreSQL: return "PostgreSQL";
                    case DatabaseType.Oracle: return "Oracle";
                    case DatabaseType.MongoDB: return "MongoDB";
                    case DatabaseType.Redis: return "Redis";
                    case DatabaseType.Elasticsearch: return "Elasticsearch";
                    case DatabaseType.SQLite: return "SQLite";
                    case DatabaseType.CouchDB: return "CouchDB";
                    default: return "Unknown";
                }
            }
        }
    }

    public class DatabaseDetector
    {
        private static readonly int[] MS_SQL_PORTS = { 1433, 1434 };
        private static readonly int[] MySQL_PORTS = { 3306 };
        private static readonly int[] MariaDB_PORTS = { 3307 };
        private static readonly int[] PostgreSQL_PORTS = { 5432, 5433 };
        private static readonly int[] Oracle_PORTS = { 1521, 1522 };
        private static readonly int[] MongoDB_PORTS = { 27017, 27018 };
        private static readonly int[] Redis_PORTS = { 6379 };
        private static readonly int[] Elasticsearch_PORTS = { 9200 };
        private static readonly int[] CouchDB_PORTS = { 5984 };

        private AppSettings _settings;

        public DatabaseDetector()
        {
            _settings = AppSettings.Load();
        }

        public DatabaseDetector(AppSettings settings)
        {
            _settings = settings;
        }

        public void ReloadSettings(AppSettings settings)
        {
            _settings = settings;
        }

        private int[] GetPorts(DatabaseType type, int[] defaults)
        {
            var key = type.ToString();
            if (_settings.CustomPorts.TryGetValue(key, out int custom))
            {
                return new[] { custom };
            }
            return defaults;
        }

        public List<DatabaseInfo> Detect()
        {
            var databases = new List<DatabaseInfo>();
            var runningServices = GetRunningServices();
            var runningProcesses = GetRunningProcesses();

            databases.AddRange(DetectByServices(runningServices, runningProcesses));
            databases.AddRange(DetectByProcesses(runningProcesses));
            databases.AddRange(DetectByPorts(runningProcesses));

            var unique = databases
                .GroupBy(d => $"{d.Type}-{d.Port?.ToString() ?? d.ProcessName ?? ""}")
                .Select(g => g.First())
                .OrderBy(d => d.TypeDisplayName)
                .ToList();

            return unique;
        }

        private List<DatabaseInfo> DetectByServices(List<ServiceInfo> services, List<ProcessInfo> processes)
        {
            var result = new List<DatabaseInfo>();

            foreach (var svc in services)
            {
                var svcLower = svc.Name.ToLowerInvariant();
                var displayLower = svc.DisplayName.ToLowerInvariant();

                DatabaseType? type = null;
                if (ContainsAny(svcLower, displayLower, "sqlserver", "mssql", "sql server")) type = DatabaseType.SQLServer;
                else if (ContainsAny(svcLower, displayLower, "mysql")) type = DatabaseType.MySQL;
                else if (ContainsAny(svcLower, displayLower, "maria")) type = DatabaseType.MariaDB;
                else if (ContainsAny(svcLower, displayLower, "postgres", "pgsql")) type = DatabaseType.PostgreSQL;
                else if (ContainsAny(svcLower, displayLower, "oracle")) type = DatabaseType.Oracle;
                else if (ContainsAny(svcLower, displayLower, "mongodb", "mongo")) type = DatabaseType.MongoDB;
                else if (ContainsAny(svcLower, displayLower, "redis")) type = DatabaseType.Redis;
                else if (ContainsAny(svcLower, displayLower, "elasticsearch", "elastic")) type = DatabaseType.Elasticsearch;
                else if (ContainsAny(svcLower, displayLower, "couchdb")) type = DatabaseType.CouchDB;

                if (type.HasValue)
                {
                    result.Add(new DatabaseInfo
                    {
                        Type = type.Value,
                        Name = svc.DisplayName,
                        IsRunningAsService = true,
                        ServiceName = svc.Name,
                        Port = GetPortForType(type.Value)
                    });
                }
            }

            return result;
        }

        private List<DatabaseInfo> DetectByProcesses(List<ProcessInfo> processes)
        {
            var result = new List<DatabaseInfo>();

            foreach (var proc in processes)
            {
                var name = proc.Name.ToLowerInvariant();
                DatabaseType? type = null;

                if (name.Contains("sqlservr")) type = DatabaseType.SQLServer;
                else if (name.Contains("mysqld") || name.Equals("mysql", StringComparison.OrdinalIgnoreCase)) type = DatabaseType.MySQL;
                else if (name.Contains("mariadbd")) type = DatabaseType.MariaDB;
                else if (name.Contains("postgres")) type = DatabaseType.PostgreSQL;
                else if (name.Contains("oracle")) type = DatabaseType.Oracle;
                else if (name.Contains("mongod")) type = DatabaseType.MongoDB;
                else if (name.Contains("redis-server") || name.Contains("redis")) type = DatabaseType.Redis;
                else if (name.Contains("elasticsearch")) type = DatabaseType.Elasticsearch;
                else if (name.Contains("couchdb")) type = DatabaseType.CouchDB;

                if (type.HasValue)
                {
                    result.Add(new DatabaseInfo
                    {
                        Type = type.Value,
                        Name = proc.Name,
                        IsRunningAsProcess = true,
                        ProcessId = proc.Id,
                        ProcessName = proc.Name,
                        Port = GetPortForType(type.Value)
                    });
                }
            }

            return result;
        }

        private List<DatabaseInfo> DetectByPorts(List<ProcessInfo> processes)
        {
            var result = new List<DatabaseInfo>();
            var occupiedPorts = GetPortOwners(); // port -> pid (یک بار اسکن)

            // چک پورت‌ها در اولویت؛ اگر پورت اشغال باشه یعنی سرویس در حال اجراست
            CheckPort(occupiedPorts, GetPorts(DatabaseType.SQLServer, MS_SQL_PORTS), DatabaseType.SQLServer, processes, result);
            CheckPort(occupiedPorts, GetPorts(DatabaseType.MySQL, MySQL_PORTS), DatabaseType.MySQL, processes, result);
            CheckPort(occupiedPorts, GetPorts(DatabaseType.MariaDB, MariaDB_PORTS), DatabaseType.MariaDB, processes, result);
            CheckPort(occupiedPorts, GetPorts(DatabaseType.PostgreSQL, PostgreSQL_PORTS), DatabaseType.PostgreSQL, processes, result);
            CheckPort(occupiedPorts, GetPorts(DatabaseType.Oracle, Oracle_PORTS), DatabaseType.Oracle, processes, result);
            CheckPort(occupiedPorts, GetPorts(DatabaseType.MongoDB, MongoDB_PORTS), DatabaseType.MongoDB, processes, result);
            CheckPort(occupiedPorts, GetPorts(DatabaseType.Redis, Redis_PORTS), DatabaseType.Redis, processes, result);
            CheckPort(occupiedPorts, GetPorts(DatabaseType.Elasticsearch, Elasticsearch_PORTS), DatabaseType.Elasticsearch, processes, result);
            CheckPort(occupiedPorts, GetPorts(DatabaseType.CouchDB, CouchDB_PORTS), DatabaseType.CouchDB, processes, result);

            return result;
        }

        private void CheckPort(Dictionary<int, int> occupiedPorts, int[] ports, DatabaseType type, List<ProcessInfo> processes, List<DatabaseInfo> result)
        {
            foreach (var port in ports)
            {
                if (occupiedPorts.TryGetValue(port, out int pid))
                {
                    var proc = processes.FirstOrDefault(p => p.Id == pid);
                    result.Add(new DatabaseInfo
                    {
                        Type = type,
                        Name = proc?.Name ?? "Running on port " + port,
                        Port = port,
                        IsRunningAsProcess = proc != null,
                        ProcessId = pid,
                        ProcessName = proc?.Name
                    });
                }
            }
        }

        /// <summary>
        /// مپ پورت‌های شنیده‌شده به PID مالک آنها را با یک بار اجرای netstat می‌سازد.
        /// قبلاً برای هر پورت یک بار netstat اجرا می‌شد (O(n²)).
        /// </summary>
        private Dictionary<int, int> GetPortOwners()
        {
            var occupiedPorts = new Dictionary<int, int>();

            // ابتدا پورت‌های در حال شنیدن را (IPv4 و IPv6) مشخص کن تا فقط همین‌ها PID بگیرند.
            try
            {
                foreach (var listener in IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners())
                {
                    occupiedPorts[listener.Port] = 0;
                }
            }
            catch
            {
                // اگر دسترسی به اطلاعات پورت ممکن نشد، ادامه بده
            }

            if (occupiedPorts.Count == 0) return occupiedPorts;

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "netstat",
                    Arguments = "-ano",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var proc = System.Diagnostics.Process.Start(startInfo))
                {
                    if (proc == null) return occupiedPorts;
                    var output = proc.StandardOutput.ReadToEnd();
                    foreach (var line in output.Split('\n'))
                    {
                        if (!line.ToLowerInvariant().Contains("listening")) continue;
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 5) continue;
                        if (!int.TryParse(parts[4], out int pid)) continue;

                        // آدرس محلی می‌تواند IPv4 (0.0.0.0:1433) یا IPv6 ([::]:1433) باشد.
                        var local = parts[1];
                        var colon = local.LastIndexOf(':');
                        if (colon < 0 || colon + 1 >= local.Length) continue;
                        if (int.TryParse(local.Substring(colon + 1), out int port) &&
                            occupiedPorts.ContainsKey(port))
                        {
                            occupiedPorts[port] = pid;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Write("Detect.Ports", ex);
            }

            return occupiedPorts;
        }

        private List<ServiceInfo> GetRunningServices()
        {
            var result = new List<ServiceInfo>();

            try
            {
                using (var searcher = new System.Management.ManagementObjectSearcher(
                    "SELECT * FROM Win32_Service WHERE State = 'Running'"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        var s = new ServiceInfo
                        {
                            Name = obj["Name"]?.ToString() ?? "",
                            DisplayName = obj["DisplayName"]?.ToString() ?? ""
                        };

                        if (IsDatabaseService(s))
                            result.Add(s);
                    }
                }
            }
            catch (Exception ex)
            {
                // اگر WMI در دسترس نبود، ادامه بده
                AppLog.Write("Detect.Services", ex);
            }

            return result;
        }

        private List<ProcessInfo> GetRunningProcesses()
        {
            var result = new List<ProcessInfo>();

            try
            {
                foreach (var proc in System.Diagnostics.Process.GetProcesses())
                {
                    try
                    {
                        var name = proc.ProcessName.ToLowerInvariant();
                        if (IsDatabaseProcessName(name))
                        {
                            result.Add(new ProcessInfo { Id = proc.Id, Name = proc.ProcessName });
                        }
                    }
                    catch
                    {
                        // دسترسی به برخی پروسس‌ها ممکن نیست
                    }
                }
            }
            catch (Exception ex) { AppLog.Write("Detect.Processes", ex); }

            return result;
        }

        private bool IsDatabaseService(ServiceInfo s)
        {
            var n = s.Name.ToLowerInvariant();
            var d = s.DisplayName.ToLowerInvariant();
            return ContainsAny(n, d,
                "sqlserver", "mssql", "mysql", "maria", "postgres", "pgsql",
                "oracle", "mongodb", "mongo", "redis", "elasticsearch", "elastic", "couchdb");
        }

        private bool IsDatabaseProcessName(string name)
        {
            return name.Contains("sqlservr") ||
                   name.Contains("mysqld") ||
                   name.Equals("mysql", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("mariadbd") ||
                   name.Contains("postgres") ||
                   name.Contains("oracle") ||
                   name.Contains("mongod") ||
                   name.Contains("redis-server") ||
                   name.Contains("redis") ||
                   name.Contains("elasticsearch") ||
                   name.Contains("couchdb");
        }

        private bool ContainsAny(string a, string b, params string[] values)
        {
            return values.Any(v => a.Contains(v) || b.Contains(v));
        }

        private int? GetPortForType(DatabaseType type)
        {
            int? def = null;
            switch (type)
            {
                case DatabaseType.SQLServer: def = 1433; break;
                case DatabaseType.MySQL: def = 3306; break;
                case DatabaseType.MariaDB: def = 3307; break;
                case DatabaseType.PostgreSQL: def = 5432; break;
                case DatabaseType.Oracle: def = 1521; break;
                case DatabaseType.MongoDB: def = 27017; break;
                case DatabaseType.Redis: def = 6379; break;
                case DatabaseType.Elasticsearch: def = 9200; break;
                case DatabaseType.CouchDB: def = 5984; break;
            }
            if (def.HasValue)
            {
                var key = type.ToString();
                if (_settings.CustomPorts.TryGetValue(key, out int custom))
                    return custom;
            }
            return def;
        }

        private class ServiceInfo
        {
            public string Name { get; set; } = "";
            public string DisplayName { get; set; } = "";
        }

        private class ProcessInfo
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }
    }
}
