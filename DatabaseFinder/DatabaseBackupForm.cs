using System.Diagnostics;

namespace DatabaseFinder
{
    public class DatabaseBackupForm : AppForm
    {
        protected override bool ModernLayout => true;
        private readonly List<DatabaseInfo> _servers;
        private List<DatabaseBackupItem> _items = new();
        private readonly TextBox _txtDest;
        private readonly TreeView _tree;
        private readonly TextBox _txtLog;
        private readonly Button _btnStart;
        private readonly Button _btnFindPass;
        private readonly Button _btnSysadmin;
        private readonly Button _btnManifest;
        private readonly Button _btnOpen;
        private readonly Label _lblStatus;
        private string _manifestPath = "";
        private string _planDest = "";
        private readonly CheckBox _chkCompress;
        private readonly CheckBox _chkVerify;
        private readonly CheckBox _chkChecksum;

        public DatabaseBackupForm(List<DatabaseInfo> servers)
        {
            _servers = servers;

            Text = L.Text("S028");
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(780, 660);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(700, 560);

            var lblTitle = new Label
            {
                Text = L.Text("S029"),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                AutoSize = true,
                Location = new Point(12, 8)
            };
            Controls.Add(lblTitle);

            var grpDest = new GroupBox
            {
                Text = L.Text("S030"),
                Location = new Point(12, 42),
                Size = new Size(756, 82)
            };

            _txtDest = new TextBox
            {
                Location = new Point(12, 24),
                Size = new Size(560, 27),
                Text = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "DatabaseFinder", "Backup")
            };
            grpDest.Controls.Add(_txtDest);

            var btnBrowse = new Button
            {
                Text = L.Text("S031"),
                Location = new Point(580, 23),
                Size = new Size(100, 28),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnBrowse.Click += BtnBrowse_Click;
            grpDest.Controls.Add(btnBrowse);

            var lblHint = new Label
            {
                Text = L.Text("S032"),
                Location = new Point(12, 56),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8.5F)
            };
            grpDest.Controls.Add(lblHint);
            Controls.Add(grpDest);

            var lblItems = new Label
            {
                Text = L.Text("S033"),
                Location = new Point(12, 134),
                AutoSize = true
            };
            Controls.Add(lblItems);

            _tree = new TreeView
            {
                Location = new Point(12, 158),
                Size = new Size(500, 310),
                CheckBoxes = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9.5F)
            };
            _tree.AfterCheck += (s, e) =>
            {
                var node = e.Node;
                if (node != null && node.Nodes != null)
                    foreach (TreeNode child in node.Nodes) child.Checked = node.Checked;
            };
            Controls.Add(_tree);

