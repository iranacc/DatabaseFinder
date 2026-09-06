using System.Text;

namespace DatabaseFinder
{
    public partial class Form1 : Form
    {
        private readonly DatabaseDetector _detector = new();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DetectDatabases();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            DetectDatabases();
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            var sb = new StringBuilder();
            foreach (DataGridViewRow row in dgvDatabases.Rows)
            {
                sb.AppendLine($"{row.Cells[0].Value} | پورت: {row.Cells[1].Value} | سرویس: {row.Cells[2].Value} | پروسس: {row.Cells[3].Value}");
            }

            if (sb.Length > 0)
            {
                Clipboard.SetText(sb.ToString());
                lblStatus.Text = "لیست کپی شد!";
            }
        }

        private void dgvDatabases_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = dgvDatabases.Rows[e.RowIndex];
                var info = $"نام دیتابیس: {row.Cells[0].Value}\n" +
                           $"پورت: {row.Cells[1].Value}\n" +
                           $"سرویس: {row.Cells[2].Value}\n" +
                           $"پروسس: {row.Cells[3].Value}\n" +
                           $"نحوه تشخیص: {row.Cells[4].Value}";
                MessageBox.Show(info, "جزئیات دیتابیس", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void DetectDatabases()
        {
            lblStatus.Text = "در حال جستجو...";
            lblStatus.Refresh();
            dgvDatabases.DataSource = null;

            try
            {
                var results = _detector.Detect();
                var models = new List<DatabaseDisplayModel>();

                foreach (var db in results)
                {
                    var how = new List<string>();
                    if (db.IsRunningAsService) how.Add("سرویس");
                    if (db.IsRunningAsProcess) how.Add("پروسس");
                    if (db.Port.HasValue) how.Add("پورت");

                    models.Add(new DatabaseDisplayModel
                    {
                        TypeDisplayName = db.TypeDisplayName,
                        Port = db.Port?.ToString() ?? "-",
                        ServiceName = db.ServiceName ?? (db.IsRunningAsService ? db.Name : "-"),
                        ProcessDisplay = db.ProcessId > 0 ? $"{db.ProcessName} (PID: {db.ProcessId})" : "-",
                        DetectionMethod = string.Join(" + ", how)
                    });
                }

                dgvDatabases.DataSource = models;
                lblStatus.Text = $"تعداد دیتابیس‌های یافت شده: {results.Count}";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "خطا در تشخیص دیتابیس‌ها";
                MessageBox.Show($"خطا:\n{ex.Message}\n\nممکنه نیاز به اجرای برنامه با دسترسی Administrator باشه.",
                    "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}