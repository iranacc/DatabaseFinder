using System.Net;
using System.Net.NetworkInformation;

namespace DatabaseFinder
{
    /// <summary>
    /// خواندن رنج شبکهٔ محلی از کارت شبکه و تبدیل ورودی (تک IP / بازهٔ dash / CIDR) به فهرست آدرس‌ها.
    /// </summary>
    public static class NetworkRanges
    {
        public static List<string> GetLocalRanges()
        {
            var result = new List<string>();
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    IPInterfaceProperties props;
                    try { props = ni.GetIPProperties(); } catch { continue; }
                    foreach (var ua in props.UnicastAddresses)
                    {
                        var ip = ua.Address;
                        if (ip == null || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) continue;
                        var mask = ua.IPv4Mask;
                        if (mask == null) continue;
                        var ib = ip.GetAddressBytes();
                        var mb = mask.GetAddressBytes();
                        var prefix = mb.Sum(CountBits);
                        if (prefix < 2 || prefix > 30) continue;
                        var nb = new byte[4];
                        for (int i = 0; i < 4; i++) nb[i] = (byte)(ib[i] & mb[i]);
                        result.Add($"{new IPAddress(nb)}/{prefix}");
                    }
                }
            }
            catch { }
            return result.Distinct().ToList();
        }

        private static int CountBits(byte b)
        {
            int n = 0;
            while (b != 0) { n += b & 1; b >>= 1; }
            return n;
        }

        public static List<string> Expand(string input)
        {
            var hosts = new List<string>();
            if (string.IsNullOrWhiteSpace(input)) return hosts;
            var tokens = input.Split(new[] { ',', ';', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var raw in tokens)
            {
                var token = raw.Trim().Replace(" ", "");
                if (token.Length == 0) continue;
                if (IPAddress.TryParse(token, out var ip))
                {
                    hosts.Add(ip.ToString());
                    continue;
                }
                if (TryCidr(token, out var fromCidr)) { hosts.AddRange(fromCidr); continue; }
                if (TryDash(token, out var fromDash)) { hosts.AddRange(fromDash); continue; }
                return new List<string>();
            }
            return hosts.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static bool TryCidr(string token, out List<string> hosts)
        {
            hosts = new List<string>();
            var idx = token.IndexOf('/');
            if (idx <= 0 || idx == token.Length - 1) return false;
            if (!IPAddress.TryParse(token.Substring(0, idx), out var ip)) return false;
            if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return false;
            if (!int.TryParse(token.Substring(idx + 1), out int prefix)) return false;
            if (prefix < 16 || prefix > 32) return false;

            var start = ToLong(ip);
            var total = 1L << (32 - prefix);
            var from = start;
            var to = start + total - 1;
            if (prefix <= 30) { from += 1; to -= 1; } // شبکه و broadcast
            if (to > 0xFFFFFFFFL) to = 0xFFFFFFFFL;
            return Materialize(from, to, hosts);
        }

        private static bool TryDash(string token, out List<string> hosts)
        {
            hosts = new List<string>();
            var dash = token.IndexOf('-');
            if (dash <= 0 || dash == token.Length - 1) return false;
            var left = token.Substring(0, dash);
            var right = token.Substring(dash + 1);
            if (!IPAddress.TryParse(left, out var startIp)) return false;
            if (startIp.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return false;

            var from = ToLong(startIp);
            long to;
            if (right.Contains('.'))
            {
                if (!IPAddress.TryParse(right, out var endIp) || endIp.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return false;
                to = ToLong(endIp);
            }
            else
            {
                if (!int.TryParse(right, out int last) || last < 0 || last > 255) return false;
                to = (from & 0xFFFFFF00L) | (uint)last;
            }
            if (to < from) return false;
            return Materialize(from, to, hosts);
        }

        private static bool Materialize(long from, long to, List<string> hosts)
        {
            long count = to - from + 1;
            if (count <= 0 || count > 65536) return false;
            for (long v = from; v <= to; v++) hosts.Add(ToIp(v));
            return true;
        }

        private static long ToLong(IPAddress ip)
        {
            var b = ip.GetAddressBytes();
            return ((long)b[0] << 24) | ((long)b[1] << 16) | ((long)b[2] << 8) | (uint)b[3];
        }

        private static string ToIp(long v)
        {
            return $"{(v >> 24) & 0xFF}.{(v >> 16) & 0xFF}.{(v >> 8) & 0xFF}.{v & 0xFF}";
        }
    }
}