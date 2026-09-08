namespace DatabaseFinder
{
    public class ProfilesForm : AppForm
    {
        protected override bool ModernLayout => true;
        private readonly List<DatabaseProfile> _profiles;
        private readonly DataGridView _dgv;
        private readonly Button _btnTest;
        private readonly Button _btnDelete;
        private readonly Button _btnClose;
        private readonly Label _lblStatus;

        public ProfilesForm()
        {
            _profiles = ProfileManager.Load();

            Text = L.Text("S248");
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(700, 420);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var lblTitle = new Label
            {
                Text = L.Text("S249"),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                AutoSize = true,
                Location = new Point(12, 10)
            };
            Controls.Add(lblTitle);

            _dgv = new DataGridView
            {
                Location = new Point(12, 45),
                Size = new Size(676, 280),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            _dgv.Columns.Add("colName", L.Text("S250"));
            _dgv.Columns.Add("colType", L.Text("S251"));
            _dgv.Columns.Add("colHost", L.Text("S252"));
            _dgv.Columns.Add("colPort", L.Text("S103"));
            _dgv.Columns.Add("colUser", L.Text("S253"));

            foreach (var p in _profiles)
            {
                _dgv.Rows.Add(p.Name, p.TypeDisplayName, p.Host, p.Port, p.Username);
            }
            Controls.Add(_dgv);

            _btnTest = new Button
            {
                Text = L.Text("S092"),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(12, 345),
                Size = new Size(130, 35)
            };
            _btnTest.Click += BtnTest_Click;
            Controls.Add(_btnTest);

            _btnDelete = new Button
            {
                Text = L.Text("S254"),
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(150, 345),
                Size = new Size(100, 35)
            };
            _btnDelete.Click += BtnDelete_Click;
            Controls.Add(_btnDelete);

            _btnClose = new Button
            {
                Text = L.Text("S095"),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(588, 345),
                Size = new Size(100, 35)
            };
            _btnClose.Click += (s, e) => Close();
            Controls.Add(_btnClose);

            _lblStatus = new Label
            {
                AutoSize = true,
                Location = new Point(12, 390),
                ForeColor = Color.Gray
            };
            Controls.Add(_lblStatus);
            FormLayout.Build(this,lblTitle,null,_dgv,_lblStatus,_btnTest,_btnDelete,_btnClose);
        }

        private void BtnTest_Click(object? sender, EventArgs e)
        {
            if (_dgv.SelectedRows.Count == 0)
            {
                _lblStatus.Text = L.Text("S255");
                return;
            }

            var row = _dgv.SelectedRows[0].Index;
            var profile = _profiles[row];

            _lblStatus.Text = L.Format("S256", profile.Host, profile.Port);
            _lblStatus.Refresh();

            var db = new DatabaseInfo
            {
                Type = profile.Type,
                Name = profile.Name,
                Port = profile.Port > 0 ? profile.Port : null
            };

            var result = DatabaseTester.TestConnection(db);

            if (result.Success)
            {
                _lblStatus.Text = L.Format("S257", profile.Name) +
                    (result.Version.Length > 0 ? L.Format("S258", result.Version) : "");
                _lblStatus.ForeColor = Color.FromArgb(76, 175, 80);
            }
            else
            {
                _lblStatus.Text = L.Format("S259", profile.Name, result.Message);
                _lblStatus.ForeColor = Color.FromArgb(244, 67, 54);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (_dgv.SelectedRows.Count == 0)
            {
                _lblStatus.Text = L.Text("S255");
                return;
            }

            var rowIdx = _dgv.SelectedRows[0].Index;
            var profile = _profiles[rowIdx];

            var confirm = MessageBox.Show(
                L.Format("S260", profile.Name),
                L.Text("S261"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                _profiles.RemoveAt(rowIdx);
                ProfileManager.Save(_profiles);
                _dgv.Rows.RemoveAt(rowIdx);
                _lblStatus.Text = L.Text("S262");
            }
        }
    }
}