            var btnSelectAll = new Button
            {
                Text = L.Text("S034"),
                Location = new Point(520, 158),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnSelectAll.Click += (s, e) => SetAllChecked(true);
            Controls.Add(btnSelectAll);

            var btnClearAll = new Button
            {
                Text = L.Text("S035"),
                Location = new Point(648, 158),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClearAll.Click += (s, e) => SetAllChecked(false);
            Controls.Add(btnClearAll);

            var btnExpand = new Button
            {
                Text = L.Text("S036"),
                Location = new Point(520, 194),
                Size = new Size(248, 30),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnExpand.Click += (s, e) => _tree.ExpandAll();
            Controls.Add(btnExpand);

            _btnFindPass = new Button
            {
                Text = L.Text("S357"),
                Location = new Point(520, 194),
                Size = new Size(248, 30),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnFindPass.Click += BtnFindPass_Click;
            Controls.Add(_btnFindPass);

            _btnSysadmin = new Button
            {
                Text = L.Text("S358"),
                Location = new Point(520, 194),
                Size = new Size(248, 30),
                BackColor = Color.FromArgb(211, 47, 47),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnSysadmin.Click += BtnSysadmin_Click;
            Controls.Add(_btnSysadmin);

            var lblLog = new Label
            {
                Text = L.Text("S037"),
                Location = new Point(12, 478),
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            Controls.Add(lblLog);

            _txtLog = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Location = new Point(12, 502),
                Size = new Size(756, 100),
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9F),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_txtLog);

            _btnStart = new Button
            {
                Text = L.Text("S038"),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(12, 614),
                Size = new Size(150, 36),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnStart.Click += BtnStart_Click;
            Controls.Add(_btnStart);

            _btnManifest = new Button
            {
                Text = L.Text("S039"),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(172, 614),
                Size = new Size(180, 36),
                Enabled = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnManifest.Click += BtnManifest_Click;
            Controls.Add(_btnManifest);

            _btnOpen = new Button
            {
                Text = L.Text("S040"),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(362, 614),
                Size = new Size(150, 36),
                Enabled = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnOpen.Click += BtnOpen_Click;
            Controls.Add(_btnOpen);

            _lblStatus = new Label
            {
                AutoSize = true,
                Location = new Point(12, 596),
                ForeColor = Color.Gray,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            Controls.Add(_lblStatus);

            var grpOptions = new GroupBox
            {
                Text = L.Text("S332"),
                Location = new Point(520, 232),
                Size = new Size(248, 140),
                ForeColor = Color.FromArgb(66, 66, 66),
                Font = new Font("Segoe UI", 9F)
            };
            _chkCompress = new CheckBox
            {
                Text = L.Text("S333"),
                Checked = true,
                Location = new Point(14, 28),
                AutoSize = true,
                ForeColor = Color.FromArgb(66, 66, 66)
            };
            _chkVerify = new CheckBox
            {
                Text = L.Text("S334"),
                Checked = true,
                Location = new Point(14, 58),
                AutoSize = true,
                ForeColor = Color.FromArgb(66, 66, 66)
            };
            _chkChecksum = new CheckBox
            {
                Text = L.Text("S335"),
                Checked = false,
                Location = new Point(14, 88),
                AutoSize = true,
                ForeColor = Color.FromArgb(66, 66, 66)
            };
            grpOptions.Controls.Add(_chkCompress);
            grpOptions.Controls.Add(_chkVerify);
            grpOptions.Controls.Add(_chkChecksum);
            Controls.Add(grpOptions);

            Load += DatabaseBackupForm_Load;
            OperationLayout.Build(this,lblTitle,_txtDest,btnBrowse,lblHint,_tree,new Control[]{btnSelectAll,btnClearAll,btnExpand,_btnFindPass,_btnSysadmin},grpOptions,lblLog,_txtLog,_lblStatus,_btnStart,_btnManifest,_btnOpen);
        }

        private async void DatabaseBackupForm_Load(object? sender, EventArgs e)
        {
            _btnStart.Enabled = false;
            _lblStatus.Text = L.Text("S041");
            AppendLog(L.Text("S042"));

            var result = await Task.Run(() =>
                DatabaseBackuper.BuildBackupPlan(_servers, _txtDest.Text, message => AppendLogSafe(message)));

            _items = result;
            _tree.Nodes.Clear();

            foreach (var group in _items.GroupBy(i => i.Server.TypeDisplayName))
            {
                var serverNode = new TreeNode(group.Key) { Checked = true };
                foreach (var item in group)
                {
                    var node = new TreeNode(TextFor(item))
                    {
                        Tag = item,
                        Checked = !string.IsNullOrEmpty(item.Error) || item.Server.IsOnline
                    };
                    if (!string.IsNullOrEmpty(item.Error))
                    {
                        node.Text += "  ⚠ " + ShortError(item.Error);
                        node.ForeColor = Color.FromArgb(211, 47, 47);
                        node.Checked = false;
                    }
                    serverNode.Nodes.Add(node);
                }
                _tree.Nodes.Add(serverNode);
            }

            _tree.ExpandAll();
            _planDest = _txtDest.Text.Trim();
            _btnStart.Enabled = true;
            _lblStatus.Text = L.Format("S043", _items.Count);
        }

        private async Task RebuildPlanAsync()
        {
            var dest = _txtDest.Text.Trim();
            if (string.IsNullOrEmpty(dest) || string.Equals(dest, _planDest, StringComparison.OrdinalIgnoreCase))
                return;

            var checkedNames = new HashSet<string>();
            foreach (TreeNode node in _tree.Nodes)
            {
                foreach (TreeNode child in node.Nodes)
                {
                    if (child.Checked && child.Tag is DatabaseBackupItem item)
                        checkedNames.Add(item.DatabaseName);
                }
            }

            _btnStart.Enabled = false;
            AppendLog(L.Text("S042"));

            var result = await Task.Run(() =>
                DatabaseBackuper.BuildBackupPlan(_servers, dest, message => AppendLogSafe(message)));

            _items = result;
            _tree.Nodes.Clear();

            foreach (var group in _items.GroupBy(i => i.Server.TypeDisplayName))
            {
                var serverNode = new TreeNode(group.Key) { Checked = true };
                foreach (var item in group)
                {
                    var node = new TreeNode(TextFor(item))
                    {
                        Tag = item,
                        Checked = (item.Server.IsOnline && string.IsNullOrEmpty(item.Error))
                            && checkedNames.Contains(item.DatabaseName)
                    };
                    if (!string.IsNullOrEmpty(item.Error))
                    {
                        node.Text += "  ⚠ " + ShortError(item.Error);
                        node.ForeColor = Color.FromArgb(211, 47, 47);
                        node.Checked = false;
                    }
                    serverNode.Nodes.Add(node);
                }
                _tree.Nodes.Add(serverNode);
            }

            _tree.ExpandAll();
            _planDest = dest;
            _btnStart.Enabled = true;
            _lblStatus.Text = L.Format("S043", _items.Count);
        }

        private static string TextFor(DatabaseBackupItem item)
        {
            var method = string.IsNullOrEmpty(item.Method) ? "" : $"  ({item.Method})";
            return $"{item.DatabaseName}{method}";
        }

        private static string ShortError(string error)
        {
            if (string.IsNullOrEmpty(error)) return "";
            return error.Length > 60 ? error.Substring(0, 60) + "..." : error;
        }

        private void AppendLog(string message)
        {
            if (_txtLog.IsDisposed) return;
            _txtLog.AppendText(message + Environment.NewLine);
        }

        private void AppendLogSafe(string message)
        {
            try
            {
                if (_txtLog.IsDisposed) return;
                if (_txtLog.InvokeRequired)
                {
                    _txtLog.BeginInvoke(new Action(() => AppendLog(message)));
                }
                else
                {
                    AppendLog(message);
                }
            }
            catch { }
        }

        private void SetAllChecked(bool check)
        {
            foreach (TreeNode node in _tree.Nodes)
            {
                node.Checked = check;
                foreach (TreeNode child in node.Nodes) child.Checked = check;
            }
        }

        private List<DatabaseBackupItem> GetCheckedItems()
        {
            var selected = new List<DatabaseBackupItem>();
            foreach (TreeNode node in _tree.Nodes)
            {
                foreach (TreeNode child in node.Nodes)
                {
                    if (child.Checked && child.Tag is DatabaseBackupItem item)
                        selected.Add(item);
                }
            }
            return selected;
        }

        private async void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = L.Text("S044"),
                SelectedPath = _txtDest.Text
            };
            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                _txtDest.Text = fbd.SelectedPath;
                await RebuildPlanAsync();
            }
        }

        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            if (!string.Equals(_planDest, _txtDest.Text.Trim(), StringComparison.OrdinalIgnoreCase))
                await RebuildPlanAsync();

            var items = GetCheckedItems();
            if (items.Count == 0)
            {
                _lblStatus.Text = L.Text("S045");
                return;
            }

            foreach (var it in items)
            {
                it.Compress = _chkCompress.Checked;
                it.Verify = _chkVerify.Checked;
                it.Checksum = _chkChecksum.Checked;
            }

            var destRoot = _txtDest.Text.Trim();
            if (string.IsNullOrEmpty(destRoot))
            {
                _lblStatus.Text = L.Text("S046");
                return;
            }

            _btnStart.Enabled = false;
            _btnStart.Text = L.Text("S047");
            _btnManifest.Enabled = false;
            _btnOpen.Enabled = false;
            _txtLog.Clear();
            AppendLog(L.Format("S048", destRoot));
            AppendLog(L.Format("S049", items.Count));
            AppendLog("");

            try
            {
                await Task.Run(() =>
                    DatabaseBackuper.ExecuteBackup(items, message => AppendLogSafe(message)));

                var ok = items.Count(i => i.Done);
                var failed = items.Count(i => i.Failed);

                AppendLog("");
                AppendLog("--------------------------");
                AppendLog(L.Format("S050", ok, failed, DatabaseFileLocator.FormatSize(items.Sum(i => i.BytesProduced))));

                if (ok > 0)
                {
                    AppendLog(L.Text("S051"));
                    _manifestPath = await Task.Run(() => ManifestGenerator.Generate(destRoot, items));
                    AppendLog(L.Format("S052", _manifestPath));
                    _btnManifest.Enabled = true;
                    _btnOpen.Enabled = true;
                }

                _lblStatus.Text = failed > 0
                    ? L.Format("S053", ok, failed)
                    : L.Format("S054", ok);
                _lblStatus.ForeColor = failed > 0 ? Color.FromArgb(211, 47, 47) : Color.FromArgb(76, 175, 80);
            }
            catch (Exception ex)
            {
                AppendLog(L.Format("S055", ex.Message));
                _lblStatus.Text = L.Text("S056");
                _lblStatus.ForeColor = Color.FromArgb(211, 47, 47);
            }
            finally
            {
                _btnStart.Enabled = true;
                _btnStart.Text = L.Text("S038");
            }
        }

        private async void BtnFindPass_Click(object? sender, EventArgs e)
        {
            _btnFindPass.Enabled = false;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                var hits = await SavedPasswordFinder.FindAsync(m => AppendLogSafe(m), cts.Token);
                if (hits.Count == 0)
                {
                    MessageBox.Show(L.Format("S352", "?", 0), L.Text("S357"),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var first = hits[0];
                if (MessageBox.Show($"{first.FilePath}\nUser: {first.Username}\n{first.Line}", L.Text("S357"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    var profiles = ProfileManager.Load();
                    var sql = _servers.FirstOrDefault(s => s.Type == DatabaseType.SQLServer);
                    var host = sql?.Host ?? "localhost";
                    var port = sql?.Port ?? 1433;
                    var p = profiles.FirstOrDefault(x => x.Type == DatabaseType.SQLServer && x.Host == host && x.Port == port);
                    if (p == null)
                    {
                        p = new DatabaseProfile { Name = $"SQL Server @ {host}", Type = DatabaseType.SQLServer, Host = host, Port = port };
                        profiles.Add(p);
                    }
                    p.Username = first.Username.StartsWith("sa") ? "sa" : first.Username;
                    p.Password = first.Password;
                    ProfileManager.Save(profiles);
                    AppendLog(L.Format("S351", first.FilePath));
                    await RebuildPlanAsync();
                }
            }
            catch (Exception ex) { AppendLog(L.Format("S055", ex.Message)); }
            finally { _btnFindPass.Enabled = true; }
        }

        private async void BtnSysadmin_Click(object? sender, EventArgs e)
        {
            if (!SysadminGranter.IsAdministrator())
            {
                MessageBox.Show(L.Text("S360"), L.Text("S358"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show(L.Text("S359"), L.Text("S358"), MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            _btnSysadmin.Enabled = false;
            try
            {
                var sql = _servers.FirstOrDefault(s => s.Type == DatabaseType.SQLServer);
                var svc = sql?.ServiceName ?? "MSSQLSERVER";
                AppendLog(L.Format("S354", svc, Environment.UserName));
                var err = await Task.Run(() => SysadminGranter.GrantForInstance(svc, m => AppendLogSafe(m)));
                if (err == null)
                {
                    AppendLog(L.Text("S356"));
                    _planDest = "";
                    await RebuildPlanAsync();
                }
                else AppendLog(L.Format("S055", err));
            }
            finally { _btnSysadmin.Enabled = true; }
        }

        private async void BtnManifest_Click(object? sender, EventArgs e)
        {
            _btnManifest.Enabled = false;
            AppendLog(L.Text("S051"));
            try
            {
                _manifestPath = await Task.Run(() => ManifestGenerator.Generate(_txtDest.Text.Trim(), GetCheckedItems()));
                AppendLog(L.Format("S052", _manifestPath));
                _lblStatus.Text = L.Text("S057");
            }
            catch (Exception ex)
            {
                AppendLog(L.Format("S055", ex.Message));
            }
            finally
            {
                _btnManifest.Enabled = true;
            }
        }

        private void BtnOpen_Click(object? sender, EventArgs e)
        {
            try
            {
                var target = _txtDest.Text.Trim();
                if (!string.IsNullOrEmpty(_manifestPath) && File.Exists(_manifestPath))
                    target = Path.GetDirectoryName(_manifestPath) ?? target;
                Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _lblStatus.Text = L.Format("S058", ex.Message);
            }
        }
    }
}
