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
        private readonly Button _btnDeepSweep;
        private readonly Button _btnSweepFolder;
        private readonly Button _btnLabScript;
        private readonly Button _btnReport;
        private readonly Button _btnManifest;
        private readonly Button _btnOpen;
        private readonly Label _lblStatus;
        private readonly ProgressBar _progress;
        private readonly RadioButton _rdoVss;
        private readonly RadioButton _rdoStopStart;
        private readonly RadioButton _rdoReportOnly;
        private readonly CheckBox _chkVerifyCopy;
        private List<CopyAcquisition> _lastAcquisitions = new();
        private Control[] _planControls = Array.Empty<Control>();
        private bool[]? _planEnabled;
        private bool _caseAsked;
        private CaseInfo? _caseInfo;
        private string _manifestPath = "";
        private string _reportPath = "";
        private int _activeProgressLineStart = -1;
        private int _currentCompletedWork = 0;
        private System.Diagnostics.Stopwatch? _copyStopwatch;
        private long _grandTotalBytes;
        private long _previouslyCompletedBytes;
        private string _currentFileLabel = "";
        private long _lastFileTotalBytes;
        private int _currentTotalWork = 0;
        private readonly HashSet<DatabaseCopyItem> _checkedItems = new();
        private bool _updatingTreeChecks;
        private TextBox _txtSearch = null!;
        private Button _btnClearSearch = null!;
        private ComboBox _cmbSort = null!;
        private ComboBox _cmbGroup = null!;
        private ComboBox _cmbFilter = null!;
        private Label _lblFilterSummary = null!;
        private Panel _treeToolbar = null!;

        private CaseInfo? AskCaseInfo() => CaseInfoForm.AskOnce(this, ref _caseAsked, ref _caseInfo);

        public DatabaseCopyForm(List<DatabaseInfo> servers)
        {
            _servers = servers;

            Text = L.Text("S059");
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(760, 620);
            Font = UiFont.Make(10f);
            BackColor = Color.White;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(680, 520);

            var lblTitle = new Label
            {
                Text = L.Text("S060"),
                Font = UiFont.Make(13f, FontStyle.Bold),
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
                ShowNodeToolTips = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Font = UiFont.Make(9.5f)
            };
            _tree.AfterCheck += Tree_AfterCheck;
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

            _btnDeepSweep = new Button
            {
                Text = L.Text("S365"),
                Location = new Point(500, 172),
                Size = new Size(248, 30),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnDeepSweep.Click += BtnDeepSweep_Click;
            Controls.Add(_btnDeepSweep);

            _btnSweepFolder = new Button
            {
                Text = L.Text("S427"),
                Location = new Point(500, 172),
                Size = new Size(248, 30),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnSweepFolder.Click += BtnSweepFolder_Click;
            Controls.Add(_btnSweepFolder);

            _btnLabScript = new Button
            {
                Text = L.Text("S369"),
                Location = new Point(500, 172),
                Size = new Size(248, 30),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnLabScript.Click += BtnLabScript_Click;
            Controls.Add(_btnLabScript);

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

            _btnReport = new Button
            {
                Text = L.Text("S386"),
                Location = new Point(12, 614),
                Size = new Size(150, 36),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnReport.Click += BtnReport_Click;
            Controls.Add(_btnReport);

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
                Font = UiFont.Make(8.8f)
            };
            grpLocked.Controls.Add(_rdoVss);

            var lblVssHint = new Label
            {
                Text = L.Text("S066"),
                Location = new Point(28, 58),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = UiFont.Make(7.8f)
            };
            grpLocked.Controls.Add(lblVssHint);

            _rdoStopStart = new RadioButton
            {
                Text = L.Text("S067"),
                Location = new Point(10, 86),
                Size = new Size(228, 34),
                Font = UiFont.Make(8.8f)
            };
            grpLocked.Controls.Add(_rdoStopStart);

            var lblStopHint = new Label
            {
                Text = L.Text("S068"),
                Location = new Point(28, 118),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = UiFont.Make(7.8f)
            };
            grpLocked.Controls.Add(lblStopHint);

            _rdoReportOnly = new RadioButton
            {
                Text = L.Text("S069"),
                Location = new Point(10, 148),
                Size = new Size(180, 24),
                Font = UiFont.Make(8.8f)
            };
            grpLocked.Controls.Add(_rdoReportOnly);

            _chkVerifyCopy = new CheckBox
            {
                Text = L.Text("S373"),
                Location = new Point(10, 178),
                Size = new Size(228, 34),
                Checked = true,
                Font = UiFont.Make(8.8f)
            };
            grpLocked.Controls.Add(_chkVerifyCopy);
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
                Font = UiFont.Make(10f, FontStyle.Bold),
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
            _progress = new ProgressBar { Minimum = 0, Maximum = 1, Value = 0, Style = ProgressBarStyle.Continuous };

            Load += DatabaseCopyForm_Load;
            _treeToolbar = BuildTreeToolbar();
            OperationLayout.Build(this,lblTitle,_txtDest,btnBrowse,null,_tree,_treeToolbar,new Control[]{btnSelectAll,btnClearAll,_btnManualPath,_btnFindPass,_btnSysadmin,_btnDeepSweep,_btnSweepFolder,_btnLabScript},grpLocked,lblLog,_txtLog,_lblStatus,_progress,_btnCopy,_btnManifest,_btnOpen,_btnReport);

            // حین کپی نباید چیزی از این کنترل‌ها تغییر کند: نخ کار روی همان آیتم‌ها
            // می‌خواند و مسیر دستی/انتخاب‌ها وسط کار عوض می‌شوند
            _planControls = new Control[]
            {
                btnSelectAll, btnClearAll, _btnManualPath, _btnFindPass, _btnSysadmin, _btnDeepSweep, _btnSweepFolder,
                _btnLabScript, btnBrowse, _txtDest, _tree, grpLocked, _txtSearch, _btnClearSearch, _cmbSort, _cmbGroup, _cmbFilter
            };
        }

        /// <summary>قفل کنترل‌های برنامه‌ریزی هنگام اجرای مأموریت.</summary>
        private void SetPlanBusy(bool busy)
        {
            if (busy)
            {
                _planEnabled ??= _planControls.Select(c => c.Enabled).ToArray();
                foreach (var c in _planControls)
                {
                    if (!c.IsDisposed) c.Enabled = false;
                }
                return;
            }
            if (_planEnabled == null) return;
            for (var i = 0; i < _planControls.Length && i < _planEnabled.Length; i++)
            {
                if (!_planControls[i].IsDisposed) _planControls[i].Enabled = _planEnabled[i];
            }
            _planEnabled = null;
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
                L.Text("S432"), // حجم (بیشترین اول)
                L.Text("S433"), // حجم (کمترین اول)
                L.Text("S434"), // نام (الف تا ی)
                L.Text("S435"), // نام (ی تا الف)
                L.Text("S436"), // تاریخ تغییر (جدیدترین)
                L.Text("S437")  // وضعیت (در حال استفاده اول)
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
                L.Text("S439"), // دسته‌بندی: حجم
                L.Text("S440"), // دسته‌بندی: وضعیت
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
                L.Text("S443"), // فقط در حال استفاده 🔒
                L.Text("S444"), // فقط آزاد و آماده کپی
                L.Text("S445"), // فقط حجیم (> ۱ گیگابایت)
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
                if (e.Node.Tag is DatabaseCopyItem item)
                {
                    if (e.Node.Checked) _checkedItems.Add(item);
                    else _checkedItems.Remove(item);

                    foreach (TreeNode fn in e.Node.Nodes)
                        fn.Checked = e.Node.Checked;

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
                else if (e.Node.Tag is FileCopyItem && e.Node.Parent?.Tag is DatabaseCopyItem parentItem)
                {
                    bool anyChecked = false;
                    foreach (TreeNode fn in e.Node.Parent.Nodes)
                    {
                        if (fn.Checked) { anyChecked = true; break; }
                    }
                    e.Node.Parent.Checked = anyChecked;
                    if (anyChecked) _checkedItems.Add(parentItem);
                    else _checkedItems.Remove(parentItem);

                    if (e.Node.Parent.Parent != null)
                    {
                        bool groupAll = true;
                        foreach (TreeNode sibling in e.Node.Parent.Parent.Nodes)
                        {
                            if (!sibling.Checked) { groupAll = false; break; }
                        }
                        e.Node.Parent.Parent.Checked = groupAll;
                    }
                }
                else
                {
                    // Group node
                    foreach (TreeNode child in e.Node.Nodes)
                    {
                        child.Checked = e.Node.Checked;
                        foreach (TreeNode fn in child.Nodes) fn.Checked = e.Node.Checked;
                        if (child.Tag is DatabaseCopyItem childItem)
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

        private async void DatabaseCopyForm_Load(object? sender, EventArgs e)
        {
            _btnCopy.Enabled = false;
            _lblStatus.Text = L.Text("S072");
            AppendLog(L.Text("S073"));

            var result = await Task.Run(() =>
                DatabaseFileLocator.BuildPlan(_servers, message => AppendLogSafe(message)));

            _items = result;
            _checkedItems.Clear();
            foreach (var it in _items) _checkedItems.Add(it);

            FillTree();

            _btnCopy.Enabled = true;
            _lblStatus.Text = L.Format("S077", _items.Count);

            await OfferDeepSweepAsync();
        }

        private void FillTree()
        {
            _tree.Nodes.Clear();
            if (_items.Count == 0)
            {
                UpdateFilterSummary(0, 0, 0, 0);
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
                    (i.FolderName != null && i.FolderName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    i.Files.Any(f => f.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            // 2. فیلتر وضعیت یا حجم
            var filterIndex = _cmbFilter?.SelectedIndex ?? 0;
            query = filterIndex switch
            {
                1 => query.Where(i => i.Files.Any(f => f.IsInUse)),
                2 => query.Where(i => !i.Files.Any(f => f.IsInUse)),
                3 => query.Where(i => i.TotalSize >= 1024L * 1024 * 1024),
                4 => query.Where(i => !string.IsNullOrEmpty(i.Error)),
                _ => query
            };

            var filteredList = query.ToList();

            // 3. مرتب‌سازی (SORT BY)
            var sortIndex = _cmbSort?.SelectedIndex ?? 0;
            IOrderedEnumerable<DatabaseCopyItem> sorted = sortIndex switch
            {
                0 => filteredList.OrderByDescending(i => i.TotalSize).ThenBy(i => i.DatabaseName),
                1 => filteredList.OrderBy(i => i.TotalSize).ThenBy(i => i.DatabaseName),
                2 => filteredList.OrderBy(i => i.DatabaseName, StringComparer.OrdinalIgnoreCase),
                3 => filteredList.OrderByDescending(i => i.DatabaseName, StringComparer.OrdinalIgnoreCase),
                4 => filteredList.OrderByDescending(i => i.Files.Select(f => f.Modified).DefaultIfEmpty().Max()),
                5 => filteredList.OrderByDescending(i => i.Files.Any(f => f.IsInUse)).ThenByDescending(i => i.TotalSize),
                _ => filteredList.OrderByDescending(i => i.TotalSize)
            };

            var orderedItems = sorted.ToList();

            // 4. دسته‌بندی (GROUP BY)
            var groupIndex = _cmbGroup?.SelectedIndex ?? 0;
            var groups = groupIndex switch
            {
                1 => orderedItems.GroupBy(i =>
                    i.TotalSize >= 1024L * 1024 * 1024 ? L.Text("S449") :
                    i.TotalSize >= 100L * 1024 * 1024 ? L.Text("S450") : L.Text("S451")),
                2 => orderedItems.GroupBy(i =>
                    !string.IsNullOrEmpty(i.Error) ? L.Text("S454") :
                    i.Files.Any(f => f.IsInUse) ? L.Text("S452") : L.Text("S453")),
                3 => orderedItems.GroupBy(_ => L.Text("S442")),
                _ => orderedItems.GroupBy(i => i.Server.IsOnline
                    ? $"{i.Server.TypeDisplayName}@{i.Server.Host}:{i.Server.Port}"
                    : L.Format("S074", i.Server.FormatName))
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
                        var node = CreateDatabaseNode(item);
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

            foreach (TreeNode group in _tree.Nodes)
            {
                group.Expand();
            }
            UpdateFilterSummary(filteredList.Count, _items.Count, filteredList.Sum(i => i.TotalSize), _checkedItems.Count);
        }

        private void UpdateFilterSummary(int visibleCount, int totalCount, long visibleBytes, int selectedCount)
        {
            if (_lblFilterSummary == null || _lblFilterSummary.IsDisposed) return;
            _lblFilterSummary.Text = L.Format("S448", visibleCount, totalCount, DatabaseFileLocator.FormatSize(visibleBytes), selectedCount);
        }

        private void UpdateFilterSummary()
        {
            var visibleItems = GetVisibleItems();
            UpdateFilterSummary(visibleItems.Count, _items.Count, visibleItems.Sum(i => i.TotalSize), _checkedItems.Count);
        }

        private List<DatabaseCopyItem> GetVisibleItems()
        {
            var list = new List<DatabaseCopyItem>();
            foreach (TreeNode group in _tree.Nodes)
            {
                foreach (TreeNode child in group.Nodes)
                {
                    if (child.Tag is DatabaseCopyItem item) list.Add(item);
                }
            }
            return list;
        }

        private TreeNode CreateDatabaseNode(DatabaseCopyItem item)
        {
            var sizeText = item.Files.Count > 0
                ? L.Format("S075", item.Files.Count, DatabaseFileLocator.FormatSize(item.TotalSize))
                : "";
            var locationText = !item.Server.IsOnline && item.Server.LocalPath != null
                ? "  [" + System.IO.Path.GetDirectoryName(item.Server.LocalPath) + "]"
                : "";
            var node = new TreeNode($"{item.DatabaseName}{sizeText}{locationText}")
            {
                Tag = item
            };
            if (item.Files.Count > 0)
            {
                var liveBy = item.Files.Where(f => f.IsInUse)
                    .Select(f => f.InUseBy).Distinct().ToList();
                if (liveBy.Count > 0)
                    node.Text += "  🔒 " + (liveBy.Count == 1 && !string.IsNullOrEmpty(liveBy[0])
                        ? liveBy[0] : L.Text("S367"));
                var newest = item.Files.Select(f => f.Modified).Max();
                if (newest != default)
                    node.Text += "  •  " + newest.ToString("yyyy-MM-dd HH:mm");
                node.ToolTipText = string.Join("\n", item.Files.Select(f =>
                    $"{f.DisplayName}  •  {f.Modified:yyyy-MM-dd HH:mm}  •  " +
                    (f.IsInUse ? "🔒 " + (f.InUseBy ?? L.Text("S367")) : L.Text("S368"))));

                foreach (var file in item.Files)
                {
                    var ext = System.IO.Path.GetExtension(file.SourcePath ?? file.DisplayName).ToLowerInvariant();
                    var role = ext switch
                    {
                        ".mdf" => L.Text("S461"),
                        ".ndf" => L.Text("S462"),
                        ".ldf" => L.Text("S463"),
                        _ => L.Text("S464")
                    };
                    var lockBadge = file.IsInUse ? "  🔒" : "";
                    var fileNodeText = $"{file.DisplayName}  ({DatabaseFileLocator.FormatSize(file.Size)})  [{role}]{lockBadge}";
                    var fileNode = new TreeNode(fileNodeText)
                    {
                        Tag = file,
                        ToolTipText = file.SourcePath ?? file.DisplayName
                    };
                    node.Nodes.Add(fileNode);
                }
            }
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
            return node;
        }

        /// <summary>
        /// اگر برای SQL Server آنلاینی هیچ فایلی پیدا نشده باشد، به‌جای
        /// توقف با خطا، اجازه جست‌وجوی عمیق درایوها را از کاربر می‌گیرد.
        /// </summary>
        private async Task OfferDeepSweepAsync()
        {
            List<DatabaseInfo> need;
            try
            {
                need = DatabaseFileLocator.SqlServersNeedingSweep(_servers, _items);
            }
            catch { return; }

            if (need.Count == 0) return;

            if (MessageBox.Show(L.Text("S362"), Text,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1) != DialogResult.Yes)
                return;

            _btnCopy.Enabled = false;
            _lblStatus.Text = L.Format("S363", 0, 0);
            try
            {
                foreach (var s in need)
                {
                    var found = await Task.Run(() =>
                        DatabaseFileLocator.SweepSqlServerFiles(s, m => AppendLogSafe(m)));
                    // موارد خطای قبلی همان سرور را حذف کن تا درخت تمیز بماند
                    _items.RemoveAll(i => i.Server.Type == DatabaseType.SQLServer
                        && string.Equals(i.Server.Host, s.Host, StringComparison.OrdinalIgnoreCase)
                        && (i.Server.Port ?? 0) == (s.Port ?? 0)
                        && i.Files.Count == 0);
                    _items.AddRange(found);
                    foreach (var it in found) _checkedItems.Add(it);
                }

                FillTree();
                _tree.ExpandAll();
                _lblStatus.Text = L.Format("S077", _items.Count);
            }
            catch (Exception ex)
            {
                AppendLog(L.Format("S055", ex.Message));
            }
            finally
            {
                _btnCopy.Enabled = true;
            }
        }

        /// <summary>
        /// جست‌وجوی عمیق دستی: حتی اگر در مسیرهای معمول چیزی پیدا شده باشد،
        /// کل درایوها گشته می‌شود (مثلا دیتای سپیدار در D:\sepidar) و موارد
        /// جدید بدون تکرار به درخت اضافه می‌شوند.
        /// </summary>
        private async void BtnDeepSweep_Click(object? sender, EventArgs e)
        {
            var targets = _servers
                .Where(s => s.Type == DatabaseType.SQLServer && s.IsOnline)
                .ToList();
            if (targets.Count == 0)
            {
                _lblStatus.Text = L.Text("S085");
                return;
            }

            _btnDeepSweep.Enabled = false;
            _btnSweepFolder.Enabled = false;
            _btnCopy.Enabled = false;
            _lblStatus.Text = L.Format("S363", 0, 0);
            try
            {
                var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var it in _items)
                    foreach (var f in it.Files)
                        known.Add(f.SourcePath);

                var added = 0;
                foreach (var s in targets)
                {
                    var found = await Task.Run(() =>
                        DatabaseFileLocator.SweepSqlServerFiles(s, m => AppendLogSafe(m)));
                    foreach (var item in found)
                    {
                        item.Files.RemoveAll(f => known.Contains(f.SourcePath));
                        if (item.Files.Count == 0) continue;
                        foreach (var f in item.Files) known.Add(f.SourcePath);
                        _items.Add(item);
                        _checkedItems.Add(item);
                        added++;
                    }
                }

                FillTree();
                _tree.ExpandAll();
                _lblStatus.Text = L.Format("S077", _items.Count);
                if (added == 0) AppendLog(L.Text("S366"));
            }
            catch (Exception ex)
            {
                AppendLog(L.Format("S055", ex.Message));
            }
            finally
            {
                _btnDeepSweep.Enabled = true;
                _btnSweepFolder.Enabled = true;
                _btnCopy.Enabled = true;
            }
        }

        /// <summary>
        /// جست‌وجوی دیتای SQL در یک مسیر انتخابی خاص (بدون جست‌وجوی همه درایوها).
        /// </summary>
        private async void BtnSweepFolder_Click(object? sender, EventArgs e)
        {
            var targets = _servers
                .Where(s => s.Type == DatabaseType.SQLServer && s.IsOnline)
                .ToList();
            if (targets.Count == 0)
            {
                _lblStatus.Text = L.Text("S085");
                return;
            }

            using var fbd = new FolderBrowserDialog
            {
                Description = L.Text("S428")
            };
            var start = GuessManualBrowseStart(_items.FirstOrDefault(i => i.Server.Type == DatabaseType.SQLServer) ?? new DatabaseCopyItem(), _txtDest.Text);
            if (start != null && Directory.Exists(start)) fbd.SelectedPath = start;

            if (fbd.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(fbd.SelectedPath) || !Directory.Exists(fbd.SelectedPath))
                return;

            var chosenPath = fbd.SelectedPath;

            _btnDeepSweep.Enabled = false;
            _btnSweepFolder.Enabled = false;
            _btnCopy.Enabled = false;
            _lblStatus.Text = L.Format("S363", 0, 0);
            try
            {
                var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var it in _items)
                    foreach (var f in it.Files)
                        known.Add(f.SourcePath);

                var added = 0;
                foreach (var s in targets)
                {
                    var found = await Task.Run(() =>
                        DatabaseFileLocator.SweepSqlServerFolder(s, chosenPath, m => AppendLogSafe(m)));
                    foreach (var item in found)
                    {
                        item.Files.RemoveAll(f => known.Contains(f.SourcePath));
                        if (item.Files.Count == 0) continue;
                        foreach (var f in item.Files) known.Add(f.SourcePath);
                        _items.Add(item);
                        _checkedItems.Add(item);
                        added++;
                    }
                }

                FillTree();
                _tree.ExpandAll();
                _lblStatus.Text = L.Format("S077", _items.Count);
                if (added == 0) AppendLog(L.Text("S366"));
            }
            catch (Exception ex)
            {
                AppendLog(L.Format("S055", ex.Message));
            }
            finally
            {
                _btnDeepSweep.Enabled = true;
                _btnSweepFolder.Enabled = true;
                _btnCopy.Enabled = true;
            }
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
            if (_activeProgressLineStart >= 0 && _activeProgressLineStart <= _txtLog.TextLength)
            {
                _txtLog.Select(_activeProgressLineStart, _txtLog.TextLength - _activeProgressLineStart);
                _txtLog.SelectedText = message + Environment.NewLine;
                _activeProgressLineStart = -1;
            }
            else
            {
                _activeProgressLineStart = -1;
                _txtLog.AppendText(message + Environment.NewLine);
            }
            _txtLog.SelectionStart = _txtLog.TextLength;
            _txtLog.ScrollToCaret();
        }

        private void ReportFileProgress(string fileLabel, long copied, long total)
        {
            try
            {
                if (_txtLog.IsDisposed) return;
                if (_txtLog.InvokeRequired)
                {
                    _txtLog.BeginInvoke(new Action(() => ReportFileProgress(fileLabel, copied, total)));
                    return;
                }

                if (!string.Equals(fileLabel, _currentFileLabel, StringComparison.OrdinalIgnoreCase))
                {
                    if (_lastFileTotalBytes > 0)
                        _previouslyCompletedBytes += _lastFileTotalBytes;
                    _currentFileLabel = fileLabel;
                    _lastFileTotalBytes = total;
                }

                var pct = total > 0 ? (int)Math.Min(100, (copied * 100) / total) : (copied > 0 ? 100 : 0);
                var sizeStr = $"{DatabaseFileLocator.FormatSize(copied)} / {DatabaseFileLocator.FormatSize(total)}";

                string speedStr = "";
                string etaStr = "";
                if (_copyStopwatch != null && _copyStopwatch.Elapsed.TotalSeconds > 0.5)
                {
                    long totalCopied = Math.Max(0, _previouslyCompletedBytes + copied);
                    double sec = _copyStopwatch.Elapsed.TotalSeconds;
                    double speed = totalCopied / sec;
                    if (speed > 1024)
                    {
                        speedStr = DatabaseFileLocator.FormatSize((long)speed) + "/s";
                        if (_grandTotalBytes > totalCopied)
                        {
                            double remainSec = (_grandTotalBytes - totalCopied) / speed;
                            if (remainSec > 0 && remainSec < 86400)
                            {
                                var ts = TimeSpan.FromSeconds(remainSec);
                                var formattedTs = ts.TotalHours >= 1
                                    ? $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                                    : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
                                etaStr = L.Format("S470", formattedTs);
                            }
                        }
                    }
                }

                string progressText;
                if (!string.IsNullOrEmpty(speedStr) && !string.IsNullOrEmpty(etaStr))
                {
                    progressText = L.Format("S468", fileLabel, pct, sizeStr, speedStr, etaStr);
                }
                else
                {
                    progressText = L.Format("S429", fileLabel, pct, sizeStr);
                }

                UpdateActiveLogLine(progressText);

                if (_currentTotalWork > 0)
                {
                    string statusExtra = !string.IsNullOrEmpty(speedStr) ? $" • {speedStr}" : "";
                    if (!string.IsNullOrEmpty(etaStr)) statusExtra += $" • {etaStr}";
                    _lblStatus.Text = $"{L.Format("S412", Math.Min(_currentTotalWork, _currentCompletedWork + 1), _currentTotalWork)} ({pct}%){statusExtra}";
                }

                if (_grandTotalBytes > 0)
                {
                    ulong currentBytes = (ulong)Math.Min(_grandTotalBytes, _previouslyCompletedBytes + copied);
                    TaskbarProgress.SetValue(this.Handle, currentBytes, (ulong)_grandTotalBytes);
                }
                else
                {
                    TaskbarProgress.SetValue(this.Handle, (ulong)Math.Min(_currentTotalWork, _currentCompletedWork), (ulong)Math.Max(1, _currentTotalWork));
                }
            }
            catch { }
        }

        private void UpdateActiveLogLine(string text)
        {
            if (_activeProgressLineStart < 0 || _activeProgressLineStart > _txtLog.TextLength)
            {
                _activeProgressLineStart = _txtLog.TextLength;
                _txtLog.AppendText(text);
            }
            else
            {
                _txtLog.Select(_activeProgressLineStart, _txtLog.TextLength - _activeProgressLineStart);
                _txtLog.SelectedText = text;
            }
            _txtLog.SelectionStart = _txtLog.TextLength;
            _txtLog.ScrollToCaret();
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
                        foreach (TreeNode fn in child.Nodes) fn.Checked = check;
                        if (child.Tag is DatabaseCopyItem item)
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

        private List<DatabaseCopyItem> GetCheckedItems()
        {
            var list = new List<DatabaseCopyItem>();
            foreach (TreeNode group in _tree.Nodes)
            {
                foreach (TreeNode dbNode in group.Nodes)
                {
                    if (dbNode.Tag is DatabaseCopyItem item && _checkedItems.Contains(item))
                    {
                        var checkedFiles = new List<FileCopyItem>();
                        foreach (TreeNode fn in dbNode.Nodes)
                        {
                            if (fn.Tag is FileCopyItem fi && fn.Checked)
                                checkedFiles.Add(fi);
                        }
                        if (checkedFiles.Count > 0 && checkedFiles.Count < item.Files.Count)
                        {
                            var clone = new DatabaseCopyItem
                            {
                                Server = item.Server,
                                DatabaseName = item.DatabaseName,
                                FolderName = item.FolderName,
                                Files = checkedFiles,
                                UseManualPath = item.UseManualPath,
                                ManualPath = item.ManualPath,
                                Error = item.Error
                            };
                            list.Add(clone);
                        }
                        else
                        {
                            list.Add(item);
                        }
                    }
                }
            }
            return list.Count > 0 ? list : _items.Where(i => _checkedItems.Contains(i)).ToList();
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

            var target = item.Value.Item2;

            // اگر فایل خودکار پیدا شده، با تایید کاربر اجازه بازنویسی بده
            // (مسیر خودکار در حالت بدون لاگین ممکن است ناقص باشد).
            // اگر قبلاً مسیر دستی دارد، این فقط تعویض پوشه است و نباید دوباره پرسیده شود.
            if (!target.UseManualPath && target.Files.Count > 0 && string.IsNullOrEmpty(target.Error))
            {
                if (MessageBox.Show(L.Format("S361", target.DatabaseName), L.Text("S062"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                    return;
            }

            using var fbd = new FolderBrowserDialog
            {
                Description = L.Format("S082", target.DatabaseName)
            };
            // مرورگر باید از جایی باز شود که فایل‌های دیتابیس آنجاست، نه از پوشه‌ی مقصد
            var start = GuessManualBrowseStart(target, _txtDest.Text);
            if (start != null) fbd.SelectedPath = start;

            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                // یک پوشه برای چند دیتابیس: داده دو بار کپی و دو بار در مانیفست ثبت می‌شود
                var sharedWith = CountSharingManualPath(target, fbd.SelectedPath);
                if (sharedWith > 0 && MessageBox.Show(L.Format("S426", sharedWith), L.Text("S062"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                    return;

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

        /// <summary>نرمال‌سازی مسیر پوشه برای مقایسه (بدون اسلش انتهایی).</summary>
        private static string NormalizeDir(string path) =>
            (path ?? "").Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        /// <summary>چند دیتابیس دیگر همین پوشه را به‌عنوان مسیر دستی دارند؟</summary>
        private int CountSharingManualPath(DatabaseCopyItem target, string path) =>
            _items.Count(i => !ReferenceEquals(i, target) && i.UseManualPath &&
                string.Equals(NormalizeDir(i.ManualPath), NormalizeDir(path), StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// نقطه‌ی شروع مرورگر پوشه: مسیر دستی فعلی، وگرنه پوشه‌ی فایل‌های پیدا‌شده،
        /// وگرنه مسیر محلی فایل آفلاین، وگرنه مقصد (اگر وجود داشته باشد).
        /// </summary>
        private static string? GuessManualBrowseStart(DatabaseCopyItem item, string fallback)
        {
            if (item.UseManualPath && Directory.Exists(item.ManualPath))
                return item.ManualPath;

            var known = item.Files.FirstOrDefault(f => !string.IsNullOrEmpty(f.SourcePath))?.SourcePath;
            if (!string.IsNullOrEmpty(known))
            {
                var dir = Path.GetDirectoryName(known);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir)) return dir;
            }

            if (!string.IsNullOrEmpty(item.Server.LocalPath))
            {
                var dir = Path.GetDirectoryName(item.Server.LocalPath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir)) return dir;
            }

            if (!string.IsNullOrWhiteSpace(fallback) && Directory.Exists(fallback)) return fallback;

            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Directory.Exists(docs) ? docs : null;
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

            // پیش‌برآورد فضا: وسط ماموریت با خطای دیسک مواجه نشویم
            // (برای مسیر دستی باید اندازه‌ی واقعی پوشه شمرده شود، نه Files که خالی است)
            try
            {
                _lblStatus.Text = L.Text("S425");
                var need = await Task.Run(() => DatabaseFileLocator.EstimateRequiredBytes(items));
                var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(destRoot)) ?? destRoot);
                if (need > 0 && drive.IsReady && need > drive.AvailableFreeSpace)
                {
                    _lblStatus.Text = L.Format("S374", drive.Name,
                        DatabaseFileLocator.FormatSize(need),
                        DatabaseFileLocator.FormatSize(drive.AvailableFreeSpace));
                    _lblStatus.ForeColor = Color.FromArgb(211, 47, 47);
                    return;
                }
            }
            catch { }

            // FAT32 4GB limit pre-flight check
            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(destRoot)) ?? destRoot);
                if (drive.IsReady && drive.DriveFormat.Equals("FAT32", StringComparison.OrdinalIgnoreCase))
                {
                    var over4Gb = items.SelectMany(i => i.Files).FirstOrDefault(f => f.Size > 4294967295L);
                    if (over4Gb != null)
                    {
                        var msg = L.Format("S467", drive.Name, over4Gb.DisplayName, DatabaseFileLocator.FormatSize(over4Gb.Size));
                        if (MessageBox.Show(this, msg, L.Text("S466"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                        {
                            return;
                        }
                    }
                }
            }
            catch { }

            _btnCopy.Enabled = false;
            _btnCopy.Text = L.Text("S086");
            _btnManifest.Enabled = false;
            _btnOpen.Enabled = false;
            _btnReport.Enabled = false;
            SetPlanBusy(true);
            _progress.Value = 0;
            _progress.Maximum = Math.Max(1, items.Sum(i => Math.Max(1, i.Files.Count)));
            _reportPath = "";
            _activeProgressLineStart = -1;
            _currentCompletedWork = 0;
            _currentTotalWork = _progress.Maximum;
            _grandTotalBytes = items.Sum(i => i.TotalSize);
            _previouslyCompletedBytes = 0;
            _lastFileTotalBytes = 0;
            _currentFileLabel = "";
            _copyStopwatch = System.Diagnostics.Stopwatch.StartNew();
            TaskbarProgress.SetState(this.Handle, TaskbarProgressState.Normal);
            TaskbarProgress.SetValue(this.Handle, 0, (ulong)Math.Max(1, _currentTotalWork));
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
                var verify = _chkVerifyCopy.Checked;
                var result = await Task.Run(() =>
                    DatabaseFileLocator.ExecuteCopy(items, destRoot, lockedHandling, message => AppendLogSafe(message), verify, (done, total) => ReportProgress(done, total), (fileLabel, copied, total) => ReportFileProgress(fileLabel, copied, total)));

                AppendLog("");
                AppendLog("--------------------------");
                AppendLog(L.Format("S088", result.FilesCopied, DatabaseFileLocator.FormatSize(result.BytesCopied), result.Failed));
                if (_copyStopwatch != null)
                {
                    _copyStopwatch.Stop();
                    var ts = _copyStopwatch.Elapsed;
                    var timeStr = ts.TotalHours >= 1
                        ? $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                        : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
                    double avgSpeed = ts.TotalSeconds > 0 ? result.BytesCopied / ts.TotalSeconds : 0;
                    AppendLog(L.Format("S469", timeStr, DatabaseFileLocator.FormatSize((long)avgSpeed) + "/s"));
                }
                if (result.Mismatched > 0)
                    AppendLog(L.Format("S375", result.Mismatched));

                _lastAcquisitions = result.Acquisitions;
                if (result.FilesCopied > 0)
                {
                    AppendLog(L.Text("S051"));
                    var ci = AskCaseInfo();
                    _manifestPath = await Task.Run(() => ManifestGenerator.Generate(destRoot, null, null, result.Acquisitions, ci));
                    AppendLog(L.Format("S052", _manifestPath));
                    _reportPath = await Task.Run(() => ReportGenerator.GenerateFromManifest(
                        Path.ChangeExtension(_manifestPath, ".json")));
                    AppendLog(L.Format("S052", _reportPath));
                    _btnManifest.Enabled = true;
                    _btnOpen.Enabled = true;
                    _btnReport.Enabled = true;
                }

                _lblStatus.Text = L.Format("S413", result.FilesCopied, result.Failed,
                    DatabaseFileLocator.FormatSize(result.BytesCopied));
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
                SetPlanBusy(false);
                _activeProgressLineStart = -1;
                _btnCopy.Enabled = true;
                _btnCopy.Text = L.Text("S071");
                _progress.Value = _progress.Maximum;
                TaskbarProgress.SetState(this.Handle, TaskbarProgressState.NoProgress);
            }
        }

        private void ReportProgress(int done, int total)
        {
            try
            {
                if (_progress.IsDisposed) return;
                if (_progress.InvokeRequired) { _progress.BeginInvoke(new Action(() => ReportProgress(done, total))); return; }
                _currentCompletedWork = done;
                _currentTotalWork = total;
                _progress.Maximum = Math.Max(1, total);
                _progress.Value = Math.Min(Math.Max(0, done), _progress.Maximum);
                _lblStatus.Text = L.Format("S412", done, total);
            }
            catch { }
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

                    // حالت B: پیشنهاد ریست sa روی همین سیستم زنده
                    if (MessageBox.Show(L.Text("S370"), L.Text("S358"), MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                    {
                        var pwd = SysadminGranter.NewStrongPassword();
                        var rerr = await Task.Run(() => SysadminGranter.ResetSaPasswordOnSite(
                            svc, pwd, m => AppendLogSafe(m)));
                        if (rerr == null)
                        {
                            AppendLog(L.Format("S371", pwd));
                            MessageBox.Show(L.Format("S371", pwd), L.Text("S358"),
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else AppendLog(L.Format("S055", rerr));
                    }
                }
                else
                {
                    AppendLog(L.Format("S055", err));
                    _lblStatus.Text = L.Format("S055", err);
                }
            }
            finally { _btnSysadmin.Enabled = true; }
        }

        /// <summary>
        /// حالت A (تمیز): ساخت اسکریپت ریست sa برای اجرای روی کپی آزمایشگاهی.
        /// روی سیستم مودی هیچ تغییری ایجاد نمی‌کند.
        /// </summary>
        private void BtnLabScript_Click(object? sender, EventArgs e)
        {
            try
            {
                var sql = _servers.FirstOrDefault(s => s.Type == DatabaseType.SQLServer);
                var svc = sql?.ServiceName ?? "MSSQLSERVER";
                var pwd = SysadminGranter.NewStrongPassword();
                var dest = _txtDest.Text.Trim();
                if (string.IsNullOrEmpty(dest)) dest = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DatabaseFilesBackup");
                var bat = SysadminGranter.GenerateLabResetScript(dest, svc, pwd);
                AppendLog(L.Format("S052", bat));
                AppendLog(L.Format("S371", pwd));
                MessageBox.Show(bat + "\n\n" + L.Format("S371", pwd), L.Text("S369"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { AppendLog(L.Format("S055", ex.Message)); }
        }

        private async void BtnManifest_Click(object? sender, EventArgs e)
        {
            _btnManifest.Enabled = false;
            AppendLog(L.Text("S051"));
            try
            {
                _manifestPath = await Task.Run(() => ManifestGenerator.Generate(
                    _txtDest.Text.Trim(), null, null, _lastAcquisitions, AskCaseInfo()));
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
