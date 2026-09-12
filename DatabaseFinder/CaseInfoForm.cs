namespace DatabaseFinder
{
    /// <summary>
    /// مشخصات پرونده ماده ۱۸۱ برای سربرگ مانیفست. کاملا اختیاری است؛
    /// اگر کاربر رد کند null برمی‌گردد و مانیفست بدون این بخش ساخته می‌شود.
    /// </summary>
    public class CaseInfo
    {
        public string CaseNumber { get; set; } = "";
        public string Officer { get; set; } = "";
        public string Warrant { get; set; } = "";
        public string Notes { get; set; } = "";
        public bool IsEmpty => string.IsNullOrWhiteSpace(CaseNumber)
            && string.IsNullOrWhiteSpace(Officer)
            && string.IsNullOrWhiteSpace(Warrant)
            && string.IsNullOrWhiteSpace(Notes);
    }

    public class CaseInfoForm : Form
    {
        private readonly TextBox _txtCase = new();
        private readonly TextBox _txtOfficer = new();
        private readonly TextBox _txtWarrant = new();
        private readonly TextBox _txtNotes = new();

        public CaseInfo? Result { get; private set; }

        public CaseInfoForm()
        {
            Text = L.Text("S376");
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 330);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;
            RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No;
            RightToLeftLayout = L.IsFa;

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                ColumnCount = 2,
                RowCount = 6
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (var i = 0; i < 4; i++) table.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

            AddRow(table, 0, L.Text("S377"), _txtCase);
            AddRow(table, 1, L.Text("S378"), _txtOfficer);
            AddRow(table, 2, L.Text("S379"), _txtWarrant);
            AddRow(table, 3, L.Text("S380"), _txtNotes);

            var btnOk = new Button
            {
                Text = L.Text("S381"),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill
            };
            btnOk.Click += (s, e) =>
            {
                Result = new CaseInfo
                {
                    CaseNumber = _txtCase.Text.Trim(),
                    Officer = _txtOfficer.Text.Trim(),
                    Warrant = _txtWarrant.Text.Trim(),
                    Notes = _txtNotes.Text.Trim()
                };
                if (Result.IsEmpty) Result = null;
                DialogResult = DialogResult.OK;
                Close();
            };

            var btnSkip = new Button
            {
                Text = L.Text("S382"),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill
            };
            btnSkip.Click += (s, e) => { Result = null; DialogResult = DialogResult.Cancel; Close(); };

            var buttons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            buttons.Controls.Add(btnOk, 0, 0);
            buttons.Controls.Add(btnSkip, 1, 0);
            table.Controls.Add(buttons, 0, 5);
            table.SetColumnSpan(buttons, 2);
            Controls.Add(table);
        }

        private static void AddRow(TableLayoutPanel table, int row, string label, TextBox box)
        {
            var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left };
            box.Dock = DockStyle.Fill;
            table.Controls.Add(lbl, 0, row);
            table.Controls.Add(box, 1, row);
        }

        /// <summary>یک‌بار در هر نشست فرم می‌پرسد؛ جواب (حتی رد کردن) نگه داشته می‌شود.</summary>
        public static CaseInfo? AskOnce(IWin32Window owner, ref bool asked, ref CaseInfo? kept)
        {
            if (asked) return kept;
            asked = true;
            using var dlg = new CaseInfoForm();
            kept = dlg.ShowDialog(owner) == DialogResult.OK ? dlg.Result : null;
            return kept;
        }
    }
}
