using System.Diagnostics;

namespace DatabaseFinder
{
    /// <summary>
    /// ثبت سبک خطاها برای دیباگ بدون تغییر رفتار برنامه.
    /// خروجی: پنجرهٔ تخته‌نویس و فایل %AppData%\DatabaseFinder\debug.log (ضمیمه‌شونده).
    /// تنها پیام‌های فنی ثبت می‌شوند؛ با مانیفست پرونده ارتباطی ندارد.
    /// </summary>
    public static class AppLog
    {
        private static readonly object Sync = new();
        private static string FilePath => AppPaths.LogPath;

        public static void Write(string category, Exception ex)
        {
            try
            {
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {category}: {ex}";
                Debug.WriteLine(line);
                lock (Sync)
                {
                    var dir = Path.GetDirectoryName(FilePath);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    File.AppendAllText(FilePath, line + Environment.NewLine);
                }
            }
            catch
            {
                // هرگز نباید لاگ‌گیری خودش خطا ایجاد کند
            }
        }
    }
}