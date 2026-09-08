namespace DatabaseFinder
{
    public class DatabaseDetailForm : AppForm
    {
        protected override bool ModernLayout => true;
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
            Text = L.Format("S091", db.TypeDisplayName);
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
                Text = L.Text("S092"),
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
                Text = L.Text("S093")
            };
            _grpTest.Controls.Add(_txtTestResult);
            Controls.Add(_grpTest);

            _btnTest = new Button
            {
                Text = L.Text("S092"),
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
                Text = L.Text("S094"),
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
                Text = L.Text("S095"),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(12, 455),
                Size = new Size(100, 35)
            };
            _btnCancel.Click += (s, e) => Close();
            Controls.Add(_btnCancel);
            var detailsLayout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2};detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));detailsLayout.RowStyles.Add(new RowStyle(SizeType.Percent,60));detailsLayout.RowStyles.Add(new RowStyle(SizeType.Percent,40));
            _txtDetails.Dock=DockStyle.Fill;_grpTest.Dock=DockStyle.Fill;_grpTest.Padding=new Padding(12);_txtTestResult.Dock=DockStyle.Fill;detailsLayout.Controls.Add(_txtDetails,0,0);detailsLayout.Controls.Add(_grpTest,0,1);
            FormLayout.Build(this,lblTitle,null,detailsLayout,_lblStatus,_btnTest,_btnSaveProfile,_btnCancel);
        }

        private string BuildDetails()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(L.Format("S096", _db.Name));
            sb.AppendLine(L.Format("S097", _db.TypeDisplayName));
            sb.AppendLine(L.Format("S098", (_db.Port?.ToString() ?? "-")));
            sb.AppendLine(L.Format("S099", (_db.ServiceName ?? "-")));
            sb.AppendLine(L.Format("S100", (_db.ProcessName ?? "-"), (_db.ProcessId > 0 ? _db.ProcessId.ToString() : "-")));
            var how = new List<string>();
            if (_db.IsRunningAsService) how.Add(L.Text("S101"));
            if (_db.IsRunningAsProcess) how.Add(L.Text("S102"));
            if (_db.Port.HasValue) how.Add(L.Text("S103"));
            sb.AppendLine(L.Format("S104", string.Join(" + ", how)));
            sb.AppendLine(L.Format("S105", (_db.Port?.ToString() ?? "-")));
            return sb.ToString();
        }

        private async void BtnTest_Click(object? sender, EventArgs e)
        {
            _btnTest.Enabled = false;
            _btnTest.Text = L.Text("S106");
            _txtTestResult.Text = L.Text("S107");

            var result = await Task.Run(() => DatabaseTester.TestConnection(_db));

            _btnTest.Enabled = true;
            _btnTest.Text = L.Text("S092");

            var color = result.Success ? Color.FromArgb(232, 245, 233) : Color.FromArgb(255, 235, 238);
            _txtTestResult.BackColor = color;

            if (result.Success)
            {
                _txtTestResult.Text = L.Text("S108") +
                                      L.Format("S109", result.Message) +
                                      (result.Version.Length > 0 ? L.Format("S110", result.Version) : "");
                _lblStatus.Text = L.Text("S111");
                _lblStatus.ForeColor = Color.FromArgb(76, 175, 80);
            }
            else
            {
                _txtTestResult.Text = L.Format("S112", result.Message);
                _lblStatus.Text = L.Text("S113");
                _lblStatus.ForeColor = Color.FromArgb(244, 67, 54);
            }
        }

        private void BtnSaveProfile_Click(object? sender, EventArgs e)
        {
            var profiles = ProfileManager.Load();
            if (profiles.Any(p => p.Name == _db.Name && p.Type == _db.Type))
            {
                _lblStatus.Text = L.Text("S114");
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

            _lblStatus.Text = L.Text("S115");
            _lblStatus.ForeColor = Color.FromArgb(76, 175, 80);
        }
    }
}
