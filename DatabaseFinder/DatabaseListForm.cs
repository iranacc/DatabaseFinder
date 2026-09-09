namespace DatabaseFinder;

/// <summary>پنجرهٔ سادهٔ نمایش نام دیتابیس‌های یک سرویس.</summary>
public class DatabaseListForm : AppForm
{
    protected override bool ModernLayout => true;

    public DatabaseListForm(DatabaseInfo db)
    {
        var service = db.Port is int p && p > 0 ? $"{db.Host}:{p}" : db.Host;
        Text = L.Format("S330", service);

        var title = new Label { Text = Text, AutoSize = true };
        var list = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, Font = new Font("Segoe UI", 10.5F), RightToLeft = RightToLeft.No };
        var status = new Label { Dock = DockStyle.Fill, AutoSize = false, AutoEllipsis = true, Text = "" };

        var names = db.DatabaseNames;
        if (names != null && names.Count > 0)
        {
            list.Items.AddRange(names.ToArray());
        }
        else
        {
            list.Visible = false;
            status.Text = L.Text("S331");
            status.ForeColor = UiTheme.Muted;
        }

        var close = UiTheme.Button(L.Text("S095"), (_, _) => Close(), true);
        FormLayout.Build(this, title, null, list, status, close);
        list.SelectedIndex = -1;
        ClientSize = new Size(480, 440);
        MinimumSize = new Size(400, 320);
    }
}