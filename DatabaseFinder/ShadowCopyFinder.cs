using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;

namespace DatabaseFinder
{
    public class ShadowCopySnapshot
    {
        public string Id { get; set; } = "";
        public string DeviceObject { get; set; } = "";
        public string VolumeName { get; set; } = "";
        public string DriveLetter { get; set; } = "";
        public DateTime CreationTime { get; set; }
        public string OriginatingMachine { get; set; } = "";

        public string DisplayName
        {
            get
            {
                var drive = !string.IsNullOrEmpty(DriveLetter) ? DriveLetter : VolumeName;
                var dateStr = CreationTime != DateTime.MinValue ? JalaliDateHelper.Format(CreationTime) : "Unknown Date";
                var idShort = Id.Length > 8 ? Id.Trim('{', '}').Substring(0, 8) : Id;
                return $"{drive} ({dateStr}) [{idShort}]";
            }
        }

        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// موتور کشف، اتصال و پویش نسخه‌های قبلی ویندوز (Volume Shadow Copies / Previous Versions).
    /// به بازرسان و ممیزان مالیاتی اجازه می‌دهد پایگاه‌های داده و بکاپ‌هایی را که در دیسک فعلی
    /// دستکاری یا حذف شده‌اند را از درون Snapshotهای گذشته استخراج و بررسی نمایند.
    /// </summary>
    public static class ShadowCopyFinder
    {
        private static readonly List<string> _activeMounts = new();
        private static readonly object _mountLock = new();

