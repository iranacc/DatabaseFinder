namespace DatabaseFinder;

public class AppForm : Form
{
    protected virtual bool ModernLayout => false;
    protected override void OnLoad(EventArgs e)
    {
        RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No;
        RightToLeftLayout = !ModernLayout && L.IsFa;
        Icon ??= Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        UiTheme.Apply(this);
        base.OnLoad(e);
    }
}

public static class UiTheme
{
    public static readonly Color Accent = Color.FromArgb(36, 91, 214);
    public static readonly Color Canvas = Color.FromArgb(243, 245, 249);
    public static readonly Color Ink = Color.FromArgb(36, 51, 75);
    public static readonly Color Muted = Color.FromArgb(95, 110, 132);
    public static readonly Color Line = Color.FromArgb(223, 229, 238);
    public static void Apply(Control control)
    {
        if (control is Form) { control.BackColor = Canvas; control.ForeColor = Ink; }
        if (control is Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Line;
            button.FlatAppearance.BorderSize = 1;
            button.BackColor = Equals(button.Tag, "primary") ? Accent : Color.White;
            button.ForeColor = Equals(button.Tag, "primary") ? Color.White : Ink;
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
        }
        if (control is Label label) label.ForeColor = label.Font.Bold ? Ink : Muted;
        if (control is GroupBox) control.ForeColor = Ink;
        if (control is TextBoxBase box) { box.BorderStyle = BorderStyle.FixedSingle; box.RightToLeft = RightToLeft.No; }
        if (control is CheckedListBox list) { list.BorderStyle = BorderStyle.None; list.BackColor = Color.White; }
        if (control is TreeView tree) { tree.BorderStyle = BorderStyle.None; tree.BackColor = Color.White; tree.ItemHeight = 28; }
        if (control is DataGridView grid)
        {
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(248, 250, 253), ForeColor = Muted, Font = new Font("Segoe UI", 9.5F), Padding = new Padding(5) };
            grid.ColumnHeadersHeight = 42;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Line;
            grid.DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.White, ForeColor = Ink, SelectionBackColor = Color.FromArgb(232, 240, 254), SelectionForeColor = Ink, Padding = new Padding(5) };
            grid.RowTemplate.Height = 38;
            foreach (DataGridViewRow row in grid.Rows) row.Height = 38;
            grid.DataBindingComplete += (_, _) => { foreach (DataGridViewRow row in grid.Rows) row.Height = 38; };
        }
        foreach (Control child in control.Controls) Apply(child);
    }
    public static FlowLayoutPanel Flow(params Control[] controls)
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Padding = new Padding(6), RightToLeft = RightToLeft.No, FlowDirection = L.IsFa ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        foreach (var control in controls) { control.Anchor = AnchorStyles.None; control.Margin = new Padding(5); if (control is Label or System.Windows.Forms.Button) control.RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No; panel.Controls.Add(control); }
        return panel;
    }
    public static Button Button(string text, EventHandler click, bool primary = false)
    {
        var button = new Button { Text = text, AutoSize = true, MinimumSize = new Size(100, 36), Padding = new Padding(10, 5, 10, 5), Tag = primary ? "primary" : null };
        button.Click += click;
        return button;
    }
}
