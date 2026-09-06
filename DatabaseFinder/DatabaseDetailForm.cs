namespace DatabaseFinder
{
    public class DatabaseDetailForm : Form
    {
        private readonly DatabaseInfo _db;
        private readonly TextBox _txtDetails;
        private readonly Button _btnTest;
        private readonly Button _btnSaveProfile;
        private readonly GroupBox _grpTest;
        private readonly TextBox _txtTestResult;
        private readonly Label _lblStatus;
        private readonly Button _btnCancel;

        public DatabaseDetailForm(DatabaseInfo db)
        {
            _db = db;
            Text = $"جزئیات {db.TypeDisplayName}";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 520);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var lblTitle = new Label
            {
                Text = db.TypeDisplayName,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                AutoSize = true,
                Location = new Point(12, 10)
            };
            Controls.Add(lblTitle);

            var details = BuildDetails();
            _txtDetails = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 10F),
                Text = details,
                Location = new Point(12, 45),
                Size = new Size(496, 180)
            };
            Controls.Add(_txtDetails);

            _grpTest = new GroupBox
            {
                Text = "تست اتصال",
                Location = new Point(12, 235),
                Size = new Size(496, 120)
            };
            _txtTestResult = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(255, 253, 231),
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(12, 25),
                Size = new Size(470, 85),
                Text = "برای تست اتصال روی دکمه «تست اتصال» کلیک کنید."
            };
            _grpTest.Controls.Add(_txtTestResult);
            Controls.Add(_grpTest);

            _btnTest = new Button
            {
                Text = "تست اتصال",
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(12, 370),
                Size = new Size(120, 35)
            };
            _btnTest.Click += BtnTest_Click;
            Controls.Add(_btnTest);

            _btnSaveProfile = new Button
            {
                Text = "ذخیره در پروفایل‌ها",
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(140, 370),
                Size = new Size(160, 35)
            };
            _btnSaveProfile.Click += BtnSaveProfile_Click;
            Controls.Add(_btnSaveProfile);

            _lblStatus = new Label
            {
                AutoSize = true,
                Location = new Point(12, 415),
                ForeColor = Color.Gray
            };
            Controls.Add(_lblStatus);

            _btnCancel = new Button
            {
                Text = "بستن",
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(12, 455),
                Size = new Size(100, 35)
            };
            _btnCancel.Click += (s, e) => Close();
            Controls.Add(_btnCancel);
        }

        private string BuildDetails()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"نام دیتابیس:  {_db.Name}");
            sb.AppendLine($"نوع:          {_db.TypeDisplayName}");
            sb.AppendLine($"پورت:         {(_db.Port?.ToString() ?? "-")}");
            sb.AppendLine($"سرویس:        {(_db.ServiceName ?? "-")}");
            sb.AppendLine($"پروسس:        {(_db.ProcessName ?? "-")} (PID: {(_db.ProcessId > 0 ? _db.ProcessId.ToString() : "-")})");
            var how = new List<string>();
            if (_db.IsRunningAsService) how.Add("سرویس");
            if (_db.IsRunningAsProcess) how.Add("پروسس");
            if (_db.Port.HasValue) how.Add("پورت");
            sb.AppendLine($"تشخیص:        {string.Join(" + ", how)}");
            sb.AppendLine($"آدرس:         localhost:{(_db.Port?.ToString() ?? "-")}");
            return sb.ToString();
        }

        private async void BtnTest_Click(object? sender, EventArgs e)
        {
            _btnTest.Enabled = false;
            _btnTest.Text = "در حال تست...";
            _txtTestResult.Text = "در حال برقراری اتصال...";

            var result = await Task.Run(() => DatabaseTester.TestConnection(_db));

            _btnTest.Enabled = true;
            _btnTest.Text = "تست اتصال";

            var color = result.Success ? Color.FromArgb(232, 245, 233) : Color.FromArgb(255, 235, 238);
            _txtTestResult.BackColor = color;

            if (result.Success)
            {
                _txtTestResult.Text = $"✅ اتصال برقرار شد!\n" +
                                      $"وضعیت: {result.Message}\n" +
                                      (result.Version.Length > 0 ? $"نسخه: {result.Version}\n" : "");
                _lblStatus.Text = "اتصال موفق";
                _lblStatus.ForeColor = Color.FromArgb(76, 175, 80);
            }
            else
            {
                _txtTestResult.Text = $"❌ اتصال برقرار نشد\n{result.Message}";
                _lblStatus.Text = "اتصال ناموفق";
                _lblStatus.ForeColor = Color.FromArgb(244, 67, 54);
            }
        }

        private void BtnSaveProfile_Click(object? sender, EventArgs e)
        {
            var profiles = ProfileManager.Load();
            if (profiles.Any(p => p.Name == _db.Name && p.Type == _db.Type))
            {
                _lblStatus.Text = "این دیتابیس قبلاً در پروفایل‌ها ذخیره شده است.";
                _lblStatus.ForeColor = Color.FromArgb(255, 152, 0);
                return;
            }

            profiles.Add(new DatabaseProfile
            {
                Name = _db.Name,
                Type = _db.Type,
                Host = "localhost",
                Port = _db.Port ?? 0,
                Username = "",
                Password = "",
                DatabaseName = ""
            });
            ProfileManager.Save(profiles);

            _lblStatus.Text = "در پروفایل‌ها ذخیره شد ✓";
            _lblStatus.ForeColor = Color.FromArgb(76, 175, 80);
        }
    }
}
