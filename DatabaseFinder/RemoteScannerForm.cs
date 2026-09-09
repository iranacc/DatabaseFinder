using System.Net.Sockets;

namespace DatabaseFinder
{
    public class RemoteScannerForm : AppForm
    {
        protected override bool ModernLayout => true;
        private readonly TextBox _txtTarget;
        private readonly ComboBox _cmbNets;
        private readonly TextBox _txtPorts;
        private readonly CheckBox _chkOpenOnly;
        private readonly Button _btnScan;
        private readonly Button _btnConnect;
        private readonly Button _btnClose;
        private readonly DataGridView _dgvResults;
        private readonly Label _lblStatus;
        private readonly ProgressBar _progress;
        private List<RemoteScanResult> _results = new();

        public RemoteScannerForm()
        {
            Text = L.Text("S284");
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
                Text = L.Text("S285"),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                AutoSize = true,
                Location = new Point(12, 10)
            };
            Controls.Add(lblTitle);

            var lblHost = new Label
            {
                Text = L.Text("S316"),
                Location = new Point(12, 52),
                AutoSize = true
            };
            _txtTarget = new TextBox
            {
                Location = new Point(160, 48),
                Size = new Size(200, 27),
                PlaceholderText = L.Text("S325")
            };
            _cmbNets = new ComboBox
            {
                Location = new Point(370, 48),
                Size = new Size(170, 27),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            var localNets = NetworkRanges.GetLocalRanges();
            _cmbNets.Items.Add(L.Text("S318"));
            foreach (var n in localNets) _cmbNets.Items.Add(n);
            _cmbNets.SelectedIndex = 0;
            if (localNets.Count > 0) _txtTarget.Text = localNets[0].ToString();
            _cmbNets.SelectedIndexChanged += (s, e) =>
            {
                if (_cmbNets.SelectedIndex > 0 && _cmbNets.SelectedItem is string range)
                    _txtTarget.Text = range;
            };
            Controls.Add(lblHost);
            Controls.Add(_txtTarget);
            Controls.Add(_cmbNets);

            var lblPorts = new Label
            {
                Text = L.Text("S287"),
                Location = new Point(12, 88),
                AutoSize = true
            };
            _txtPorts = new TextBox
            {
                Location = new Point(160, 84),
                Size = new Size(200, 27),
                PlaceholderText = L.Text("S288")
            };
            Controls.Add(lblPorts);
            Controls.Add(_txtPorts);

            _btnScan = new Button
            {
                Text = L.Text("S289"),
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
            _dgvResults.Columns.Add("colStatus", L.Text("S290"));
            _dgvResults.Columns.Add("colHost", L.Text("S326"));
            _dgvResults.Columns.Add("colType", L.Text("S251"));
            _dgvResults.Columns.Add("colPort", L.Text("S103"));
            _dgvResults.Columns.Add("colVersion", L.Text("S231"));
            _dgvResults.Columns.Add("colMsg", L.Text("S291"));
            _dgvResults.SelectionChanged += (s, e) => UpdateConnectState();
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
                Text = L.Text("S292"),
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
                Text = L.Text("S095"),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(588, 445),
                Size = new Size(100, 35)
            };
            _btnClose.Click += (s, e) => Close();
            Controls.Add(_btnClose);

            _chkOpenOnly = new CheckBox
            {
                Text = L.Text("S327"),
                Checked = true,
                AutoSize = true,
                Padding = new Padding(6, 22, 0, 0)
            };
            _chkOpenOnly.CheckedChanged += (s, e) => BindResults();
            Controls.Add(_chkOpenOnly);

            _lblStatus = new Label
            {
                AutoSize = true,
                Location = new Point(12, 70),
                ForeColor = Color.Gray
            };
            Controls.Add(_lblStatus);
            var fields=UiTheme.Flow(FormLayout.Field(L.Text("S316"),_txtTarget,260),FormLayout.Field(L.Text("S317"),_cmbNets,200),FormLayout.Field(L.Text("S287"),_txtPorts,300),_chkOpenOnly);
            FormLayout.Build(this,lblTitle,fields,_dgvResults,_lblStatus,_btnScan,_btnConnect,_btnClose);
            _progress.Dock=DockStyle.Bottom;Controls.Add(_progress);_progress.BringToFront();
        }

