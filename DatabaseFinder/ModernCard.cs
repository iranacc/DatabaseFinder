using System.Drawing.Drawing2D;

namespace DatabaseFinder;

public class ModernCard : Panel
{
    public int CornerRadius { get; set; } = 8;
    public Color BorderColor { get; set; } = Color.Empty;
    public int BorderThickness { get; set; } = 1;

    public ModernCard()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Padding = new Padding(12);
        BackColor = UiTheme.Surface;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        if (rect.Width <= 0 || rect.Height <= 0) return;

        using var path = CreateRoundedRectangle(rect, CornerRadius);

        // Fill background
        using (var brush = new SolidBrush(BackColor))
        {
            g.FillPath(brush, path);
        }

        // Draw border
        var borderColor = BorderColor.IsEmpty ? UiTheme.Line : BorderColor;
        if (BorderThickness > 0 && borderColor != Color.Transparent)
        {
            using var pen = new Pen(borderColor, BorderThickness);
            g.DrawPath(pen, path);
        }
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0)
        {
            path.AddRectangle(rect);
            return path;
        }

        var diameter = radius * 2;
        var arc = new Rectangle(rect.Location, new Size(diameter, diameter));

        // Top-left
        path.AddArc(arc, 180, 90);
        // Top-right
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);
        // Bottom-right
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        // Bottom-left
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
        return path;
    }
}
