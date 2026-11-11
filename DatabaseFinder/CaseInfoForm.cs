namespace DatabaseFinder
{
    /// <summary>
    /// مشخصات پرونده ماده ۱۸۱ برای سربرگ مانیفست و صورت‌جلسه رسمی حقوقی.
    /// کاملاً اختیاری است؛ اگر کاربر رد کند null برمی‌گردد و صورت‌جلسه با مشخصات پیش‌فرض سیستمی تولید می‌شود.
    /// </summary>
    public class CaseInfo
    {
        public string CaseNumber { get; set; } = "";
        public string Officer { get; set; } = "";
        public string Warrant { get; set; } = "";
        public string Notes { get; set; } = "";
        public string TaxpayerName { get; set; } = "";
        public string NationalId { get; set; } = "";
        public string TaxpayerRepresentative { get; set; } = "";
        public string Location { get; set; } = "";

        public bool IsEmpty => string.IsNullOrWhiteSpace(CaseNumber)
            && string.IsNullOrWhiteSpace(Officer)
            && string.IsNullOrWhiteSpace(Warrant)
            && string.IsNullOrWhiteSpace(Notes)
            && string.IsNullOrWhiteSpace(TaxpayerName)
            && string.IsNullOrWhiteSpace(NationalId)
            && string.IsNullOrWhiteSpace(TaxpayerRepresentative)
            && string.IsNullOrWhiteSpace(Location);
    }

    public class CaseInfoForm : Form
    {
        private readonly TextBox _txtTaxpayer = new();
        private readonly TextBox _txtNationalId = new();
        private readonly TextBox _txtCase = new();
        private readonly TextBox _txtWarrant = new();
        private readonly TextBox _txtOfficer = new();
        private readonly TextBox _txtRep = new();
        private readonly TextBox _txtLocation = new();
        private readonly TextBox _txtNotes = new();

        public CaseInfo? Result { get; private set; }

        public CaseInfoForm()
        {
            Text = L.Text("S476");
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(540, 520);
            MinimumSize = new Size(500, 480);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = UiFont.Make(9.5f);
            BackColor = UiTheme.Canvas;
            RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No;
            RightToLeftLayout = L.IsFa;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 12, 14, 12),
                ColumnCount = 1,
                RowCount = 3
            };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Form content card
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));  // Spacer
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));  // Action buttons

            var card = new ModernCard
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 10, 14, 10),
                CornerRadius = 8
            };

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                AutoScroll = true
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            for (var i = 0; i < 7; i++)
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            AddRow(table, 0, L.Text("S471"), _txtTaxpayer);
            AddRow(table, 1, L.Text("S472"), _txtNationalId);
            AddRow(table, 2, L.Text("S379"), _txtWarrant);
            AddRow(table, 3, L.Text("S377"), _txtCase);
            AddRow(table, 4, L.Text("S378"), _txtOfficer);
            AddRow(table, 5, L.Text("S473"), _txtRep);
            AddRow(table, 6, L.Text("S474"), _txtLocation);
            
            _txtNotes.Multiline = true;
            _txtNotes.ScrollBars = ScrollBars.Vertical;
            _txtNotes.Dock = DockStyle.Fill;
            AddRow(table, 7, L.Text("S380"), _txtNotes);

            card.Controls.Add(table);
            root.Controls.Add(card, 0, 0);

            var btnOk = new Button
            {
                Text = L.Text("S381"),
                BackColor = UiTheme.Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Font = UiFont.Make(9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) =>
            {
                Result = new CaseInfo
                {
                    TaxpayerName = _txtTaxpayer.Text.Trim(),
                    NationalId = _txtNationalId.Text.Trim(),
                    Warrant = _txtWarrant.Text.Trim(),
                    CaseNumber = _txtCase.Text.Trim(),
                    Officer = _txtOfficer.Text.Trim(),
                    TaxpayerRepresentative = _txtRep.Text.Trim(),
                    Location = _txtLocation.Text.Trim(),
                    Notes = _txtNotes.Text.Trim()
                };
                if (Result.IsEmpty) Result = null;
                DialogResult = DialogResult.OK;
                Close();
            };

            var btnSkip = new Button
            {
                Text = L.Text("S382"),
                BackColor = UiTheme.Muted,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Font = UiFont.Make(9.5f),
                Cursor = Cursors.Hand
            };
            btnSkip.FlatAppearance.BorderSize = 0;
            btnSkip.Click += (s, e) => { Result = null; DialogResult = DialogResult.Cancel; Close(); };

            var buttons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            buttons.Controls.Add(btnOk, 0, 0);
            buttons.Controls.Add(btnSkip, 1, 0);

            root.Controls.Add(buttons, 0, 2);
            Controls.Add(root);
        }

        private static void AddRow(TableLayoutPanel table, int row, string label, TextBox box)
        {
            var lbl = new Label
            {
                Text = label,
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Margin = new Padding(0, 6, 0, 0),
                ForeColor = UiTheme.Ink
            };
            box.Dock = DockStyle.Fill;
            box.Margin = new Padding(0, 2, 0, 6);
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