        /// <summary>
        /// دریافت لیست تمام Shadow Copyهای موجود روی سیستم (از طریق WMI یا vssadmin).
        /// </summary>
        public static List<ShadowCopySnapshot> GetAvailableShadowCopies()
        {
            var list = new List<ShadowCopySnapshot>();

            // 1. تلاش اول: پرس‌وجوی ساخت‌یافته از طریق WMI Win32_ShadowCopy
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT ID, DeviceObject, VolumeName, InstallDate, OriginatingMachine FROM Win32_ShadowCopy");
                
                foreach (ManagementObject mo in searcher.Get())
                {
                    using (mo)
                    {
                        var id = mo["ID"]?.ToString() ?? "";
                        var dev = mo["DeviceObject"]?.ToString() ?? "";
                        var vol = mo["VolumeName"]?.ToString() ?? "";
                        var machine = mo["OriginatingMachine"]?.ToString() ?? "";
                        var rawDate = mo["InstallDate"]?.ToString() ?? "";

                        var dt = ParseWmiDate(rawDate);

                        if (!string.IsNullOrEmpty(dev))
                        {
                            var drive = ResolveDriveLetter(vol);
                            list.Add(new ShadowCopySnapshot
                            {
                                Id = id,
                                DeviceObject = dev,
                                VolumeName = vol,
                                DriveLetter = drive,
                                CreationTime = dt,
                                OriginatingMachine = machine
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Write("ShadowCopyFinder.WmiQuery: " + ex.Message, ex);
            }

            // 2. تلاش دوم: اگر WMI پاسخی نداد، پارس خروجی دستور vssadmin list shadows
            if (list.Count == 0)
            {
                try
                {
                    var vssOutput = RunVssAdminList();
                    if (!string.IsNullOrWhiteSpace(vssOutput))
                    {
                        var parsed = ParseVssadminOutput(vssOutput);
                        if (parsed.Count > 0)
                            list.AddRange(parsed);
                    }
                }
                catch (Exception ex)
                {
                    AppLog.Write("ShadowCopyFinder.VssAdminFallback: " + ex.Message, ex);
                }
            }

            return list.OrderByDescending(x => x.CreationTime).ToList();
        }

        /// <summary>
        /// پارس رشته‌های خروجی دستور vssadmin list shadows در محیط ویندوز.
        /// </summary>
        public static List<ShadowCopySnapshot> ParseVssadminOutput(string text)
        {
            var results = new List<ShadowCopySnapshot>();
            if (string.IsNullOrWhiteSpace(text)) return results;

            var blocks = Regex.Split(text, @"(?=Shadow Copy ID:|\bID:\s*\{)", RegexOptions.IgnoreCase);
            foreach (var block in blocks)
            {
                if (!block.Contains("Device", StringComparison.OrdinalIgnoreCase) &&
                    !block.Contains("HarddiskVolumeShadowCopy", StringComparison.OrdinalIgnoreCase))
                    continue;

                var idMatch = Regex.Match(block, @"ID:\s*(\{[0-9a-fA-F\-]+\})", RegexOptions.IgnoreCase);
                var devMatch = Regex.Match(block, @"(?:Shadow Copy Volume|Device Name):\s*(\S+)", RegexOptions.IgnoreCase);
                var origMatch = Regex.Match(block, @"Original Volume:\s*\(?([A-Za-z]:)?\)?\s*(\S+)?", RegexOptions.IgnoreCase);
                var dateMatch = Regex.Match(block, @"Creation Time:\s*([^\r\n]+)", RegexOptions.IgnoreCase);
                var machMatch = Regex.Match(block, @"Originating Machine:\s*([^\r\n]+)", RegexOptions.IgnoreCase);

                var id = idMatch.Success ? idMatch.Groups[1].Value : Guid.NewGuid().ToString("B");
                var dev = devMatch.Success ? devMatch.Groups[1].Value.Trim() : "";
                var drive = origMatch.Success && !string.IsNullOrEmpty(origMatch.Groups[1].Value)
                    ? origMatch.Groups[1].Value.Trim()
                    : "";
                var vol = origMatch.Success && !string.IsNullOrEmpty(origMatch.Groups[2].Value)
                    ? origMatch.Groups[2].Value.Trim()
                    : "";

                var dateStr = dateMatch.Success ? dateMatch.Groups[1].Value.Trim() : "";
                DateTime.TryParse(dateStr, out var creationTime);

                var machine = machMatch.Success ? machMatch.Groups[1].Value.Trim() : "";

                if (!string.IsNullOrEmpty(dev))
                {
                    if (string.IsNullOrEmpty(drive) && !string.IsNullOrEmpty(vol))
                        drive = ResolveDriveLetter(vol);

                    results.Add(new ShadowCopySnapshot
                    {
                        Id = id,
                        DeviceObject = dev,
                        VolumeName = vol,
                        DriveLetter = drive,
                        CreationTime = creationTime,
                        OriginatingMachine = machine
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// اتصال و مونت موقت یک Snapshot به یک فولدر محلی در %TEMP% با استفاده از دایرکتوری سمبلیک‌لینک (mklink /d).
        /// </summary>
        public static string? MountSnapshot(ShadowCopySnapshot snapshot, out string? error)
        {
            error = null;
            if (snapshot == null || string.IsNullOrEmpty(snapshot.DeviceObject))
            {
                error = L.Pick("مشخصات Shadow Copy معتبر نیست.", "Invalid Shadow Copy specification.");
                return null;
            }

            var cleanId = snapshot.Id.Trim('{', '}').Replace("-", "");
            if (string.IsNullOrEmpty(cleanId)) cleanId = Guid.NewGuid().ToString("N");

            var baseTemp = Path.Combine(Path.GetTempPath(), "DatabaseFinder_VSS");
            try { Directory.CreateDirectory(baseTemp); } catch { }

            var mountDir = Path.Combine(baseTemp, $"Snap_{cleanId}");

            lock (_mountLock)
            {
                // اگر قبلاً مونت شده و وجود دارد، همان را استفاده می‌کنیم
                if (Directory.Exists(mountDir))
                {
                    if (!_activeMounts.Contains(mountDir)) _activeMounts.Add(mountDir);
                    return mountDir;
                }

                // مسیر تارگت باید با بک‌اسلش پایان یابد
                var target = snapshot.DeviceObject.TrimEnd('\\') + "\\";

                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c mklink /d \"{mountDir}\" \"{target}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using var p = Process.Start(psi);
                    if (p != null)
                    {
                        p.WaitForExit(5000);
                        var outMsg = p.StandardOutput.ReadToEnd();
                        var errMsg = p.StandardError.ReadToEnd();

                        if (Directory.Exists(mountDir))
                        {
                            _activeMounts.Add(mountDir);
                            return mountDir;
                        }

                        error = !string.IsNullOrWhiteSpace(errMsg) ? errMsg.Trim() : outMsg.Trim();
                    }
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
            }

            return null;
        }

        /// <summary>
        /// قطع اتصال و حذف پیوند سمبلیک‌لینک بدون دستکاری داده‌های اصلی.
        /// </summary>
        public static void UnmountSnapshot(string mountDir)
        {
            if (string.IsNullOrEmpty(mountDir)) return;
            lock (_mountLock)
            {
                try
                {
                    if (Directory.Exists(mountDir))
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c rmdir \"{mountDir}\"",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using var p = Process.Start(psi);
                        p?.WaitForExit(3000);
                    }
                }
                catch { }
                _activeMounts.Remove(mountDir);
            }
        }

        /// <summary>
        /// پاکسازی تمام پیوندهای سمبلیک‌لینک فعال در پایان برنامه.
        /// </summary>
        public static void UnmountAll()
        {
            lock (_mountLock)
            {
                foreach (var m in _activeMounts.ToList())
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c rmdir \"{m}\"",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using var p = Process.Start(psi);
                        p?.WaitForExit(2000);
                    }
                    catch { }
                }
                _activeMounts.Clear();

                try
                {
                    var baseTemp = Path.Combine(Path.GetTempPath(), "DatabaseFinder_VSS");
                    if (Directory.Exists(baseTemp))
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c rmdir /s /q \"{baseTemp}\"",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using var p = Process.Start(psi);
                        p?.WaitForExit(3000);
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// مقایسه و بررسی فایل کشف‌شده از Shadow Copy با وضعیت همان فایل روی دیسک فعال فعلی سیستم.
        /// </summary>
        public static void AnnotateShadowCopyResult(DatabaseInfo item, string mountDir, ShadowCopySnapshot snapshot)
        {
            if (item == null || string.IsNullOrEmpty(item.LocalPath)) return;

            item.IsFromShadowCopy = true;
            item.ShadowCopyDate = snapshot.CreationTime;
            item.ShadowCopyId = snapshot.Id;
            item.ShadowCopyDevice = snapshot.DeviceObject;

            if (!string.IsNullOrEmpty(mountDir) && item.LocalPath.StartsWith(mountDir, StringComparison.OrdinalIgnoreCase))
            {
                var relPath = item.LocalPath.Substring(mountDir.Length).TrimStart('\\');
                var liveDrive = !string.IsNullOrEmpty(snapshot.DriveLetter) ? snapshot.DriveLetter : "C:";
                if (!liveDrive.EndsWith("\\")) liveDrive += "\\";

                var livePath = Path.Combine(liveDrive, relPath);
                item.LiveDiskEquivalentPath = livePath;

                // بررسی وجود یا عدم وجود فایل در دیسک فعال فعلی سیستم
                try
                {
                    item.IsDeletedFromCurrentDisk = !File.Exists(livePath);
                }
                catch
                {
                    item.IsDeletedFromCurrentDisk = false;
                }
            }
        }

        private static string RunVssAdminList()
        {
            var psi = new ProcessStartInfo
            {
                FileName = "vssadmin.exe",
                Arguments = "list shadows",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var p = Process.Start(psi);
            if (p == null) return "";
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5000);
            return output;
        }

        private static string ResolveDriveLetter(string volumeName)
        {
            if (string.IsNullOrEmpty(volumeName)) return "";
            if (Regex.IsMatch(volumeName, @"^[A-Za-z]:")) return volumeName.Substring(0, 2).ToUpperInvariant();

            try
            {
                foreach (var d in DriveInfo.GetDrives())
                {
                    if (d.IsReady)
                    {
                        var root = d.RootDirectory.FullName;
                        // در صورت تطابق Volume GUID
                        if (volumeName.Contains(d.Name.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                            return d.Name.TrimEnd('\\');
                    }
                }
            }
            catch { }

            return "C:";
        }

        private static DateTime ParseWmiDate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return DateTime.MinValue;
            try
            {
                // CIM_DATETIME فرمت استاندارد: yyyyMMddHHmmss.ffffff+zzz
                if (raw.Length >= 14 && long.TryParse(raw.Substring(0, 14), out _))
                {
                    int y = int.Parse(raw.Substring(0, 4));
                    int m = int.Parse(raw.Substring(4, 2));
                    int d = int.Parse(raw.Substring(6, 2));
                    int h = int.Parse(raw.Substring(8, 2));
                    int min = int.Parse(raw.Substring(10, 2));
                    int s = int.Parse(raw.Substring(12, 2));
                    return new DateTime(y, m, d, h, min, s);
                }
            }
            catch { }
            return DateTime.MinValue;
        }
    }
}
