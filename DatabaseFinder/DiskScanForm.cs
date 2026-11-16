using System.Data;
using System.Diagnostics;

namespace DatabaseFinder
{
    public partial class DiskScanForm : AppForm
    {
        public List<DatabaseInfo> Found { get; private set; } = new();

        private readonly CheckedListBox _lstRoots;
        private readonly CheckedListBox _lstFormats;
        private readonly NumericUpDown _numMinSize;
        private readonly CheckBox _chkDeepCarving;
        private readonly CheckBox _chkArchivePeeking;
        private readonly RadioButton _rbQuick;
        private readonly RadioButton _rbFull;
        private readonly RadioButton _rbExternal;
        private readonly RadioButton _rbShadow;
        private readonly Button _btnRefreshRoots;
        private readonly Button _btnCreateShadow;
        private readonly Button _btnScan;
        private readonly Button _btnCancelScan;
        private readonly Button _btnCopy;
        private readonly Button _btnMerge;
        private readonly ProgressBar _bar;
        private readonly Label _lblStatus;
        private readonly DataGridView _grid;
        private ContextMenuStrip? _ctxResults;

        private List<DatabaseInfo> _allResults = new();
        private readonly HashSet<string> _selectedPaths = new(StringComparer.OrdinalIgnoreCase);
        private bool _isUpdatingGridRows;

        private TextBox _txtFilter = null!;
        private Button _btnClearFilter = null!;
        private ComboBox _cmbFilterType = null!;
        private ComboBox _cmbFilterDate = null!;
        private Button _btnSmartSelect = null!;
        private ContextMenuStrip _menuSmartSelect = null!;
        private Label _lblSelectionSummary = null!;
        private TableLayoutPanel _toolbar = null!;

        private CancellationTokenSource? _cts;

