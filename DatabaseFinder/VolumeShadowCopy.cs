using System.Management;

namespace DatabaseFinder
{
    /// <summary>
    /// ساخت و حذف Shadow Copy حجم (VSS) برای کپی فایل‌های قفل‌شده بدون توقف سرویس دیتابیس.
    /// نیاز به دسترسی Administrator دارد.
    /// </summary>
    public static class VolumeShadowCopy
    {
        /// <summary>
        /// یک Shadow Copy از ریشه حجم می‌سازد و مسیر دِوایس آن را برمی‌گرداند
        /// (مثلاً \\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy1).
        /// </summary>
        public static string? TryCreateShadow(string volumeRoot, out string? error)
        {
            error = null;
            var shadowId = string.Empty;
            try
            {
                var mc = new ManagementClass("Win32_ShadowCopy");
                var inParams = mc.GetMethodParameters("Create");
                inParams["Volume"] = NormalizeVolume(volumeRoot);

                var outParams = mc.InvokeMethod("Create", inParams, null);
                var ret = outParams?["ReturnValue"] is int rv ? rv
                    : outParams?["ReturnValue"] != null ? Convert.ToInt32(outParams["ReturnValue"])
                    : -1;

                shadowId = outParams?["ShadowID"]?.ToString() ?? "";

                if (ret == 0 && !string.IsNullOrEmpty(shadowId))
                {
                    var deviceObject = GetDeviceObject(shadowId);
                    if (!string.IsNullOrEmpty(deviceObject))
                        return deviceObject;

                    error = "Shadow Copy ساخته شد اما DeviceObject آن یافت نشد.";
                    return null;
                }

                error = ret switch
                {
                    5 => "دسترسی ناکافی؛ برنامه را با Administrator اجرا کنید.",
                    _ => $"ایجاد Shadow Copy ناموفق بود (کد {ret})."
                };
                return null;
            }
            catch (ManagementException ex)
            {
                // نسخه‌های قدیمی‌تر ارائه‌دهنده ممکن است فقط Volume را بپذیرند؛ تا آنجا
                // که در این دستگاه پارامترهای AdditionalContext/ClientAccessible وجود ندارد.
                error = "VSS: " + ex.Message;
                return null;
            }
            catch (Exception ex)
            {
                error = $"VSS: {ex.Message}";
                return null;
            }
        }

        public static void DeleteShadow(string idOrDeviceObject)
        {
            if (string.IsNullOrEmpty(idOrDeviceObject)) return;
            try
            {
                var escaped = idOrDeviceObject.Replace("'", "''");
                string where;
                if (idOrDeviceObject.StartsWith("\\", StringComparison.Ordinal))
                    where = $"DeviceObject='{escaped}'";
                else
                    where = $"ID='{escaped}'";

                using var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_ShadowCopy WHERE {where}");
                foreach (ManagementObject mo in searcher.Get())
                {
                    using (mo) mo.Delete();
                }
            }
            catch { }
        }

        private static string? GetDeviceObject(string id)
        {
            try
            {
                var escaped = id.Replace("'", "''");
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT DeviceObject FROM Win32_ShadowCopy WHERE ID='{escaped}'");
                foreach (ManagementObject obj in searcher.Get())
                {
                    using (obj)
                    {
                        var device = obj["DeviceObject"]?.ToString();
                        if (!string.IsNullOrEmpty(device)) return device;
                    }
                }
            }
            catch { }
            return null;
        }

        private static string NormalizeVolume(string volumeRoot)
        {
            var v = (volumeRoot ?? "").Trim();
            if (string.IsNullOrEmpty(v)) return v;
            if (v[0] == '\\') return v;
            if (v.EndsWith("\\", StringComparison.Ordinal)) return v;
            return v + "\\";
        }

        /// <summary>
        /// مسیر فایل درون Shadow Copy را برمی‌گرداند: deviceObject + مسیر نسبی.
        /// </summary>
        public static string MapToShadow(string deviceObject, string sourcePath, string volumeRoot)
        {
            var rel = sourcePath.Substring(volumeRoot.Length).TrimStart('\\');
            return deviceObject.TrimEnd('\\') + "\\" + rel;
        }
    }
}