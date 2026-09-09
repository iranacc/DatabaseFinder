using System.Text;

namespace DatabaseFinder
{
    public partial class Form1 : AppForm
    {
        private readonly DatabaseDetector _detector;
        private AppSettings _settings;
        private System.Windows.Forms.Timer? _refreshTimer;
        private List<DatabaseInfo> _lastResults = new();

        public Form1(MainViewState? restored = null)
        {
            _settings = AppSettings.Load();
            _detector = new DatabaseDetector(_settings);
            InitializeComponent();
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            _restored = restored;
            BuildModernShell();
            FormClosed += (_, _) => { _refreshTimer?.Dispose(); notifyIcon.Dispose(); };
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            if (_restored == null) await DetectDatabasesAsync();
            else RestoreView(_restored);

            if (_settings.AutoRefresh)
            {
                _refreshTimer = new System.Windows.Forms.Timer();
                _refreshTimer.Interval = _settings.AutoRefreshIntervalSec * 1000;
                _refreshTimer.Tick += async (s, ev) => await DetectDatabasesAsync(showStatus: false);
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
                    notifyIcon.ShowBalloonTip(1500, "Database Finder", L.Text("S215"), ToolTipIcon.Info);
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
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

        private async void miRefresh_Click(object sender, EventArgs e)
        {
            await DetectDatabasesAsync();
        }

        private void miExit_Click(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            Close();
            Application.Exit();
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            switch (cmbScanMode.SelectedIndex)
            {
                case 1:
                    RunOfflineScan();
                    break;
                case 2:
                    await DetectDatabasesAsync();
                    RunOfflineScan();
                    break;
                default:
                    await DetectDatabasesAsync();
                    break;
            }
        }

        private void cmbScanMode_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbScanMode.SelectedIndex == 1)
            {
                RunOfflineScan();
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
                    lblStatus.Text = L.Format("S216", offline.Count);
                }
                else
                {
                    lblStatus.Text = L.Text("S217");
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
                row.Cells["colDbCount"].Value = db.IsOnline && db.DatabaseCount is int cnt ? cnt.ToString() : "-";

                if (!db.IsOnline)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 244, 222);
                    row.Cells["colCheck"].Style.BackColor = Color.FromArgb(255, 244, 222);
                    row.Cells["colCheck"].Style.SelectionBackColor = Color.FromArgb(255, 224, 178);
                    if (db.IsBackup) row.DefaultCellStyle.ForeColor = Color.FromArgb(27, 94, 32);
                }
            }
            UpdateDetails();
        }

        private async void btnSettings_Click(object sender, EventArgs e)
        {
            var form = new SettingsForm(_settings);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                // بازیابی تنظیمات بعد از تغییرات
                _settings = AppSettings.Load();
                _detector.ReloadSettings(_settings);
                await DetectDatabasesAsync();
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
                lblStatus.Text = L.Text("S218");
                return;
            }
            ShowDetailForSelected();
        }

        private void btnQuery_Click(object sender, EventArgs e)
        {
            if (dgvDatabases.SelectedRows.Count == 0)
            {
                lblStatus.Text = L.Text("S218");
                return;
            }

            var db = GetSelectedDatabase();
            if (db.Type == DatabaseType.Unknown)
            {
                lblStatus.Text = L.Text("S219");
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
                lblStatus.Text = L.Text("S220");
                return;
            }

            if (dbs.Count == 0)
            {
                lblStatus.Text = L.Text("S221");
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
                lblStatus.Text = L.Text("S220");
                return;
            }

            if (dbs.Count == 0)
            {
                lblStatus.Text = L.Text("S222");
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
                sb.AppendLine(L.Format("S223", row.Cells["colType"].Value, row.Cells["colPort"].Value, row.Cells["colService"].Value, row.Cells["colProcess"].Value));
            }

            if (sb.Length > 0)
            {
                Clipboard.SetText(sb.ToString());
                lblStatus.Text = L.Text("S224");
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
            if (row.Tag is DatabaseInfo original) return original;
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
                if (db.IsRunningAsService) how.Add(L.Text("S101"));
                if (db.IsRunningAsProcess) how.Add(L.Text("S102"));
                if (db.Port.HasValue) how.Add(L.Text("S103"));
            }

            var isDisplayName = db.IsOnline ? db.TypeDisplayName : db.Name;
            var displayName = isDisplayName;
            if (!db.IsOnline && !string.IsNullOrEmpty(db.FormatName) && !string.Equals(db.FormatName, db.TypeDisplayName, StringComparison.Ordinal))
            {
                displayName = $"{db.Name} [{L.DisplayFormat(db.FormatName)}]";
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
                    : (db.IsBackup ? L.Text("S225") : L.Text("S226")),
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

        private bool _detectBusy = false;

        private async Task DetectDatabasesAsync(bool showStatus = true)
        {
            if (_detectBusy || IsDisposed || !IsHandleCreated) return;
            _detectBusy = true;
            try
            {
                if (showStatus)
                {
                    lblStatus.Text = L.Text("S227");
                    lblStatus.Refresh();
                }

                var results = _detector.Detect();
                _lastResults = results.ToList();
                var models = results.Select(BuildModel).ToList();

                RebindGrid(models);
                lblStatus.Text = L.Format("S228", results.Count);

                // تست اتصال در پس‌زمینه برای دریافت نسخه؛ رابط را مسدود نمی‌کند.
                // مهلت سراسری: حتی اگر سرویسی پاسخ بنر ندهد، تشخیص هرگز به‌طور نامحدود معلق نمی‌ماند.
                var withPorts = results.Where(d => d.Port.HasValue).ToList();
                if (withPorts.Count == 0) return;

                // شمارش واقعی دیتابیس‌های هر سرویس با اتصال (پروفایل یا احراز هویت ویندوز).
                // قبل از تست نسخه اجرا می‌شود تا تعداد بلافاصله بعد از بارگذاری جدول نمایان شود.
                try
                {
                    await Task.Run(() => DatabaseCounter.Measure(withPorts));
                    if (IsDisposed || !IsHandleCreated) return;
                    for (int i = 0; i < withPorts.Count; i++)
                    {
                        var db = withPorts[i];
                        if (db.DatabaseCount is int cnt)
                        {
                            var idx = results.IndexOf(db);
                            if (idx >= 0 && idx < dgvDatabases.Rows.Count)
                                dgvDatabases.Rows[idx].Cells["colDbCount"].Value = cnt.ToString();
                        }
                    }
                }
                catch
                {
                    // شمارش یک قابلیت اختیاری است؛ خطا در آن نباید جریان تشخیص را متوقف کند.
                }

                try
                {
                    var versions = await Task.WhenAll(withPorts.Select(DatabaseTester.TestConnectionAsync))
                        .WaitAsync(TimeSpan.FromSeconds(8));

                    if (IsDisposed || !IsHandleCreated) return;
                    for (int i = 0; i < withPorts.Count; i++)
                    {
                        var testResult = versions[i];
                        if (!testResult.Success || testResult.Version.Length == 0) continue;

                        var db = withPorts[i];
                        db.Version = testResult.Version;

                        // مدل و ردیف متناظر از روی شاخص، نه تطبیق نام+پورت (که ممکن بود ردیف اشتباه را بگیرد)
                        var idx = results.IndexOf(db);
                        if (idx >= 0 && idx < models.Count)
                        {
                            models[idx].Version = testResult.Version;
                            if (idx < dgvDatabases.Rows.Count)
                                dgvDatabases.Rows[idx].Cells["colVersion"].Value = testResult.Version;
                        }
                    }
                }
                catch (TimeoutException)
                {
                    // دادهٔ نسخه بهترین تلاش است؛ عدم دریافت آن نباید جریان تشخیص را مسدود کند.
                }
            }
            catch (Exception ex)
            {
                if (IsDisposed || !IsHandleCreated) return;
                lblStatus.Text = L.Text("S229");
                MessageBox.Show(L.Format("S230", ex.Message),
                    L.Text("S195"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _detectBusy = false;
            }
        }
    }
}
