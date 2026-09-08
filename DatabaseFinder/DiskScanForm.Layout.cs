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
        settings.Controls.Add(new Label { Text = L.Text("S159"), AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold), Padding = new Padding(5) }, 0, 0);
        var rootLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 }; rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rbQuick.AutoSize = true; _rbFull.AutoSize = true;
        var mode = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false }; mode.Controls.Add(_rbQuick); mode.Controls.Add(_rbFull); rootLayout.Controls.Add(mode, 0, 0);
        _lstRoots.Dock = DockStyle.Fill; _lstRoots.RightToLeft = RightToLeft.No; _lstRoots.HorizontalScrollbar = true; rootLayout.Controls.Add(_lstRoots, 0, 1);
        foreach (var b in new[] { allRoots, noRoots }) { b.AutoSize = false; b.Size = new Size(104, 34); b.MinimumSize = Size.Empty; }
        addRoot.AutoSize = false; addRoot.Size = new Size(230, 38);
        rootLayout.Controls.Add(UiTheme.Flow(allRoots, noRoots), 0, 2); rootLayout.Controls.Add(UiTheme.Flow(addRoot), 0, 3); settings.Controls.Add(rootLayout, 0, 1);
        _formatSummary = new Label { AutoSize = true, MaximumSize = new Size(255, 0), Padding = new Padding(5) };
        var choose = UiTheme.Button(L.Text("ChooseFormats"), (_, _) => { using var picker = new FormatPickerForm(_selectedFormats); if (picker.ShowDialog(this) == DialogResult.OK) { _selectedFormats = picker.SelectedFormats; UpdateFormatSummary(); } }); choose.AutoSize = false; choose.Size = new Size(230, 40);
        settings.Controls.Add(UiTheme.Flow(_formatSummary, choose), 0, 2);
        settings.Controls.Add(UiTheme.Flow(new Label { Text = L.Text("S166"), AutoSize = true }, _numMinSize, new Label { Text = "MB", AutoSize = true }), 0, 3);
        var tip = new ToolTip(); tip.SetToolTip(_rbQuick, L.Text("S168")); tip.SetToolTip(_rbFull, L.Text("S168")); Disposed += (_, _) => tip.Dispose();
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(10), RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No }; content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize)); content.RowStyles.Add(new RowStyle(SizeType.AutoSize)); content.RowStyles.Add(new RowStyle(SizeType.Absolute, 7)); content.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); content.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.Controls.Add(new Label { Text = L.Text("S158"), AutoSize = true, Font = new Font("Segoe UI", 18, FontStyle.Bold), Padding = new Padding(5, 5, 5, 15) }, 0, 0);
        foreach (var b in new[] { _btnScan, _btnCancelScan, allResults, noResults, _btnCopy, _btnMerge, close }) { b.AutoSize = true; b.MinimumSize = new Size(90, 36); }
        _btnScan.Tag = "primary"; _btnCopy.Tag = "primary";
        content.Controls.Add(UiTheme.Flow(_btnScan, _btnCancelScan, allResults, noResults), 0, 1);
        _bar.Dock = DockStyle.Fill; _bar.Margin = Padding.Empty; content.Controls.Add(_bar, 0, 2);
        _grid.Dock = DockStyle.Fill; _grid.Margin = new Padding(0, 8, 0, 0); _grid.RightToLeft = RightToLeft.No;
        _grid.Columns["sel"].Width = 80; _grid.Columns["path"].Width = 270; _grid.Columns["format"].Width = 135; _grid.Columns["ext"].Width = 110; _grid.Columns["name"].Width = 150;
        content.Controls.Add(_grid, 0, 3);
        _lblStatus.Dock = DockStyle.Fill; _lblStatus.AutoSize = false; _lblStatus.AutoEllipsis = true; _lblStatus.Padding = new Padding(5); content.Controls.Add(_lblStatus, 0, 4);
        content.Controls.Add(UiTheme.Flow(_btnCopy, _btnMerge, close), 0, 5);
        shell.Controls.Add(settings, L.IsFa ? 1 : 0, 0); shell.Controls.Add(content, L.IsFa ? 0 : 1, 0); Controls.Add(shell);
        _btnCancelScan.EnabledChanged += (_, _) => settings.Enabled = !_btnCancelScan.Enabled;
        FormClosing += (_, e) => { if (_btnCancelScan.Enabled) { _cts?.Cancel(); e.Cancel = true; _lblStatus.Text = L.Text("StoppingScan"); } };
        // The old format list is no longer part of the visual tree.
        roots.Dispose(); formats.Dispose(); options.Dispose(); results.Dispose();
        UpdateFormatSummary(); ResumeLayout(true);
    }
    private void UpdateFormatSummary() => _formatSummary.Text = L.Format("FormatSummary", _selectedFormats.Count, _selectedFormats.Sum(f => f.Extensions.Length));
}
