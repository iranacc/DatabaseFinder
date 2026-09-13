namespace DatabaseFinder;

public class AppForm : Form
{
    protected virtual bool ModernLayout => false;
    private ToolTip? _toolTip;
    protected override void OnLoad(EventArgs e)
    {
        RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No;
        RightToLeftLayout = !ModernLayout && L.IsFa;
        Icon ??= Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        UiTheme.Apply(this);
        _toolTip = new ToolTip { AutoPopDelay = 5000, InitialDelay = 350, ReshowDelay = 100 };
        foreach (var button in AllControls(this).OfType<Button>())
            if (!string.IsNullOrWhiteSpace(button.Text)) _toolTip.SetToolTip(button, button.Text);
        FormClosed += (_, _) => { _toolTip?.Dispose(); _toolTip = null; };
        base.OnLoad(e);
    }

    private static IEnumerable<Control> AllControls(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var descendant in AllControls(child)) yield return descendant;
        }
    }
}

public static class UiTheme
{
    public enum UiIcon { Database, Folder, Network, Profile, Settings }
    public static bool Dark { get; set; }
    public static Color Accent => Dark ? Color.FromArgb(91, 145, 255) : Color.FromArgb(36, 91, 214);
    public static Color Canvas => Dark ? Color.FromArgb(28, 33, 42) : Color.FromArgb(243, 245, 249);
    public static Color Surface => Dark ? Color.FromArgb(38, 45, 56) : Color.White;
    public static Color Ink => Dark ? Color.FromArgb(238, 242, 247) : Color.FromArgb(36, 51, 75);
    public static Color Muted => Dark ? Color.FromArgb(172, 183, 198) : Color.FromArgb(95, 110, 132);
    public static Color Line => Dark ? Color.FromArgb(70, 82, 99) : Color.FromArgb(223, 229, 238);
    public static void Apply(Control control)
    {
        if (control is Form) { control.BackColor = Canvas; control.ForeColor = Ink; }
        if (control is Panel or TableLayoutPanel or FlowLayoutPanel) control.BackColor = Surface;
        if (control is Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Line;
            button.FlatAppearance.BorderSize = 1;
            button.BackColor = Equals(button.Tag, "primary") ? Accent : Surface;
            button.ForeColor = Equals(button.Tag, "primary") ? Color.White : Ink;
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
        }
        if (control is Label label) { label.BackColor = Surface; label.ForeColor = label.Font.Bold ? Ink : Muted; }
        if (control is LinkLabel link) { link.BackColor = Surface; link.LinkColor = Accent; link.ActiveLinkColor = Accent; }
        if (control is GroupBox box) { box.BackColor = Surface; box.ForeColor = Ink; }
        if (control is TextBoxBase textBox) { textBox.BorderStyle = BorderStyle.FixedSingle; textBox.BackColor = Surface; textBox.ForeColor = Ink; textBox.RightToLeft = RightToLeft.No; }
        if (control is ComboBox combo) { combo.BackColor = Surface; combo.ForeColor = Ink; }
        if (control is CheckedListBox list) { list.BorderStyle = BorderStyle.None; list.BackColor = Surface; list.ForeColor = Ink; }
        if (control is TreeView tree) { tree.BorderStyle = BorderStyle.None; tree.BackColor = Surface; tree.ForeColor = Ink; tree.ItemHeight = 28; }
        if (control is ListBox listBox) { listBox.BackColor = Surface; listBox.ForeColor = Ink; }
        if (control is NumericUpDown numeric) { numeric.BackColor = Surface; numeric.ForeColor = Ink; }
        if (control is DataGridView grid)
        {
            grid.BackgroundColor = Surface;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Dark ? Color.FromArgb(45, 54, 68) : Color.FromArgb(248, 250, 253), ForeColor = Muted, Font = new Font("Segoe UI", 9.5F), Padding = new Padding(5) };
            grid.ColumnHeadersHeight = 42;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Line;
            grid.DefaultCellStyle = new DataGridViewCellStyle { BackColor = Surface, ForeColor = Ink, SelectionBackColor = Dark ? Color.FromArgb(54, 78, 112) : Color.FromArgb(232, 240, 254), SelectionForeColor = Ink, Padding = new Padding(5) };
            grid.RowTemplate.Height = 38;
            foreach (DataGridViewRow row in grid.Rows) row.Height = 38;
            grid.DataBindingComplete += (_, _) => { foreach (DataGridViewRow row in grid.Rows) row.Height = 38; };
        }
        foreach (Control child in control.Controls) Apply(child);
    }
    public static FlowLayoutPanel Flow(params Control[] controls)
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Padding = new Padding(6), BackColor = Surface, RightToLeft = RightToLeft.No, FlowDirection = L.IsFa ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        foreach (var control in controls) { control.Anchor = AnchorStyles.None; control.Margin = new Padding(5); if (control is Label or System.Windows.Forms.Button) control.RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No; panel.Controls.Add(control); }
        return panel;
    }
    public static Button Button(string text, EventHandler click, bool primary = false)
    {
        var button = new Button { Text = text, AutoSize = true, MinimumSize = new Size(100, 36), Padding = new Padding(10, 5, 10, 5), Tag = primary ? "primary" : null };
        button.Click += click;
        return button;
    }

    public static Bitmap Icon(UiIcon kind)
    {
        var image = new Bitmap(20, 20);
        using var g = Graphics.FromImage(image);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var pen = new Pen(Accent, 1.7F);
        using var brush = new SolidBrush(Accent);
        switch (kind)
        {
            case UiIcon.Database:
                g.DrawEllipse(pen, 3, 2, 14, 5); g.DrawLine(pen, 3, 4, 3, 16); g.DrawLine(pen, 17, 4, 17, 16); g.DrawArc(pen, 3, 12, 14, 5, 0, 180); g.DrawArc(pen, 3, 14, 14, 5, 0, 180); break;
            case UiIcon.Folder:
                g.FillRectangle(brush, 2, 5, 7, 3); g.DrawRectangle(pen, 2, 5, 16, 11); g.DrawLine(pen, 2, 8, 18, 8); break;
            case UiIcon.Network:
                g.DrawLine(pen, 10, 5, 5, 13); g.DrawLine(pen, 10, 5, 15, 13); g.DrawLine(pen, 5, 13, 15, 13); g.FillEllipse(brush, 7, 2, 6, 6); g.FillEllipse(brush, 2, 11, 6, 6); g.FillEllipse(brush, 12, 11, 6, 6); break;
            case UiIcon.Profile:
                g.FillEllipse(brush, 7, 2, 6, 6); g.DrawArc(pen, 3, 9, 14, 10, 180, 180); break;
            case UiIcon.Settings:
                g.DrawEllipse(pen, 5, 5, 10, 10); g.FillEllipse(brush, 8, 8, 4, 4); for (var i = 0; i < 8; i++) { var a = i * Math.PI / 4; g.DrawLine(pen, 10 + (float)Math.Cos(a) * 6, 10 + (float)Math.Sin(a) * 6, 10 + (float)Math.Cos(a) * 9, 10 + (float)Math.Sin(a) * 9); } break;
        }
        return image;
    }
}
