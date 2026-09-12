namespace DatabaseFinder
{
    /// <summary>
    /// همه ردهای برنامه (تنظیمات، پروفایل‌ها، لاگ، دانلود آپدیت) از این‌جا آدرس می‌گیرند.
    /// حالت پرتابل: اگر فایل خالی portable.txt کنار فایل اجرایی باشد، همه‌چیز داخل
    /// پوشه Data کنار exe (یعنی فلش خودتان) ذخیره می‌شود و روی سیستم هدف ردی نمی‌ماند.
    /// بدون مارکر، رفتار قبلی (%AppData%\DatabaseFinder) حفظ می‌شود.
    /// </summary>
    public static class AppPaths
    {
        private const string MarkerName = "portable.txt";
        private static string? _dataDir;

        public static string ExeDir
        {
            get
            {
                try { return AppDomain.CurrentDomain.BaseDirectory; }
                catch { return AppContext.BaseDirectory; }
            }
        }

        public static bool IsPortable
        {
            get
            {
                try { return File.Exists(Path.Combine(ExeDir, MarkerName)); }
                catch { return false; }
            }
        }

        public static string DataDir => _dataDir ??= Resolve();

        private static string Resolve()
        {
            if (IsPortable)
            {
                try
                {
                    var d = Path.Combine(ExeDir, "Data");
                    Directory.CreateDirectory(d);
                    return d;
                }
                catch { }
            }
            try
            {
                var d = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "DatabaseFinder");
                Directory.CreateDirectory(d);
                return d;
            }
            catch
            {
                return Path.GetTempPath();
            }
        }

        public static string SettingsPath => Path.Combine(DataDir, "settings.json");
        public static string ProfilesPath => Path.Combine(DataDir, "profiles.json");
        public static string LogPath => Path.Combine(DataDir, "debug.log");
        public static string DownloadPath(string fileName) =>
            Path.Combine(IsPortable ? DataDir : Path.GetTempPath(), fileName);
    }
}