        public DiskScanForm()
        {
            Text = L.Text("S157");
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(900, 700);
            Font = UiFont.Make(10f);
            BackColor = Color.White;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(820, 580);

            var lblTitle = new Label
            {
                Text = L.Text("S158"),
                Font = UiFont.Make(13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                AutoSize = true,
                Location = new Point(12, 8)
            };
            Controls.Add(lblTitle);

            // ---- ناحیه ریشه‌ها ----
            var grpRoots = new GroupBox
            {
                Text = L.Text("S159"),
                Location = new Point(12, 42),
                Size = new Size(330, 230)
            };

            _rbQuick = new RadioButton
            {
                Text = L.Pick("⚡ اسکن هوشمند (مالی، دیتابیس و فلش)", "⚡ Smart scan (Accounting, DB & USB)"),
                Checked = true,
                Location = new Point(12, 16),
                AutoSize = true
            };
            _rbFull = new RadioButton
            {
                Text = L.Pick("🌐 اسکن کامل تمام درایوها (داخلی + شبکه)", "🌐 Full scan (All fixed & network drives)"),
                Location = new Point(12, 38),
                AutoSize = true
            };
            _rbExternal = new RadioButton
            {
                Text = L.Pick("🔌 فقط درایوهای اکسترنال و فلش (USB)", "🔌 External & USB drives only"),
                Location = new Point(12, 60),
                AutoSize = true
            };
            _rbShadow = new RadioButton
            {
                Text = L.Pick("🕰️ نسخه‌های قبلی (Shadow Copies)", "🕰️ Previous versions (Shadow Copies)"),
                Location = new Point(12, 82),
                AutoSize = true
            };
            _rbQuick.CheckedChanged += (s, e) => { if (_rbQuick.Checked) ReloadRoots(); };
            _rbFull.CheckedChanged += (s, e) => { if (_rbFull.Checked) ReloadRoots(); };
            _rbExternal.CheckedChanged += (s, e) => { if (_rbExternal.Checked) ReloadRoots(); };
            _rbShadow.CheckedChanged += (s, e) => { if (_rbShadow.Checked) ReloadRoots(); };

            _btnRefreshRoots = new Button
            {
                Text = L.Pick("🔄 بازخوانی", "🔄 Refresh"),
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(33, 33, 33),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnRefreshRoots.Click += (s, e) => ReloadRoots();

            _btnCreateShadow = new Button
            {
                Text = L.Pick("➕ ساخت VSS", "➕ New VSS"),
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(33, 33, 33),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnCreateShadow.Click += (s, e) => CreateNewShadowSnapshotDialog();

            FormClosed += (_, _) => ShadowCopyFinder.UnmountAll();

            _lstRoots = new CheckedListBox
            {
                Location = new Point(6, 48),
                Size = new Size(318, 118),
                CheckOnClick = true,
                Font = UiFont.Make(9f)
            };

            var btnAddRoot = new Button
            {
                Text = L.Text("S162"),
                Location = new Point(6, 172),
                Size = new Size(318, 26),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAddRoot.Click += BtnAddRoot_Click;

            var btnAllRoots = new Button
            {
                Text = L.Text("S034"),
                Location = new Point(6, 202),
                Size = new Size(154, 24),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAllRoots.Click += (s, e) => SetRootsChecked(true);

            var btnNoneRoots = new Button
            {
                Text = L.Text("S163"),
                Location = new Point(168, 202),
                Size = new Size(156, 24),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnNoneRoots.Click += (s, e) => SetRootsChecked(false);

            grpRoots.Controls.AddRange(new Control[] { _rbQuick, _rbFull, _rbExternal, _lstRoots, btnAddRoot, btnAllRoots, btnNoneRoots, _btnRefreshRoots });
            Controls.Add(grpRoots);

            // ---- ناحیه فرمت‌ها ----
            var grpFormats = new GroupBox
            {
                Text = L.Text("S164"),
                Location = new Point(352, 42),
                Size = new Size(250, 230)
            };

            _lstFormats = new CheckedListBox
            {
                Location = new Point(6, 22),
                Size = new Size(238, 160),
                CheckOnClick = true,
                Font = UiFont.Make(9f)
            };
            foreach (var f in DiskFormatRegistry.All)
                _lstFormats.Items.Add($"{f.Name}  (.{string.Join("/.", f.Extensions).TrimStart('.')})");

            var btnAllFmt = new Button
            {
                Text = L.Text("S034"),
                Location = new Point(6, 190),
                Size = new Size(116, 28),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAllFmt.Click += (s, e) => SetFormatsChecked(true);

            var btnNoneFmt = new Button
            {
                Text = L.Text("S163"),
                Location = new Point(128, 190),
                Size = new Size(116, 28),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnNoneFmt.Click += (s, e) => SetFormatsChecked(false);

            grpFormats.Controls.AddRange(new Control[] { _lstFormats, btnAllFmt, btnNoneFmt });
            Controls.Add(grpFormats);

            // ---- تنظیمات ----
            var grpOpt = new GroupBox
            {
                Text = L.Text("S165"),
                Location = new Point(612, 42),
                Size = new Size(276, 230)
            };

            var lblMin = new Label { Text = L.Text("S166"), Location = new Point(12, 28), AutoSize = true };
            _numMinSize = new NumericUpDown
            {
                Location = new Point(140, 24),
                Size = new Size(100, 27),
                Minimum = 0,
                Maximum = 1048576,
                Value = 3,
                Increment = 1
            };
            var lblMinUnit = new Label { Text = L.Text("S167"), Location = new Point(12, 56), AutoSize = true, ForeColor = Color.Gray };

            _chkDeepCarving = new CheckBox
            {
                Text = L.Pick("⚡ کشف فایل‌های تغییرنام‌یافته (Signature Carving)", "⚡ Carve Disguised / Hidden DBs"),
                Checked = true,
                AutoSize = true,
                Font = UiFont.Make(8.8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(230, 81, 0),
                Cursor = Cursors.Hand,
                Location = new Point(12, 85)
            };
            var tipCarving = new ToolTip();
            tipCarving.SetToolTip(_chkDeepCarving, L.Pick(
                "بررسی بایت‌های جادویی فایل‌های با پسوند نامتعارف یا بدون پسوند (مانند .tmp, .dat, .bin, .old) برای کشف پایگاه‌های داده مخفی‌شده.",
                "Inspect binary magic bytes of extensionless or suspicious files (.tmp, .dat, .bin, .old) to uncover disguised/hidden databases."));
            Disposed += (_, _) => tipCarving.Dispose();

            _chkArchivePeeking = new CheckBox
            {
                Text = L.Pick("📦 پویش درون فایل‌های فشرده (Archive Peeking بدون Extract)", "📦 Peek inside archives (ZIP/RAR/TAR without extract)"),
                Checked = true,
                AutoSize = true,
                Font = UiFont.Make(8.8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 121, 107),
                Cursor = Cursors.Hand,
                Location = new Point(12, 110)
            };
            var tipArchive = new ToolTip();
            tipArchive.SetToolTip(_chkArchivePeeking, L.Pick(
                "بازرسی جدول محتوای فایل‌های فشرده (ZIP, RAR, TAR, GZ, 7Z) در چند میلی‌ثانیه بدون استخراج روی دیسک، جهت یافتن بکاپ‌ها و دیتابیس‌های فشرده‌شده.",
                "Inspect table of contents of ZIP, RAR, TAR, GZ in milliseconds without extracting to disk to discover compressed databases."));
            Disposed += (_, _) => tipArchive.Dispose();

            var lblNote = new Label
            {
                Text = L.Text("S168"),
                Location = new Point(12, 138),
                Size = new Size(252, 75),
                ForeColor = Color.Gray,
                Font = UiFont.Make(8.5f)
            };

            grpOpt.Controls.AddRange(new Control[] { lblMin, _numMinSize, lblMinUnit, _chkDeepCarving, _chkArchivePeeking, lblNote });
            Controls.Add(grpOpt);

            // ---- دکمه‌ها ----
            _btnScan = new Button
            {
                Text = L.Text("S169"),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = UiFont.Make(11f, FontStyle.Bold),
                Location = new Point(12, 280),
                Size = new Size(150, 38)
            };
            _btnScan.Click += BtnScan_Click;
            Controls.Add(_btnScan);

            _btnCancelScan = new Button
            {
                Text = L.Text("S170"),
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(170, 280),
                Size = new Size(90, 38),
                Enabled = false
            };
            _btnCancelScan.Click += (s, e) => _cts?.Cancel();
            Controls.Add(_btnCancelScan);

            _lblStatus = new Label
            {
                AutoSize = true,
                Location = new Point(280, 292),
                ForeColor = Color.Gray
            };
            Controls.Add(_lblStatus);

            _bar = new ProgressBar
            {
                Location = new Point(12, 326),
                Size = new Size(876, 20),
                Style = ProgressBarStyle.Continuous,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_bar);

            // ---- نتایج ----
            var lblResults = new Label
            {
                Text = L.Text("S171"),
                Location = new Point(12, 356),
                AutoSize = true
            };
            Controls.Add(lblResults);

            var btnAllResults = new Button
            {
                Text = L.Text("S034"),
                Location = new Point(660, 350),
                Size = new Size(104, 26),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnAllResults.Click += (s, e) => SetResultsChecked(true);
            Controls.Add(btnAllResults);

            var btnNoneResults = new Button
            {
                Text = L.Text("S163"),
                Location = new Point(772, 350),
                Size = new Size(104, 26),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnNoneResults.Click += (s, e) => SetResultsChecked(false);
            Controls.Add(btnNoneResults);

            _grid = new DataGridView
            {
                Location = new Point(12, 380),
                Size = new Size(876, 240),
                ReadOnly = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            _grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = L.Text("S172"), Name = "sel", Width = 45 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L.Text("S173"), Name = "path", Width = 260, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L.Text("S174"), Name = "format", Width = 110, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L.Text("S175"), Name = "ext", Width = 90, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L.Text("S176"), Name = "name", Width = 110, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L.Text("S177"), Name = "size", Width = 80, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L.Text("S178"), Name = "date", Width = 100, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L.Text("S179"), Name = "backup", Width = 60, ReadOnly = true });

            _grid.CellMouseClick += Grid_CellMouseClick;
            _grid.CellDoubleClick += Grid_CellDoubleClick;
            _grid.CellValueChanged += Grid_CellValueChanged;
            _grid.SortCompare += Grid_SortCompare;
            _grid.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (_grid.IsCurrentCellDirty) _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            Controls.Add(_grid);

            _ctxResults = new ContextMenuStrip();
            _ctxResults.Items.Add(L.Text("S180"), null, (s, e) => RunPathAction(OpenFolder));
            _ctxResults.Items.Add(L.Text("S181"), null, (s, e) => RunPathAction(OpenFile));
            _ctxResults.Items.Add(L.Text("S182"), null, (s, e) => RunPathAction(CopyPath));
            _ctxResults.Items.Add(L.Text("S183"), null, (s, e) => RunPathAction(CopyFileToFolder));
            _ctxResults.Items.Add(new ToolStripSeparator());
            _ctxResults.Items.Add(L.Pick("🔍 شناسنامه و متادیتای درونی بکاپ...", "🔍 Inspect Backup Metadata..."), null, (s, e) => ShowBackupMetadataDialog());
            _ctxResults.Items.Add(new ToolStripSeparator());
            _ctxResults.Items.Add(L.Pick("انتخاب همه نسخه‌های این پایگاه داده", "Select all versions of this database"), null, (s, e) => SelectDatabaseFamily(true));
            _ctxResults.Items.Add(L.Pick("فقط آخرین نسخه این پایگاه داده", "Select only latest version of this database"), null, (s, e) => SelectDatabaseFamily(false));
            _ctxResults.Items.Add(new ToolStripSeparator());
            var mnuFixExt = new ToolStripMenuItem(L.Pick("⚠️ اصلاح و بازگردانی پسوند واقعی فایل...", "⚠️ Fix/Restore True File Extension..."), null, (s, e) => RestoreRealFileExtension());
            _ctxResults.Items.Add(mnuFixExt);
            var mnuExtractArchive = new ToolStripMenuItem(L.Pick("📦 استخراج این فایل از آرشیو به...", "📦 Extract This File From Archive..."), null, (s, e) => ExtractSelectedArchiveEntry());
            _ctxResults.Items.Add(mnuExtractArchive);
            var mnuSelectDups = new ToolStripMenuItem(L.Pick("👥 انتخاب تمام نسخه‌های یکسان این فایل", "👥 Select all duplicate copies of this file"), null, (s, e) => SelectDuplicatesOfCurrent(true));
            _ctxResults.Items.Add(mnuSelectDups);
            var mnuKeepOnlyThis = new ToolStripMenuItem(L.Pick("👥 نگه‌داشتن فقط این نسخه و لغو سایر کپی‌ها", "👥 Keep only this copy and deselect duplicates"), null, (s, e) => SelectDuplicatesOfCurrent(false));
            _ctxResults.Items.Add(mnuKeepOnlyThis);
            var mnuCompareLive = new ToolStripMenuItem(L.Pick("🕰️ بررسی وضعیت در دیسک فعلی و استخراج...", "🕰️ Check on Live Disk & Export..."), null, (s, e) => CheckAndRestoreShadowFile());
            _ctxResults.Items.Add(mnuCompareLive);
            _ctxResults.Opening += (s, e) =>
            {
                var pt = _grid.PointToClient(Cursor.Position);
                var h = _grid.HitTest(pt.X, pt.Y);
                if (h.RowIndex < 0 || h.RowIndex >= _grid.Rows.Count)
                {
                    e.Cancel = true;
                    return;
                }
                var curDi = _grid.Rows[h.RowIndex].Tag as DatabaseInfo;
                mnuFixExt.Visible = curDi != null && curDi.IsDisguised && !string.IsNullOrEmpty(curDi.DetectedTrueExtension);
                mnuExtractArchive.Visible = curDi != null && curDi.IsInsideArchive;
                bool hasDups = curDi != null && (curDi.IsDuplicate || curDi.IsPrimaryDuplicate) && curDi.DuplicateGroupCount > 1;
                mnuSelectDups.Visible = hasDups;
                mnuKeepOnlyThis.Visible = hasDups;
                mnuCompareLive.Visible = curDi != null && curDi.IsFromShadowCopy;
                _ctxResults.Tag = _grid.Rows[h.RowIndex].Cells[1].Value as string;
            };
            _grid.ContextMenuStrip = _ctxResults;

            // ---- نوار فیلتر و انتخاب هوشمند ----
            _txtFilter = new TextBox
            {
                Width = 190,
                Font = UiFont.Make(9f),
                PlaceholderText = L.Pick("جستجو در نام، مسیر، فرمت...", "Search name, path, format..."),
                Margin = new Padding(0, 2, 2, 2)
            };
            _txtFilter.TextChanged += (_, _) =>
            {
                _btnClearFilter.Visible = !string.IsNullOrEmpty(_txtFilter.Text);
                ApplyFilterAndRefreshGrid();
            };

            _btnClearFilter = new Button
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
                Margin = new Padding(0, 3, 6, 0)
            };
            _btnClearFilter.FlatAppearance.BorderSize = 0;
            _btnClearFilter.Click += (_, _) => _txtFilter.Clear();

            _cmbFilterType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 160,
                Font = UiFont.Make(8.8f),
                Margin = new Padding(0, 2, 6, 2)
            };
            _cmbFilterType.Items.AddRange(new object[]
            {
                L.Pick("همه فایل‌ها", "All Files"),
                L.Pick("⚠️ فقط فایل‌های تغییرنام‌یافته و پنهان", "Disguised / Carved Only"),
                L.Pick("📦 فقط فایل‌های درون آرشیو (Inside Archives)", "Inside Archives Only"),
                L.Pick("👥 فقط فایل‌های تکراری (Duplicates Only)", "Duplicates Only"),
                L.Pick("⭐ بدون فایل‌های تکراری (Unique Only - یکتا)", "Unique Files Only"),
                L.Pick("🕰️ فقط نسخه‌های قبلی (Shadow Copies)", "Shadow Copies Only"),
                L.Pick("🔥 فقط دیتابیس‌های حذف‌شده از دیسک", "Deleted on Live Disk Only"),
                L.Pick("فقط بکاپ‌های کامل (Full)", "Full Backups Only"),
                L.Pick("فقط فایل‌های پشتیبان (BAK/DUMP)", "Backups Only"),
                L.Pick("فقط پایگاه‌های فعال (MDF/IBD)", "Active DBs Only"),
                L.Pick("بزرگ‌تر از ۵۰۰ مگابایت", "Size > 500 MB"),
                L.Pick("بزرگ‌تر از ۱ گیگابایت", "Size > 1 GB")
            });
            _cmbFilterType.SelectedIndex = 0;
            _cmbFilterType.SelectedIndexChanged += (_, _) => ApplyFilterAndRefreshGrid();

            _cmbFilterDate = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 130,
                Font = UiFont.Make(8.8f),
                Margin = new Padding(0, 2, 6, 2)
            };
            _cmbFilterDate.Items.AddRange(new object[]
            {
                L.Pick("همه تاریخ‌ها", "All Dates"),
                L.Pick("۳۰ روز اخیر", "Last 30 Days"),
                L.Pick("۹۰ روز اخیر", "Last 90 Days"),
                L.Pick("۱ سال اخیر", "Last 1 Year"),
                L.Pick("سال ۱۴۰۴ شمسی", "Year 1404"),
                L.Pick("سال ۱۴۰۳ شمسی", "Year 1403"),
                L.Pick("سال ۱۴۰۲ شمسی", "Year 1402"),
                L.Pick("سال ۱۴۰۱ و قبل‌تر", "Year 1401 & Older")
            });
            _cmbFilterDate.SelectedIndex = 0;
            _cmbFilterDate.SelectedIndexChanged += (_, _) => ApplyFilterAndRefreshGrid();

            _menuSmartSelect = new ContextMenuStrip { Font = UiFont.Make(9f) };
            _menuSmartSelect.Items.Add(L.Pick("⭐ فقط آخرین نسخه از هر پایگاه داده (Latest only)", "⭐ Latest Backup per Database"), null, (_, _) => SelectLatestPerDatabase());
            _menuSmartSelect.Items.Add(L.Pick("🧹 لغو انتخاب فایل‌های تکراری (Deselect Duplicates)", "🧹 Deselect Duplicates (Keep 1 Primary)"), null, (_, _) => DeselectDuplicates());
            _menuSmartSelect.Items.Add(L.Pick("👥 فقط انتخاب فایل‌های تکراری (Select Duplicates)", "👥 Select Duplicates Only"), null, (_, _) => SelectDuplicatesOnly());
            _menuSmartSelect.Items.Add(L.Pick("⭐ انتخاب فقط نسخه‌های یکتا (بدون کپی مازاد)", "⭐ Select Unique Files Only"), null, (_, _) => SelectUniqueOnly());
            _menuSmartSelect.Items.Add(L.Pick("⭐ فقط پشتیبان‌های کامل (Full Backups Only - حذف لاگ‌ها)", "⭐ Full Backups Only (Exclude Logs/Diff)"), null, (_, _) => SelectFullBackupsOnly());
            _menuSmartSelect.Items.Add(L.Pick("⚠️ انتخاب فایل‌های تغییرنام‌یافته و پنهان (Select Disguised)", "⚠️ Select Disguised / Carved Files"), null, (_, _) => SelectDisguisedOnly());
            _menuSmartSelect.Items.Add(L.Pick("📦 انتخاب فایل‌های درون آرشیو (Select Archive Inside)", "📦 Select Inside-Archive Files"), null, (_, _) => SelectArchiveInsideOnly());
            _menuSmartSelect.Items.Add(L.Pick("🕰️ انتخاب فایل‌های کشف‌شده از Shadow Copies", "🕰️ Select Shadow Copies Files Only"), null, (_, _) => SelectShadowCopiesOnly());
            _menuSmartSelect.Items.Add(L.Pick("🔥 فقط انتخاب دیتابیس‌های حذف‌شده از دیسک فعلی", "🔥 Select Files Deleted From Live Disk Only"), null, (_, _) => SelectDeletedFromDiskOnly());
            _menuSmartSelect.Items.Add(L.Pick("🔍 انتخاب بر اساس الگو یا متن... (Select by Pattern)", "🔍 Select by Pattern / Wildcard..."), null, (_, _) => SelectByPattern());
            _menuSmartSelect.Items.Add(new ToolStripSeparator());
            _menuSmartSelect.Items.Add(L.Pick("💾 انتخاب فایل‌های پایگاه داده فعال (MDF, ...)", "💾 Active Database Files Only"), null, (_, _) => SelectActiveDatabasesOnly());
            _menuSmartSelect.Items.Add(L.Pick("📦 انتخاب فایل‌های پشتیبان (BAK, DUMP, ...)", "📦 Backup Files Only"), null, (_, _) => SelectBackupsOnly());
            _menuSmartSelect.Items.Add(new ToolStripSeparator());
            _menuSmartSelect.Items.Add(L.Pick("🔄 معکوس کردن انتخاب‌ها (Invert Selection)", "🔄 Invert Selection"), null, (_, _) => InvertSelection());
            _menuSmartSelect.Items.Add(L.Pick("❌ لغو انتخاب همه (Deselect All)", "❌ Deselect All"), null, (_, _) => DeselectAll());

            _btnSmartSelect = new Button
            {
                Text = L.Pick("⚡ انتخاب هوشمند ▾", "⚡ Smart Select ▾"),
                Font = UiFont.Make(9f, FontStyle.Bold),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(155, 30),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 1, 6, 1)
            };
            _btnSmartSelect.FlatAppearance.BorderSize = 0;
            _btnSmartSelect.Click += (_, _) =>
            {
                _menuSmartSelect.Show(_btnSmartSelect, new Point(0, _btnSmartSelect.Height));
            };

            _lblSelectionSummary = new Label
            {
                AutoSize = true,
                Font = UiFont.Make(9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                Margin = new Padding(4, 5, 4, 2),
                Text = L.Pick("آماده اسکن", "Ready to scan")
            };

            // ---- دکمه‌های پایین ----
            _btnCopy = new Button
            {
                Text = L.Text("S184"),
                BackColor = Color.FromArgb(139, 195, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = UiFont.Make(10f, FontStyle.Bold),
                Location = new Point(12, 642),
                Size = new Size(200, 40),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnCopy.Click += BtnCopy_Click;
            Controls.Add(_btnCopy);

            _btnMerge = new Button
            {
                Text = L.Text("S185"),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = UiFont.Make(10f, FontStyle.Bold),
                Location = new Point(220, 642),
                Size = new Size(220, 40),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnMerge.Click += (s, e) =>
            {
                _grid.EndEdit();
                var selected = GetSelectedResults();
                if (selected.Count == 0) { _lblStatus.Text = L.Text("S198"); return; }
                Found = selected;
                DialogResult = DialogResult.OK;
            };
            Controls.Add(_btnMerge);

            var btnClose = new Button
            {
                Text = L.Text("S095"),
                Location = new Point(792, 642),
                Size = new Size(96, 40),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnClose.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.Add(btnClose);

            ReloadRoots();
            SetFormatsChecked(true);
            BuildSearchLayout(grpRoots, grpFormats, grpOpt, btnAddRoot, btnAllRoots, btnNoneRoots, lblResults, btnAllResults, btnNoneResults, btnClose);

            Load += (s, e) => { };
        }

        private void ReloadRoots()
        {
            _lstRoots.Items.Clear();
            var mode = _rbShadow.Checked ? ScanMode.ShadowCopies
                     : _rbExternal.Checked ? ScanMode.ExternalOnly
                     : _rbFull.Checked ? ScanMode.Full
                     : ScanMode.Smart;

            var discovered = SmartPathLocator.GetDiscoveredRoots(mode);
            if (discovered.Count == 0 && mode == ScanMode.ExternalOnly)
            {
                var placeholder = new DiscoveredRoot
                {
                    Path = "",
                    DisplayName = L.Pick("⚠️ هیچ حافظه اکسترنال یا فلش USB متصل نیست", "⚠️ No external USB drives detected"),
                    Category = RootCategory.ExternalDrive,
                    IsSelectedByDefault = false
                };
                _lstRoots.Items.Add(placeholder);
                return;
            }

            foreach (var root in discovered)
            {
                var idx = _lstRoots.Items.Add(root);
                _lstRoots.SetItemChecked(idx, root.IsSelectedByDefault);
            }
        }

        private void SetFormatsChecked(bool check)
        {
            for (int i = 0; i < _lstFormats.Items.Count; i++)
                _lstFormats.SetItemChecked(i, check);
        }

        private void SetRootsChecked(bool check)
        {
            for (int i = 0; i < _lstRoots.Items.Count; i++)
            {
                var item = _lstRoots.Items[i];
                if (item is DiscoveredRoot dr && string.IsNullOrWhiteSpace(dr.Path))
                    continue;
                _lstRoots.SetItemChecked(i, check);
            }
        }

        private void BtnAddRoot_Click(object? sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = L.Text("S186")
            };
            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                var selected = fbd.SelectedPath;
                var exists = false;
                foreach (var item in _lstRoots.Items)
                {
                    var p = item is DiscoveredRoot dr ? dr.Path : item?.ToString();
                    if (string.Equals(p, selected, StringComparison.OrdinalIgnoreCase)) { exists = true; break; }
                }
                if (!exists)
                {
                    var customRoot = new DiscoveredRoot
                    {
                        Path = selected,
                        DisplayName = "📂 " + selected,
                        Category = RootCategory.CustomFolder,
                        IsSelectedByDefault = true
                    };
                    var idx = _lstRoots.Items.Add(customRoot);
                    _lstRoots.SetItemChecked(idx, true);
                }
            }
        }

        private (List<string> roots, Dictionary<string, (ShadowCopySnapshot snap, string mountDir)> shadowMounts) GetSelectedRootsWithShadows()
        {
            var roots = new List<string>();
            var shadowMounts = new Dictionary<string, (ShadowCopySnapshot snap, string mountDir)>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in _lstRoots.CheckedItems)
            {
                if (item is DiscoveredRoot dr)
                {
                    if (dr.Category == RootCategory.ShadowCopy && dr.ShadowSnapshot != null)
                    {
                        var mountDir = ShadowCopyFinder.MountSnapshot(dr.ShadowSnapshot, out var mErr);
                        if (!string.IsNullOrEmpty(mountDir) && Directory.Exists(mountDir))
                        {
                            shadowMounts[mountDir] = (dr.ShadowSnapshot, mountDir);
                            roots.Add(mountDir);
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(dr.Path) && Directory.Exists(dr.Path))
                    {
                        roots.Add(dr.Path);
                    }
                }
                else if (item != null)
                {
                    var p = item.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(p) && Directory.Exists(p))
                        roots.Add(p);
                }
            }
            return (roots.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), shadowMounts);
        }

        private List<string> GetSelectedRoots() => GetSelectedRootsWithShadows().roots;

        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_DEVICECHANGE)
            {
                var wparam = m.WParam.ToInt64();
                if (wparam == DBT_DEVICEARRIVAL || wparam == DBT_DEVICEREMOVECOMPLETE)
                {
                    try
                    {
                        if (IsHandleCreated && !IsDisposed)
                        {
                            BeginInvoke(new Action(() =>
                            {
                                if (!IsDisposed)
                                    ReloadRoots();
                            }));
                        }
                    }
                    catch { }
                }
            }
        }

        private List<DiskFormat> GetSelectedFormats()
        {
            return _selectedFormats;
        }

        private async void BtnScan_Click(object? sender, EventArgs e)
        {
            var (roots, shadowMounts) = GetSelectedRootsWithShadows();
            if (roots.Count == 0)
            {
                _lblStatus.Text = L.Text("S187");
                return;
            }

            var formats = GetSelectedFormats();
            if (formats.Count == 0)
            {
                _lblStatus.Text = L.Text("S188");
                return;
            }

            var minSize = (long)_numMinSize.Value * 1024 * 1024;

            _cts = new CancellationTokenSource();
            _btnScan.Enabled = false;
            _btnCancelScan.Enabled = true;
            _allResults.Clear();
            _selectedPaths.Clear();
            _grid.Rows.Clear();
            Found = new List<DatabaseInfo>();
            _bar.Value = 0;
            _bar.Style = ProgressBarStyle.Continuous;
            UpdateSelectionSummary();

            var scanner = new DiskScanner();
            try
            {
                var enableCarving = _chkDeepCarving?.Checked ?? true;
                var enablePeeking = _chkArchivePeeking?.Checked ?? true;
                var result = await Task.Run(() => scanner.Scan(roots, formats, minSize, enableCarving, enablePeeking, OnProgress, _cts.Token));

                if (shadowMounts.Count > 0)
                {
                    foreach (var r in result)
                    {
                        if (string.IsNullOrEmpty(r.LocalPath)) continue;
                        foreach (var kvp in shadowMounts)
                        {
                            if (r.LocalPath.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                            {
                                ShadowCopyFinder.AnnotateShadowCopyResult(r, kvp.Key, kvp.Value.snap);
                                break;
                            }
                        }
                    }
                }

                var dupResult = DuplicateDetector.Analyze(result);

                if (_cts.IsCancellationRequested)
                {
                    _lblStatus.Text = L.Format("S189", result.Count);
                }
                else
                {
                    if (dupResult.TotalDuplicates > 0)
                    {
                        _lblStatus.Text = L.Pick(
                            $"اسکن پایان یافت. {result.Count} فایل یافت شد ({dupResult.TotalDuplicates} کپی تکراری کشف شد و از حالت انتخاب اولیه خارج گردید).",
                            $"Scan complete. {result.Count} files found ({dupResult.TotalDuplicates} duplicate copies detected and deselected by default).");
                    }
                    else if (result.Count(x => x.IsBackup) > 15)
                    {
                        _lblStatus.Text = L.Pick(
                            $"اسکن پایان یافت. {result.Count} فایل یافت شد. (از «⚡ انتخاب هوشمند» برای گزینش سریع جدیدترین نسخه‌ها استفاده کنید)",
                            $"Scan complete. {result.Count} files found. (Use '⚡ Smart Select' to pick latest backups quickly)");
                    }
                    else
                    {
                        _lblStatus.Text = L.Format("S190", result.Count);
                    }
                    _lblStatus.ForeColor = result.Count > 0 ? Color.FromArgb(76, 175, 80) : Color.Gray;
                }

                _allResults = result;
                _selectedPaths.Clear();
                foreach (var r in result)
                {
                    if (!string.IsNullOrEmpty(r.LocalPath))
                    {
                        if (!r.IsDuplicate)
                        {
                            _selectedPaths.Add(r.LocalPath);
                        }
                    }
                }

                Found = result;
                ApplyFilterAndRefreshGrid();
            }
            catch (Exception ex)
            {
                _lblStatus.Text = L.Format("S055", ex.Message);
                _lblStatus.ForeColor = Color.FromArgb(211, 47, 47);
            }
            finally
            {
                _btnScan.Enabled = true;
                _btnCancelScan.Enabled = false;
                _bar.Value = _bar.Maximum;
            }
        }

        private void FillGrid(List<DatabaseInfo> results)
        {
            _allResults = results ?? new();
            DuplicateDetector.Analyze(_allResults);
            _selectedPaths.Clear();
            foreach (var r in _allResults)
            {
                if (!string.IsNullOrEmpty(r.LocalPath) && !r.IsDuplicate)
                    _selectedPaths.Add(r.LocalPath);
            }
            ApplyFilterAndRefreshGrid();
        }

        private void OnProgress(DiskScanner.ScanProgress p)
        {
            try
            {
                BeginInvoke(new Action(() =>
                {
                    if (p.DirectoriesScanned > 0)
                    {
                        _bar.Maximum = Math.Max(_bar.Maximum, p.DirectoriesScanned + 100);
                        _bar.Value = Math.Min(_bar.Value + 1, _bar.Maximum);
                    }
                    else
                    {
                        _bar.Maximum = 100;
                    }

                    if (!string.IsNullOrEmpty(p.CurrentDirectory))
                    {
                        _lblStatus.Text = L.Format("S191", p.CurrentDirectory);
                    }
                    else
                    {
                        _lblStatus.Text = L.Format("S192", p.FilesScanned);
                    }
                }));
            }
            catch { }
        }

        private void ApplyFilterAndRefreshGrid()
        {
            if (_allResults == null) return;

            var filterText = _txtFilter?.Text?.Trim() ?? "";
            var filterType = _cmbFilterType?.SelectedIndex ?? 0;
            var filterDate = _cmbFilterDate?.SelectedIndex ?? 0;

            var tokens = filterText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var filtered = _allResults.Where(r =>
            {
                // 1. فیلتر متنی
                if (tokens.Length > 0)
                {
                    var combined = $"{r.Name} {r.LocalPath} {r.FormatName} {Path.GetExtension(r.LocalPath ?? "")}".ToLowerInvariant();
                    foreach (var token in tokens)
                    {
                        if (!combined.Contains(token.ToLowerInvariant())) return false;
                    }
                }

                // 2. فیلتر نوع
                if (filterType == 1 && !r.IsDisguised) return false; // فقط فایل‌های استتارشده
                if (filterType == 2 && !r.IsInsideArchive) return false; // فقط فایل‌های درون آرشیو
                if (filterType == 3 && (!r.IsDuplicate && !r.IsPrimaryDuplicate || r.DuplicateGroupCount <= 1)) return false; // فقط فایل‌های تکراری
                if (filterType == 4 && r.IsDuplicate) return false; // فقط فایل‌های یکتا (بدون کپی مازاد)
                if (filterType == 5 && !r.IsFromShadowCopy) return false; // فقط نسخه‌های قبلی (Shadow Copies)
                if (filterType == 6 && (!r.IsFromShadowCopy || !r.IsDeletedFromCurrentDisk)) return false; // فقط فایل‌های حذف‌شده از دیسک
                if (filterType == 7 && (!r.IsBackup || (r.BackupHeader != null && !r.BackupHeader.IsFullBackup))) return false; // فقط بکاپ‌های کامل (Full)
                if (filterType == 8 && !r.IsBackup) return false; // فقط بکاپ‌ها
                if (filterType == 9 && r.IsBackup) return false;  // فقط دیتابیس‌های فعال
                if (filterType == 10 && r.FileSize < 500L * 1024 * 1024) return false; // > 500MB
                if (filterType == 11 && r.FileSize < 1024L * 1024 * 1024) return false; // > 1GB

                // 3. فیلتر تاریخ
                if (!MatchesDateFilter(r.FileModified, filterDate)) return false;

                return true;
            }).ToList();

            _isUpdatingGridRows = true;
            try
            {
                _grid.Rows.Clear();
                foreach (var r in filtered)
                {
                    var row = new DataGridViewRow();
                    row.CreateCells(_grid);
                    bool isChecked = !string.IsNullOrEmpty(r.LocalPath) && _selectedPaths.Contains(r.LocalPath);
                    row.Cells[0].Value = isChecked;

                    if (r.IsInsideArchive && !string.IsNullOrEmpty(r.ArchiveContainerPath) && !string.IsNullOrEmpty(r.ArchiveEntryPath))
                    {
                        row.Cells[1].Value = $"{Path.GetFileName(r.ArchiveContainerPath)} ➔ {r.ArchiveEntryPath}";
                    }
                    else
                    {
                        row.Cells[1].Value = r.LocalPath;
                    }
                    row.Cells[2].Value = r.FormatName;
                    
                    if (r.IsDisguised && !string.IsNullOrEmpty(r.DetectedTrueExtension))
                    {
                        var curExt = Path.GetExtension(r.LocalPath ?? "");
                        if (string.IsNullOrEmpty(curExt)) curExt = L.Pick("(بدون پسوند)", "(none)");
                        row.Cells[3].Value = $"{curExt} ➔ {r.DetectedTrueExtension}";
                    }
                    else if (r.IsInsideArchive && !string.IsNullOrEmpty(r.ArchiveEntryPath))
                    {
                        var innerExt = Path.GetExtension(r.ArchiveEntryPath).TrimStart('.').ToLowerInvariant();
                        var arcExt = Path.GetExtension(r.ArchiveContainerPath ?? "").TrimStart('.').ToUpperInvariant();
                        row.Cells[3].Value = $"{innerExt} ({arcExt})";
                    }
                    else
                    {
                        row.Cells[3].Value = Path.GetExtension(r.LocalPath ?? "").TrimStart('.').ToLowerInvariant();
                    }

                    if (r.IsFromShadowCopy)
                    {
                        var baseName = !string.IsNullOrWhiteSpace(r.RealDatabaseName) ? r.RealDatabaseName : r.Name;
                        row.Cells[4].Value = r.IsDeletedFromCurrentDisk ? $"🔥 [حذف‌شده] {baseName}" : $"🕰️ {baseName}";
                    }
                    else
                    {
                        row.Cells[4].Value = !string.IsNullOrWhiteSpace(r.RealDatabaseName) ? r.RealDatabaseName : r.Name;
                    }

                    row.Cells[5].Value = DatabaseFileLocator.FormatSize(r.FileSize);
                    row.Cells[6].Value = (r.FileModified <= DateTime.MinValue || r.FileModified.Year < 1900)
                        ? "-"
                        : (L.IsFa ? $"{JalaliDateHelper.FormatShort(r.FileModified)} {r.FileModified:HH:mm}" : r.FileModified.ToString("yyyy-MM-dd HH:mm"));

                    if (r.IsDisguised)
                    {
                        row.Cells[7].Value = "⚠️ " + (r.IsBackup ? L.Text("S193") : L.Pick("استتار", "Disguised"));
                    }
                    else if (r.IsDuplicate)
                    {
                        row.Cells[7].Value = $"👥 " + L.Pick($"کپی تکراری (#{r.DuplicateGroupId})", $"Duplicate (#{r.DuplicateGroupId})");
                    }
                    else if (r.IsPrimaryDuplicate)
                    {
                        row.Cells[7].Value = $"⭐ " + (r.IsBackup ? L.Text("S193") : L.Pick("اصلی", "Primary"));
                    }
                    else if (r.IsFromShadowCopy)
                    {
                        row.Cells[7].Value = r.IsDeletedFromCurrentDisk
                            ? "🔥 " + L.Pick("حذف‌شده از دیسک", "Deleted from disk")
                            : "🕰️ " + L.Pick("نسخه قبلی", "Previous version");
                    }
                    else if (r.IsInsideArchive)
                    {
                        row.Cells[7].Value = "📦 " + (r.IsBackup ? L.Text("S193") : L.Pick("آرشیو", "Archive"));
                    }
                    else
                    {
                        row.Cells[7].Value = !string.IsNullOrWhiteSpace(r.BackupTypeDisplay)
                            ? r.BackupTypeDisplay
                            : (r.IsBackup ? L.Text("S193") : "");
                    }

                    row.Tag = r;

                    if (r.IsDisguised)
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 224);
                        var disgTip = r.DisguisedReason + (r.BackupHeader != null ? "\n\n" + r.BackupHeader.SummaryText : "");
                        row.Cells[1].ToolTipText = disgTip;
                        row.Cells[2].ToolTipText = disgTip;
                        row.Cells[3].ToolTipText = disgTip;
                        row.Cells[4].ToolTipText = disgTip;
                    }
                    else if (r.IsDuplicate)
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(243, 229, 245); // Soft lavender for duplicate copies
                        var dupTip = L.Pick(
                            $"⚠️ این فایل نسخه کپی تکراری است (گروه #{r.DuplicateGroupId} - شامل {r.DuplicateGroupCount} نسخه کاملاً یکسان):\n" +
                            string.Join("\n", (r.DuplicateSiblingPaths ?? new()).Select(p => "• " + p)),
                            $"⚠️ Duplicate file copy (Group #{r.DuplicateGroupId} - {r.DuplicateGroupCount} identical copies):\n" +
                            string.Join("\n", (r.DuplicateSiblingPaths ?? new()).Select(p => "• " + p)));
                        row.Cells[1].ToolTipText = dupTip;
                        row.Cells[4].ToolTipText = dupTip;
                        row.Cells[7].ToolTipText = dupTip;
                    }
                    else if (r.IsFromShadowCopy)
                    {
                        row.DefaultCellStyle.BackColor = r.IsDeletedFromCurrentDisk
                            ? Color.FromArgb(255, 235, 238) // Soft coral / light red
                            : Color.FromArgb(237, 242, 247); // Soft slate / blue-grey

                        var vssDate = r.ShadowCopyDate.HasValue ? JalaliDateHelper.Format(r.ShadowCopyDate.Value) : "-";
                        var vssTip = L.Pick(
                            $"🕰️ نسخه قبلی از نقطه بازیابی (Shadow Copy):\n" +
                            $"• تاریخ نسخه پشتیبان: {vssDate}\n" +
                            $"• شناسه Snapshot: {r.ShadowCopyId}\n" +
                            $"• مسیر متناظر روی دیسک فعلی: {r.LiveDiskEquivalentPath ?? "-"}\n" +
                            (r.IsDeletedFromCurrentDisk
                                ? "🔥 توجه: این فایل در حال حاضر روی دیسک زنده سیستم وجود ندارد و حذف شده است!"
                                : "ℹ️ این فایل روی دیسک زنده نیز موجود است."),
                            $"🕰️ Previous version from Shadow Copy:\n" +
                            $"• Snapshot Date: {vssDate}\n" +
                            $"• Snapshot ID: {r.ShadowCopyId}\n" +
                            $"• Live Disk Equivalent: {r.LiveDiskEquivalentPath ?? "-"}\n" +
                            (r.IsDeletedFromCurrentDisk
                                ? "🔥 WARNING: This file is currently deleted/missing from the live disk!"
                                : "ℹ️ This file also exists on the live disk."));
                        row.Cells[1].ToolTipText = vssTip;
                        row.Cells[4].ToolTipText = vssTip;
                        row.Cells[7].ToolTipText = vssTip;
                    }
                    else if (r.IsInsideArchive)
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(224, 242, 241); // Soft teal for inside archives
                        var arcTip = L.Pick($"فایل فشرده: {r.ArchiveContainerPath}\nمسیر درون آرشیو: {r.ArchiveEntryPath}\nحجم غیرفشرده: {DatabaseFileLocator.FormatSize(r.FileSize)}" +
                                            (r.ArchiveCompressedSize > 0 ? $"\nحجم فشرده: {DatabaseFileLocator.FormatSize(r.ArchiveCompressedSize)}" : ""),
                                            $"Container Archive: {r.ArchiveContainerPath}\nInner Entry: {r.ArchiveEntryPath}\nUncompressed: {DatabaseFileLocator.FormatSize(r.FileSize)}" +
                                            (r.ArchiveCompressedSize > 0 ? $"\nCompressed: {DatabaseFileLocator.FormatSize(r.ArchiveCompressedSize)}" : ""));
                        row.Cells[1].ToolTipText = arcTip;
                        row.Cells[2].ToolTipText = arcTip;
                    }
                    else
                    {
                        if (r.IsPrimaryDuplicate)
                        {
                            var primaryTip = L.Pick(
                                $"⭐ نسخه اصلی انتخابی (دارای {r.DuplicateGroupCount - 1} کپی تکراری دیگر در دیسک):\n" +
                                string.Join("\n", (r.DuplicateSiblingPaths ?? new()).Select(p => "• " + p)),
                                $"⭐ Primary keeper (Has {r.DuplicateGroupCount - 1} duplicate copies on disk):\n" +
                                string.Join("\n", (r.DuplicateSiblingPaths ?? new()).Select(p => "• " + p)));
                            row.Cells[1].ToolTipText = primaryTip;
                            row.Cells[4].ToolTipText = primaryTip;
                            row.Cells[7].ToolTipText = primaryTip;
                        }
                        else if (r.BackupHeader != null)
                        {
                            var summary = r.BackupHeader.SummaryText;
                            row.Cells[1].ToolTipText = summary;
                            row.Cells[4].ToolTipText = summary;
                        }
                        if (r.IsBackup) row.DefaultCellStyle.BackColor = Color.FromArgb(232, 245, 233);
                    }

                    _grid.Rows.Add(row);
                }
            }
            finally
            {
                _isUpdatingGridRows = false;
            }

            UpdateSelectionSummary();
        }

        private static bool MatchesDateFilter(DateTime dt, int filterIndex)
        {
            if (filterIndex <= 0) return true;
            var now = DateTime.Now;
            switch (filterIndex)
            {
                case 1: return dt >= now.AddDays(-30);
                case 2: return dt >= now.AddDays(-90);
                case 3: return dt >= now.AddDays(-365);
                case 4: return JalaliDateHelper.ToJalali(dt).Year == 1404;
                case 5: return JalaliDateHelper.ToJalali(dt).Year == 1403;
                case 6: return JalaliDateHelper.ToJalali(dt).Year == 1402;
                case 7: return JalaliDateHelper.ToJalali(dt).Year <= 1401;
                default: return true;
            }
        }

        private void UpdateSelectionSummary()
        {
            int totalCount = _allResults.Count;
            int visibleCount = _grid.Rows.Count;
            int selectedCount = _allResults.Count(x => !string.IsNullOrEmpty(x.LocalPath) && _selectedPaths.Contains(x.LocalPath));
            long selectedBytes = _allResults.Where(x => !string.IsNullOrEmpty(x.LocalPath) && _selectedPaths.Contains(x.LocalPath)).Sum(x => x.FileSize);

            var sizeFormatted = DatabaseFileLocator.FormatSize(selectedBytes);

            if (_lblSelectionSummary != null)
            {
                _lblSelectionSummary.Text = L.Pick(
                    $"نمایش: {visibleCount} از {totalCount} فایل | انتخاب‌شده: {selectedCount} فایل ({sizeFormatted})",
                    $"Showing: {visibleCount} of {totalCount} files | Selected: {selectedCount} files ({sizeFormatted})");
            }
        }

        public static string GetDatabaseFamilyKey(DatabaseInfo r)
        {
            if (!string.IsNullOrWhiteSpace(r.RealDatabaseName))
            {
                return r.RealDatabaseName.Trim().ToLowerInvariant();
            }

            var fileName = Path.GetFileNameWithoutExtension(r.LocalPath ?? r.Name ?? "");
            if (string.IsNullOrWhiteSpace(fileName)) return "unknown";

            // 1. حذف الگوهای زمان‌بندی نگهداری SQL Server مانند: _backup_2024_02_10_010002_1234567
            var s = System.Text.RegularExpressions.Regex.Replace(fileName, @"(?i)[_ -]backup[_ -]\d{4}[_ -]\d{2}[_ -]\d{2}[_ -]\d+.*$", "");

            // 2. حذف الگوهای تاریخ شمسی و میلادی و زمان
            s = System.Text.RegularExpressions.Regex.Replace(s, @"(?i)[_ -]?(?:13|14|19|20)\d{2}[_ /-]?(?:0[1-9]|1[0-2])[_ /-]?(?:0[1-9]|[12]\d|3[01])(?:[_ -]?\d{2}[_ -]?\d{2}(?:[_ -]?\d{2})?)?", "");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"(?i)[_ -]?(?:13|14|19|20)\d{6,10}", "");

            // 3. حذف سال‌های ۲ رقمی شمسی در انتهای نام مانند: _99 یا _01 یا _02
            s = System.Text.RegularExpressions.Regex.Replace(s, @"[_ -](?:9[0-9]|0[0-9]|1[0-9])$", "");

            // 4. حذف پسوندهای مرسوم بکاپ مانند: _Full, _Diff, _Log, _Auto, _Backup
            s = System.Text.RegularExpressions.Regex.Replace(s, @"(?i)[_ -]?(?:autobackup|backup|bak|full|diff|differential|log|manual)", "");

            // 5. حذف ارقام ترتیبی انتهای نام: _1, _02
            s = System.Text.RegularExpressions.Regex.Replace(s, @"[_ -]+\d+$", "");

            s = s.Trim('_', '-', ' ', '.');

            if (string.IsNullOrWhiteSpace(s))
            {
                var parentDir = Path.GetFileName(Path.GetDirectoryName(r.LocalPath ?? "") ?? "");
                s = !string.IsNullOrWhiteSpace(parentDir) ? parentDir : fileName;
            }

            return s.ToLowerInvariant();
        }

        private void SelectLatestPerDatabase()
        {
            if (_allResults.Count == 0) return;

            var candidateItems = _grid.Rows.Cast<DataGridViewRow>()
                .Select(r => r.Tag as DatabaseInfo)
                .Where(di => di != null)
                .Cast<DatabaseInfo>()
                .ToList();

            if (candidateItems.Count == 0) candidateItems = _allResults;

            var groups = candidateItems.GroupBy(r =>
            {
                var dir = Path.GetDirectoryName(r.LocalPath ?? "")?.ToLowerInvariant() ?? "";
                var family = GetDatabaseFamilyKey(r);
                return $"{dir}|{family}";
            });

            int unselectedOld = 0;

            foreach (var grp in groups)
            {
                var backups = grp.Where(x => x.IsBackup).OrderByDescending(x => x.FileModified).ToList();
                var liveFiles = grp.Where(x => !x.IsBackup).ToList();

                if (backups.Count > 0)
                {
                    // جدیدترین بکاپ انتخاب می‌شود
                    var latestBackup = backups[0];
                    if (!string.IsNullOrEmpty(latestBackup.LocalPath))
                    {
                        _selectedPaths.Add(latestBackup.LocalPath);
                    }

                    // بکاپ‌های قدیمی‌تر لغو انتخاب می‌شوند
                    for (int i = 1; i < backups.Count; i++)
                    {
                        var oldBackup = backups[i];
                        if (!string.IsNullOrEmpty(oldBackup.LocalPath) && _selectedPaths.Contains(oldBackup.LocalPath))
                        {
                            _selectedPaths.Remove(oldBackup.LocalPath);
                            unselectedOld++;
                        }
                    }
                }

                // فایل‌های دیتابیس فعال حفظ می‌شوند
                foreach (var live in liveFiles)
                {
                    if (!string.IsNullOrEmpty(live.LocalPath))
                    {
                        _selectedPaths.Add(live.LocalPath);
                    }
                }
            }

            _isUpdatingGridRows = true;
            try
            {
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.Tag is DatabaseInfo di && !string.IsNullOrEmpty(di.LocalPath))
                    {
                        row.Cells[0].Value = _selectedPaths.Contains(di.LocalPath);
                    }
                }
            }
            finally
            {
                _isUpdatingGridRows = false;
            }

