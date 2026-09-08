namespace DatabaseFinder;

internal static class OperationLayout
{
    public static void Build(Form form, Label title, TextBox destination, Button browse, Label? hint, TreeView tree, Control[] selectionActions, GroupBox? locked, Label logLabel, TextBox log, Label status, params Button[] actions)
    {
        form.SuspendLayout(); form.Controls.Clear(); form.ClientSize = new Size(1060, 780); form.MinimumSize = new Size(980, 730); form.AutoScaleMode = AutoScaleMode.Dpi;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(22), ColumnCount = 1, RowCount = 7 }; root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 130)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        title.Font = new Font("Segoe UI", 17, FontStyle.Bold); title.Padding = new Padding(0, 0, 0, 15); root.Controls.Add(title, 0, 0);
        var dest = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, AutoSize = true, Padding = new Padding(8), BackColor = Color.White, RightToLeft = RightToLeft.No }; dest.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); dest.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); destination.Dock = DockStyle.Fill; destination.Margin = new Padding(5); browse.AutoSize = true; browse.MinimumSize = new Size(110, 32); dest.Controls.Add(destination, 0, 0); dest.Controls.Add(browse, 1, 0);
        if (hint != null) { hint.AutoSize = true; hint.MaximumSize = new Size(920, 0); hint.Padding = new Padding(5); dest.Controls.Add(hint, 0, 1); dest.SetColumnSpan(hint, 2); }
        root.Controls.Add(dest, 0, 1);
        var middle = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 14, 0, 0), RightToLeft = RightToLeft.No }; middle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); middle.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
        tree.Dock = DockStyle.Fill; tree.Margin = new Padding(0, 0, 12, 0); tree.RightToLeft = RightToLeft.No; middle.Controls.Add(tree, 0, 0);
        var side = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 }; side.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); side.RowStyles.Add(new RowStyle(SizeType.AutoSize)); side.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        foreach (var b in selectionActions.OfType<Button>()) { b.AutoSize = true; b.MinimumSize = new Size(125, 34); b.MaximumSize = new Size(292, 0); }
        side.Controls.Add(UiTheme.Flow(selectionActions), 0, 0);
        if (locked != null)
        {
            var children = locked.Controls.Cast<Control>().OrderBy(c => c.Top).ToArray(); locked.Controls.Clear(); locked.Dock = DockStyle.Fill; locked.AutoSize = false;
            var options = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(8), RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No };
            foreach (var c in children) { c.Anchor = AnchorStyles.None; c.Margin = new Padding(3, 5, 3, 5); c.Font = new Font("Segoe UI", 9); if (c is Label) { c.AutoSize = true; c.MaximumSize = new Size(270, 0); } else { c.AutoSize = false; c.Size = new Size(275, 42); } options.Controls.Add(c); }
            locked.Controls.Add(options); side.Controls.Add(locked, 0, 1);
        }
        middle.Controls.Add(side, 1, 0); root.Controls.Add(middle, 0, 2); logLabel.Dock = DockStyle.Fill; logLabel.AutoSize = false; logLabel.Padding = new Padding(0, 5, 0, 0); root.Controls.Add(logLabel, 0, 3);
        log.Dock = DockStyle.Fill; log.Margin = Padding.Empty; root.Controls.Add(log, 0, 4); status.AutoSize = false; status.AutoEllipsis = true; status.Dock = DockStyle.Fill; status.Padding = new Padding(0, 5, 0, 0); root.Controls.Add(status, 0, 5);
        foreach (var b in actions) { b.AutoSize = true; b.MinimumSize = new Size(120, 38); }
        actions[0].Tag = "primary"; root.Controls.Add(UiTheme.Flow(actions), 0, 6); form.Controls.Add(root); form.ResumeLayout(true);
    }
}
