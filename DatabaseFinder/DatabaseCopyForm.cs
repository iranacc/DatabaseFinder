using System.Diagnostics;

namespace DatabaseFinder
{
    public class DatabaseCopyForm : AppForm
    {
        protected override bool ModernLayout => true;
        private readonly List<DatabaseInfo> _servers;
        private List<DatabaseCopyItem> _items = new();
        private readonly TextBox _txtDest;
        private readonly TreeView _tree;
        private readonly TextBox _txtLog;
        private readonly Button _btnCopy;
        private readonly Button _btnManualPath;
        private readonly Button _btnFindPass;
        private readonly Button _btnSysadmin;
        private readonly Button _btnManifest;
        private readonly Button _btnOpen;
        private readonly Label _lblStatus;
        private readonly RadioButton _rdoVss;
        private readonly RadioButton _rdoStopStart;
        private readonly RadioButton _rdoReportOnly;
        private string _manifestPath = "";

        public DatabaseCopyForm(List<DatabaseInfo> servers)
        {
            _servers = servers;

            Text = L.Text("S059");
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(760, 620);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(680, 520);

            var lblTitle = new Label
            {
                Text = L.Text("S060"),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                AutoSize = true,
                Location = new Point(12, 8)
            };
            Controls.Add(lblTitle);

            var grpDest = new GroupBox
            {
                Text = L.Text("S061"),
                Location = new Point(12, 42),
                Size = new Size(736, 60)
            };

            _txtDest = new TextBox
            {
                Location = new Point(12, 24),
                Size = new Size(560, 27),
                Text = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "DatabaseFilesBackup")
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
            Controls.Add(grpDest);

            var lblItems = new Label
            {
                Text = L.Text("S033"),
                Location = new Point(12, 112),
                AutoSize = true
            };
            Controls.Add(lblItems);

            _tree = new TreeView
            {
                Location = new Point(12, 136),
                Size = new Size(480, 330),
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
                Location = new Point(500, 136),
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
                Location = new Point(628, 136),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClearAll.Click += (s, e) => SetAllChecked(false);
            Controls.Add(btnClearAll);

            _btnManualPath = new Button
            {
                Text = L.Text("S062"),
                Location = new Point(500, 172),
                Size = new Size(248, 30),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnManualPath.Click += BtnManualPath_Click;
            Controls.Add(_btnManualPath);

            _btnFindPass = new Button
            {
                Text = L.Text("S357"),
                Location = new Point(500, 172),
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
                Location = new Point(500, 172),
                Size = new Size(248, 30),
                BackColor = Color.FromArgb(211, 47, 47),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnSysadmin.Click += BtnSysadmin_Click;
            Controls.Add(_btnSysadmin);

            _btnManifest = new Button
            {
                Text = L.Text("S063"),
                Location = new Point(500, 208),
                Size = new Size(248, 30),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnManifest.Click += BtnManifest_Click;
            Controls.Add(_btnManifest);

            _btnOpen = new Button
            {
                Text = L.Text("S040"),
                Location = new Point(500, 244),
                Size = new Size(248, 30),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnOpen.Click += BtnOpen_Click;
            Controls.Add(_btnOpen);

            var grpLocked = new GroupBox
            {
                Text = L.Text("S064"),
                Location = new Point(500, 286),
                Size = new Size(248, 182),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            _rdoVss = new RadioButton
            {
                Text = L.Text("S065"),
                Location = new Point(10, 26),
                Size = new Size(228, 34),
                Checked = true,
                Font = new Font("Segoe UI", 8.8F)
            };
            grpLocked.Controls.Add(_rdoVss);

            var lblVssHint = new Label
            {
                Text = L.Text("S066"),
                Location = new Point(28, 58),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 7.8F)
            };
            grpLocked.Controls.Add(lblVssHint);

            _rdoStopStart = new RadioButton
            {
                Text = L.Text("S067"),
                Location = new Point(10, 86),
                Size = new Size(228, 34),
                Font = new Font("Segoe UI", 8.8F)
            };
            grpLocked.Controls.Add(_rdoStopStart);

            var lblStopHint = new Label
            {
                Text = L.Text("S068"),
                Location = new Point(28, 118),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 7.8F)
            };
            grpLocked.Controls.Add(lblStopHint);

            _rdoReportOnly = new RadioButton
            {
                Text = L.Text("S069"),
                Location = new Point(10, 148),
                Size = new Size(180, 24),
                Font = new Font("Segoe UI", 8.8F)
            };
            grpLocked.Controls.Add(_rdoReportOnly);
            Controls.Add(grpLocked);

            var lblLog = new Label
            {
                Text = L.Text("S070"),
                Location = new Point(12, 476),
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            Controls.Add(lblLog);

            _txtLog = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Location = new Point(12, 500),
                Size = new Size(736, 76),
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9F),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_txtLog);

            _btnCopy = new Button
            {
                Text = L.Text("S071"),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(12, 576),
                Size = new Size(140, 34),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnCopy.Click += BtnCopy_Click;
            Controls.Add(_btnCopy);

            _lblStatus = new Label
            {
                AutoSize = true,
                Location = new Point(170, 584),
                ForeColor = Color.Gray,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            Controls.Add(_lblStatus);

            Load += DatabaseCopyForm_Load;
            OperationLayout.Build(this,lblTitle,_txtDest,btnBrowse,null,_tree,new Control[]{btnSelectAll,btnClearAll,_btnManualPath,_btnFindPass,_btnSysadmin},grpLocked,lblLog,_txtLog,_lblStatus,_btnCopy,_btnManifest,_btnOpen);
        }

        private async void DatabaseCopyForm_Load(object? sender, EventArgs e)
        {
            _btnCopy.Enabled = false;
            _lblStatus.Text = L.Text("S072");
            AppendLog(L.Text("S073"));

            var result = await Task.Run(() =>
                DatabaseFileLocator.BuildPlan(_servers, message => AppendLogSafe(message)));

            _items = result;
            _tree.Nodes.Clear();

            foreach (var group in _items.GroupBy(i =>
                i.Server.IsOnline
                    ? $"{i.Server.TypeDisplayName}@{i.Server.Host}:{i.Server.Port}"
                    : L.Format("S074", i.Server.FormatName)))
            {
                var serverNode = new TreeNode(group.Key)
                {
                    Checked = true
                };
                foreach (var item in group)
                {
                    var sizeText = item.Files.Count > 0
                        ? L.Format("S075", item.Files.Count, DatabaseFileLocator.FormatSize(item.TotalSize))
                        : "";
                    var locationText = !item.Server.IsOnline && item.Server.LocalPath != null
                        ? "  [" + System.IO.Path.GetDirectoryName(item.Server.LocalPath) + "]"
                        : "";
                    var node = new TreeNode($"{item.DatabaseName}{sizeText}{locationText}")
                    {
                        Tag = item,
                        Checked = true
                    };
                    if (!string.IsNullOrEmpty(item.Error))
                    {
                        node.Text += "  ⚠ " + ShortError(item.Error);
                        node.ForeColor = Color.FromArgb(211, 47, 47);
                    }
                    else if (item.UseManualPath)
                    {
                        node.Text += L.Text("S076");
                        node.ForeColor = Color.FromArgb(255, 152, 0);
                    }
                    serverNode.Nodes.Add(node);
                }
                _tree.Nodes.Add(serverNode);
            }

            _tree.ExpandAll();
            _btnCopy.Enabled = true;
            _lblStatus.Text = L.Format("S077", _items.Count);
        }

        private static string HandlingText(LockedFileHandling h)
        {
            switch (h)
            {
                case LockedFileHandling.Vss: return L.Text("S078");
                case LockedFileHandling.StopServices: return L.Text("S079");
                default: return L.Text("S069");
            }
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

        private List<DatabaseCopyItem> GetCheckedItems()
        {
            var selected = new List<DatabaseCopyItem>();
            foreach (TreeNode node in _tree.Nodes)
            {
                foreach (TreeNode child in node.Nodes)
                {
                    if (child.Checked && child.Tag is DatabaseCopyItem item)
                        selected.Add(item);
                }
            }
            return selected;
        }

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = L.Text("S080"),
                SelectedPath = _txtDest.Text
            };
            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                _txtDest.Text = fbd.SelectedPath;
            }
        }

        private void BtnManualPath_Click(object? sender, EventArgs e)
        {
            var item = ResolveManualTarget();
            if (item == null)
            {
                _lblStatus.Text = L.Text("S085");
                return;
            }

            // اگر فایل خودکار پیدا شده، با تایید کاربر اجازه بازنویسی بده
            // (مسیر خودکار در حالت بدون لاگین ممکن است ناقص باشد).
            if (item.Value.Item2.Files.Count > 0 && string.IsNullOrEmpty(item.Value.Item2.Error))
            {
                if (MessageBox.Show(L.Format("S361", item.Value.Item2.DatabaseName), L.Text("S062"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                    return;
            }

            using var fbd = new FolderBrowserDialog
            {
                Description = L.Format("S082", item.Value.Item2.DatabaseName),
                SelectedPath = item.Value.Item2.UseManualPath && Directory.Exists(item.Value.Item2.ManualPath)
                    ? item.Value.Item2.ManualPath
                    : _txtDest.Text
            };
            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                var target = item.Value.Item2;
                target.ManualPath = fbd.SelectedPath;
                target.UseManualPath = true;
                target.Error = null;
                var node = item.Value.Item1;
                node.Text = L.Format("S083", target.DatabaseName, fbd.SelectedPath);
                node.ForeColor = Color.FromArgb(255, 152, 0);
                node.Checked = true;
                _tree.SelectedNode = node;
                _lblStatus.Text = L.Text("S084");
            }
        }

        /// <summary>
        /// گره دیتابیس هدف را هوشمند پیدا می‌کند: گره انتخابی، وگرنه اولین
        /// فرزند خطادار گروه انتخابی، وگرنه اولین مورد خطادار کل درخت.
        /// </summary>
        private (TreeNode, DatabaseCopyItem)? ResolveManualTarget()
        {
            if (_tree.SelectedNode?.Tag is DatabaseCopyItem direct)
                return (_tree.SelectedNode, direct);

            if (_tree.SelectedNode != null)
            {
                foreach (TreeNode child in _tree.SelectedNode.Nodes)
                {
                    if (child.Tag is DatabaseCopyItem c && !string.IsNullOrEmpty(c.Error))
                    {
                        _tree.SelectedNode = child;
                        return (child, c);
                    }
                }
                if (_tree.SelectedNode.Nodes.Count > 0 &&
                    _tree.SelectedNode.Nodes[0].Tag is DatabaseCopyItem first)
                {
                    _tree.SelectedNode = _tree.SelectedNode.Nodes[0];
                    return (_tree.SelectedNode, first);
                }
            }

            foreach (TreeNode group in _tree.Nodes)
            {
                foreach (TreeNode child in group.Nodes)
                {
                    if (child.Tag is DatabaseCopyItem c && !string.IsNullOrEmpty(c.Error))
                    {
                        _tree.SelectedNode = child;
                        return (child, c);
                    }
                }
            }

            foreach (TreeNode group in _tree.Nodes)
            {
                if (group.Nodes.Count > 0 && group.Nodes[0].Tag is DatabaseCopyItem any)
                {
                    _tree.SelectedNode = group.Nodes[0];
                    return (group.Nodes[0], any);
                }
            }

            return null;
        }

        private async void BtnCopy_Click(object? sender, EventArgs e)
        {
            var items = GetCheckedItems();
            if (items.Count == 0)
            {
                _lblStatus.Text = L.Text("S045");
                return;
            }

            var destRoot = _txtDest.Text.Trim();
            if (string.IsNullOrEmpty(destRoot))
            {
                _lblStatus.Text = L.Text("S046");
                return;
            }

            _btnCopy.Enabled = false;
            _btnCopy.Text = L.Text("S086");
            _btnManifest.Enabled = false;
            _btnOpen.Enabled = false;
            _txtLog.Clear();
            AppendLog(L.Format("S048", destRoot));
            AppendLog(L.Format("S049", items.Count));
            var lockedHandling = _rdoVss.Checked ? LockedFileHandling.Vss
                : _rdoStopStart.Checked ? LockedFileHandling.StopServices
                : LockedFileHandling.ReportOnly;
            AppendLog(L.Format("S087", HandlingText(lockedHandling)));
            AppendLog("");

            try
            {
                var result = await Task.Run(() =>
                    DatabaseFileLocator.ExecuteCopy(items, destRoot, lockedHandling, message => AppendLogSafe(message)));

                AppendLog("");
                AppendLog("--------------------------");
                AppendLog(L.Format("S088", result.FilesCopied, DatabaseFileLocator.FormatSize(result.BytesCopied), result.Failed));

                if (result.FilesCopied > 0)
                {
                    AppendLog(L.Text("S051"));
                    _manifestPath = await Task.Run(() => ManifestGenerator.Generate(destRoot));
                    AppendLog(L.Format("S052", _manifestPath));
                    _btnManifest.Enabled = true;
                    _btnOpen.Enabled = true;
                }

                _lblStatus.Text = L.Format("S089", result.FilesCopied, result.Failed);
                _lblStatus.ForeColor = result.Failed > 0 ? Color.FromArgb(211, 47, 47) : Color.FromArgb(76, 175, 80);
            }
            catch (Exception ex)
            {
                AppendLog(L.Format("S055", ex.Message));
                _lblStatus.Text = L.Text("S090");
                _lblStatus.ForeColor = Color.FromArgb(211, 47, 47);
            }
            finally
            {
                _btnCopy.Enabled = true;
                _btnCopy.Text = L.Text("S071");
            }
        }

        private async void BtnFindPass_Click(object? sender, EventArgs e)
        {
            _btnFindPass.Enabled = false;
            AppendLog(L.Format("S350", "?"));
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
                var msg = $"{first.FilePath}\nUser: {first.Username}\n{first.Line}\n\n" + L.Text("S357");
                if (MessageBox.Show(msg, L.Text("S357"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
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
                    _lblStatus.Text = L.Text("S273");
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
                    AppendLog(L.Text("S354").Replace("{#}", svc));
                    _lblStatus.Text = L.Text("S356");
                }
                else
                {
                    AppendLog(L.Format("S055", err));
                    _lblStatus.Text = L.Format("S055", err);
                }
            }
            finally { _btnSysadmin.Enabled = true; }
        }

        private async void BtnManifest_Click(object? sender, EventArgs e)
        {
            _btnManifest.Enabled = false;
            AppendLog(L.Text("S051"));
            try
            {
                _manifestPath = await Task.Run(() => ManifestGenerator.Generate(_txtDest.Text.Trim()));
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
