using System.Net.Sockets;

namespace DatabaseFinder
{
    public class RemoteScannerForm : Form
    {
        private readonly TextBox _txtHost;
        private readonly TextBox _txtPorts;
        private readonly Button _btnScan;
        private readonly Button _btnConnect;
        private readonly Button _btnClose;
        private readonly DataGridView _dgvResults;
        private readonly Label _lblStatus;
        private readonly ProgressBar _progress;
        private List<RemoteScanResult> _results = new();

        public RemoteScannerForm()
        {
            Text = "اسکن دیتابیس سیستم راه دور";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(700, 500);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var lblTitle = new Label
            {
                Text = "اسکن دیتابیس‌های سیستم راه دور",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                AutoSize = true,
                Location = new Point(12, 10)
            };
            Controls.Add(lblTitle);

            var lblHost = new Label
            {
                Text = "آدرس (IP / Hostname):",
                Location = new Point(12, 52),
                AutoSize = true
            };
            _txtHost = new TextBox
            {
                Location = new Point(160, 48),
                Size = new Size(200, 27),
                Text = "192.168.1.100"
            };
            Controls.Add(lblHost);
            Controls.Add(_txtHost);

            var lblPorts = new Label
            {
                Text = "پورت‌ها (اختیاری):",
                Location = new Point(12, 88),
                AutoSize = true
            };
            _txtPorts = new TextBox
            {
                Location = new Point(160, 84),
                Size = new Size(200, 27),
                PlaceholderText = "خالی = پورت‌های استاندارد (مثل 3306,5432)"
            };
            Controls.Add(lblPorts);
            Controls.Add(_txtPorts);

            _btnScan = new Button
            {
                Text = "اسکن",
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(390, 48),
                Size = new Size(90, 30)
            };
            _btnScan.Click += BtnScan_Click;
            Controls.Add(_btnScan);

            _dgvResults = new DataGridView
            {
                Location = new Point(12, 125),
                Size = new Size(676, 280),
                ReadOnly = true,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            _dgvResults.Columns.Add("colStatus", "وضعیت");
            _dgvResults.Columns.Add("colType", "نوع");
            _dgvResults.Columns.Add("colPort", "پورت");
            _dgvResults.Columns.Add("colVersion", "نسخه");
            _dgvResults.Columns.Add("colMsg", "توضیحات");
            Controls.Add(_dgvResults);

            _progress = new ProgressBar
            {
                Location = new Point(12, 415),
                Size = new Size(676, 18),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };
            Controls.Add(_progress);

            _btnConnect = new Button
            {
                Text = "اجرای کوئری...",
                BackColor = Color.FromArgb(0, 188, 212),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(12, 445),
                Size = new Size(130, 35),
                Enabled = false
            };
            _btnConnect.Click += BtnConnect_Click;
            Controls.Add(_btnConnect);

            _btnClose = new Button
            {
                Text = "بستن",
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(588, 445),
                Size = new Size(100, 35)
            };
            _btnClose.Click += (s, e) => Close();
            Controls.Add(_btnClose);

            _lblStatus = new Label
            {
                AutoSize = true,
                Location = new Point(12, 70),
                ForeColor = Color.Gray
            };
            Controls.Add(_lblStatus);
        }

        private async void BtnScan_Click(object? sender, EventArgs e)
        {
            var host = _txtHost.Text.Trim();
            if (string.IsNullOrEmpty(host))
            {
                _lblStatus.Text = "آدرس را وارد کنید.";
                return;
            }

            _btnScan.Enabled = false;
            _progress.Visible = true;
            _lblStatus.Text = "در حال اسکن...";

            var portsStr = _txtPorts.Text.Trim();
            int[]? customPorts = null;
            if (!string.IsNullOrEmpty(portsStr))
            {
                var parts = portsStr.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
                customPorts = parts.Select(p => int.TryParse(p, out int v) ? v : 0).Where(v => v > 0).ToArray();
            }

            _results = await Task.Run(() => RemoteScanner.Scan(host, customPorts ?? Array.Empty<int>()));

            _dgvResults.Rows.Clear();
            var openCount = 0;
            foreach (var r in _results)
            {
                var open = r.IsOpen;
                if (open) openCount++;
                _dgvResults.Rows.Add(
                    open ? "✅ باز" : "❌ بسته",
                    r.TypeDisplayName,
                    r.Port,
                    r.Version,
                    r.Message
                );
            }

            _progress.Visible = false;
            _btnScan.Enabled = true;
            _btnConnect.Enabled = openCount > 0;
            _lblStatus.Text = $"اسکن کامل شد. {openCount} دیتابیس در {host} یافت شد.";
        }

        private void BtnConnect_Click(object? sender, EventArgs e)
        {
            if (_dgvResults.SelectedRows.Count == 0)
            {
                _lblStatus.Text = "یک دیتابیس را از نتایج انتخاب کنید.";
                return;
            }

            var row = _dgvResults.SelectedRows[0].Index;
            var result = _results[row];

            var qf = new QueryRunnerForm(new DatabaseInfo
            {
                Type = result.Type,
                Name = result.TypeDisplayName,
                Host = _txtHost.Text.Trim(),
                Port = result.Port,
                Version = result.Version
            });
            qf.ShowDialog(this);
        }
    }
}