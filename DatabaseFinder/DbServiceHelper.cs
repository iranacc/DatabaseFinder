using System.Management;
using System.ServiceProcess;

namespace DatabaseFinder
{
    /// <summary>
    /// یافتن، توقف و راه‌اندازی مجدد سرویس‌های ویندوز دیتابیس
    /// برای کپی فایل‌های فیزیکی قفل‌شده (نیاز به Administrator).
    /// </summary>
    public static class DbServiceHelper
    {
        private static string[] GetExeNames(DatabaseType type)
        {
            switch (type)
            {
                case DatabaseType.SQLServer: return new[] { "sqlservr.exe" };
                case DatabaseType.MySQL:
                case DatabaseType.MariaDB: return new[] { "mysqld.exe" };
                case DatabaseType.PostgreSQL: return new[] { "postgres.exe" };
                default: return Array.Empty<string>();
            }
        }

        /// <summary>
        /// اسامی سرویس‌های ویندوز مربوط به یک نوع دیتابیس را پیدا می‌کند
        /// (بر اساس نام سرویس ثبت‌شده + مسیر اجرایی سرویس).
        /// </summary>
        public static List<string> GetDatabaseServices(DatabaseType type, DatabaseInfo? server = null)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (server != null && !string.IsNullOrEmpty(server.ServiceName))
                names.Add(server.ServiceName!);

            var exeNames = GetExeNames(type);
            if (exeNames.Length > 0)
            {
                try
                {
                    using var searcher = new ManagementObjectSearcher(
                        "SELECT Name, PathName FROM Win32_Service");
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        using (obj)
                        {
                            var path = obj["PathName"]?.ToString() ?? "";
                            foreach (var exe in exeNames)
                            {
                                if (path.IndexOf(exe, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    var name = obj["Name"]?.ToString();
                                    if (!string.IsNullOrEmpty(name))
                                        names.Add(name!);
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            return names.ToList();
        }

        public static bool StopServices(List<string> serviceNames, Action<string>? log, out string? error)
        {
            error = null;
            foreach (var name in serviceNames)
            {
                try
                {
                    using var sc = new ServiceController(name);
                    if (sc.Status == ServiceControllerStatus.Running ||
                        sc.Status == ServiceControllerStatus.StartPending)
                    {
                        log?.Invoke($"توقف سرویس «{name}» ...");
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(60));
                        log?.Invoke($"سرویس «{name}» متوقف شد.");
                    }
                }
                catch (Exception ex)
                {
                    error = $"{name}: {ex.Message}";
                    return false;
                }
            }
            return true;
        }

        public static bool StartServices(List<string> serviceNames, Action<string>? log, out string? error)
        {
            error = null;
            foreach (var name in serviceNames)
            {
                try
                {
                    using var sc = new ServiceController(name);
                    if (sc.Status == ServiceControllerStatus.Stopped ||
                        sc.Status == ServiceControllerStatus.StopPending)
                    {
                        log?.Invoke($"راه‌اندازی سرویس «{name}» ...");
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(90));
                        log?.Invoke($"سرویس «{name}» در حال اجرا است.");
                    }
                }
                catch (Exception ex)
                {
                    error = $"{name}: {ex.Message}";
                    return false;
                }
            }
            return true;
        }
    }
}