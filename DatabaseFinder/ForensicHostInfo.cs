using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace DatabaseFinder
{
    /// <summary>
    /// کارت شبکه فعال سیستم میزبان جهت شناسایی فنی و اثبات هویت در صورت‌جلسه.
    /// </summary>
    public class ForensicNetworkAdapter
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string MacAddress { get; set; } = "";
        public List<string> IpAddresses { get; set; } = new();
    }

    /// <summary>
    /// مشخصات دیجیتال و شواهد سخت‌افزاری/سیستمی سیستم میزبان مودی حین بازرسی ماده ۱۸۱.
    /// </summary>
    public class ForensicHostInfo
    {
        public string MachineName { get; set; } = Environment.MachineName;
        public string DomainName { get; set; } = Environment.UserDomainName;
        public string UserName { get; set; } = Environment.UserName;
        public string OsDescription { get; set; } = RuntimeInformation.OSDescription;
        public string OsArchitecture { get; set; } = RuntimeInformation.OSArchitecture.ToString();
        public int ProcessorCount { get; set; } = Environment.ProcessorCount;
        public bool IsAdministrator { get; set; }
        public List<ForensicNetworkAdapter> NetworkAdapters { get; set; } = new();

        /// <summary>
        /// استخراج خودکار و بدون خطر مشخصات هویتی و شبکه‌ای سیستم میزبان.
        /// </summary>
        public static ForensicHostInfo Collect()
        {
            var info = new ForensicHostInfo();

            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                info.IsAdministrator = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { }

            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up
                             && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .ToList();

                foreach (var ni in interfaces)
                {
                    var rawMac = ni.GetPhysicalAddress().ToString();
                    var formattedMac = string.IsNullOrEmpty(rawMac) ? "" :
                        string.Join(":", Enumerable.Range(0, rawMac.Length / 2)
                            .Select(i => rawMac.Substring(i * 2, 2)));

                    var ips = new List<string>();
                    try
                    {
                        var ipProps = ni.GetIPProperties();
                        foreach (var u in ipProps.UnicastAddresses)
                        {
                            if (u.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                ips.Add(u.Address.ToString());
                            }
                        }
                    }
                    catch { }

                    if (ips.Count > 0 || !string.IsNullOrEmpty(formattedMac))
                    {
                        info.NetworkAdapters.Add(new ForensicNetworkAdapter
                        {
                            Name = ni.Name,
                            Description = ni.Description,
                            MacAddress = formattedMac,
                            IpAddresses = ips
                        });
                    }
                }
            }
            catch { }

            return info;
        }
    }
}
