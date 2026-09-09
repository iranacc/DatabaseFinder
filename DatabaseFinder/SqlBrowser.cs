using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DatabaseFinder
{
    public sealed record SqlBrowserInstance(string Host, string ServerName, string InstanceName, string Version, int TcpPort);

    public static class SqlBrowser
    {
        public const int DefaultPort = 1434;

        /// <summary>
        /// درخواست SSRP برای فهرست همهٔ instance های SQL Server دستگاه و بازگشت پورت TCP هر کدام.
        /// </summary>
        public static List<SqlBrowserInstance> Probe(string host, int port = DefaultPort, int timeoutMs = 1000)
        {
            var result = new List<SqlBrowserInstance>();
            if (!IPAddress.TryParse(host, out var ip)) return result;

            using var udp = new UdpClient();
            try
            {
                udp.Client.ReceiveTimeout = timeoutMs;
                udp.Connect(new IPEndPoint(ip, port));
                udp.Send(new byte[] { 0x03 }, 1); // CLNT_ANY_INST: درخواست همهٔ instance ها
                var endpoint = new IPEndPoint(IPAddress.Any, 0);
                var data = udp.Receive(ref endpoint);
                Parse(data, host, result);
            }
            catch { }
            return result;
        }

        public static void Parse(byte[] data, string host, List<SqlBrowserInstance> result)
        {
            if (data == null || data.Length < 3 || data[0] != 0x05) return;

            var payloadLen = data[1] | (data[2] << 8);
            var end = Math.Min(data.Length, 3 + payloadLen);
            var text = Encoding.ASCII.GetString(data, 3, Math.Max(0, end - 3));

            foreach (var record in text.Split(new[] { ";;" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = record.Split(';');
                string instance = "MSSQLSERVER", version = "", serverName = "";
                int tcpPort = -1;

                for (int i = 0; i + 1 < parts.Length; i += 2)
                {
                    var key = parts[i].Trim();
                    var value = parts[i + 1].Trim();
                    if (key.Equals("InstanceName", StringComparison.OrdinalIgnoreCase)) instance = value;
                    else if (key.Equals("Version", StringComparison.OrdinalIgnoreCase)) version = value;
                    else if (key.Equals("ServerName", StringComparison.OrdinalIgnoreCase)) serverName = value;
                    else if (key.Equals("tcp", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out var p)) tcpPort = p;
                }

                if (tcpPort > 0 && tcpPort <= 65535)
                    result.Add(new SqlBrowserInstance(host, serverName, instance, version, tcpPort));
            }
        }
    }
}