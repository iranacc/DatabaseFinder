namespace DatabaseFinder;

public sealed record MainViewState(List<DatabaseInfo> Results, HashSet<int> Checked, int Mode);

public partial class Form1
{
    protected override bool ModernLayout => true;
    public event Action<string>? LanguageRequested;
    private readonly MainViewState? _restored;
    private Label _selectionCount = null!;
    private TextBox _pathDetail = null!;
    private LinkLabel _lnkUpdate = null!;
    private Label _detailTitle = null!;
    private Label _detailInfo = null!;
    private Button _btnCopyPath = null!;
    private Label _metricTotal = null!;
    private Label _metricOnline = null!;
    private Label _metricFiles = null!;
    private Label _metricSelected = null!;
    private Button _btnStartService = null!;
    private Button _btnEnableService = null!;
    private Label _emptyState = null!;
    private ToolTip _toolTip = null!;
    private Button _btnTheme = null!;
    public MainViewState CaptureView()
    {
        dgvDatabases.EndEdit();
        var selected = dgvDatabases.Rows.Cast<DataGridViewRow>().Where(r => Convert.ToBoolean(r.Cells["colCheck"].Value ?? false)).Select(r => _lastResults.IndexOf((DatabaseInfo)r.Tag!)).ToHashSet();
        return new(_lastResults.ToList(), selected, cmbScanMode.SelectedIndex);
    }
    private void RestoreView(MainViewState state)
    {
        _lastResults = state.Results;
        var models = state.Results.Select(BuildModel).ToList();
        for (var i = 0; i < models.Count; i++) models[i].Selected = state.Checked.Contains(i);
        RebindGrid(models);
        cmbScanMode.SelectedIndexChanged -= cmbScanMode_SelectedIndexChanged;
        cmbScanMode.SelectedIndex = state.Mode;
        cmbScanMode.SelectedIndexChanged += cmbScanMode_SelectedIndexChanged;
        lblStatus.Text = L.Format("S228", models.Count);
        UpdateSelection();
    }
    public void CloseForLanguageChange() { Close(); }
    private void BuildModernShell()
    {
        SuspendLayout();
        Controls.Clear();
        Text = "Database Finder " + UpdateChecker.VersionString(UpdateChecker.CurrentVersion) + " • MSAM Group"
            + (AppPaths.IsPortable ? " • Portable" : "");
        ClientSize = new Size(1180, 720);
        MinimumSize = new Size(900, 600);
        AutoScaleMode = AutoScaleMode.Dpi;
        KeyPreview = true;
        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, RightToLeft = RightToLeft.No };
        shell.ColumnStyles.Add(new ColumnStyle(L.IsFa ? SizeType.Percent : SizeType.Absolute, L.IsFa ? 100 : 190));
        shell.ColumnStyles.Add(new ColumnStyle(L.IsFa ? SizeType.Absolute : SizeType.Percent, L.IsFa ? 190 : 100));
        var nav = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(12, 24, 12, 12), RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No };
        nav.Controls.Add(new Label { Text = "Database Finder\nMSAM GROUP", Font = new Font("Segoe UI", 12, FontStyle.Bold), Size = new Size(162, 86), ForeColor = UiTheme.Accent });
        var online = UiTheme.Button(L.Text("RunningDatabases"), (_, _) => { cmbScanMode.SelectedIndex = 0; btnRefresh.PerformClick(); }, true);
        var offline = UiTheme.Button(L.Text("FileSearch"), (_, _) => RunOfflineScan());
        foreach (var button in new[] { online, offline, btnRemote, btnProfiles, btnSettings })
        {
            button.AutoSize = false; button.Size = new Size(150, 44); button.Margin = new Padding(0, 4, 0, 4); nav.Controls.Add(button);
        }
        online.Image = UiTheme.Icon(UiTheme.UiIcon.Database);
        offline.Image = UiTheme.Icon(UiTheme.UiIcon.Folder);
        btnRemote.Image = UiTheme.Icon(UiTheme.UiIcon.Network);
        btnProfiles.Image = UiTheme.Icon(UiTheme.UiIcon.Profile);
        btnSettings.Image = UiTheme.Icon(UiTheme.UiIcon.Settings);
        foreach (var button in new[] { online, offline, btnRemote, btnProfiles, btnSettings })
        {
            button.ImageAlign = ContentAlignment.MiddleLeft;
            button.TextImageRelation = TextImageRelation.ImageBeforeText;
        }
        _toolTip = new ToolTip { AutoPopDelay = 5000, InitialDelay = 350, ReshowDelay = 100 };
        _btnTheme = UiTheme.Button(UiTheme.Dark ? L.Text("S417") : L.Text("S415"), (_, _) => ToggleTheme());
        _btnTheme.Image = UiTheme.Icon(UiTheme.UiIcon.Settings);
        _btnTheme.ImageAlign = ContentAlignment.MiddleLeft;
        _btnTheme.TextImageRelation = TextImageRelation.ImageBeforeText;
        foreach (var button in new[] { btnRefresh, btnCopyFiles, btnBackup, btnQuery, btnTest, btnRemote, btnProfiles, btnSettings, _btnTheme })
            _toolTip.SetToolTip(button, button.Text);
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 7, RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        lblTitle.Text = L.Text("MainTitle");
        lblTitle.Font = new Font("Segoe UI", 18, FontStyle.Bold); lblTitle.Margin = new Padding(5, 8, 16, 8);
        var language = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 132, AccessibleName = "Language / زبان", RightToLeft = RightToLeft.No };
        language.Items.AddRange(new object[] { "فارسی", "English" }); language.SelectedIndex = L.IsFa ? 0 : 1;
        language.SelectedIndexChanged += (_, _) => LanguageRequested?.Invoke(language.SelectedIndex == 0 ? "fa" : "en");
        _lnkUpdate = new LinkLabel
        {
            AutoSize = true,
            Visible = false,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            LinkColor = UiTheme.Accent,
            ActiveLinkColor = UiTheme.Accent,
            LinkBehavior = LinkBehavior.HoverUnderline,
            Padding = new Padding(4, 9, 4, 0),
            Margin = new Padding(14, 0, 0, 0),
        };
        _lnkUpdate.Click += _lnkUpdate_Click;
        content.Controls.Add(UiTheme.Flow(lblTitle, language, _btnTheme, _lnkUpdate), 0, 0);
        btnRefresh.Tag = "primary";
        btnRefresh.AutoSize = true; btnRefresh.MinimumSize = new Size(110, 36);
        cmbScanMode.Width = 235;
        var all = UiTheme.Button(L.Text("S034"), (_, _) => CheckAll(true));
        var none = UiTheme.Button(L.Text("S163"), (_, _) => CheckAll(false));
        content.Controls.Add(UiTheme.Flow(cmbScanMode, btnRefresh, all, none), 0, 1);
        var metrics = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 4),
            FlowDirection = L.IsFa ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No
        };
        metrics.Controls.Add(BuildMetric(L.Text("S395"), Color.FromArgb(36, 91, 214), out _metricTotal));
        metrics.Controls.Add(BuildMetric(L.Text("S396"), Color.FromArgb(0, 150, 136), out _metricOnline));
        metrics.Controls.Add(BuildMetric(L.Text("S397"), Color.FromArgb(255, 152, 0), out _metricFiles));
        metrics.Controls.Add(BuildMetric(L.Text("S398"), Color.FromArgb(76, 175, 80), out _metricSelected));
        content.Controls.Add(metrics, 0, 2);
        dgvDatabases.Dock = DockStyle.Fill; dgvDatabases.Margin = new Padding(0, 6, 0, 0);
        dgvDatabases.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        colCheck.Width = 80; colType.Width = 135; colStatus.Width = 115; colVersion.Width = 90; colPort.Width = 65; colService.Width = 155;
        colProcess.Visible = false; colLocation.Visible = false; colSizeInfo.Visible = false;
        colHow.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; colHow.MinimumWidth = 135;
        dgvDatabases.CellFormatting += (_, e) =>
        {
            if (e.ColumnIndex < 0) return;
            var column = dgvDatabases.Columns[e.ColumnIndex].Name;
            if (column is "colType" or "colService" or "colVersion")
                e.CellStyle!.Alignment = DataGridViewContentAlignment.MiddleLeft;
            if (column == "colStatus" && e.RowIndex >= 0 && e.RowIndex < dgvDatabases.Rows.Count)
            {
                var db = dgvDatabases.Rows[e.RowIndex].Tag as DatabaseInfo;
                var color = UiTheme.Dark
                    ? db?.IsServiceStopped == true
                        ? Color.FromArgb(92, 76, 42)
                        : db?.IsOnline == true ? Color.FromArgb(42, 82, 67) : Color.FromArgb(86, 61, 43)
                    : db?.IsServiceStopped == true
                        ? Color.FromArgb(255, 243, 205)
                        : db?.IsOnline == true ? Color.FromArgb(220, 244, 231) : Color.FromArgb(255, 235, 218);
                var style = e.CellStyle!;
                style.BackColor = color;
                style.SelectionBackColor = color;
                style.ForeColor = UiTheme.Ink;
                style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }
        };
        _emptyState = new Label
        {
            Dock = DockStyle.Fill,
            Text = L.Text("S416"),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = UiTheme.Muted,
            BackColor = UiTheme.Surface,
            Font = new Font("Segoe UI", 11F),
            Visible = false
        };
        var gridHost = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface };
        gridHost.Controls.Add(dgvDatabases); gridHost.Controls.Add(_emptyState);
        content.Controls.Add(gridHost, 0, 3);
        _detailTitle = new Label { Dock = DockStyle.Top, Height = 25, Padding = new Padding(8, 3, 8, 0), Text = L.Text("SelectDetails"), Font = new Font("Segoe UI", 10.5F, FontStyle.Bold) };
        _detailInfo = new Label { Dock = DockStyle.Top, Height = 23, Padding = new Padding(8, 1, 8, 0), AutoEllipsis = true };
        _pathDetail = new TextBox { ReadOnly = true, BorderStyle = BorderStyle.None, Width = 700, Height = 26, BackColor = Color.White, RightToLeft = RightToLeft.No };
        _btnCopyPath = UiTheme.Button(L.Text("S410"), (_, _) => CopySelectedPath());
        _btnCopyPath.MinimumSize = new Size(105, 28); _btnCopyPath.Enabled = false;
        var pathRow = UiTheme.Flow(_pathDetail, _btnCopyPath); pathRow.Dock = DockStyle.Bottom; pathRow.Height = 32; pathRow.Padding = new Padding(4, 0, 4, 0);
        var detail = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(9) };
        detail.Controls.Add(pathRow); detail.Controls.Add(_detailInfo); detail.Controls.Add(_detailTitle); content.Controls.Add(detail, 0, 4);
        _selectionCount = new Label { AutoSize = true, Padding = new Padding(4, 9, 4, 0) };
        _btnStartService = UiTheme.Button(L.Text("S399"), async (_, _) => await StartSelectedServiceAsync(false));
        _btnEnableService = UiTheme.Button(L.Text("S400"), async (_, _) => await StartSelectedServiceAsync(true));
        _btnStartService.Visible = false;
        _btnEnableService.Visible = false;
        foreach (var b in new[] { btnCopyFiles, btnBackup, btnQuery, btnTest, btnCopy }) { b.AutoSize = true; b.MinimumSize = new Size(100, 36); }
        btnCopyFiles.Tag = "primary";
        content.Controls.Add(UiTheme.Flow(_selectionCount, _btnStartService, _btnEnableService, btnCopyFiles, btnBackup, btnTest, btnQuery, btnCopy), 0, 5);
        lblStatus.Dock = DockStyle.Fill; lblStatus.AutoSize = false; lblStatus.AutoEllipsis = true; content.Controls.Add(lblStatus, 0, 6);
        shell.Controls.Add(nav, L.IsFa ? 1 : 0, 0); shell.Controls.Add(content, L.IsFa ? 0 : 1, 0); Controls.Add(shell);
        UiTheme.Apply(this);
        dgvDatabases.CurrentCellDirtyStateChanged += (_, _) => { if (dgvDatabases.IsCurrentCellDirty) dgvDatabases.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        dgvDatabases.CellValueChanged += (_, _) => UpdateSelection();
        dgvDatabases.DataBindingComplete += (_, _) => UpdateSelection();
        dgvDatabases.SelectionChanged += (_, _) => UpdateDetails();
        UpdateSelection(); ResumeLayout(true);
    }
    private static Panel BuildMetric(string title, Color accent, out Label value)
    {
        var panel = new Panel
        {
            BackColor = UiTheme.Surface,
            Size = new Size(166, 64),
            Margin = new Padding(0, 0, 10, 0),
            Padding = new Padding(12, 7, 12, 6)
        };
        var stripe = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = accent };
        var caption = new Label { Text = title, Dock = DockStyle.Top, Height = 20, ForeColor = UiTheme.Muted, Font = new Font("Segoe UI", 8.5F), AutoEllipsis = true };
        value = new Label { Text = "0", Dock = DockStyle.Fill, ForeColor = UiTheme.Ink, Font = new Font("Segoe UI", 17F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
        panel.Controls.Add(value); panel.Controls.Add(caption); panel.Controls.Add(stripe);
        return panel;
    }
    private void CheckAll(bool value)
    {
        foreach (DataGridViewRow row in dgvDatabases.Rows) row.Cells["colCheck"].Value = value;
        dgvDatabases.EndEdit(); UpdateSelection();
    }
    private void UpdateSelection()
    {
        if (_selectionCount == null) return;
        var count = dgvDatabases.Rows.Cast<DataGridViewRow>().Count(r => Convert.ToBoolean(r.Cells["colCheck"].Value ?? false));
        _selectionCount.Text = L.Format("SelectedCount", count);
        btnCopyFiles.Enabled = count > 0; btnBackup.Enabled = count > 0;
        if (_metricTotal != null)
        {
            _metricTotal.Text = _lastResults.Count.ToString();
            _metricOnline.Text = _lastResults.Count(d => d.IsOnline).ToString();
            _metricFiles.Text = _lastResults.Count(d => !d.IsOnline).ToString();
            _metricSelected.Text = count.ToString();
        }
        UpdateServiceActions();
    }
    private void UpdateDetails()
    {
        if (_detailTitle == null || dgvDatabases.CurrentRow?.Tag is not DatabaseInfo db) return;
        var status = db.IsServiceStopped
            ? L.Text("S383")
            : db.IsOnline ? L.Text("S406") : db.IsBackup ? L.Text("S407") : L.Text("S408");
        _detailTitle.Text = $"{db.Name}  •  {db.TypeDisplayName}";
        _detailInfo.Text = string.Join("   |   ", new[]
        {
            status,
            string.IsNullOrWhiteSpace(db.Version) ? "-" : db.Version,
            db.IsServiceStopped ? (db.ServiceName ?? "-") : $"{db.Host}:{db.Port}"
        });
        _pathDetail.Text = db.IsOnline
            ? $"{db.Host}:{db.Port}   |   {db.ServiceName}"
            : (db.LocalPath ?? db.ServiceName ?? "-");
        _btnCopyPath.Enabled = !string.IsNullOrWhiteSpace(db.LocalPath);
        UpdateServiceActions();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F5) { btnRefresh.PerformClick(); return true; }
        if (keyData == (Keys.Control | Keys.A)) { CheckAll(true); return true; }
        if (keyData == (Keys.Control | Keys.Shift | Keys.A)) { CheckAll(false); return true; }
        if (keyData == (Keys.Control | Keys.C) && dgvDatabases.Focused) { btnCopy.PerformClick(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }
    private void CopySelectedPath()
    {
        if (string.IsNullOrWhiteSpace(_pathDetail.Text) || _pathDetail.Text == "-") return;
        Clipboard.SetText(_pathDetail.Text);
        lblStatus.Text = L.Text("S411");
    }
    private void ToggleTheme()
    {
        var view = CaptureView();
        _settings.DarkMode = !UiTheme.Dark;
        _settings.Save();
        UiTheme.Dark = _settings.DarkMode;
        BuildModernShell();
        RestoreView(view);
    }
    private void ShowShortcuts()
    {
        MessageBox.Show(this, L.Text("S419"), L.Text("S418"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    private void UpdateServiceActions()
    {
        if (_btnStartService == null || _btnEnableService == null) return;
        var db = dgvDatabases.CurrentRow?.Tag as DatabaseInfo;
        var canStart = db?.IsServiceStopped == true && !string.IsNullOrWhiteSpace(db.ServiceName);
        var mode = db?.ServiceStartMode ?? "";
        _btnStartService.Visible = canStart && !string.Equals(mode, "Disabled", StringComparison.OrdinalIgnoreCase);
        _btnEnableService.Visible = canStart && string.Equals(mode, "Disabled", StringComparison.OrdinalIgnoreCase);
    }
    private async Task StartSelectedServiceAsync(bool enable)
    {
        if (dgvDatabases.CurrentRow?.Tag is not DatabaseInfo db ||
            !db.IsServiceStopped || string.IsNullOrWhiteSpace(db.ServiceName)) return;

        var prompt = enable ? L.Format("S402", db.ServiceName) : L.Format("S401", db.ServiceName);
        if (MessageBox.Show(prompt, L.Text("S337"), MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes) return;

        _btnStartService.Enabled = false;
        _btnEnableService.Enabled = false;
        var success = await Task.Run(() => DbServiceHelper.StartService(db.ServiceName!, enable, out var error)
            ? (Success: true, Error: (string?)null)
            : (Success: false, Error: error));
        if (!success.Success)
        {
            MessageBox.Show(success.Error ?? L.Text("S404"), L.Text("S337"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateServiceActions();
            return;
        }

        lblStatus.Text = L.Format("S403", db.ServiceName);
        await DetectDatabasesAsync();
    }
}
