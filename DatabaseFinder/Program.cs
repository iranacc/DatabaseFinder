namespace DatabaseFinder;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        // تور ایمنی: هر خطای مهارنشده به‌جای بسته‌شدن بی‌صدا، در debug.log ثبت می‌شود.
        Application.ThreadException += (s, e) => CrashGuard("UI", e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (s, e) => CrashGuard("BG", e.ExceptionObject as Exception);
        L.Language = AppSettings.Load().Language;
        Application.Run(new AppSession());
    }

    static void CrashGuard(string where, Exception? ex)
    {
        try
        {
            AppLog.Write("Crash." + where, ex ?? new Exception("unknown"));
            MessageBox.Show("خطای غیرمنتظره ثبت شد. جزئیات در فایل debug.log (%AppData%\\DatabaseFinder) ذخیره شد.\n\n" + ex?.Message,
                "Database Finder", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch { }
    }
}

internal sealed class AppSession : ApplicationContext
{
    private readonly Action<string> _persistLanguage;
    public AppSession(MainViewState? initial = null, Action<string>? persistLanguage = null)
    {
        _persistLanguage = persistLanguage ?? (language => { var settings = AppSettings.Load(); settings.Language = language; settings.Save(); });
        MainForm = Create(initial); MainForm.Show();
    }
    private Form1 Create(MainViewState? state)
    {
        var form = new Form1(state);
        form.LanguageRequested += language =>
        {
            _persistLanguage(language);
            L.Language = language;
            var replacement = Create(form.CaptureView());
            replacement.StartPosition = FormStartPosition.Manual;
            replacement.Bounds = form.Bounds;
            replacement.WindowState = form.WindowState;
            MainForm = replacement;
            replacement.Show();
            form.CloseForLanguageChange();
        };
        return form;
    }
}
