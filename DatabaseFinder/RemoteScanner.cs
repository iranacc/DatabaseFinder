using System.Net.Sockets;

namespace DatabaseFinder
{
    public class RemoteScanResult
    {
        public string Host { get; set; } = "";
        public string ServerName { get; set; } = "";
        public DatabaseType Type { get; set; }
        public int Port { get; set; }
        public bool IsOpen { get; set; }
        public string Version { get; set; } = "";
        public string Message { get; set; } = "";

        public string TypeDisplayName
        {
            get => new DatabaseInfo { Type = Type }.TypeDisplayName;
        }
    }

    public static class RemoteScanner
    {
        public static readonly (DatabaseType Type, int Port)[] KnownPorts = new[]
        {
            (DatabaseType.SQLServer, 1433),
            (DatabaseType.MySQL, 3306),
            (DatabaseType.MariaDB, 3307),
            (DatabaseType.PostgreSQL, 5432),
            (DatabaseType.Oracle, 1521),
            (DatabaseType.MongoDB, 27017),
            (DatabaseType.Redis, 6379),
            (DatabaseType.Elasticsearch, 9200),
            (DatabaseType.CouchDB, 5984),
        };

        public static List<RemoteScanResult> Scan(string host, int[] ports, int timeoutMs = 2000)
        {
            var results = new List<RemoteScanResult>();
            var toScan = new List<int>();

            if (ports == null || ports.Length == 0)
            {
                toScan.AddRange(KnownPorts.Select(p => p.Port));
            }
            else
            {
                toScan.AddRange(ports);
            }

            foreach (var port in toScan.Distinct())
            {
                var type = KnownPorts.FirstOrDefault(kp => kp.Port == port).Type;
                results.Add(CheckPort(host, port, DatabaseType.Unknown, timeoutMs));
            }

            // ترکیب نتیجه با نوع شناخته‌شده
            foreach (var r in results)
            {
                var known = KnownPorts.FirstOrDefault(kp => kp.Port == r.Port);
                if (known.Port != 0)
                {
                    r.Type = known.Type;
                }
            }

            return results.OrderBy(r => r.TypeDisplayName).ToList();
        }

        /// <summary>
        /// اسکن چند هاست با همزمانی محدود برای رنج‌های شبکه. هر هاست با <see cref="ScanWithBrowser"/> پردازش می‌شود.
        /// </summary>
        public static List<RemoteScanResult> ScanNetwork(IEnumerable<string> hosts, int[] ports, int timeoutMs = 1500, int concurrency = 50)
        {
            var results = new List<RemoteScanResult>();
            var sync = new object();
            try
            {
                Parallel.ForEachAsync(hosts, new ParallelOptions { MaxDegreeOfParallelism = concurrency },
                    (host, _) =>
                    {
                        var perHost = ScanWithBrowser(host, ports, timeoutMs);
                        lock (sync) results.AddRange(perHost);
                        return ValueTask.CompletedTask;
                    }).GetAwaiter().GetResult();
            }
            catch { }
            return results.OrderBy(r => r.Host).ThenBy(r => r.TypeDisplayName).ToList();
        }

        /// <summary>
        /// اسکن TCP پورت‌های شناخته‌شده به‌همراه پرس‌وجوی SQL Browser (UDP 1434) برای یافتن
        /// instance های دارای پورت داینامیک که روی پورت استاندارد نیستند.
        /// </summary>
        public static List<RemoteScanResult> ScanWithBrowser(string host, int[] ports, int timeoutMs = 1500)
        {
            var results = Scan(host, ports, timeoutMs);
            var seen = new HashSet<int>(results.Where(r => r.IsOpen).Select(r => r.Port));

            foreach (var inst in SqlBrowser.Probe(host, SqlBrowser.DefaultPort, Math.Min(timeoutMs, 1000)))
            {
                if (seen.Contains(inst.TcpPort)) continue;

                var check = CheckPort(host, inst.TcpPort, DatabaseType.SQLServer, timeoutMs);
                if (!check.IsOpen) continue;

                check.ServerName = inst.ServerName;
                if (!string.IsNullOrEmpty(inst.Version)) check.Version = inst.Version;
                if (!string.IsNullOrEmpty(inst.InstanceName) &&
                    !inst.InstanceName.Equals("MSSQLSERVER", StringComparison.OrdinalIgnoreCase))
                {
                    check.Message = inst.InstanceName;
                }
                results.Add(check);
                seen.Add(inst.TcpPort);
            }

            return results.OrderBy(r => r.Host).ThenBy(r => r.TypeDisplayName).ToList();
        }

        public static RemoteScanResult CheckPort(string host, int port, DatabaseType type, int timeoutMs = 2000)
        {
            var result = new RemoteScanResult { Host = host, Port = port, Type = type };

            try
            {
                using var client = new TcpClient();
                var task = client.ConnectAsync(host, port);
                if (!task.Wait(timeoutMs))
                {
                    result.IsOpen = false;
                    result.Message = L.Text("S282");
                    return result;
                }

                if (client.Connected)
                {
                    result.IsOpen = true;
                    result.Message = L.Text("S283");

                    // تشخیص نسخه با بنر
                    try
                    {
                        client.SendTimeout = timeoutMs;
                        client.ReceiveTimeout = timeoutMs;
                        var stream = client.GetStream();

                        switch (type)
                        {
                            case DatabaseType.Redis:
                                result.Version = ReadBanner(stream, "*1\r\n$4\r\nINFO\r\n", "redis_version:");
                                break;
                            case DatabaseType.PostgreSQL:
                                result.Version = ReadBanner(stream, null, null);
                                break;
                            case DatabaseType.MySQL:
                            case DatabaseType.MariaDB:
                            case DatabaseType.SQLServer:
                                result.Version = ReadBanner(stream, null, null);
                                break;
                            default:
                                result.Version = ReadBanner(stream, null, null);
                                break;
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                result.IsOpen = false;
                result.Message = ex.Message;
            }

            return result;
        }

        private static string ReadBanner(NetworkStream stream, string? send, string? keyword)
        {
            try
            {
                if (!string.IsNullOrEmpty(send))
                {
                    var bytes = System.Text.Encoding.ASCII.GetBytes(send);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush();
                }

                var buffer = new byte[1024];
                int read = stream.Read(buffer, 0, buffer.Length);
                if (read > 0)
                {
                    var text = System.Text.Encoding.UTF8.GetString(buffer, 0, read);
                    if (!string.IsNullOrEmpty(keyword))
                    {
                        var idx = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
                        if (idx >= 0)
                        {
                            var len = Math.Min(30, text.Length - idx - keyword.Length);
                            return text.Substring(idx + keyword.Length, len).Trim();
                        }
                    }
                    return text.Replace("\r", " ").Replace("\n", " ").Trim();
                }
            }
            catch { }
            return "";
        }
    }
}