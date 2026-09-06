namespace DatabaseFinder
{
    public class SettingsForm : Form
    {
        private readonly AppSettings _settings;
        private readonly NumericUpDown[] _portBoxes = new NumericUpDown[9];
        private readonly CheckBox _chkMinimizeToTray;
        private readonly CheckBox _chkAutoRefresh;
        private readonly CheckBox _chkNotifications;
        private readonly NumericUpDown _numInterval;
        private readonly Button _btnSave;
        private readonly Button _btnCancel;
        private readonly Label _lblStatus;

        private static readonly (DatabaseType Type, string Label, int Default)[] INDICES = new[]
        {
            (DatabaseType.SQLServer, "SQL Server", 1433),
            (DatabaseType.MySQL, "MySQL", 3306),
            (DatabaseType.MariaDB, "MariaDB", 3307),
            (DatabaseType.PostgreSQL, "PostgreSQL", 5432),
            (DatabaseType.Oracle, "Oracle", 1521),
            (DatabaseType.MongoDB, "MongoDB", 27017),
            (DatabaseType.Redis, "Redis", 6379),
            (DatabaseType.Elasticsearch, "Elasticsearch", 9200),
            (DatabaseType.CouchDB, "CouchDB", 5984),
        };

        public SettingsForm(AppSettings settings)
        {
            _settings = settings;
            Text = "تنظیمات";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 520);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var lblTitle = new Label
            {
                Text = "تنظیمات برنامه",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                AutoSize = true,
                Location = new Point(12, 10)
            };
            Controls.Add(lblTitle);

            var grpPorts = new GroupBox
            {
                Text = "پورت‌های سفارشی",
                Location = new Point(12, 45),
                Size = new Size(396, 260)
            };

            int y = 30;
            for (int i = 0; i < INDICES.Length; i++)
            {
                var (type, label, def) = INDICES[i];
                var lbl = new Label
                {
                    Text = $"{label}:",
                    Location = new Point(15, y + 5),
                    AutoSize = true
                };
                var num = new NumericUpDown
                {
                    Location = new Point(120, y),
                    Size = new Size(100, 25),
                    Minimum = 1,
                    Maximum = 65535,
                    Value = _settings.GetPort(type, def)
                };
                grpPorts.Controls.Add(lbl);
                grpPorts.Controls.Add(num);
                _portBoxes[i] = num;
                y += 28;
            }
            Controls.Add(grpPorts);

            var lblGeneral = new Label
            {
                Text = "عمومی:",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Location = new Point(12, 315),
                AutoSize = true
            };
            Controls.Add(lblGeneral);

            _chkMinimizeToTray = new CheckBox
            {
                Text = "مینیمم شدن به سینی سیستم",
                Checked = _settings.MinimizeToTray,
                Location = new Point(12, 345),
                AutoSize = true
            };
            Controls.Add(_chkMinimizeToTray);

            _chkNotifications = new CheckBox
            {
                Text = "نمایش اعلان (استارت‌آپ)",
                Checked = _settings.ShowNotifications,
                Location = new Point(12, 372),
                AutoSize = true
            };
            Controls.Add(_chkNotifications);

            _chkAutoRefresh = new CheckBox
            {
                Text = "به‌روزرسانی خودکار",
                Checked = _settings.AutoRefresh,
                Location = new Point(12, 399),
                AutoSize = true
            };
            _chkAutoRefresh.CheckedChanged += (s, e) => { if (_numInterval != null) _numInterval.Enabled = _chkAutoRefresh.Checked; };
            Controls.Add(_chkAutoRefresh);

            var lblInt = new Label
            {
                Text = "فاصله (ثانیه):",
                Location = new Point(180, 402),
                AutoSize = true
            };
            _numInterval = new NumericUpDown
            {
                Location = new Point(275, 398),
                Size = new Size(80, 25),
                Minimum = 10,
                Maximum = 600,
                Value = _settings.AutoRefreshIntervalSec,
                Enabled = _settings.AutoRefresh
            };
            Controls.Add(lblInt);
            Controls.Add(_numInterval);

            _btnSave = new Button
            {
                Text = "ذخیره",
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(12, 450),
                Size = new Size(120, 35)
            };
            _btnSave.Click += BtnSave_Click;
            Controls.Add(_btnSave);

            _btnCancel = new Button
            {
                Text = "انصراف",
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(140, 450),
                Size = new Size(120, 35)
            };
            _btnCancel.Click += (s, e) => Close();
            Controls.Add(_btnCancel);

            _lblStatus = new Label
            {
                AutoSize = true,
                Location = new Point(12, 490),
                ForeColor = Color.Gray
            };
            Controls.Add(_lblStatus);
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            // ذخیره پورت‌ها
            for (int i = 0; i < INDICES.Length; i++)
            {
                int.TryParse(_portBoxes[i].Value.ToString(), out int val);
                _settings.CustomPorts[INDICES[i].Type.ToString()] = val;
            }

            _settings.MinimizeToTray = _chkMinimizeToTray.Checked;
            _settings.ShowNotifications = _chkNotifications.Checked;
            _settings.AutoRefresh = _chkAutoRefresh.Checked;
            _settings.AutoRefreshIntervalSec = (int)_numInterval.Value;

            _settings.Save();
            _lblStatus.Text = "تنظیمات ذخیره شد ✓";
            _lblStatus.ForeColor = Color.FromArgb(76, 175, 80);
            _lblStatus.Refresh();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
