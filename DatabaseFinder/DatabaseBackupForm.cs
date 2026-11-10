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
        private readonly Button _btnReport;
        private readonly Label _lblStatus;
        private readonly ProgressBar _progress;
        private string _manifestPath = "";
        private string _reportPath = "";
        private string _planDest = "";
        private bool _caseAsked;
        private CaseInfo? _caseInfo;
        private readonly CheckBox _chkCompress;
        private readonly CheckBox _chkVerify;
        private readonly CheckBox _chkChecksum;
        private readonly HashSet<DatabaseBackupItem> _checkedItems = new();
        private bool _updatingTreeChecks;
        private TextBox _txtSearch = null!;
        private Button _btnClearSearch = null!;
        private ComboBox _cmbSort = null!;
        private ComboBox _cmbGroup = null!;
        private ComboBox _cmbFilter = null!;
        private Label _lblFilterSummary = null!;
        private Panel _treeToolbar = null!;

        public DatabaseBackupForm(List<DatabaseInfo> servers)
        {
            _servers = servers;

            Text = L.Text("S028");
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(780, 660);
            Font = UiFont.Make(10f);
            BackColor = Color.White;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(700, 560);

            var lblTitle = new Label
            {
                Text = L.Text("S029"),
                Font = UiFont.Make(13f, FontStyle.Bold),
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
                Font = UiFont.Make(8.5f)
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
                Font = UiFont.Make(9.5f)
            };
            _tree.AfterCheck += Tree_AfterCheck;
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
                Font = UiFont.Make(10f, FontStyle.Bold),
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

            _btnReport = new Button
            {
                Text = L.Text("S386"),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(522, 614),
                Size = new Size(150, 36),
                Enabled = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnReport.Click += BtnReport_Click;
            Controls.Add(_btnReport);

            _lblStatus = new Label
            {
                AutoSize = true,
                Location = new Point(12, 596),
                ForeColor = Color.Gray,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            Controls.Add(_lblStatus);
            _progress = new ProgressBar { Minimum = 0, Maximum = 1, Value = 0, Style = ProgressBarStyle.Continuous };

            var grpOptions = new GroupBox
            {
                Text = L.Text("S332"),
                Location = new Point(520, 232),
                Size = new Size(248, 140),
                ForeColor = Color.FromArgb(66, 66, 66),
                Font = UiFont.Make(9f)
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
            _treeToolbar = BuildTreeToolbar();
            OperationLayout.Build(this,lblTitle,_txtDest,btnBrowse,lblHint,_tree,_treeToolbar,new Control[]{btnSelectAll,btnClearAll,btnExpand,_btnFindPass,_btnSysadmin},grpOptions,lblLog,_txtLog,_lblStatus,_progress,_btnStart,_btnManifest,_btnOpen,_btnReport);
        }

        private Panel BuildTreeToolbar()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                Margin = Padding.Empty,
                Padding = new Padding(0, 0, 0, 6),
                BackColor = Color.Transparent
            };

            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = Color.Transparent
            };
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

            // Row 1: Search and Live Summary
            var row1 = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = L.IsFa ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = Color.Transparent,
                RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No
            };

            _txtSearch = new TextBox
            {
                Width = 240,
                Font = UiFont.Make(9f),
                PlaceholderText = L.Text("S447"),
                Margin = new Padding(0, 1, 4, 1)
            };
            _txtSearch.TextChanged += (_, _) =>
            {
                _btnClearSearch.Visible = !string.IsNullOrEmpty(_txtSearch.Text);
                FillTree();
            };

            _btnClearSearch = new Button
            {
                Text = "✕",
                Width = 26,
                Height = 26,
                FlatStyle = FlatStyle.Flat,
                Font = UiFont.Make(8.5f, FontStyle.Bold),
                ForeColor = UiTheme.Muted,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Visible = false,
                Margin = new Padding(0, 2, 6, 0)
            };
            _btnClearSearch.FlatAppearance.BorderSize = 0;
            _btnClearSearch.Click += (_, _) => _txtSearch.Clear();

            _lblFilterSummary = new Label
            {
                AutoSize = true,
                Font = UiFont.Make(8.5f),
                ForeColor = UiTheme.Muted,
                Margin = new Padding(6, 6, 6, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };

            row1.Controls.Add(_txtSearch);
            row1.Controls.Add(_btnClearSearch);
            row1.Controls.Add(_lblFilterSummary);

            // Row 2: Sort, Group, Filter dropdowns
            var row2 = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = L.IsFa ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = Color.Transparent,
                RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No
            };

            _cmbSort = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 195,
                Font = UiFont.Make(8.8f),
                Margin = new Padding(0, 1, 6, 1)
            };
            _cmbSort.Items.AddRange(new object[]
            {
                L.Text("S434"), // نام (الف تا ی)
                L.Text("S435"), // نام (ی تا الف)
                L.Text("S456"), // روش بکاپ
                L.Text("S460")  // وضعیت (بدون خطا اول)
            });
            _cmbSort.SelectedIndex = 0;
            _cmbSort.SelectedIndexChanged += (_, _) => FillTree();

            _cmbGroup = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 175,
                Font = UiFont.Make(8.8f),
                Margin = new Padding(0, 1, 6, 1)
            };
            _cmbGroup.Items.AddRange(new object[]
            {
                L.Text("S438"), // دسته‌بندی: سرور
                L.Text("S457"), // دسته‌بندی: روش بکاپ
                L.Text("S441")  // بدون دسته‌بندی (یکپارچه)
            });
            _cmbGroup.SelectedIndex = 0;
            _cmbGroup.SelectedIndexChanged += (_, _) => FillTree();

            _cmbFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 160,
                Font = UiFont.Make(8.8f),
                Margin = new Padding(0, 1, 6, 1)
            };
            _cmbFilter.Items.AddRange(new object[]
            {
                L.Text("S442"), // همه دیتابیس‌ها
                L.Text("S458"), // فقط آنلاین و آماده بکاپ
                L.Text("S446")  // فقط دارای خطا ⚠
            });
            _cmbFilter.SelectedIndex = 0;
            _cmbFilter.SelectedIndexChanged += (_, _) => FillTree();

            row2.Controls.Add(_cmbSort);
            row2.Controls.Add(_cmbGroup);
            row2.Controls.Add(_cmbFilter);

            rootLayout.Controls.Add(row1, 0, 0);
            rootLayout.Controls.Add(row2, 0, 1);
            panel.Controls.Add(rootLayout);
            return panel;
        }

        private void Tree_AfterCheck(object? sender, TreeViewEventArgs e)
        {
            if (_updatingTreeChecks || e.Node == null) return;
            try
            {
                _updatingTreeChecks = true;
                if (e.Node.Tag is DatabaseBackupItem item)
                {
                    if (e.Node.Checked) _checkedItems.Add(item);
                    else _checkedItems.Remove(item);

                    if (e.Node.Parent != null)
                    {
                        bool all = true;
                        foreach (TreeNode sibling in e.Node.Parent.Nodes)
                        {
                            if (!sibling.Checked) { all = false; break; }
                        }
                        e.Node.Parent.Checked = all;
                    }
                }
                else
                {
                    foreach (TreeNode child in e.Node.Nodes)
                    {
                        child.Checked = e.Node.Checked;
                        if (child.Tag is DatabaseBackupItem childItem)
                        {
                            if (e.Node.Checked) _checkedItems.Add(childItem);
                            else _checkedItems.Remove(childItem);
                        }
                    }
                }
            }
            finally
            {
                _updatingTreeChecks = false;
                UpdateFilterSummary();
            }
        }

        private async void DatabaseBackupForm_Load(object? sender, EventArgs e)
        {
            _btnStart.Enabled = false;
            _lblStatus.Text = L.Text("S041");
            AppendLog(L.Text("S042"));

            var result = await Task.Run(() =>
                DatabaseBackuper.BuildBackupPlan(_servers, _txtDest.Text, message => AppendLogSafe(message)));

            _items = result;
            _checkedItems.Clear();
            foreach (var item in _items)
            {
                if (item.Server.IsOnline && string.IsNullOrEmpty(item.Error))
                    _checkedItems.Add(item);
            }

            FillTree();
            _planDest = _txtDest.Text.Trim();
            _btnStart.Enabled = true;
            _lblStatus.Text = L.Format("S043", _items.Count);
        }

        private async Task RebuildPlanAsync()
        {
            var dest = _txtDest.Text.Trim();
            if (string.IsNullOrEmpty(dest) || string.Equals(dest, _planDest, StringComparison.OrdinalIgnoreCase))
                return;

            var prevNames = _checkedItems.Select(i => i.DatabaseName).ToHashSet(StringComparer.OrdinalIgnoreCase);

            _btnStart.Enabled = false;
            AppendLog(L.Text("S042"));

            var result = await Task.Run(() =>
                DatabaseBackuper.BuildBackupPlan(_servers, dest, message => AppendLogSafe(message)));

            _items = result;
            _checkedItems.Clear();
            foreach (var item in _items)
            {
                if (item.Server.IsOnline && string.IsNullOrEmpty(item.Error) && prevNames.Contains(item.DatabaseName))
                    _checkedItems.Add(item);
            }

            FillTree();
            _planDest = dest;
            _btnStart.Enabled = true;
            _lblStatus.Text = L.Format("S043", _items.Count);
        }

        private void FillTree()
        {
            _tree.Nodes.Clear();
            if (_items.Count == 0)
            {
                UpdateFilterSummary(0, 0, 0);
                return;
            }

            var query = _items.AsEnumerable();

            // 1. فیلتر متنی نام دیتابیس
            var term = _txtSearch?.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(term))
            {
                query = query.Where(i =>
                    i.DatabaseName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (i.Server.TypeDisplayName != null && i.Server.TypeDisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (i.Method != null && i.Method.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (i.FolderName != null && i.FolderName.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            // 2. فیلتر وضعیت
            var filterIndex = _cmbFilter?.SelectedIndex ?? 0;
            query = filterIndex switch
            {
                1 => query.Where(i => i.Server.IsOnline && string.IsNullOrEmpty(i.Error)),
                2 => query.Where(i => !string.IsNullOrEmpty(i.Error)),
                _ => query
            };

            var filteredList = query.ToList();

            // 3. مرتب‌سازی (SORT BY)
            var sortIndex = _cmbSort?.SelectedIndex ?? 0;
            IOrderedEnumerable<DatabaseBackupItem> sorted = sortIndex switch
            {
                0 => filteredList.OrderBy(i => i.DatabaseName, StringComparer.OrdinalIgnoreCase),
                1 => filteredList.OrderByDescending(i => i.DatabaseName, StringComparer.OrdinalIgnoreCase),
                2 => filteredList.OrderBy(i => i.Method, StringComparer.OrdinalIgnoreCase).ThenBy(i => i.DatabaseName),
                3 => filteredList.OrderBy(i => !string.IsNullOrEmpty(i.Error)).ThenBy(i => i.DatabaseName),
                _ => filteredList.OrderBy(i => i.DatabaseName)
            };

            var orderedItems = sorted.ToList();

            // 4. دسته‌بندی (GROUP BY)
            var groupIndex = _cmbGroup?.SelectedIndex ?? 0;
            var groups = groupIndex switch
            {
                1 => orderedItems.GroupBy(i => string.IsNullOrEmpty(i.Method) ? "-" : i.Method),
                2 => orderedItems.GroupBy(_ => L.Text("S442")),
                _ => orderedItems.GroupBy(i => i.Server.TypeDisplayName)
            };

            _updatingTreeChecks = true;
            try
            {
                foreach (var group in groups)
                {
                    var groupNode = new TreeNode(group.Key);
                    bool allChecked = group.Any();
                    foreach (var item in group)
                    {
                        var node = CreateBackupNode(item);
                        var isChecked = _checkedItems.Contains(item);
                        node.Checked = isChecked;
                        if (!isChecked) allChecked = false;
                        groupNode.Nodes.Add(node);
                    }
                    groupNode.Checked = allChecked;
                    _tree.Nodes.Add(groupNode);
                }
            }
            finally
            {
                _updatingTreeChecks = false;
            }

            _tree.ExpandAll();
            UpdateFilterSummary(filteredList.Count, _items.Count, _checkedItems.Count);
        }

        private TreeNode CreateBackupNode(DatabaseBackupItem item)
        {
            var node = new TreeNode(TextFor(item))
            {
                Tag = item
            };
            if (!string.IsNullOrEmpty(item.Error))
            {
                node.Text += "  ⚠ " + ShortError(item.Error);
                node.ForeColor = Color.FromArgb(211, 47, 47);
            }
            return node;
        }

        private void UpdateFilterSummary(int visibleCount, int totalCount, int selectedCount)
        {
            if (_lblFilterSummary == null || _lblFilterSummary.IsDisposed) return;
            _lblFilterSummary.Text = L.Format("S459", visibleCount, totalCount, selectedCount);
        }

        private void UpdateFilterSummary()
        {
            var visibleCount = 0;
            foreach (TreeNode group in _tree.Nodes)
                visibleCount += group.Nodes.Count;
            UpdateFilterSummary(visibleCount, _items.Count, _checkedItems.Count);
        }

        private CaseInfo? AskCaseInfo() => CaseInfoForm.AskOnce(this, ref _caseAsked, ref _caseInfo);

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
            _updatingTreeChecks = true;
            try
            {
                foreach (TreeNode node in _tree.Nodes)
                {
                    node.Checked = check;
                    foreach (TreeNode child in node.Nodes)
                    {
                        child.Checked = check;
                        if (child.Tag is DatabaseBackupItem item)
                        {
                            if (check) _checkedItems.Add(item);
                            else _checkedItems.Remove(item);
                        }
                    }
                }
            }
            finally
            {
                _updatingTreeChecks = false;
                UpdateFilterSummary();
            }
        }

        private List<DatabaseBackupItem> GetCheckedItems()
        {
            return _items.Where(i => _checkedItems.Contains(i)).ToList();
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
            _btnReport.Enabled = false;
            _progress.Value = 0;
            _progress.Maximum = Math.Max(1, items.Count);
            _reportPath = "";
            _txtLog.Clear();
            AppendLog(L.Format("S048", destRoot));
            AppendLog(L.Format("S049", items.Count));
            AppendLog("");

            try
            {
                await Task.Run(() =>
                    DatabaseBackuper.ExecuteBackup(items, message => AppendLogSafe(message),
                        (done, total) => ReportProgress(done, total)));

                var ok = items.Count(i => i.Done);
                var failed = items.Count(i => i.Failed);

                AppendLog("");
                AppendLog("--------------------------");
                AppendLog(L.Format("S050", ok, failed, DatabaseFileLocator.FormatSize(items.Sum(i => i.BytesProduced))));

                if (ok > 0)
                {
                    AppendLog(L.Text("S051"));
                    var ci = AskCaseInfo();
                    _manifestPath = await Task.Run(() => ManifestGenerator.Generate(destRoot, items, null, null, ci));
                    AppendLog(L.Format("S052", _manifestPath));
                    _reportPath = await Task.Run(() => ReportGenerator.GenerateFromManifest(
                        Path.ChangeExtension(_manifestPath, ".json")));
                    AppendLog(L.Format("S052", _reportPath));
                    _btnManifest.Enabled = true;
                    _btnOpen.Enabled = true;
                    _btnReport.Enabled = true;
                }

                _lblStatus.Text = L.Format("S414", ok, failed,
                    DatabaseFileLocator.FormatSize(items.Sum(i => i.BytesProduced)));
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
                _progress.Value = _progress.Maximum;
            }
        }

        private void ReportProgress(int done, int total)
        {
            try
            {
                if (_progress.IsDisposed) return;
                if (_progress.InvokeRequired) { _progress.BeginInvoke(new Action(() => ReportProgress(done, total))); return; }
                _progress.Maximum = Math.Max(1, total);
                _progress.Value = Math.Min(Math.Max(0, done), _progress.Maximum);
                _lblStatus.Text = L.Format("S412", done, total);
            }
            catch { }
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
                _manifestPath = await Task.Run(() => ManifestGenerator.Generate(_txtDest.Text.Trim(), GetCheckedItems(), null, null, AskCaseInfo()));
                AppendLog(L.Format("S052", _manifestPath));
                _reportPath = await Task.Run(() => ReportGenerator.GenerateFromManifest(
                    Path.ChangeExtension(_manifestPath, ".json")));
                AppendLog(L.Format("S052", _reportPath));
                _btnReport.Enabled = true;
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

        private void BtnReport_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_reportPath) && File.Exists(_reportPath))
                    Process.Start(new ProcessStartInfo { FileName = _reportPath, UseShellExecute = true });
                else
                    _lblStatus.Text = L.Format("S055", _reportPath);
            }
            catch (Exception ex)
            {
                _lblStatus.Text = L.Format("S058", ex.Message);
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
