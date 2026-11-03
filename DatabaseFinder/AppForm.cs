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
    public static Color Accent => Dark ? Color.FromArgb(96, 165, 250) : Color.FromArgb(37, 99, 235);
    public static Color AccentHover => Dark ? Color.FromArgb(147, 197, 253) : Color.FromArgb(29, 78, 216);
    public static Color Canvas => Dark ? Color.FromArgb(15, 23, 42) : Color.FromArgb(248, 250, 252);
    public static Color Surface => Dark ? Color.FromArgb(30, 41, 59) : Color.White;
    public static Color SurfaceAlt => Dark ? Color.FromArgb(51, 65, 85) : Color.FromArgb(241, 245, 249);
    public static Color Ink => Dark ? Color.FromArgb(248, 250, 252) : Color.FromArgb(15, 23, 42);
    public static Color Muted => Dark ? Color.FromArgb(148, 163, 184) : Color.FromArgb(100, 116, 139);
    public static Color Line => Dark ? Color.FromArgb(51, 65, 85) : Color.FromArgb(226, 232, 240);
    public static Color Success => Color.FromArgb(16, 185, 129);
    public static Color Warning => Color.FromArgb(245, 158, 11);
    public static Color Danger => Color.FromArgb(239, 68, 68);

    public static void Apply(Control control)
    {
        if (control is Form form)
        {
            form.BackColor = Canvas;
            form.ForeColor = Ink;
            form.Font = UiFont.Make(9.5f);
        }
        else if (control is ModernCard)
        {
            // Keep card's background intact if explicitly set
        }
        else if (control is Panel or TableLayoutPanel or FlowLayoutPanel)
        {
            if (control.Parent is Form) control.BackColor = Canvas;
            else if (control.BackColor == Color.White || control.BackColor == Color.FromArgb(38, 45, 56) || control.BackColor == Color.FromArgb(243, 245, 249))
                control.BackColor = Surface;
        }

        if (control is Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.Font = UiFont.Make(button.Font.Size > 0 ? button.Font.Size : 9.5f, Equals(button.Tag, "primary") || button.Font.Bold ? FontStyle.Bold : FontStyle.Regular);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;

            if (Equals(button.Tag, "primary"))
            {
                button.BackColor = Accent;
                button.ForeColor = Color.White;
                button.FlatAppearance.BorderColor = Accent;
                button.FlatAppearance.MouseOverBackColor = AccentHover;
                button.FlatAppearance.MouseDownBackColor = Dark ? Color.FromArgb(70, 120, 230) : Color.FromArgb(30, 64, 175);
            }
            else if (Equals(button.Tag, "danger"))
            {
                button.BackColor = Dark ? Color.FromArgb(127, 29, 29) : Color.FromArgb(254, 242, 242);
                button.ForeColor = Dark ? Color.FromArgb(254, 202, 202) : Danger;
                button.FlatAppearance.BorderColor = Dark ? Color.FromArgb(185, 28, 28) : Color.FromArgb(254, 202, 202);
                button.FlatAppearance.MouseOverBackColor = Dark ? Color.FromArgb(153, 27, 27) : Color.FromArgb(254, 226, 226);
            }
            else
            {
                button.BackColor = Surface;
                button.ForeColor = Ink;
                button.FlatAppearance.BorderColor = Line;
                button.FlatAppearance.MouseOverBackColor = SurfaceAlt;
                button.FlatAppearance.MouseDownBackColor = Line;
            }
        }

        if (control is Label label && !(label is LinkLabel))
        {
            var isBold = label.Font.Bold;
            var size = label.Font.Size > 0 ? label.Font.Size : 9.5f;
            label.Font = UiFont.Make(size, isBold ? FontStyle.Bold : FontStyle.Regular);
            if (label.ForeColor == Color.Gray || label.ForeColor == Color.FromArgb(95, 110, 132) || label.ForeColor == Color.FromArgb(172, 183, 198))
                label.ForeColor = Muted;
            else if (!isBold && label.ForeColor != Accent && label.ForeColor != Danger && label.ForeColor != Success && label.ForeColor != Warning)
                label.ForeColor = Ink;
        }

        if (control is LinkLabel link)
        {
            link.Font = UiFont.Make(link.Font.Size > 0 ? link.Font.Size : 9.5f, link.Font.Bold ? FontStyle.Bold : FontStyle.Regular);
            link.LinkColor = Accent;
            link.ActiveLinkColor = AccentHover;
        }

        if (control is GroupBox box)
        {
            box.BackColor = Surface;
            box.ForeColor = Ink;
            box.Font = UiFont.Make(box.Font.Size > 0 ? box.Font.Size : 9f, FontStyle.Bold);
        }

        if (control is TextBoxBase textBox)
        {
            textBox.BorderStyle = BorderStyle.FixedSingle;
            if (textBox.Multiline && textBox.ReadOnly)
            {
                textBox.Font = new Font("Consolas", 9f);
                textBox.BackColor = Dark ? Color.FromArgb(15, 23, 42) : Color.FromArgb(24, 30, 42);
                textBox.ForeColor = Color.FromArgb(226, 232, 240);
            }
            else
            {
                textBox.Font = UiFont.Make(textBox.Font.Size > 0 ? textBox.Font.Size : 9.5f);
                textBox.BackColor = Surface;
                textBox.ForeColor = Ink;
            }
            textBox.RightToLeft = RightToLeft.No;
        }

        if (control is ComboBox combo)
        {
            combo.BackColor = Surface;
            combo.ForeColor = Ink;
            combo.Font = UiFont.Make(combo.Font.Size > 0 ? combo.Font.Size : 9.5f);
        }

        if (control is CheckedListBox list)
        {
            list.BorderStyle = BorderStyle.None;
            list.BackColor = Surface;
            list.ForeColor = Ink;
            list.Font = UiFont.Make(9.5f);
        }

        if (control is TreeView tree)
        {
            tree.BorderStyle = BorderStyle.None;
            tree.BackColor = Surface;
            tree.ForeColor = Ink;
            tree.Font = UiFont.Make(9.5f);
            tree.ItemHeight = 30;
        }

        if (control is ListBox listBox)
        {
            listBox.BackColor = Surface;
            listBox.ForeColor = Ink;
            listBox.Font = UiFont.Make(9.5f);
        }

        if (control is NumericUpDown numeric)
        {
            numeric.BackColor = Surface;
            numeric.ForeColor = Ink;
            numeric.Font = UiFont.Make(numeric.Font.Size > 0 ? numeric.Font.Size : 9.5f);
        }

        if (control is DataGridView grid)
        {
            grid.BackgroundColor = Surface;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = SurfaceAlt,
                ForeColor = Ink,
                Font = UiFont.Make(9.5f, FontStyle.Bold),
                Padding = new Padding(6)
            };
            grid.ColumnHeadersHeight = 42;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Line;
            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Surface,
                ForeColor = Ink,
                Font = UiFont.Make(9f),
                SelectionBackColor = Dark ? Color.FromArgb(51, 65, 85) : Color.FromArgb(224, 238, 255),
                SelectionForeColor = Ink,
                Padding = new Padding(5)
            };
            grid.RowsAdded += (_, e) =>
            {
                for (var i = e.RowIndex; i < e.RowIndex + e.RowCount; i++) grid.Rows[i].Height = 38;
            };
            grid.RowTemplate.Height = 38;
            foreach (DataGridViewRow row in grid.Rows) row.Height = 38;
            grid.DataBindingComplete += (_, _) => { foreach (DataGridViewRow row in grid.Rows) row.Height = 38; };
        }

        foreach (Control child in control.Controls) Apply(child);
    }

    public static FlowLayoutPanel Flow(params Control[] controls)
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Padding = new Padding(6),
            BackColor = Surface,
            RightToLeft = RightToLeft.No,
            FlowDirection = L.IsFa ? FlowDirection.RightToLeft : FlowDirection.LeftToRight
        };
        foreach (var control in controls)
        {
            control.Anchor = AnchorStyles.None;
            control.Margin = new Padding(5);
            if (control is Label or System.Windows.Forms.Button)
                control.RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No;
            panel.Controls.Add(control);
        }
        return panel;
    }

    public static Button Button(string text, EventHandler click, bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(100, 36),
            Padding = new Padding(10, 5, 10, 5),
            Tag = primary ? "primary" : null
        };
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
                g.DrawEllipse(pen, 3, 2, 14, 5);
                g.DrawLine(pen, 3, 4, 3, 16);
                g.DrawLine(pen, 17, 4, 17, 16);
                g.DrawArc(pen, 3, 12, 14, 5, 0, 180);
                g.DrawArc(pen, 3, 14, 14, 5, 0, 180);
                break;
            case UiIcon.Folder:
                g.FillRectangle(brush, 2, 5, 7, 3);
                g.DrawRectangle(pen, 2, 5, 16, 11);
                g.DrawLine(pen, 2, 8, 18, 8);
                break;
            case UiIcon.Network:
                g.DrawLine(pen, 10, 5, 5, 13);
                g.DrawLine(pen, 10, 5, 15, 13);
                g.DrawLine(pen, 5, 13, 15, 13);
                g.FillEllipse(brush, 7, 2, 6, 6);
                g.FillEllipse(brush, 2, 11, 6, 6);
                g.FillEllipse(brush, 12, 11, 6, 6);
                break;
            case UiIcon.Profile:
                g.FillEllipse(brush, 7, 2, 6, 6);
                g.DrawArc(pen, 3, 9, 14, 10, 180, 180);
                break;
            case UiIcon.Settings:
                g.DrawEllipse(pen, 5, 5, 10, 10);
                g.FillEllipse(brush, 8, 8, 4, 4);
                for (var i = 0; i < 8; i++)
                {
                    var a = i * Math.PI / 4;
                    g.DrawLine(pen, 10 + (float)Math.Cos(a) * 6, 10 + (float)Math.Sin(a) * 6, 10 + (float)Math.Cos(a) * 9, 10 + (float)Math.Sin(a) * 9);
                }
                break;
        }
        return image;
    }
}
