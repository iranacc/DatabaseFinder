using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

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
                else if (name.Contains("mysqld") || name.Contains("mysql")) type = DatabaseType.MySQL;
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
            var processIds = new HashSet<int>(processes.Select(p => p.Id));
            var occupiedPorts = new Dictionary<int, int>(); // port -> pid

            try
            {
                foreach (var listener in IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners())
                {
                    var port = listener.Port;
                    int pid = GetProcessIdForPort(listener);
                    occupiedPorts[port] = pid;
                }
            }
            catch
            {
                // اگر دسترسی به اطلاعات پورت ممکن نشد، ادامه بده
            }

            // چک پورت‌ها در اولویت؛ اگر پورت اشغال باشه یعنی سرویس در حال اجراست
            CheckPort(occupiedPorts, MS_SQL_PORTS, DatabaseType.SQLServer, processes, result);
            CheckPort(occupiedPorts, MySQL_PORTS, DatabaseType.MySQL, processes, result);
            CheckPort(occupiedPorts, MariaDB_PORTS, DatabaseType.MariaDB, processes, result);
            CheckPort(occupiedPorts, PostgreSQL_PORTS, DatabaseType.PostgreSQL, processes, result);
            CheckPort(occupiedPorts, Oracle_PORTS, DatabaseType.Oracle, processes, result);
            CheckPort(occupiedPorts, MongoDB_PORTS, DatabaseType.MongoDB, processes, result);
            CheckPort(occupiedPorts, Redis_PORTS, DatabaseType.Redis, processes, result);
            CheckPort(occupiedPorts, Elasticsearch_PORTS, DatabaseType.Elasticsearch, processes, result);
            CheckPort(occupiedPorts, CouchDB_PORTS, DatabaseType.CouchDB, processes, result);

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

        private int GetProcessIdForPort(IPEndPoint endpoint)
        {
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
                    if (proc == null) return 0;
                    var output = proc.StandardOutput.ReadToEnd();
                    foreach (var line in output.Split('\n'))
                    {
                        if (line.ToLowerInvariant().Contains("listening") &&
                            line.Contains($":{endpoint.Port}"))
                        {
                            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 5 && int.TryParse(parts[4], out int pid))
                            {
                                return pid;
                            }
                        }
                    }
                }
            }
            catch
            {
            }
            return 0;
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
            catch
            {
                // اگر WMI در دسترس نبود، ادامه بده
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
            catch { }

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
                   name.Contains("mysql") ||
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
            switch (type)
            {
                case DatabaseType.SQLServer: return 1433;
                case DatabaseType.MySQL: return 3306;
                case DatabaseType.MariaDB: return 3307;
                case DatabaseType.PostgreSQL: return 5432;
                case DatabaseType.Oracle: return 1521;
                case DatabaseType.MongoDB: return 27017;
                case DatabaseType.Redis: return 6379;
                case DatabaseType.Elasticsearch: return 9200;
                case DatabaseType.CouchDB: return 5984;
                default: return null;
            }
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
