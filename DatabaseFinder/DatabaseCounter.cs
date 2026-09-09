using System.Data;
using System.Net;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;

namespace DatabaseFinder
{
    /// <summary>شمارش واقعی دیتابیس‌های هر سرویس با اتصال و اجرای کوئری شمارنده.</summary>
    public static class DatabaseCounter
    {
        public static void Measure(IReadOnlyCollection<DatabaseInfo> services)
        {
            if (services.Count == 0) return;
            var profiles = ProfileManager.Load();
            Parallel.ForEach(services, new ParallelOptions { MaxDegreeOfParallelism = 4 }, service =>
            {
                try { service.DatabaseCount = CountFor(service, profiles); }
                catch { service.DatabaseCount = null; }
            });
        }

        private static int? CountFor(DatabaseInfo db, List<DatabaseProfile> profiles)
        {
            switch (db.Type)
            {
                case DatabaseType.SQLServer:
                    return CountSqlServer(db, profiles);
                case DatabaseType.MySQL:
                case DatabaseType.MariaDB:
                    return CountMySql(db, profiles);
                case DatabaseType.PostgreSQL:
                    return CountPostgre(db, profiles);
                default:
                    return null;
            }
        }

        private static int? CountMySql(DatabaseInfo db, List<DatabaseProfile> profiles)
        {
            var p = FindProfile(db, profiles);
            if (p == null || string.IsNullOrEmpty(p.Username)) return null;
            var csb = new MySqlConnectionStringBuilder
            {
                Server = db.Host,
                Port = (uint)(db.Port ?? 3306),
                UserID = p.Username,
                Password = p.Password,
                ConnectionTimeout = 3
            };
            using var conn = new MySqlConnection(csb.ConnectionString);
            conn.Open();
            return ScalarCount(conn, "SELECT COUNT(*) FROM information_schema.SCHEMATA WHERE SCHEMA_NAME NOT IN ('information_schema','mysql','performance_schema','sys');");
        }

        private static int? CountPostgre(DatabaseInfo db, List<DatabaseProfile> profiles)
        {
            var p = FindProfile(db, profiles);
            if (p == null || string.IsNullOrEmpty(p.Username)) return null;
            var csb = new NpgsqlConnectionStringBuilder
            {
                Host = db.Host,
                Port = db.Port ?? 5432,
                Username = p.Username,
                Password = p.Password,
                Database = string.IsNullOrEmpty(p.DatabaseName) ? "postgres" : p.DatabaseName,
                Timeout = 5
            };
            using var conn = new NpgsqlConnection(csb.ConnectionString);
            conn.Open();
            return ScalarCount(conn, "SELECT COUNT(*) FROM pg_database WHERE datistemplate = false;");
        }

        private static int? CountSqlServer(DatabaseInfo db, List<DatabaseProfile> profiles)
        {
            var p = FindProfile(db, profiles);
            var cs = BuildSqlServerCs(db, p);
            using var conn = new SqlConnection(cs);
            conn.Open();
            return ScalarCount(conn, "SELECT COUNT(*) FROM sys.databases WHERE database_id > 4;");
        }

        private static string BuildSqlServerCs(DatabaseInfo db, DatabaseProfile? p)
        {
            var serverName = db.Port.HasValue && db.Port.Value > 0
                ? $"{db.Host},{db.Port.Value}"
                : db.Host;
            return p != null && !string.IsNullOrEmpty(p.Username)
                ? $"Data Source={serverName};User ID={p.Username};Password={p.Password};TrustServerCertificate=True;Connect Timeout=5;"
                : $"Data Source={serverName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5;";
        }

        private static DatabaseProfile? FindProfile(DatabaseInfo db, List<DatabaseProfile> profiles)
        {
            var byPort = profiles.Where(p => p.Type == db.Type && p.Port == (db.Port ?? 0)).ToList();
            return byPort.FirstOrDefault(p => HostsEquivalent(p.Host, db.Host));
        }

        private static readonly HashSet<string> LocalAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            "localhost", "127.0.0.1", "::1", "."
        };

        private static bool HostsEquivalent(string a, string b)
        {
            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return true;
            if (LocalAliases.Contains(a) && LocalAliases.Contains(b)) return true;
            try
            {
                return string.Equals(a, Dns.GetHostName(), StringComparison.OrdinalIgnoreCase)
                    && LocalAliases.Contains(b);
            }
            catch { return false; }
        }

        private static int ScalarCount(IDbConnection conn, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandTimeout = 5;
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }
}