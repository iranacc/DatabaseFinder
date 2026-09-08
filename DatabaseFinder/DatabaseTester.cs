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
