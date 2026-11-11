using System.Diagnostics;
using Microsoft.Win32;

namespace DatabaseFinder
{
    /// <summary>
    /// تبدیل فایل‌های گزارش HTML به سند رسمی PDF با استفاده از پرینتر بدون سر (Headless) Microsoft Edge یا Google Chrome.
    /// بدون نیاز به نصب هرگونه لایبرری خارجی یا پیش‌نیاز جدید روی سیستم میزبان.
    /// </summary>
    public static class PdfExporter
    {
        private static string? _cachedBrowserPath;
        private static bool _searched;

        /// <summary>
        /// بررسی آماده بودن قابلیت صدور مستقیم PDF در سیستم ویندوزی جاری.
        /// </summary>
        public static bool IsPdfSupported => !string.IsNullOrEmpty(FindChromiumExecutable());

        /// <summary>
        /// یافتن مسیر مرورگر Edge یا Chrome روی سیستم.
        /// </summary>
        public static string? FindChromiumExecutable()
        {
            if (_searched) return _cachedBrowserPath;
            _searched = true;

            var candidates = new List<string>
            {
                @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Edge\Application\msedge.exe"),
                @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\Application\chrome.exe")
            };

            // جست‌وجو از طریق رجیستری ویندوز برای آدرس‌های غیر استاندارد
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe")
                             ?? Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe");
                if (key != null)
                {
                    var val = key.GetValue("") as string;
                    if (!string.IsNullOrEmpty(val) && File.Exists(val))
                        candidates.Insert(0, val);
                }
            }
            catch { }

            foreach (var p in candidates)
            {
                if (File.Exists(p))
                {
                    _cachedBrowserPath = p;
                    return p;
                }
            }

            return null;
        }

        /// <summary>
        /// تبدیل فایل HTML به PDF استاندارد A4.
        /// </summary>
        public static bool ConvertHtmlToPdf(string htmlPath, string pdfPath, int timeoutMs = 25000)
        {
            var browser = FindChromiumExecutable();
            if (string.IsNullOrEmpty(browser)) return false;
            if (!File.Exists(htmlPath)) return false;

            try
            {
                if (File.Exists(pdfPath))
                {
                    try { File.Delete(pdfPath); } catch { }
                }

                var fullHtml = Path.GetFullPath(htmlPath);
                var fullPdf = Path.GetFullPath(pdfPath);

                var psi = new ProcessStartInfo
                {
                    FileName = browser,
                    Arguments = $"--headless --disable-gpu --no-pdf-header-footer --run-all-compositor-stages-before-draw --print-to-pdf=\"{fullPdf}\" \"{fullHtml}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using var proc = Process.Start(psi);
                if (proc == null) return false;

                if (!proc.WaitForExit(timeoutMs))
                {
                    try { proc.Kill(true); } catch { }
                    return false;
                }

                return File.Exists(fullPdf) && new FileInfo(fullPdf).Length > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
