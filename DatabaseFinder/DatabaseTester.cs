using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace DatabaseFinder
{
    public class ConnectionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string Version { get; set; } = "";
    }

    public static class DatabaseTester
    {
        /// <summary>
        /// نسخهٔ غیرمسدودکنندهٔ تست اتصال برای استفاده از ترد UI. کل عملیات (اتصال + خواندن بنر)
        /// با یک مهلت سراسری محدود می‌شود؛ دیتابیس‌هایی که خودشان بنر نمی‌فرستند (مثل SQL Server)
        /// نباید تست را برای همیشه معلق نگه دارند.
        /// </summary>
        public static async Task<ConnectionResult> TestConnectionAsync(DatabaseInfo db)
        {
            var port = db.Port ?? 0;
            if (port == 0) return new ConnectionResult { Success = false, Message = L.Text("S149") };

            using var tcp = new TcpClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                await tcp.ConnectAsync("127.0.0.1", port, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return new ConnectionResult { Success = false, Message = L.Text("S312") };
            }
            catch (Exception ex)
            {
                return new ConnectionResult { Success = false, Message = L.Format("S150", ex.Message) };
            }

            if (!tcp.Connected)
                return new ConnectionResult { Success = false, Message = L.Text("S151") };

            var result = new ConnectionResult { Success = true, Message = L.Text("S152") };

            try
            {
                var stream = tcp.GetStream();
                result.Version = db.Type switch
                {
                    DatabaseType.Redis => await GetBannerAsync(stream, "*1\r\n$4\r\nINFO\r\n", "redis_version:", cts.Token),
                    _ => await GetBannerAsync(stream, null, null, cts.Token)
                };
            }
            catch (OperationCanceledException)
            {
                // بدون بنر (مثل SQL Server)؛ اتصال صحیح است اما نسخه دریافت نشد.
            }
            catch { }

            return result;
        }

        public static ConnectionResult TestConnection(DatabaseInfo db)
        {
            var port = db.Port ?? 0;
            if (port == 0) return new ConnectionResult { Success = false, Message = L.Text("S149") };

            // تست TCP - آیا پورت باز است؟
            using var tcp = new TcpClient();
            try
            {
                tcp.Connect("127.0.0.1", port);
            }
            catch (Exception ex)
            {
                return new ConnectionResult { Success = false, Message = L.Format("S150", ex.Message) };
            }

            if (!tcp.Connected)
                return new ConnectionResult { Success = false, Message = L.Text("S151") };

            var result = new ConnectionResult { Success = true, Message = L.Text("S152") };

            // تشخیص به کمک بنر (banner)
            try
            {
                tcp.SendTimeout = 3000;
                tcp.ReceiveTimeout = 3000;
                var stream = tcp.GetStream();

                switch (db.Type)
                {
                    case DatabaseType.Redis:
                        result.Version = GetBanner(stream, "*1\r\n$4\r\nINFO\r\n", "redis_version:");
                        break;
                    case DatabaseType.PostgreSQL:
                        result.Version = GetBannerPostgre(stream);
                        break;
                    case DatabaseType.MySQL:
                    case DatabaseType.MariaDB:
                        result.Version = GetBannerMySQL(stream);
                        break;
                    default:
                        result.Version = GetBanner(stream, null, null);
                        break;
                }
            }
            catch { }

            return result;
        }

        private static string GetBanner(NetworkStream stream, string? send, string? keyword)
        {
            try
            {
                if (!string.IsNullOrEmpty(send))
                {
                    var bytes = Encoding.ASCII.GetBytes(send);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush();
                }

                var buffer = new byte[4096];
                int read = stream.Read(buffer, 0, buffer.Length);
                if (read > 0)
                {
                    var text = Encoding.UTF8.GetString(buffer, 0, read);
                    if (!string.IsNullOrEmpty(keyword))
                    {
                        var idx = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
                        if (idx >= 0)
                        {
                            return text.Substring(idx + keyword.Length, Math.Min(30, text.Length - idx - keyword.Length)).Trim();
                        }
                    }
                    return Truncate(text, 80);
                }
            }
            catch { }
            return "";
        }

        private static async Task<string> GetBannerAsync(NetworkStream stream, string? send, string? keyword, CancellationToken ct)
        {
            try
            {
                if (!string.IsNullOrEmpty(send))
                {
                    var bytes = Encoding.ASCII.GetBytes(send);
                    await stream.WriteAsync(bytes, 0, bytes.Length, ct);
                    await stream.FlushAsync(ct);
                }

                var buffer = new byte[4096];
                int read = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
                if (read > 0)
                {
                    var text = Encoding.UTF8.GetString(buffer, 0, read);
                    if (!string.IsNullOrEmpty(keyword))
                    {
                        var idx = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
                        if (idx >= 0)
                        {
                            return text.Substring(idx + keyword.Length, Math.Min(30, text.Length - idx - keyword.Length)).Trim();
                        }
                    }
                    return Truncate(text, 80);
                }
            }
            catch { }
            return "";
        }

        private static string GetBannerPostgre(NetworkStream stream)
        {
            try
            {
                var buffer = new byte[4096];
                int read = stream.Read(buffer, 0, buffer.Length);
                if (read > 0)
                {
                    var text = Encoding.UTF8.GetString(buffer, 0, read);
                    return Truncate(text, 80);
                }
            }
            catch { }
            return "";
        }

        private static string GetBannerMySQL(NetworkStream stream)
        {
            try
            {
                var buffer = new byte[4096];
                int read = stream.Read(buffer, 0, buffer.Length);
                if (read > 0)
                {
                    var text = Encoding.UTF8.GetString(buffer, 0, read);
                    return Truncate(text, 80);
                }
            }
            catch { }
            return "";
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\n", " ").Replace("\r", " ");
            if (s.Length > max) s = s.Substring(0, max);
            return s.Trim();
        }
    }
}