            UpdateSelectionSummary();

            int selectedCount = _allResults.Count(x => !string.IsNullOrEmpty(x.LocalPath) && _selectedPaths.Contains(x.LocalPath));
            _lblStatus.Text = L.Pick(
                $"فقط آخرین نسخه از هر پایگاه داده انتخاب شد ({selectedCount} فایل انتخاب‌شده، {unselectedOld} بکاپ قدیمی لغو شد).",
                $"Latest version per database selected ({selectedCount} selected, {unselectedOld} older backups deselected).");
            _lblStatus.ForeColor = Color.FromArgb(76, 175, 80);
        }

        private void SelectFullBackupsOnly()
        {
            _isUpdatingGridRows = true;
            int count = 0;
            try
            {
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.Tag is DatabaseInfo di && !string.IsNullOrEmpty(di.LocalPath))
                    {
                        bool isFull = di.IsBackup && (di.BackupHeader == null || di.BackupHeader.IsFullBackup);
                        if (isFull)
                        {
                            _selectedPaths.Add(di.LocalPath);
                            count++;
                        }
                        else
                        {
                            _selectedPaths.Remove(di.LocalPath);
                        }
                        row.Cells[0].Value = isFull;
                    }
                }
            }
            finally
            {
                _isUpdatingGridRows = false;
            }

            UpdateSelectionSummary();
            _lblStatus.Text = L.Pick($"فقط پشتیبان‌های کامل (Full Backups) انتخاب شدند ({count} فایل).", $"Full backups only selected ({count} files).");
            _lblStatus.ForeColor = Color.FromArgb(76, 175, 80);
        }

        private void SelectDisguisedOnly()
        {
            _isUpdatingGridRows = true;
            int count = 0;
            try
            {
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.Tag is DatabaseInfo di && !string.IsNullOrEmpty(di.LocalPath))
                    {
                        bool isDisguised = di.IsDisguised;
                        if (isDisguised)
                        {
                            _selectedPaths.Add(di.LocalPath);
                            count++;
                        }
                        else
                        {
                            _selectedPaths.Remove(di.LocalPath);
                        }
                        row.Cells[0].Value = isDisguised;
                    }
                }
            }
            finally
            {
                _isUpdatingGridRows = false;
            }

            UpdateSelectionSummary();
            _lblStatus.Text = L.Pick($"تعداد {count} فایل تغییرنام‌یافته یا پنهان‌شده انتخاب شدند.", $"{count} disguised / carved files selected.");
            _lblStatus.ForeColor = Color.FromArgb(230, 81, 0);
        }

        private void SelectArchiveInsideOnly()
        {
            _isUpdatingGridRows = true;
            int count = 0;
            try
            {
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.Tag is DatabaseInfo di && !string.IsNullOrEmpty(di.LocalPath))
                    {
                        bool isInside = di.IsInsideArchive;
                        if (isInside)
                        {
                            _selectedPaths.Add(di.LocalPath);
                            count++;
                        }
                        else
                        {
                            _selectedPaths.Remove(di.LocalPath);
                        }
                        row.Cells[0].Value = isInside;
                    }
                }
            }
            finally
            {
                _isUpdatingGridRows = false;
            }

            UpdateSelectionSummary();
            _lblStatus.Text = L.Pick($"تعداد {count} فایل درون آرشیوهای فشرده انتخاب شدند.", $"{count} inside-archive files selected.");
            _lblStatus.ForeColor = Color.FromArgb(0, 121, 107);
        }

        private void DeselectDuplicates()
        {
            _isUpdatingGridRows = true;
            int deselectedCount = 0;
            long savedBytes = 0;
            try
            {
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.Tag is DatabaseInfo di && !string.IsNullOrEmpty(di.LocalPath))
                    {
                        if (di.IsDuplicate)
                        {
                            if (_selectedPaths.Remove(di.LocalPath))
                            {
                                deselectedCount++;
                                savedBytes += di.FileSize;
                            }
                            row.Cells[0].Value = false;
                        }
                    }
                }
            }
            finally
            {
                _isUpdatingGridRows = false;
            }

            UpdateSelectionSummary();
            _lblStatus.Text = L.Pick(
                $"تعداد {deselectedCount} فایل تکراری از حالت انتخاب خارج شدند (صرفه‌جویی در حجم: {DatabaseFileLocator.FormatSize(savedBytes)}).",
                $"{deselectedCount} duplicate files unselected (saved: {DatabaseFileLocator.FormatSize(savedBytes)}).");
            _lblStatus.ForeColor = Color.FromArgb(76, 175, 80);
        }

        private void SelectDuplicatesOnly()
        {
            _isUpdatingGridRows = true;
            int count = 0;
            try
            {
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.Tag is DatabaseInfo di && !string.IsNullOrEmpty(di.LocalPath))
                    {
                        bool isDup = di.IsDuplicate || (di.IsPrimaryDuplicate && di.DuplicateGroupCount > 1);
                        if (isDup)
                        {
                            _selectedPaths.Add(di.LocalPath);
                            count++;
                        }
                        else
                        {
                            _selectedPaths.Remove(di.LocalPath);
                        }
                        row.Cells[0].Value = isDup;
                    }
                }
            }
            finally
            {
                _isUpdatingGridRows = false;
            }

            UpdateSelectionSummary();
            _lblStatus.Text = L.Pick($"تعداد {count} فایل از مجموعه‌های تکراری انتخاب شدند.", $"{count} files from duplicate sets selected.");
            _lblStatus.ForeColor = Color.FromArgb(156, 39, 176);
        }

        private void SelectUniqueOnly()
        {
            _isUpdatingGridRows = true;
            int count = 0;
            try
            {
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.Tag is DatabaseInfo di && !string.IsNullOrEmpty(di.LocalPath))
                    {
                        bool isUnique = !di.IsDuplicate;
                        if (isUnique)
                        {
                            _selectedPaths.Add(di.LocalPath);
                            count++;
                        }
                        else
                        {
                            _selectedPaths.Remove(di.LocalPath);
                        }
                        row.Cells[0].Value = isUnique;
                    }
                }
            }
            finally
            {
                _isUpdatingGridRows = false;
            }

            UpdateSelectionSummary();
            _lblStatus.Text = L.Pick($"فقط نسخه‌های یکتا ({count} فایل) انتخاب شدند و کپی‌های مازاد لغو گردیدند.", $"Only unique files ({count} files) selected, redundant copies deselected.");
            _lblStatus.ForeColor = Color.FromArgb(33, 150, 243);
        }

        private void SelectDuplicatesOfCurrent(bool selectAll)
        {
            var path = _ctxResults?.Tag as string;
            var target = _allResults.FirstOrDefault(x => string.Equals(x.LocalPath, path, StringComparison.OrdinalIgnoreCase))
                         ?? (_grid.CurrentRow?.Tag as DatabaseInfo);

            if (target == null || target.DuplicateGroupId == 0) return;

            _isUpdatingGridRows = true;
            try
            {
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.Tag is DatabaseInfo di && di.DuplicateGroupId == target.DuplicateGroupId && !string.IsNullOrEmpty(di.LocalPath))
                    {
                        bool check = selectAll || ReferenceEquals(di, target);
                        if (check)
                        {
                            _selectedPaths.Add(di.LocalPath);
                        }
                        else
                        {
                            _selectedPaths.Remove(di.LocalPath);
                        }
                        row.Cells[0].Value = check;
                    }
                }
            }
            finally
            {
                _isUpdatingGridRows = false;
            }

            UpdateSelectionSummary();
            ApplyFilterAndRefreshGrid();
        }

        private void SelectByPattern()
        {
            var pattern = ShowPatternPrompt(
                this,
                L.Pick("الگو یا عبارت مورد نظر را وارد کنید (مانند: *1403* یا *Sepidar* یا *.bak):", "Enter search pattern (e.g. *1403* or *Sepidar* or *.bak):"),
                L.Pick("انتخاب هوشمند با الگو", "Smart Select by Pattern"),
                "*1403*");

            if (string.IsNullOrWhiteSpace(pattern)) return;

            int count = 0;
            _isUpdatingGridRows = true;
            try
            {
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.Tag is DatabaseInfo di && !string.IsNullOrEmpty(di.LocalPath))
                    {
                        var fileName = Path.GetFileName(di.LocalPath);
                        if (IsWildcardMatch(fileName, pattern) || IsWildcardMatch(di.Name, pattern) || IsWildcardMatch(di.LocalPath, pattern))
                        {
                            _selectedPaths.Add(di.LocalPath);
                            row.Cells[0].Value = true;
                            count++;
                        }
                    }
                }
            }
            finally
            {
                _isUpdatingGridRows = false;
            }

            UpdateSelectionSummary();
            _lblStatus.Text = L.Pick($"تعداد {count} فایل مطابق با الگوی «{pattern}» انتخاب شدند.", $"{count} files matching pattern '{pattern}' were selected.");
            _lblStatus.ForeColor = Color.FromArgb(76, 175, 80);
        }

        private void SelectActiveDatabasesOnly()
        {
            _isUpdatingGridRows = true;
            try
            {
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.Tag is DatabaseInfo di && !string.IsNullOrEmpty(di.LocalPath))
                    {
                        bool isLive = !di.IsBackup;
                        if (isLive) _selectedPaths.Add(di.LocalPath);
                        else _selectedPaths.Remove(di.LocalPath);
                        row.Cells[0].Value = isLive;
                    }
                }
            }
            finally
            {
                _isUpdatingGridRows = false;
            }
            UpdateSelectionSummary();
        }

        private void SelectBackupsOnly()
        {
            _isUpdatingGridRows = true;
            try
            {
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.Tag is DatabaseInfo di && !string.IsNullOrEmpty(di.LocalPath))
                    {
                        bool isBackup = di.IsBackup;
                        if (isBackup) _selectedPaths.Add(di.LocalPath);
                        else _selectedPaths.Remove(di.LocalPath);
                        row.Cells[0].Value = isBackup;
                    }
                }
            }
            finally
            {
                _isUpdatingGridRows = false;
            }
            UpdateSelectionSummary();
        }

        private void InvertSelection()
        {
            _isUpdatingGridRows = true;
            try
            {
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.Tag is DatabaseInfo di && !string.IsNullOrEmpty(di.LocalPath))
                    {
                        bool current = Convert.ToBoolean(row.Cells[0].Value ?? false);
                        bool next = !current;
                        if (next) _selectedPaths.Add(di.LocalPath);
                        else _selectedPaths.Remove(di.LocalPath);
                        row.Cells[0].Value = next;
                    }
                }
            }
            finally
            {
                _isUpdatingGridRows = false;
            }
            UpdateSelectionSummary();
        }

        private void DeselectAll()
        {
            _selectedPaths.Clear();
            _isUpdatingGridRows = true;
            try
            {
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    row.Cells[0].Value = false;
                }
            }
            finally
            {
                _isUpdatingGridRows = false;
            }
            UpdateSelectionSummary();
        }

        private void SelectDatabaseFamily(bool selectAll)
        {
            var path = _ctxResults?.Tag as string;
            if (string.IsNullOrEmpty(path)) return;

            var target = _allResults.FirstOrDefault(x => string.Equals(x.LocalPath, path, StringComparison.OrdinalIgnoreCase));
            if (target == null) return;

            var targetFamily = GetDatabaseFamilyKey(target);
            var targetDir = Path.GetDirectoryName(target.LocalPath ?? "")?.ToLowerInvariant() ?? "";

            var matching = _allResults.Where(x =>
                string.Equals(Path.GetDirectoryName(x.LocalPath ?? "")?.ToLowerInvariant() ?? "", targetDir, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(GetDatabaseFamilyKey(x), targetFamily, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            if (matching.Count == 0) return;

            if (selectAll)
            {
                foreach (var m in matching)
                {
                    if (!string.IsNullOrEmpty(m.LocalPath))
                        _selectedPaths.Add(m.LocalPath);
                }
            }
            else
            {
                var backups = matching.Where(x => x.IsBackup).OrderByDescending(x => x.FileModified).ToList();
                var liveFiles = matching.Where(x => !x.IsBackup).ToList();

                if (backups.Count > 0)
                {
                    if (!string.IsNullOrEmpty(backups[0].LocalPath))
                        _selectedPaths.Add(backups[0].LocalPath!);

                    for (int i = 1; i < backups.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(backups[i].LocalPath))
                            _selectedPaths.Remove(backups[i].LocalPath!);
                    }
                }

                foreach (var live in liveFiles)
                {
                    if (!string.IsNullOrEmpty(live.LocalPath))
                        _selectedPaths.Add(live.LocalPath!);
                }
            }

            _isUpdatingGridRows = true;
            try
            {
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.Tag is DatabaseInfo di && !string.IsNullOrEmpty(di.LocalPath))
                    {
                        row.Cells[0].Value = _selectedPaths.Contains(di.LocalPath);
                    }
                }
            }
            finally
            {
                _isUpdatingGridRows = false;
            }

            UpdateSelectionSummary();
            _lblStatus.Text = selectAll
                ? L.Pick($"تمامی {matching.Count} فایل مربوط به پایگاه داده «{targetFamily}» انتخاب شدند.", $"All {matching.Count} files for database '{targetFamily}' selected.")
                : L.Pick($"تنها آخرین نسخه از پایگاه داده «{targetFamily}» انتخاب شد.", $"Only latest version of database '{targetFamily}' selected.");
            _lblStatus.ForeColor = Color.FromArgb(76, 175, 80);
        }

        private void Grid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (_isUpdatingGridRows || e.RowIndex < 0 || e.ColumnIndex != 0) return;
            var row = _grid.Rows[e.RowIndex];
            if (row.Tag is DatabaseInfo di && !string.IsNullOrEmpty(di.LocalPath))
            {
                bool isChecked = Convert.ToBoolean(row.Cells[0].Value ?? false);
                if (isChecked) _selectedPaths.Add(di.LocalPath);
                else _selectedPaths.Remove(di.LocalPath);
                UpdateSelectionSummary();
            }
        }

        private void SetResultsChecked(bool check)
        {
            _isUpdatingGridRows = true;
            try
            {
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    row.Cells[0].Value = check;
                    if (row.Tag is DatabaseInfo di && !string.IsNullOrEmpty(di.LocalPath))
                    {
                        if (check) _selectedPaths.Add(di.LocalPath);
                        else _selectedPaths.Remove(di.LocalPath);
                    }
                }
            }
            finally
            {
                _isUpdatingGridRows = false;
            }
            UpdateSelectionSummary();
        }

        private void Grid_SortCompare(object? sender, DataGridViewSortCompareEventArgs e)
        {
            var r1 = _grid.Rows[e.RowIndex1].Tag as DatabaseInfo;
            var r2 = _grid.Rows[e.RowIndex2].Tag as DatabaseInfo;

            if (e.Column.Name == "size")
            {
                long s1 = r1?.FileSize ?? 0;
                long s2 = r2?.FileSize ?? 0;
                e.SortResult = s1.CompareTo(s2);
                e.Handled = true;
            }
            else if (e.Column.Name == "date")
            {
                DateTime d1 = r1?.FileModified ?? DateTime.MinValue;
                DateTime d2 = r2?.FileModified ?? DateTime.MinValue;
                e.SortResult = d1.CompareTo(d2);
                e.Handled = true;
            }
            else if (e.Column.Name == "backup")
            {
                bool b1 = r1?.IsBackup ?? false;
                bool b2 = r2?.IsBackup ?? false;
                e.SortResult = b1.CompareTo(b2);
                e.Handled = true;
            }
            else
            {
                string str1 = e.CellValue1?.ToString() ?? "";
                string str2 = e.CellValue2?.ToString() ?? "";
                e.SortResult = string.Compare(str1, str2, StringComparison.CurrentCultureIgnoreCase);
                e.Handled = true;
            }

            if (e.SortResult == 0 && r1 != null && r2 != null)
            {
                e.SortResult = string.Compare(r1.LocalPath ?? "", r2.LocalPath ?? "", StringComparison.CurrentCultureIgnoreCase);
            }
        }

        private void Grid_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (e.ColumnIndex == 0) return; // کلیک مستقیم روی خود تیک، خودش توگل می‌کند
            var cell = _grid.Rows[e.RowIndex].Cells[0];
            cell.Value = !(cell.Value as bool? ?? false);
        }

        private void Grid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex == 0) return; // روی ستون چک‌باکس عمل دابل‌کلیک فعال نمی‌شود
            if (_grid.Rows[e.RowIndex].Tag is DatabaseInfo di)
            {
                ShowBackupMetadataDialog(di.LocalPath);
            }
        }

        private void ShowBackupMetadataDialog(string? targetPath = null)
        {
            var path = targetPath ?? (_ctxResults?.Tag as string);
            if (string.IsNullOrEmpty(path))
            {
                if (_grid.CurrentRow?.Tag is DatabaseInfo curDi)
                    path = curDi.LocalPath;
            }

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                MessageBox.Show(this, L.Pick("لطفاً یک فایل را برای نمایش شناسنامه انتخاب کنید.", "Please select a file to inspect."),
                    L.Pick("شناسنامه متادیتا", "Metadata Inspector"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var di = _allResults.FirstOrDefault(x => string.Equals(x.LocalPath, path, StringComparison.OrdinalIgnoreCase))
                     ?? (_grid.CurrentRow?.Tag as DatabaseInfo);

            SqlBackupHeaderInfo header;
            if (di?.BackupHeader != null)
            {
                header = di.BackupHeader;
            }
            else
            {
                header = SqlBackupInspector.Inspect(path);
                if (di != null) di.BackupHeader = header;
            }

            using var dlg = new Form
            {
                Text = L.Pick($"شناسنامه و متادیتای درونی: {Path.GetFileName(path)}", $"Backup Metadata: {Path.GetFileName(path)}"),
                Size = new Size(720, 520),
                MinimumSize = new Size(580, 420),
                StartPosition = FormStartPosition.CenterParent,
                Font = UiFont.Make(9.5f),
                RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No,
                RightToLeftLayout = L.IsFa,
                BackColor = Color.White
            };

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Padding = new Padding(14)
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));

            var lblTitle = new Label
            {
                Text = L.Pick($"🔍 شناسنامه و متادیتای درونی پشتیبان ({Path.GetFileName(path)})", $"🔍 Backup Internal Metadata ({Path.GetFileName(path)})"),
                Font = UiFont.Make(11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                Dock = DockStyle.Fill,
                TextAlign = L.IsFa ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(lblTitle, 0, 0);

            var fileLength = 0L;
            try { fileLength = new FileInfo(path).Length; } catch { }

            var txtDetails = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10f),
                BackColor = Color.FromArgb(248, 249, 250),
                Text = header.SummaryText + "\r\n\r\n" +
                       L.Pick($"مسیر فایل فیزیکی: {path}", $"Physical File Path: {path}") + "\r\n" +
                       L.Pick($"حجم روی دیسک: {DatabaseFileLocator.FormatSize(fileLength)}", $"File Size on Disk: {DatabaseFileLocator.FormatSize(fileLength)}") + "\r\n" +
                       L.Pick($"منبع استخراج متادیتا: {header.Source}", $"Metadata Source: {header.Source}")
            };
            mainPanel.Controls.Add(txtDetails, 0, 1);

            var btnClose = new Button
            {
                Text = L.Pick("بستن", "Close"),
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Size = new Size(100, 34),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            var pnlBottom = new Panel { Dock = DockStyle.Fill };
            pnlBottom.Controls.Add(btnClose);
            mainPanel.Controls.Add(pnlBottom, 0, 2);

            dlg.Controls.Add(mainPanel);
            dlg.AcceptButton = btnClose;
            dlg.ShowDialog(this);
        }

        private void RestoreRealFileExtension()
        {
            var path = _ctxResults?.Tag as string;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            var target = _allResults.FirstOrDefault(x => string.Equals(x.LocalPath, path, StringComparison.OrdinalIgnoreCase))
                         ?? (_grid.CurrentRow?.Tag as DatabaseInfo);
            if (target == null || !target.IsDisguised || string.IsNullOrEmpty(target.DetectedTrueExtension)) return;

            var dir = Path.GetDirectoryName(path) ?? "";
            var nameWithoutExt = Path.GetFileNameWithoutExtension(path);
            var newExt = target.DetectedTrueExtension;
            var suggestedNewPath = Path.Combine(dir, nameWithoutExt + newExt);

            var msg = L.Pick(
                $"فایل: {Path.GetFileName(path)}\n" +
                $"فرمت واقعی شناسایی‌شده: {target.FormatName}\n" +
                $"پسوند صحیح: {newExt}\n\n" +
                $"آیا مایل به ذخیره رونوشتی از این فایل با پسوند واقعی «{newExt}» هستید؟\n\n" +
                $"• گزینه Yes: ذخیره یک رونوشت مستقل با پسوند صحیح در مسیر انتخابی\n" +
                $"• گزینه No: تغییر نام مستقیم همین فایل در محل خود\n" +
                $"• گزینه Cancel: انصراف",
                $"File: {Path.GetFileName(path)}\n" +
                $"Real Format: {target.FormatName}\n" +
                $"Correct Extension: {newExt}\n\n" +
                $"Do you want to save a copy with the real extension '{newExt}'?\n\n" +
                $"• Click Yes: Save a copy with real extension\n" +
                $"• Click No: Rename the file in-place\n" +
                $"• Click Cancel: Abort");

            var dlgRes = MessageBox.Show(this, msg, L.Pick("اصلاح و بازگردانی پسوند واقعی", "Restore Real Extension"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (dlgRes == DialogResult.Cancel) return;

            try
            {
                if (dlgRes == DialogResult.Yes)
                {
                    using var sfd = new SaveFileDialog
                    {
                        FileName = Path.GetFileName(suggestedNewPath),
                        InitialDirectory = dir,
                        Filter = $"{target.FormatName} (*{newExt})|*{newExt}|All Files (*.*)|*.*",
                        DefaultExt = newExt
                    };
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        File.Copy(path, sfd.FileName, overwrite: true);
                        MessageBox.Show(this, L.Pick($"رونوشت فایل با پسوند واقعی ذخیره شد:\n{sfd.FileName}", $"Saved copy with real extension:\n{sfd.FileName}"),
                            L.Pick("موفقیت", "Success"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else if (dlgRes == DialogResult.No)
                {
                    if (File.Exists(suggestedNewPath))
                    {
                        MessageBox.Show(this, L.Pick($"فایلی با نام «{Path.GetFileName(suggestedNewPath)}» از قبل در این مسیر وجود دارد!", "A file with that name already exists!"),
                            L.Pick("خطا", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    File.Move(path, suggestedNewPath);
                    target.LocalPath = suggestedNewPath;
                    target.IsDisguised = false;
                    ApplyFilterAndRefreshGrid();
                    MessageBox.Show(this, L.Pick($"فایل با موفقیت به نام واقعی تغییر یافت:\n{suggestedNewPath}", $"File renamed successfully:\n{suggestedNewPath}"),
                        L.Pick("موفقیت", "Success"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, L.Pick("خطا", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RunPathAction(Action<string> action)
        {
            var path = _ctxResults?.Tag as string;
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                action(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(L.Text("S194") + ex.Message, L.Text("S195"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExtractSelectedArchiveEntry()
        {
            var path = _ctxResults?.Tag as string;
            var target = _allResults.FirstOrDefault(x => string.Equals(x.LocalPath, path, StringComparison.OrdinalIgnoreCase))
                         ?? (_grid.CurrentRow?.Tag as DatabaseInfo);

            if (target == null || !target.IsInsideArchive || string.IsNullOrEmpty(target.ArchiveContainerPath) || string.IsNullOrEmpty(target.ArchiveEntryPath))
                return;

            using var sfd = new SaveFileDialog
            {
                Title = L.Pick("انتخاب مسیر ذخیره فایل استخراج‌شده", "Select Extraction Destination File"),
                FileName = Path.GetFileName(target.ArchiveEntryPath),
                Filter = "All Files (*.*)|*.*"
            };

            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    Cursor = Cursors.WaitCursor;
                    bool ok = ArchivePeeker.ExtractEntryWithProgress(target.ArchiveContainerPath, target.ArchiveEntryPath, sfd.FileName);
                    Cursor = Cursors.Default;

                    if (ok && File.Exists(sfd.FileName))
                    {
                        var msg = L.Pick(
                            $"فایل با موفقیت از آرشیو استخراج شد:\n{sfd.FileName}\n\nآیا مایل به باز کردن پوشه مقصد هستید؟",
                            $"File successfully extracted from archive:\n{sfd.FileName}\n\nDo you want to open destination folder?");
                        if (MessageBox.Show(this, msg, L.Pick("استخراج موفق", "Extraction Complete"), MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        {
                            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{sfd.FileName}\"") { UseShellExecute = true });
                        }
                    }
                    else
                    {
                        MessageBox.Show(this, L.Pick("خطا در استخراج فایل از آرشیو.", "Failed to extract file from archive."), L.Pick("خطا", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    Cursor = Cursors.Default;
                    MessageBox.Show(this, ex.Message, L.Pick("خطا", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OpenFolder(string path)
        {
            var target = _allResults.FirstOrDefault(x => string.Equals(x.LocalPath, path, StringComparison.OrdinalIgnoreCase));
            var effectivePath = target?.ArchiveContainerPath ?? (path.Contains('#') ? path.Split('#')[0] : path);
            Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + effectivePath + "\"") { UseShellExecute = true });
        }

        private void OpenFile(string path)
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        private void CopyPath(string path)
        {
            Clipboard.SetText(path);
        }

        private void CopyFileToFolder(string path)
        {
            using var dlg = new FolderBrowserDialog { Description = L.Text("S196") };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            var all = new List<string> { path };
            all.AddRange(DatabaseFileLocator.GetSqliteCompanions(path));
            var copied = new List<string>();
            foreach (var src in all)
            {
                var dest = Path.Combine(dlg.SelectedPath, Path.GetFileName(src));
                File.Copy(src, dest, overwrite: true);
                copied.Add(dest);
            }
            MessageBox.Show(L.Text("S197") + string.Join("\n", copied), string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private List<DatabaseInfo> GetSelectedResults()
        {
            return _allResults.Where(x => !string.IsNullOrEmpty(x.LocalPath) && _selectedPaths.Contains(x.LocalPath)).ToList();
        }

        private void BtnCopy_Click(object? sender, EventArgs e)
        {
            var selected = GetSelectedResults();
            if (selected.Count == 0)
            {
                _lblStatus.Text = L.Text("S198");
                return;
            }

            long totalBytes = selected.Sum(x => x.FileSize);
            if (selected.Count > 15 && totalBytes > 20L * 1024 * 1024 * 1024)
            {
                var msg = L.Pick(
                    $"شما تعداد {selected.Count} فایل به حجم کلی {DatabaseFileLocator.FormatSize(totalBytes)} انتخاب کرده‌اید.\nآیا مایل به انتقال به پنجره کپی هستید؟",
                    $"You have selected {selected.Count} files totaling {DatabaseFileLocator.FormatSize(totalBytes)}.\nDo you want to proceed to copy?");
                if (MessageBox.Show(this, msg, L.Pick("تأیید کپی حجم بالا", "Confirm Large Copy"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }
            }

            var form = new DatabaseCopyForm(selected);
            form.ShowDialog(this);
        }

        private static string? ShowPatternPrompt(IWin32Window owner, string prompt, string title, string defaultValue = "")
        {
            using var form = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ClientSize = new Size(420, 150),
                Font = UiFont.Make(9.5f),
                RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No,
                RightToLeftLayout = L.IsFa,
                BackColor = Color.White
            };

            var lbl = new Label
            {
                Text = prompt,
                Location = new Point(16, 16),
                Size = new Size(388, 36),
                TextAlign = L.IsFa ? ContentAlignment.TopRight : ContentAlignment.TopLeft
            };
            var txt = new TextBox
            {
                Text = defaultValue,
                Location = new Point(16, 56),
                Size = new Size(388, 28)
            };
            var btnOk = new Button
            {
                Text = L.Pick("تأیید", "OK"),
                DialogResult = DialogResult.OK,
                Location = new Point(216, 98),
                Size = new Size(90, 32),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            var btnCancel = new Button
            {
                Text = L.Pick("انصراف", "Cancel"),
                DialogResult = DialogResult.Cancel,
                Location = new Point(314, 98),
                Size = new Size(90, 32),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            form.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;

            return form.ShowDialog(owner) == DialogResult.OK ? txt.Text.Trim() : null;
        }

        private static bool IsWildcardMatch(string text, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern)) return true;
            if (string.IsNullOrWhiteSpace(text)) return false;

            if (!pattern.Contains('*') && !pattern.Contains('?'))
            {
                return text.Contains(pattern, StringComparison.OrdinalIgnoreCase);
            }

            var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(text, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        private void SelectShadowCopiesOnly()
        {
            _isUpdatingGridRows = true;
            _selectedPaths.Clear();
            int count = 0;
            try
            {
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.Tag is DatabaseInfo di && !string.IsNullOrEmpty(di.LocalPath))
                    {
                        bool match = di.IsFromShadowCopy;
                        row.Cells[0].Value = match;
                        if (match)
                        {
                            _selectedPaths.Add(di.LocalPath);
                            count++;
                        }
                    }
                }
            }
            finally
            {
                _isUpdatingGridRows = false;
            }
            UpdateSelectionSummary();
            _lblStatus.Text = L.Pick($"تعداد {count} فایل از نسخه‌های قبلی (Shadow Copies) انتخاب شدند.", $"{count} files from Shadow Copies selected.");
            _lblStatus.ForeColor = Color.FromArgb(33, 150, 243);
        }

        private void SelectDeletedFromDiskOnly()
        {
            _isUpdatingGridRows = true;
            _selectedPaths.Clear();
            int count = 0;
            try
            {
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.Tag is DatabaseInfo di && !string.IsNullOrEmpty(di.LocalPath))
                    {
                        bool match = di.IsFromShadowCopy && di.IsDeletedFromCurrentDisk;
                        row.Cells[0].Value = match;
                        if (match)
                        {
                            _selectedPaths.Add(di.LocalPath);
                            count++;
                        }
                    }
                }
            }
            finally
            {
                _isUpdatingGridRows = false;
            }
            UpdateSelectionSummary();
            _lblStatus.Text = L.Pick($"تعداد {count} فایل حذف‌شده از دیسک فعلی انتخاب شدند.", $"{count} deleted database files selected.");
            _lblStatus.ForeColor = Color.FromArgb(244, 67, 54);
        }

        private void CheckAndRestoreShadowFile()
        {
            var path = _ctxResults?.Tag as string;
            if (string.IsNullOrEmpty(path)) return;

            var target = _allResults.FirstOrDefault(x => string.Equals(x.LocalPath, path, StringComparison.OrdinalIgnoreCase))
                         ?? (_grid.CurrentRow?.Tag as DatabaseInfo);
            if (target == null || !target.IsFromShadowCopy) return;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(L.Pick($"نام فایل: {target.Name}", $"File Name: {target.Name}"));
            sb.AppendLine(L.Pick($"حجم فایل: {DatabaseFileLocator.FormatSize(target.FileSize)}", $"File Size: {DatabaseFileLocator.FormatSize(target.FileSize)}"));
            sb.AppendLine(L.Pick($"تاریخ Snapshot: {(target.ShadowCopyDate.HasValue ? JalaliDateHelper.Format(target.ShadowCopyDate.Value) : "-")}", $"Snapshot Date: {(target.ShadowCopyDate.HasValue ? JalaliDateHelper.Format(target.ShadowCopyDate.Value) : "-")}"));
            sb.AppendLine(L.Pick($"شناسه Snapshot: {target.ShadowCopyId}", $"Snapshot ID: {target.ShadowCopyId}"));
            sb.AppendLine(L.Pick($"مسیر متناظر در دیسک فعلی: {target.LiveDiskEquivalentPath ?? "-"}", $"Live Disk Path: {target.LiveDiskEquivalentPath ?? "-"}"));
            sb.AppendLine();

            bool existsLive = !string.IsNullOrEmpty(target.LiveDiskEquivalentPath) && File.Exists(target.LiveDiskEquivalentPath);
            if (existsLive)
            {
                var liveFi = new FileInfo(target.LiveDiskEquivalentPath!);
                sb.AppendLine(L.Pick($"وضعیت فعلی در دیسک: موجود است (حجم: {DatabaseFileLocator.FormatSize(liveFi.Length)}, تاریخ اصلاح: {JalaliDateHelper.Format(liveFi.LastWriteTime)})",
                                     $"Current Status on Live Disk: Exists (Size: {DatabaseFileLocator.FormatSize(liveFi.Length)}, Modified: {JalaliDateHelper.Format(liveFi.LastWriteTime)})"));
            }
            else
            {
                sb.AppendLine(L.Pick("وضعیت فعلی در دیسک: 🔥 حذف شده است (فقط در این نقطه بازیابی تاریخی وجود دارد!)",
                                     "Current Status on Live Disk: 🔥 DELETED (Only exists in this historical snapshot!)"));
            }

            sb.AppendLine();
            sb.AppendLine(L.Pick("آیا مایلید این نسخه تاریخی را در مسیری دلخواه ذخیره / بازیابی نمایید؟", "Would you like to export/restore this historical version to a destination path?"));

            var res = MessageBox.Show(sb.ToString(), L.Pick("شناسنامه نسخه قبلی (Shadow Copy)", "Shadow Copy Inspection"), MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (res == DialogResult.Yes)
            {
                using var sfd = new SaveFileDialog
                {
                    FileName = target.Name,
                    Title = L.Pick("ذخیره نسخه تاریخی پایگاه داده", "Save Historical Database Version"),
                    Filter = "All Files (*.*)|*.*"
                };
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        Cursor = Cursors.WaitCursor;
                        DatabaseFileLocator.CopyFileWithProgress(target.LocalPath!, sfd.FileName, (copied, total) => { });
                        Cursor = Cursors.Default;
                        MessageBox.Show(L.Pick("فایل با موفقیت استخراج و ذخیره شد.", "File exported successfully."), L.Pick("موفق", "Success"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Cursor = Cursors.Default;
                        MessageBox.Show(L.Pick($"خطا در استخراج فایل:\n{ex.Message}", $"Extraction error:\n{ex.Message}"), L.Pick("خطا", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void CreateNewShadowSnapshotDialog()
        {
            if (!SysadminGranter.IsAdministrator())
            {
                MessageBox.Show(
                    L.Pick("جهت ایجاد نقطه بازیابی Shadow Copy، دسترسی مدیر سیستم (Run as Administrator) الزامی است.",
                           "Administrator rights are required to create a Volume Shadow Copy."),
                    L.Pick("خطای دسترسی", "Access Denied"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed).Select(d => d.Name.TrimEnd('\\')).ToList();
            if (drives.Count == 0) drives.Add("C:");

            using var dlg = new Form
            {
                Text = L.Pick("ایجاد Shadow Copy فوری", "Create Immediate Shadow Copy"),
                Size = new Size(420, 220),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Font = UiFont.Make(9.5f),
                RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No
            };

            var lbl = new Label
            {
                Text = L.Pick("درایو مورد نظر برای ایجاد نقطه بازیابی Shadow Copy را انتخاب کنید:", "Select the drive to create a Volume Shadow Copy snapshot:"),
                Dock = DockStyle.Top,
                Height = 45,
                Padding = new Padding(12, 12, 12, 0)
            };
            var cmbDrives = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Top,
                Margin = new Padding(12)
            };
            foreach (var d in drives) cmbDrives.Items.Add(d);
            if (cmbDrives.Items.Count > 0) cmbDrives.SelectedIndex = 0;

            var btnOk = new Button
            {
                Text = L.Pick("ایجاد نقطه بازیابی", "Create Snapshot"),
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 34)
            };
            var btnCancel = new Button
            {
                Text = L.Pick("انصراف", "Cancel"),
                DialogResult = DialogResult.Cancel,
                Size = new Size(90, 34)
            };

            var pnlBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 50,
                Padding = new Padding(10)
            };
            pnlBottom.Controls.Add(btnCancel);
            pnlBottom.Controls.Add(btnOk);

            dlg.Controls.Add(pnlBottom);
            dlg.Controls.Add(cmbDrives);
            dlg.Controls.Add(lbl);

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                var targetDrive = cmbDrives.SelectedItem?.ToString() ?? "C:";
                Cursor = Cursors.WaitCursor;
                _lblStatus.Text = L.Pick($"در حال ساخت Shadow Copy از درایو {targetDrive}...", $"Creating Shadow Copy on {targetDrive}...");
                Application.DoEvents();

                var shadowDev = VolumeShadowCopy.TryCreateShadow(targetDrive, out var err);
                Cursor = Cursors.Default;

                if (!string.IsNullOrEmpty(shadowDev))
                {
                    MessageBox.Show(
                        L.Pick($"نقطه بازیابی با موفقیت ساخته شد:\n{shadowDev}", $"Shadow Copy created successfully:\n{shadowDev}"),
                        L.Pick("عملیات موفق", "Success"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    ReloadRoots();
                }
                else
                {
                    MessageBox.Show(
                        L.Pick($"خطا در ساخت Shadow Copy:\n{err}", $"Failed to create Shadow Copy:\n{err}"),
                        L.Pick("خطای VSS", "VSS Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }
    }
}
