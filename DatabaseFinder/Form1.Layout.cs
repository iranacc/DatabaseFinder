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
        Text = "Database Finder " + UpdateChecker.VersionString(UpdateChecker.CurrentVersion) + " • MSAM Group";
        ClientSize = new Size(1200, 740);
        MinimumSize = new Size(1040, 650);
        AutoScaleMode = AutoScaleMode.Dpi;
        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, RightToLeft = RightToLeft.No };
        shell.ColumnStyles.Add(new ColumnStyle(L.IsFa ? SizeType.Percent : SizeType.Absolute, L.IsFa ? 100 : 190));
        shell.ColumnStyles.Add(new ColumnStyle(L.IsFa ? SizeType.Absolute : SizeType.Percent, L.IsFa ? 190 : 100));
        var nav = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(12, 24, 12, 12), RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No };
        nav.Controls.Add(new Label { Text = "Database Finder\nMSAM GROUP", Font = new Font("Segoe UI", 12, FontStyle.Bold), Size = new Size(162, 86), ForeColor = UiTheme.Accent });
        var online = UiTheme.Button(L.Text("RunningDatabases"), (_, _) => { cmbScanMode.SelectedIndex = 0; btnRefresh.PerformClick(); }, true);
        var offline = UiTheme.Button(L.Text("FileSearch"), (_, _) => RunOfflineScan());
        foreach (var button in new[] { online, offline, btnRemote, btnProfiles, btnSettings })
        {
            button.AutoSize = false; button.Size = new Size(162, 44); button.Margin = new Padding(0, 4, 0, 4); nav.Controls.Add(button);
        }
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(22), ColumnCount = 1, RowCount = 6, RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
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
        content.Controls.Add(UiTheme.Flow(lblTitle, language, _lnkUpdate), 0, 0);
        btnRefresh.Tag = "primary";
        btnRefresh.AutoSize = true; btnRefresh.MinimumSize = new Size(110, 36);
        cmbScanMode.Width = 235;
        var all = UiTheme.Button(L.Text("S034"), (_, _) => CheckAll(true));
        var none = UiTheme.Button(L.Text("S163"), (_, _) => CheckAll(false));
        content.Controls.Add(UiTheme.Flow(cmbScanMode, btnRefresh, all, none), 0, 1);
        dgvDatabases.Dock = DockStyle.Fill; dgvDatabases.Margin = new Padding(0, 12, 0, 0);
        dgvDatabases.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        colCheck.Width = 80; colType.Width = 145; colVersion.Width = 90; colPort.Width = 65; colService.Width = 155;
        colProcess.Visible = false; colLocation.Visible = false; colSizeInfo.Visible = false;
        colHow.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; colHow.MinimumWidth = 135;
        dgvDatabases.CellFormatting += (_, e) => { if (e.ColumnIndex >= 0 && dgvDatabases.Columns[e.ColumnIndex].Name is "colType" or "colService" or "colVersion") e.CellStyle!.Alignment = DataGridViewContentAlignment.MiddleLeft; };
        content.Controls.Add(dgvDatabases, 0, 2);
        _detailTitle = new Label { Dock = DockStyle.Top, Height = 30, Padding = new Padding(8, 5, 8, 0), Text = L.Text("SelectDetails") };
        _pathDetail = new TextBox { Dock = DockStyle.Bottom, ReadOnly = true, BorderStyle = BorderStyle.None, Height = 26, BackColor = Color.White, RightToLeft = RightToLeft.No };
        var detail = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(9) }; detail.Controls.Add(_detailTitle); detail.Controls.Add(_pathDetail); content.Controls.Add(detail, 0, 3);
        _selectionCount = new Label { AutoSize = true, Padding = new Padding(4, 9, 4, 0) };
        foreach (var b in new[] { btnCopyFiles, btnBackup, btnQuery, btnTest, btnCopy }) { b.AutoSize = true; b.MinimumSize = new Size(100, 36); }
        btnCopyFiles.Tag = "primary";
        content.Controls.Add(UiTheme.Flow(_selectionCount, btnCopyFiles, btnBackup, btnTest, btnQuery, btnCopy), 0, 4);
        lblStatus.Dock = DockStyle.Fill; lblStatus.AutoSize = false; lblStatus.AutoEllipsis = true; content.Controls.Add(lblStatus, 0, 5);
        shell.Controls.Add(nav, L.IsFa ? 1 : 0, 0); shell.Controls.Add(content, L.IsFa ? 0 : 1, 0); Controls.Add(shell);
        dgvDatabases.CurrentCellDirtyStateChanged += (_, _) => { if (dgvDatabases.IsCurrentCellDirty) dgvDatabases.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        dgvDatabases.CellValueChanged += (_, _) => UpdateSelection();
        dgvDatabases.DataBindingComplete += (_, _) => UpdateSelection();
        dgvDatabases.SelectionChanged += (_, _) => UpdateDetails();
        UpdateSelection(); ResumeLayout(true);
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
    }
    private void UpdateDetails()
    {
        if (_detailTitle == null || dgvDatabases.CurrentRow?.Tag is not DatabaseInfo db) return;
        _detailTitle.Text = $"{db.Name}  •  {db.ProcessName}  PID: {db.ProcessId}";
        _pathDetail.Text = db.IsOnline ? $"{db.Host}:{db.Port}   |   {db.ServiceName}" : $"{db.LocalPath}   |   {FormatFileSize(db.FileSize)}   |   {db.FileModified:yyyy-MM-dd HH:mm}";
    }
}
