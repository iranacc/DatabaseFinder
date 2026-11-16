namespace DatabaseFinder;

public partial class DiskScanForm
{
    protected override bool ModernLayout => true;
    private List<DiskFormat> _selectedFormats = DiskFormatRegistry.All;
    private Label _formatSummary = null!;
    private void BuildSearchLayout(GroupBox roots, GroupBox formats, GroupBox options, Button addRoot, Button allRoots, Button noRoots, Label results, Button allResults, Button noResults, Button close)
    {
        SuspendLayout(); Controls.Clear(); ClientSize = new Size(1220, 780); MinimumSize = new Size(1060, 660); AutoScaleMode = AutoScaleMode.Dpi;
        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(18), RightToLeft = RightToLeft.No };
        shell.ColumnStyles.Add(new ColumnStyle(L.IsFa ? SizeType.Percent : SizeType.Absolute, L.IsFa ? 100 : 295)); shell.ColumnStyles.Add(new ColumnStyle(L.IsFa ? SizeType.Absolute : SizeType.Percent, L.IsFa ? 295 : 100));
        var settings = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(10), BackColor = Color.White, RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No };
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        settings.RowStyles.Add(new RowStyle(SizeType.AutoSize)); settings.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); settings.RowStyles.Add(new RowStyle(SizeType.AutoSize)); settings.RowStyles.Add(new RowStyle(SizeType.AutoSize)); settings.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settings.Controls.Add(new Label { Text = L.Text("S159"), AutoSize = true, Font = UiFont.Make(12f, FontStyle.Bold), Padding = new Padding(5) }, 0, 0);
        var rootLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 }; rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rbQuick.AutoSize = true; _rbFull.AutoSize = true; _rbExternal.AutoSize = true; _rbShadow.AutoSize = true;
        var mode = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        mode.Controls.Add(_rbQuick); mode.Controls.Add(_rbFull); mode.Controls.Add(_rbExternal); mode.Controls.Add(_rbShadow);
        rootLayout.Controls.Add(mode, 0, 0);
        _lstRoots.Dock = DockStyle.Fill; _lstRoots.RightToLeft = RightToLeft.No; _lstRoots.HorizontalScrollbar = true; rootLayout.Controls.Add(_lstRoots, 0, 1);
        foreach (var b in new[] { allRoots, noRoots }) { b.AutoSize = false; b.Size = new Size(104, 34); b.MinimumSize = Size.Empty; }
        _btnRefreshRoots.AutoSize = false; _btnRefreshRoots.Size = new Size(84, 36); _btnRefreshRoots.MinimumSize = Size.Empty;
        _btnCreateShadow.AutoSize = false; _btnCreateShadow.Size = new Size(84, 36); _btnCreateShadow.MinimumSize = Size.Empty;
        addRoot.AutoSize = false; addRoot.Size = new Size(100, 36);
        rootLayout.Controls.Add(UiTheme.Flow(allRoots, noRoots), 0, 2);
        rootLayout.Controls.Add(UiTheme.Flow(addRoot, _btnRefreshRoots, _btnCreateShadow), 0, 3);
        settings.Controls.Add(rootLayout, 0, 1);
        _formatSummary = new Label { AutoSize = true, MaximumSize = new Size(255, 0), Padding = new Padding(5) };
        var choose = UiTheme.Button(L.Text("ChooseFormats"), (_, _) => { using var picker = new FormatPickerForm(_selectedFormats); if (picker.ShowDialog(this) == DialogResult.OK) { _selectedFormats = picker.SelectedFormats; UpdateFormatSummary(); } }); choose.AutoSize = false; choose.Size = new Size(230, 40);
        settings.Controls.Add(UiTheme.Flow(_formatSummary, choose), 0, 2);
        settings.Controls.Add(UiTheme.Flow(new Label { Text = L.Text("S166"), AutoSize = true }, _numMinSize, new Label { Text = "MB", AutoSize = true }), 0, 3);
        var optFlow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = Padding.Empty };
        optFlow.Controls.Add(_chkDeepCarving);
        optFlow.Controls.Add(_chkArchivePeeking);
        settings.Controls.Add(optFlow, 0, 4);
        var tip = new ToolTip();
        tip.SetToolTip(_rbQuick, L.Pick("جستجوی مسیرهای متداول حسابداری، دیتابیس‌های SQL Server و درایوهای اکسترنال متصل", "Smart scan of accounting directories, SQL Server data, and connected external drives"));
        tip.SetToolTip(_rbFull, L.Pick("پویش کامل تمام درایوهای سیستم، حافظه‌های جانبی و درایوهای شبکه", "Full scan of all fixed, removable and network drives"));
        tip.SetToolTip(_rbExternal, L.Pick("پویش اختصاصی فلش‌مموری‌ها و حافظه‌های USB اکسترنال متصل به سیستم", "Scan only connected removable USB flash drives"));
        tip.SetToolTip(_rbShadow, L.Pick("پویش در نسخه‌های قبلی ویندوز (Volume Shadow Copies / VSS) برای کشف دیتابیس‌های حذف‌شده یا تغییریافته", "Scan Windows Volume Shadow Copies (VSS) to discover deleted or modified databases"));
        tip.SetToolTip(_btnRefreshRoots, L.Pick("بازخوانی مجدد فهرست درایوها و مسیرها", "Refresh drives and smart paths"));
        tip.SetToolTip(_btnCreateShadow, L.Pick("ایجاد یک نقطه بازیابی فوری (Shadow Copy) از درایو انتخابی سیستم قبل از هرگونه تغییر", "Create an immediate Volume Shadow Copy snapshot of a system drive"));
        Disposed += (_, _) => tip.Dispose();
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7, Padding = new Padding(10), RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 7));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.Controls.Add(new Label { Text = L.Text("S158"), AutoSize = true, Font = UiFont.Make(18f, FontStyle.Bold), Padding = new Padding(5, 5, 5, 12) }, 0, 0);
        foreach (var b in new[] { _btnScan, _btnCancelScan, allResults, noResults, _btnCopy, _btnMerge, close }) { b.AutoSize = true; b.MinimumSize = new Size(80, 34); }
        _btnScan.Tag = "primary"; _btnCopy.Tag = "primary";
        allResults.Text = L.Pick("انتخاب همه", "Select all");
        noResults.Text = L.Pick("لغو همه", "None");
        content.Controls.Add(UiTheme.Flow(_btnScan, _btnCancelScan), 0, 1);
        _bar.Dock = DockStyle.Fill; _bar.Margin = Padding.Empty; content.Controls.Add(_bar, 0, 2);

        _toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true,
            Margin = new Padding(0, 4, 0, 2),
            Padding = Padding.Empty,
            BackColor = Color.Transparent,
            RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No
        };
        _toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _toolbar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _toolbar.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var filterRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = L.IsFa ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent,
            RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No
        };

        filterRow.Controls.Add(_txtFilter);
        filterRow.Controls.Add(_btnClearFilter);
        filterRow.Controls.Add(_cmbFilterType);
        filterRow.Controls.Add(_cmbFilterDate);
        filterRow.Controls.Add(_btnSmartSelect);
        filterRow.Controls.Add(allResults);
        filterRow.Controls.Add(noResults);

        var summaryRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = L.IsFa ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0, 3, 0, 0),
            Padding = Padding.Empty,
            BackColor = Color.Transparent,
            RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No
        };
        summaryRow.Controls.Add(_lblSelectionSummary);

        _toolbar.Controls.Add(filterRow, 0, 0);
        _toolbar.Controls.Add(summaryRow, 0, 1);
        content.Controls.Add(_toolbar, 0, 3);

        _grid.Dock = DockStyle.Fill; _grid.Margin = new Padding(0, 4, 0, 0); _grid.RightToLeft = RightToLeft.No;
        _grid.Columns["sel"].Width = 65; _grid.Columns["path"].Width = 260; _grid.Columns["format"].Width = 120; _grid.Columns["ext"].Width = 80; _grid.Columns["name"].Width = 145; _grid.Columns["size"].Width = 85; _grid.Columns["date"].Width = 135; _grid.Columns["backup"].Width = 65;
        content.Controls.Add(_grid, 0, 4);
        _lblStatus.Dock = DockStyle.Fill; _lblStatus.AutoSize = false; _lblStatus.AutoEllipsis = true; _lblStatus.Padding = new Padding(5); content.Controls.Add(_lblStatus, 0, 5);
        content.Controls.Add(UiTheme.Flow(_btnCopy, _btnMerge, close), 0, 6);
        shell.Controls.Add(settings, L.IsFa ? 1 : 0, 0); shell.Controls.Add(content, L.IsFa ? 0 : 1, 0); Controls.Add(shell);
        _btnCancelScan.EnabledChanged += (_, _) => settings.Enabled = !_btnCancelScan.Enabled;
        FormClosing += (_, e) => { if (_btnCancelScan.Enabled) { _cts?.Cancel(); e.Cancel = true; _lblStatus.Text = L.Text("StoppingScan"); } };
        // The old format list is no longer part of the visual tree.
        roots.Dispose(); formats.Dispose(); options.Dispose(); results.Dispose();
        UpdateFormatSummary(); ResumeLayout(true);
    }
    private void UpdateFormatSummary() => _formatSummary.Text = L.Format("FormatSummary", _selectedFormats.Count, _selectedFormats.Sum(f => f.Extensions.Length));
}
