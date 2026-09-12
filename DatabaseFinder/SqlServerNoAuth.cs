using System.Diagnostics;
using System.Management;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace DatabaseFinder
{
    /// <summary>
    /// مسیرهای داده SQL Server بدون نیاز به لاگین SQL:
    /// رجیستری + پارامترهای سرویس + پوشه‌های پیش‌فرض.
    /// </summary>
    public static class SqlServerPathResolver
    {
        public static List<string> GetDataDirectories(DatabaseInfo? server = null)
        {
            var dirs = new List<string>();

            try
            {
                using var instances = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL");
                if (instances != null)
                {
                    foreach (var instanceName in instances.GetValueNames())
                    {
                        var internalName = instances.GetValue(instanceName)?.ToString();
                        if (string.IsNullOrEmpty(internalName)) continue;
                        if (server?.ServiceName != null && !MatchesInstance(server.ServiceName, instanceName)) continue;

                        using var setup = Registry.LocalMachine.OpenSubKey(
                            $@"SOFTWARE\Microsoft\Microsoft SQL Server\{internalName}\Setup");
                        foreach (var v in new[] { "SQLPath", "SQLDataRoot" })
                        {
                            var p = setup?.GetValue(v)?.ToString();
                            if (!string.IsNullOrEmpty(p))
                            {
                                var data = Path.Combine(p, "DATA");
                                if (Directory.Exists(data)) dirs.Add(data);
                                else if (Directory.Exists(p)) dirs.Add(p);
                            }
                        }

                        using var param = Registry.LocalMachine.OpenSubKey(
                            $@"SOFTWARE\Microsoft\Microsoft SQL Server\{internalName}\MSSQLServer\Parameters");
                        if (param != null)
                        {
                            foreach (var vn in param.GetValueNames())
                            {
                                var arg = param.GetValue(vn)?.ToString() ?? "";
                                var m = Regex.Match(arg, @"-d(.+\.mdf)", RegexOptions.IgnoreCase);
                                if (m.Success)
                                {
                                    var dir = Path.GetDirectoryName(m.Groups[1].Value.Trim());
                                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir)) dirs.Add(dir);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { AppLog.Write("SqlPath.Registry", ex); }

            // پارامتر -d از PathName سرویس (sqlservr.exe -s INST)
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name, PathName FROM Win32_Service");
                foreach (ManagementObject obj in searcher.Get())
                {
                    using (obj)
                    {
                        var path = obj["PathName"]?.ToString() ?? "";
                        if (path.IndexOf("sqlservr.exe", StringComparison.OrdinalIgnoreCase) < 0) continue;
                        var name = obj["Name"]?.ToString() ?? "";
                        if (server?.ServiceName != null && !name.Equals(server.ServiceName, StringComparison.OrdinalIgnoreCase)) continue;
                        var m = Regex.Match(path, @"-d\s*""?([^"";]+\.mdf)""?", RegexOptions.IgnoreCase);
                        if (m.Success)
                        {
                            var dir = Path.GetDirectoryName(m.Groups[1].Value.Trim());
                            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir)) dirs.Add(dir);
                        }
                    }
                }
            }
            catch (Exception ex) { AppLog.Write("SqlPath.Service", ex); }

            // پوشه‌های پیش‌فرض
            try
            {
                foreach (var pf in new[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
                })
                {
                    var root = Path.Combine(pf, "Microsoft SQL Server");
                    if (!Directory.Exists(root)) continue;
                    foreach (var data in Directory.EnumerateDirectories(root, "DATA", SearchOption.AllDirectories).Take(10))
                        dirs.Add(data);
                }
            }
            catch (Exception ex) { AppLog.Write("SqlPath.Defaults", ex); }

            return dirs.Distinct(StringComparer.OrdinalIgnoreCase).Where(Directory.Exists).ToList();
        }

        private static bool MatchesInstance(string serviceName, string instanceName)
        {
            if (serviceName.Equals("MSSQLSERVER", StringComparison.OrdinalIgnoreCase) &&
                instanceName.Equals("MSSQLSERVER", StringComparison.OrdinalIgnoreCase)) return true;
            return serviceName.IndexOf(instanceName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   instanceName.IndexOf(serviceName, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>فایل‌های mdf/ldf/ndf داخل پوشه‌های داده (فقط سطح اول، سریع).</summary>
        public static List<string> EnumerateDataFiles(List<string> dataDirs)
        {
            var files = new List<string>();
            foreach (var dir in dataDirs)
            {
                try
                {
                    foreach (var ext in new[] { "*.mdf", "*.ldf", "*.ndf" })
                        files.AddRange(Directory.EnumerateFiles(dir, ext, SearchOption.TopDirectoryOnly));
                }
                catch { }
            }
            return files.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
    }

    public class FoundCredential
    {
        public string FilePath { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Line { get; set; } = "";
    }

    /// <summary>
    /// جستجوی دستی و محدود پسورد ذخیره‌شده در کانفیگ نرم‌افزارهای مالی.
    /// عمدا اتوماتیک اجرا نمی‌شود (کند و پرنویز است)؛ فقط با دکمه دستی.
    /// </summary>
    public static class SavedPasswordFinder
    {
        private static readonly string[] VendorHints =
            { "mahak", "holoo", "parsian", "novin", "sepidar", "hamkaran", "hesabdari", "accounting" };

        private static readonly string[] Extensions =
            { ".config", ".ini", ".xml", ".json", ".txt", ".conf", ".cfg" };

        public static async Task<List<FoundCredential>> FindAsync(Action<string>? log, CancellationToken cancel, int timeoutSeconds = 60)
        {
            return await Task.Run(() =>
            {
                var found = new List<FoundCredential>();
                var roots = BuildRoots();
                log?.Invoke(L.Format("S350", roots.Count));
                var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
                var scanned = 0;

                foreach (var root in roots)
                {
                    if (cancel.IsCancellationRequested || DateTime.UtcNow > deadline) break;
                    if (!Directory.Exists(root)) continue;
                    try
                    {
                        foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories).Take(1500))
                        {
                            if (cancel.IsCancellationRequested || DateTime.UtcNow > deadline) break;
                            var ext = Path.GetExtension(file).ToLowerInvariant();
                            if (Array.IndexOf(Extensions, ext) < 0) continue;
                            try
                            {
                                var fi = new FileInfo(file);
                                if (fi.Length == 0 || fi.Length > 512 * 1024) continue;
                                scanned++;
                                var text = File.ReadAllText(file);
                                var m = Regex.Match(text,
                                    @"(?i)(password|pwd)\s*=\s*[""']?([^""';\r\n]{1,128})");
                                if (m.Success && LooksLikeSa(text, m.Groups[2].Value))
                                {
                                    found.Add(new FoundCredential
                                    {
                                        FilePath = file,
                                        Username = ExtractUser(text),
                                        Password = m.Groups[2].Value.Trim(),
                                        Line = m.Value.Length > 120 ? m.Value.Substring(0, 120) : m.Value
                                    });
                                    log?.Invoke(L.Format("S351", file));
                                    if (found.Count >= 10) return found;
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }

                log?.Invoke(L.Format("S352", scanned, found.Count));
                return found;
            }, cancel);
        }

        private static bool LooksLikeSa(string text, string pwd)
        {
            if (string.IsNullOrWhiteSpace(pwd)) return false;
            pwd = pwd.Trim();
            if (pwd.Equals("password", StringComparison.OrdinalIgnoreCase)) return false;
            if (pwd.StartsWith("*")) return false;
            // اگر کانکشن‌استیرینگ اشاره به sa یا SqlServer دارد، محتمل است
            return text.IndexOf("sa", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("sql", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("server=", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("data source", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string ExtractUser(string text)
        {
            var m = Regex.Match(text, @"(?i)(user\s*id|uid|user)\s*=\s*[""']?([^""';\r\n]{1,64})");
            return m.Success ? m.Groups[2].Value.Trim() : "sa?";
        }

        private static List<string> BuildRoots()
        {
            var roots = new List<string>();
            var bases = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            };

            foreach (var b in bases.Distinct())
            {
                if (!Directory.Exists(b)) continue;
                // کل پوشه را اسکن نمی‌کنیم؛ فقط زیرپوشه‌های مشکوک به مالی
                string[] subs = Array.Empty<string>();
                try { subs = Directory.GetDirectories(b); } catch { }
                foreach (var s in subs)
                {
                    var name = Path.GetFileName(s).ToLowerInvariant();
                    if (VendorHints.Any(h => name.Contains(h))) roots.Add(s);
                }
            }

            return roots.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
    }

    /// <summary>
    /// اقدام تهاجمی و دستی: اعطای sysadmin به کاربر ویندوزی فعلی
    /// با ری‌استارت تک‌کاربره. فقط با تایید صریح، فقط لوکال، فقط ادمین.
    /// هر مرحله برای صورتجلسه ۱۸۱ لاگ می‌شود.
    /// </summary>
    public static class SysadminGranter
    {
        public static bool IsAdministrator()
        {
            try
            {
                using var id = WindowsIdentity.GetCurrent();
                return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        public static string? GrantForInstance(string? serviceName, Action<string> log)
        {
            try
            {
                if (!IsAdministrator()) return L.Text("S310");

                var svc = string.IsNullOrEmpty(serviceName) ? "MSSQLSERVER" : serviceName;
                var instance = svc.Equals("MSSQLSERVER", StringComparison.OrdinalIgnoreCase)
                    ? "MSSQLSERVER" : svc.Replace("MSSQL$", "", StringComparison.OrdinalIgnoreCase);
                var sqlInstance = svc.Equals("MSSQLSERVER", StringComparison.OrdinalIgnoreCase)
                    ? "." : $@".\{instance}";
                var user = WindowsIdentity.GetCurrent().Name; // DOMAIN\User

                log(L.Format("S354", svc, user));
                AppLog.Write("SysadminGrant.Start", new Exception($"{svc} for {user}"));

                if (!RunNet($"stop \"{svc}\"", log)) return L.Format("S132", svc);
                // استارت تک‌کاربره
                if (!RunNet($"start \"{svc}\" /mSQLCMD", log))
                {
                    RunNet($"start \"{svc}\"", log);
                    return L.Format("S132", svc + " /mSQLCMD");
                }

                try
                {
                    var sqlcmd = FindSqlCmd();
                    if (sqlcmd == null) return L.Text("S355");
                    var sql = $"IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'{user.Replace("'", "''")}') " +
                              $"CREATE LOGIN [{user}] FROM WINDOWS; " +
                              $"ALTER SERVER ROLE sysadmin ADD MEMBER [{user}];";
                    if (!RunProcess(sqlcmd, $"-E -S \"{sqlInstance}\" -Q \"{sql}\"", log))
                        return L.Text("S331");
                }
                finally
                {
                    RunNet($"stop \"{svc}\"", log);
                    RunNet($"start \"{svc}\"", log);
                }

                log(L.Text("S356"));
                return null;
            }
            catch (Exception ex)
            {
                AppLog.Write("SysadminGrant", ex);
                return ex.Message;
            }
        }

        private static string? FindSqlCmd()
        {
            try
            {
                foreach (var p in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
                {
                    var c = Path.Combine(p.Trim(), "sqlcmd.exe");
                    if (File.Exists(c)) return c;
                }
                foreach (var pf in new[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
                })
                {
                    var root = Path.Combine(pf, "Microsoft SQL Server");
                    if (!Directory.Exists(root)) continue;
                    var hit = Directory.EnumerateFiles(root, "sqlcmd.exe", SearchOption.AllDirectories).FirstOrDefault();
                    if (hit != null) return hit;
                }
            }
            catch (Exception ex) { AppLog.Write("SysadminGrant.FindSqlCmd", ex); }
            return null;
        }

        private static bool RunNet(string args, Action<string> log)
        {
            log("net " + args);
            return RunProcess("net", args, log);
        }

        private static bool RunProcess(string exe, string args, Action<string> log)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var p = Process.Start(psi);
                if (p == null) return false;
                var stdout = p.StandardOutput.ReadToEnd();
                var stderr = p.StandardError.ReadToEnd();
                p.WaitForExit(120000);
                if (!string.IsNullOrWhiteSpace(stdout)) log(stdout.Trim());
                if (!string.IsNullOrWhiteSpace(stderr)) log(stderr.Trim());
                return p.ExitCode == 0;
            }
            catch (Exception ex)
            {
                log(ex.Message);
                return false;
            }
        }
    }
}
