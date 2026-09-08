namespace DatabaseFinder;

internal static class FormLayout
{
    public static Control Field(string label, Control input, int width = 180)
    {
        var field = new TableLayoutPanel { Width = width, Height = 72, RowCount = 2, ColumnCount = 1, Margin = new Padding(6) };
        field.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); field.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); field.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        field.Controls.Add(new Label { Text = label, AutoSize = true }, 0, 0); input.Dock = DockStyle.Top; input.Anchor = AnchorStyles.Left | AnchorStyles.Right; field.Controls.Add(input, 0, 1); return field;
    }
    public static void Build(Form form, Control title, Control? inputs, Control main, Label status, params Button[] actions)
    {
        form.Controls.Clear(); form.ClientSize = new Size(920, 680); form.MinimumSize = new Size(780, 600); form.FormBorderStyle = FormBorderStyle.Sizable; form.AutoScaleMode = AutoScaleMode.Dpi;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(22), ColumnCount = 1, RowCount = 5 }; root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        title.Font = new Font("Segoe UI", 17, FontStyle.Bold); title.Padding = new Padding(0, 0, 0, 14); root.Controls.Add(title, 0, 0);
        if (inputs != null) { inputs.Dock = DockStyle.Fill; root.Controls.Add(inputs, 0, 1); }
        main.Dock = DockStyle.Fill; root.Controls.Add(main, 0, 2);
        foreach (var b in actions) { b.AutoSize = true; b.MinimumSize = new Size(110, 38); }
        actions[0].Tag = "primary"; root.Controls.Add(UiTheme.Flow(actions), 0, 3); status.Dock = DockStyle.Fill; status.AutoSize = false; status.AutoEllipsis = true; root.Controls.Add(status, 0, 4); form.Controls.Add(root);
    }
}
