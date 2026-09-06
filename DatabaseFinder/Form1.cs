using System.Text;

namespace DatabaseFinder
{
    public partial class Form1 : Form
    {
        private readonly DatabaseDetector _detector;
        private AppSettings _settings;
        private System.Windows.Forms.Timer? _refreshTimer;
        private bool _isClosing = false;
        private bool _exitRequested = false;
        private List<DatabaseInfo> _lastResults = new();

        public Form1()
        {
            _settings = AppSettings.Load();
            _detector = new DatabaseDetector(_settings);
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DetectDatabases();

            if (_settings.AutoRefresh)
            {
                _refreshTimer = new System.Windows.Forms.Timer();
                _refreshTimer.Interval = _settings.AutoRefreshIntervalSec * 1000;
                _refreshTimer.Tick += (s, ev) => DetectDatabases(showStatus: false);
                _refreshTimer.Start();
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (_settings != null && _settings.MinimizeToTray && this.WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon.Visible = true;
                if (_settings.ShowNotifications)
                {
                    notifyIcon.ShowBalloonTip(1500, "Database Finder", "برنامه در سینی سیستم فعال است.", ToolTipIcon.Info);
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_settings.MinimizeToTray && !_exitRequested && !_isClosing)
            {
                e.Cancel = true;
                Hide();
                notifyIcon.Visible = true;
                return;
            }
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            notifyIcon.Visible = false;
        }

        private void miShow_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            notifyIcon.Visible = false;
        }

        private void miRefresh_Click(object sender, EventArgs e)
        {
            DetectDatabases();
        }

        private void miExit_Click(object sender, EventArgs e)
        {
            _exitRequested = true;
            notifyIcon.Visible = false;
            Close();
            Application.Exit();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            DetectDatabases();
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            var form = new SettingsForm(_settings);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                // بازیابی تنظیمات بعد از تغییرات
                _settings = AppSettings.Load();
                _detector.ReloadSettings(_settings);
                DetectDatabases();
            }
        }

        private void btnProfiles_Click(object sender, EventArgs e)
        {
            var form = new ProfilesForm();
            form.ShowDialog(this);
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            if (dgvDatabases.SelectedRows.Count == 0)
            {
                lblStatus.Text = "یک دیتابیس را از لیست انتخاب کنید.";
                return;
            }
            ShowDetailForSelected();
        }

        private void btnQuery_Click(object sender, EventArgs e)
        {
            if (dgvDatabases.SelectedRows.Count == 0)
            {
                lblStatus.Text = "یک دیتابیس را از لیست انتخاب کنید.";
                return;
            }

            var db = GetSelectedDatabase();
            if (db.Type == DatabaseType.Unknown)
            {
                lblStatus.Text = "اجرای کوئری برای این دیتابیس پشتیبانی نمی‌شود.";
                return;
            }

            var qf = new QueryRunnerForm(db);
            qf.ShowDialog(this);
        }

        private void btnRemote_Click(object sender, EventArgs e)
        {
            var form = new RemoteScannerForm();
            form.ShowDialog(this);
        }

        private void btnCopyFiles_Click(object sender, EventArgs e)
        {
            var selected = new List<int>();
            for (int i = 0; i < dgvDatabases.Rows.Count; i++)
            {
                var row = dgvDatabases.Rows[i];
                if (row.Cells["colCheck"].Value is bool b && b)
                {
                    selected.Add(i);
                }
            }

            if (selected.Count == 0)
            {
                lblStatus.Text = "ردیف‌هایی که می‌خواهید را تیک بزنید.";
                return;
            }

            var dbs = selected
                .Select(index => index < _lastResults.Count ? _lastResults[index] : GetDatabaseFromRow(dgvDatabases.Rows[index]))
                .Where(d => d != null)
                .Cast<DatabaseInfo>()
                .ToList();

            if (dbs.Count == 0)
            {
                lblStatus.Text = "دیتابیس قابل کپی یافت نشد.";
                return;
            }

            var form = new DatabaseCopyForm(dbs);
            form.ShowDialog(this);
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            var sb = new StringBuilder();
            foreach (DataGridViewRow row in dgvDatabases.Rows)
            {
                sb.AppendLine($"{row.Cells["colType"].Value} | پورت: {row.Cells["colPort"].Value} | سرویس: {row.Cells["colService"].Value} | پروسس: {row.Cells["colProcess"].Value}");
            }

            if (sb.Length > 0)
            {
                Clipboard.SetText(sb.ToString());
                lblStatus.Text = "لیست کپی شد!";
            }
        }