        private async void BtnScan_Click(object? sender, EventArgs e)
        {
            var target = _txtTarget.Text.Trim();
            if (string.IsNullOrEmpty(target))
            {
                _lblStatus.Text = L.Text("S319");
                return;
            }

            var hosts = NetworkRanges.Expand(target);
            if (hosts.Count == 0)
            {
                _lblStatus.Text = L.Format("S320", target);
                return;
            }

            var portsStr = _txtPorts.Text.Trim();
            int[]? customPorts = null;
            if (!string.IsNullOrEmpty(portsStr))
            {
                var parts = portsStr.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
                customPorts = parts.Select(p => int.TryParse(p, out int v) ? v : 0).Where(v => v > 0).ToArray();
            }
            var portCount = customPorts?.Length ?? RemoteScanner.KnownPorts.Length;

            var isRange = hosts.Count > 1;
            if (isRange)
            {
                var msg = L.Format("S322", hosts.Count, portCount);
                if (MessageBox.Show(msg, L.Text("S321"), MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) != DialogResult.Yes)
                {
                    return;
                }
            }

            _btnScan.Enabled = false;
            _progress.Visible = true;
            _lblStatus.Text = isRange ? L.Format("S323", hosts.Count) : L.Text("S294");

            _results = isRange
                ? await Task.Run(() => RemoteScanner.ScanNetwork(hosts, customPorts ?? Array.Empty<int>()))
                : await Task.Run(() => RemoteScanner.ScanWithBrowser(hosts[0], customPorts ?? Array.Empty<int>()));

            BindResults();
            SelectFirstOpen();

            var openCount = _results.Count(r => r.IsOpen);
            var distinctHosts = _results.Select(r => r.Host).Distinct().Count();
            _progress.Visible = false;
            _btnScan.Enabled = true;
            _lblStatus.Text = isRange
                ? L.Format("S324", openCount, distinctHosts)
                : L.Format("S297", openCount, hosts[0]);
        }

        private void SelectFirstOpen()
        {
            _dgvResults.ClearSelection();
            foreach (DataGridViewRow row in _dgvResults.Rows)
            {
                if (row.Tag is RemoteScanResult rr && rr.IsOpen && row.Visible)
                {
                    _dgvResults.CurrentCell = row.Cells[0];
                    row.Selected = true;
                    break;
                }
            }
            UpdateConnectState();
        }

        private void UpdateConnectState()
        {
            _btnConnect.Enabled = _dgvResults.SelectedRows.Count == 1
                && _dgvResults.SelectedRows[0].Tag is RemoteScanResult r && r.IsOpen;
        }

        private void BindResults()
        {
            _dgvResults.Rows.Clear();
            var showOpenOnly = _chkOpenOnly.Checked;
            foreach (var r in _results.Where(x => !showOpenOnly || x.IsOpen))
            {
                var row = _dgvResults.Rows.Add(
                    r.IsOpen ? L.Text("S295") : L.Text("S296"),
                    r.Host,
                    r.TypeDisplayName,
                    r.Port,
                    r.Version,
                    r.Message
                );
                _dgvResults.Rows[row].Tag = r;
            }
            UpdateConnectState();
        }

        private void BtnConnect_Click(object? sender, EventArgs e)
        {
            if (_dgvResults.SelectedRows.Count == 0)
            {
                _lblStatus.Text = L.Text("S298");
                return;
            }

            var result = _dgvResults.SelectedRows[0].Tag as RemoteScanResult;
            if (result == null || !result.IsOpen)
            {
                _lblStatus.Text = L.Text("S298");
                return;
            }

            var qf = new QueryRunnerForm(new DatabaseInfo
            {
                Type = result.Type,
                Name = result.TypeDisplayName,
                Host = result.Host,
                Port = result.Port,
                Version = result.Version,
                ServerName = result.ServerName
            });
            qf.ShowDialog(this);
        }
    }
}
