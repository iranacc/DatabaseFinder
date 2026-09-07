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
            switch (cmbScanMode.SelectedIndex)
            {
                case 1:
                    RunOfflineScan();
                    break;
                case 2:
                    DetectDatabases();
                    RunOfflineScan();
                    break;
                default:
                    DetectDatabases();
                    break;
            }
        }

        private void RunOfflineScan()
        {
            var form = new DiskScanForm();
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                var offline = form.Found.ToList();
                if (offline.Count > 0)
                {
                    MergeOfflineResults(offline);
                    lblStatus.Text = $"تعداد دیتابیس‌های آفلاین افزوده‌شده: {offline.Count}";
                }
                else
                {
                    lblStatus.Text = "دیتابیس آفلاینی یافت نشد.";
                }
            }
        }

        private void MergeOfflineResults(List<DatabaseInfo> offline)
        {
            var knownPaths = new HashSet<string>(
                _lastResults.Where(d => !string.IsNullOrEmpty(d.LocalPath)).Select(d => d.LocalPath!),
                StringComparer.OrdinalIgnoreCase);

            foreach (var di in offline)
            {
                if (di.LocalPath != null && knownPaths.Add(di.LocalPath))
                {
                    _lastResults.Add(di);
                }
            }

            var models = _lastResults.Select(BuildModel).ToList();
            RebindGrid(models);
        }

        private void RebindGrid(List<DatabaseDisplayModel> models)
        {
            dgvDatabases.DataSource = null;
            dgvDatabases.DataSource = models;

            for (int i = 0; i < dgvDatabases.Rows.Count && i < _lastResults.Count; i++)
            {
                var db = _lastResults[i];
                var row = dgvDatabases.Rows[i];
                row.Tag = db;
                if (!db.IsOnline)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 244, 222);
                    row.Cells["colCheck"].Style.BackColor = Color.FromArgb(255, 244, 222);
                    row.Cells["colCheck"].Style.SelectionBackColor = Color.FromArgb(255, 224, 178);
                    if (db.IsBackup) row.DefaultCellStyle.ForeColor = Color.FromArgb(27, 94, 32);
                }
            }
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
            var dbs = GetCheckedDatabases();
            if (dbs == null)
            {
                lblStatus.Text = "ردیف‌هایی که می‌خواهید را تیک بزنید.";
                return;
            }

            if (dbs.Count == 0)
            {
                lblStatus.Text = "دیتابیس قابل کپی یافت نشد.";
                return;
            }

            var form = new DatabaseCopyForm(dbs);
            form.ShowDialog(this);
        }

        private void btnBackup_Click(object sender, EventArgs e)
        {
            var dbs = GetCheckedDatabases();
            if (dbs == null)
            {
                lblStatus.Text = "ردیف‌هایی که می‌خواهید را تیک بزنید.";
                return;
            }

            if (dbs.Count == 0)
            {
                lblStatus.Text = "دیتابیس قابل بکاپ یافت نشد.";
                return;
            }

            var form = new DatabaseBackupForm(dbs);
            form.ShowDialog(this);
        }

        private List<DatabaseInfo>? GetCheckedDatabases()
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

            if (selected.Count == 0) return null;

            return selected
                .Select(index => (DatabaseInfo?)(dgvDatabases.Rows[index].Tag as DatabaseInfo)
                    ?? (index < _lastResults.Count ? _lastResults[index] : GetDatabaseFromRow(dgvDatabases.Rows[index])))
                .Where(d => d != null)
                .Cast<DatabaseInfo>()
                .ToList();
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
            if (db.IsOnline)
            {
                if (db.IsRunningAsService) how.Add("سرویس");
                if (db.IsRunningAsProcess) how.Add("پروسس");
                if (db.Port.HasValue) how.Add("پورت");
            }

            var isDisplayName = db.IsOnline ? db.TypeDisplayName : db.Name;
            var displayName = isDisplayName;
            if (!db.IsOnline && !string.IsNullOrEmpty(db.FormatName) && !string.Equals(db.FormatName, db.TypeDisplayName, StringComparison.Ordinal))
            {
                displayName = $"{db.Name} [{db.FormatName}]";
            }

            return new DatabaseDisplayModel
            {
                TypeDisplayName = db.TypeDisplayName,
                DisplayName = displayName,
                IsOnline = db.IsOnline,
                IsBackup = db.IsBackup,
                Port = db.Port?.ToString() ?? "-",
                ServiceName = db.ServiceName ?? (db.IsRunningAsService ? db.Name : "-"),
                ProcessDisplay = db.ProcessId > 0 ? $"{db.ProcessName} (PID: {db.ProcessId})" : "-",
                DetectionMethod = db.IsOnline
                    ? string.Join(" + ", how)
                    : (db.IsBackup ? "آفلاین - بکاپ" : "آفلاین - فایل"),
                Version = db.Version,
                HostAddress = db.Host,
                Location = db.IsOnline
                    ? (db.Host + ":" + db.Port)
                    : (db.LocalPath ?? "-"),
                SizeInfo = db.IsOnline
                    ? ""
                    : $"{FormatFileSize(db.FileSize)} - {db.FileModified:yyyy-MM-dd HH:mm}"
            };
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes <= 0) return "-";
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            var unit = 0;
            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }
            return $"{size:0.#} {units[unit]}";
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

                RebindGrid(models);
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