        private void dgvDatabases_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                ShowDetailForSelected();
            }
        }

        private void ShowDetailForSelected()
        {
            if (dgvDatabases.SelectedRows.Count == 0) return;
            var db = GetSelectedDatabase();
            var form = new DatabaseDetailForm(db);
            form.ShowDialog(this);
        }

        private DatabaseInfo GetSelectedDatabase()
        {
            var row = dgvDatabases.SelectedRows[0];
            return GetDatabaseFromRow(row);
        }

        private static DatabaseInfo GetDatabaseFromRow(DataGridViewRow row)
        {
            return new DatabaseInfo
            {
                Type = ParseType(row.Cells["colType"].Value?.ToString() ?? ""),
                Name = row.Cells["colType"].Value?.ToString() ?? "",
                Port = ParsePort(row.Cells["colPort"].Value?.ToString()),
                ServiceName = row.Cells["colService"].Value?.ToString(),
                ProcessName = GetProcessFromDisplay(row.Cells["colProcess"].Value?.ToString()),
                ProcessId = GetPidFromDisplay(row.Cells["colProcess"].Value?.ToString()),
                Version = row.Cells["colVersion"].Value?.ToString() ?? ""
            };
        }

        private static DatabaseType ParseType(string displayName)
        {
            switch (displayName)
            {
                case "SQL Server": return DatabaseType.SQLServer;
                case "MySQL": return DatabaseType.MySQL;
                case "MariaDB": return DatabaseType.MariaDB;
                case "PostgreSQL": return DatabaseType.PostgreSQL;
                case "Oracle": return DatabaseType.Oracle;
                case "MongoDB": return DatabaseType.MongoDB;
                case "Redis": return DatabaseType.Redis;
                case "Elasticsearch": return DatabaseType.Elasticsearch;
                case "CouchDB": return DatabaseType.CouchDB;
                default: return DatabaseType.Unknown;
            }
        }

        private static int? ParsePort(string? portStr)
        {
            if (string.IsNullOrEmpty(portStr) || portStr == "-") return null;
            return int.TryParse(portStr, out int p) ? p : null;
        }

        private static string? GetProcessFromDisplay(string? display)
        {
            if (string.IsNullOrEmpty(display) || display == "-") return null;
            var idx = display.IndexOf(" (PID:", StringComparison.Ordinal);
            return idx > 0 ? display.Substring(0, idx) : display;
        }

        private static int GetPidFromDisplay(string? display)
        {
            if (string.IsNullOrEmpty(display)) return 0;
            var idx = display.IndexOf("PID:", StringComparison.Ordinal);
            if (idx >= 0)
            {
                int.TryParse(display.Substring(idx + 4).TrimEnd(')'), out int pid);
                return pid;
            }
            return 0;
        }

        private static DatabaseDisplayModel BuildModel(DatabaseInfo db)
        {
            var how = new List<string>();
            if (db.IsRunningAsService) how.Add("سرویس");
            if (db.IsRunningAsProcess) how.Add("پروسس");
            if (db.Port.HasValue) how.Add("پورت");

            return new DatabaseDisplayModel
            {
                TypeDisplayName = db.TypeDisplayName,
                Port = db.Port?.ToString() ?? "-",
                ServiceName = db.ServiceName ?? (db.IsRunningAsService ? db.Name : "-"),
                ProcessDisplay = db.ProcessId > 0 ? $"{db.ProcessName} (PID: {db.ProcessId})" : "-",
                DetectionMethod = string.Join(" + ", how),
                Version = db.Version,
                HostAddress = db.Host
            };
        }

        private void DetectDatabases(bool showStatus = true)
        {
            if (showStatus)
            {
                lblStatus.Text = "در حال جستجو...";
                lblStatus.Refresh();
            }

            try
            {
                var results = _detector.Detect();
                _lastResults = results.ToList();
                var models = results.Select(BuildModel).ToList();

                // تست سریع اتصال برای دریافت نسخه
                foreach (var db in results.Where(d => d.Port.HasValue))
                {
                    var testResult = DatabaseTester.TestConnection(db);
                    if (testResult.Success && testResult.Version.Length > 0)
                    {
                        var model = models.FirstOrDefault(m => m.TypeDisplayName == db.TypeDisplayName && m.Port == db.Port?.ToString());
                        if (model != null) model.Version = testResult.Version;
                    }
                }

                dgvDatabases.DataSource = models;
                lblStatus.Text = $"تعداد دیتابیس‌های یافت شده: {results.Count}";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "خطا در تشخیص دیتابیس‌ها";
                MessageBox.Show($"خطا:\n{ex.Message}\n\nممکنه نیاز به اجرای برنامه با دسترسی Administrator باشه.",
                    "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
