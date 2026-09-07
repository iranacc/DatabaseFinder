using System.Data;

namespace DatabaseFinder
{
    public class DiskScanForm : Form
    {
        public List<DatabaseInfo> Found { get; private set; } = new();

        private readonly CheckedListBox _lstRoots;
        private readonly CheckedListBox _lstFormats;
        private readonly NumericUpDown _numMinSize;
        private readonly RadioButton _rbQuick;
        private readonly RadioButton _rbFull;
        private readonly Button _btnScan;
        private readonly Button _btnCancelScan;
        private readonly Button _btnCopy;
        private readonly Button _btnMerge;
        private readonly ProgressBar _bar;
        private readonly Label _lblStatus;
        private readonly DataGridView _grid;

        private CancellationTokenSource? _cts;

        public DiskScanForm()
        {
            Text = "اسکن هارد دیسک - دیتابیس‌های آفلاین";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(900, 700);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(820, 580);

            var lblTitle = new Label
            {
                Text = "اسکن هارد دیسک برای دیتابیس‌های آفلاین و بکاپ‌ها",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                AutoSize = true,
                Location = new Point(12, 8)
            };
            Controls.Add(lblTitle);

            // ---- ناحیه ریشه‌ها ----
            var grpRoots = new GroupBox
            {
                Text = "مکان‌های جستجو",
                Location = new Point(12, 42),
                Size = new Size(330, 230)
            };

            _rbQuick = new RadioButton
            {
                Text = "اسکن سریع (مکان‌های رایج)",
                Checked = true,
                Location = new Point(12, 22),
                AutoSize = true
            };
            _rbFull = new RadioButton
            {
                Text = "اسکن کامل (کل درایوها)",
                Location = new Point(190, 22),
                AutoSize = true
            };
            _rbQuick.CheckedChanged += (s, e) => ReloadRoots();
            _rbFull.CheckedChanged += (s, e) => ReloadRoots();

            _lstRoots = new CheckedListBox
            {
                Location = new Point(6, 48),
                Size = new Size(318, 118),
                CheckOnClick = true,
                Font = new Font("Segoe UI", 9F)
            };

            var btnAddRoot = new Button
            {
                Text = "افزودن پوشه دلخواه...",
                Location = new Point(6, 172),
                Size = new Size(318, 26),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAddRoot.Click += BtnAddRoot_Click;

            var btnAllRoots = new Button
            {
                Text = "انتخاب همه",
                Location = new Point(6, 202),
                Size = new Size(154, 24),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAllRoots.Click += (s, e) => SetRootsChecked(true);

            var btnNoneRoots = new Button
            {
                Text = "هیچ",
                Location = new Point(168, 202),
                Size = new Size(156, 24),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnNoneRoots.Click += (s, e) => SetRootsChecked(false);

            grpRoots.Controls.AddRange(new Control[] { _rbQuick, _rbFull, _lstRoots, btnAddRoot, btnAllRoots, btnNoneRoots });
            Controls.Add(grpRoots);

            // ---- ناحیه فرمت‌ها ----
            var grpFormats = new GroupBox
            {
                Text = "فرمت‌های مورد جستجو",
                Location = new Point(352, 42),
                Size = new Size(250, 230)
            };

            _lstFormats = new CheckedListBox
            {
                Location = new Point(6, 22),
                Size = new Size(238, 160),
                CheckOnClick = true,
                Font = new Font("Segoe UI", 9F)
            };
            foreach (var f in DiskFormatRegistry.All)
                _lstFormats.Items.Add($"{f.Name}  (.{string.Join("/.", f.Extensions).TrimStart('.')})");

            var btnAllFmt = new Button
            {
                Text = "انتخاب همه",
                Location = new Point(6, 190),
                Size = new Size(116, 28),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAllFmt.Click += (s, e) => SetFormatsChecked(true);

            var btnNoneFmt = new Button
            {
                Text = "هیچ",
                Location = new Point(128, 190),
                Size = new Size(116, 28),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnNoneFmt.Click += (s, e) => SetFormatsChecked(false);

            grpFormats.Controls.AddRange(new Control[] { _lstFormats, btnAllFmt, btnNoneFmt });
            Controls.Add(grpFormats);

            // ---- تنظیمات ----
            var grpOpt = new GroupBox
            {
                Text = "تنظیمات",
                Location = new Point(612, 42),
                Size = new Size(276, 230)
            };

            var lblMin = new Label { Text = "حداقل اندازه فایل:", Location = new Point(12, 28), AutoSize = true };
            _numMinSize = new NumericUpDown
            {
                Location = new Point(140, 24),
                Size = new Size(100, 27),
                Minimum = 0,
                Maximum = 1048576,
                Value = 512,
                Increment = 128
            };
            var lblMinUnit = new Label { Text = "کیلوبایت (0 = هر اندازه)", Location = new Point(12, 56), AutoSize = true, ForeColor = Color.Gray };

            var lblNote = new Label
            {
                Text = "«اسکن سریع» فقط مکان‌های رایج را می‌گردد و «اسکن کامل» همه درایوهای ثابت را بررسی می‌کند.",
                Location = new Point(12, 110),
                Size = new Size(252, 100),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8.5F)
            };

            grpOpt.Controls.AddRange(new Control[] { lblMin, _numMinSize, lblMinUnit, lblNote });
            Controls.Add(grpOpt);

            // ---- دکمه‌ها ----
            _btnScan = new Button
            {
                Text = "شروع اسکن",
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Location = new Point(12, 280),
                Size = new Size(150, 38)
            };
            _btnScan.Click += BtnScan_Click;
            Controls.Add(_btnScan);

            _btnCancelScan = new Button
            {
                Text = "توقف",
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(170, 280),
                Size = new Size(90, 38),
                Enabled = false
            };
            _btnCancelScan.Click += (s, e) => _cts?.Cancel();
            Controls.Add(_btnCancelScan);

            _lblStatus = new Label
            {
                AutoSize = true,
                Location = new Point(280, 292),
                ForeColor = Color.Gray
            };
            Controls.Add(_lblStatus);

            _bar = new ProgressBar
            {
                Location = new Point(12, 326),
                Size = new Size(876, 20),
                Style = ProgressBarStyle.Continuous,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_bar);

            // ---- نتایج ----
            var lblResults = new Label
            {
                Text = "نتایج یافت‌شده:",
                Location = new Point(12, 356),
                AutoSize = true
            };
            Controls.Add(lblResults);

            _grid = new DataGridView
            {
                Location = new Point(12, 380),
                Size = new Size(876, 240),
                ReadOnly = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            _grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "انتخاب", Name = "sel", Width = 45 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "مسیر", Name = "path", Width = 310, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "نوع / فارمت", Name = "format", Width = 140, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "نام دیتابیس", Name = "name", Width = 120, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "اندازه", Name = "size", Width = 80, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "تغییر یافته", Name = "date", Width = 110, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "بکاپ", Name = "backup", Width = 60, ReadOnly = true });

            Controls.Add(_grid);

            // ---- دکمه‌های پایین ----
            _btnCopy = new Button
            {
                Text = "کپی فایل‌های انتخاب‌شده",
                BackColor = Color.FromArgb(139, 195, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(12, 642),
                Size = new Size(200, 40),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnCopy.Click += BtnCopy_Click;
            Controls.Add(_btnCopy);

            _btnMerge = new Button
            {
                Text = "افزودن انتخابی به جدول اصلی",
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(220, 642),
                Size = new Size(220, 40),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnMerge.Click += (s, e) => DialogResult = DialogResult.OK;
            Controls.Add(_btnMerge);

            var btnClose = new Button
            {
                Text = "بستن",
                Location = new Point(792, 642),
                Size = new Size(96, 40),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnClose.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.Add(btnClose);

            ReloadRoots();
            SetFormatsChecked(true);

            Load += (s, e) => { };
        }

        private void ReloadRoots()
        {
            _lstRoots.Items.Clear();
            foreach (var root in DiskScanner.GetDefaultRoots(_rbQuick.Checked))
            {
                var idx = _lstRoots.Items.Add(root);
                _lstRoots.SetItemChecked(idx, true);
            }
        }

        private void SetFormatsChecked(bool check)
        {
            for (int i = 0; i < _lstFormats.Items.Count; i++)
                _lstFormats.SetItemChecked(i, check);
        }

        private void SetRootsChecked(bool check)
        {
            for (int i = 0; i < _lstRoots.Items.Count; i++)
                _lstRoots.SetItemChecked(i, check);
        }

        private void BtnAddRoot_Click(object? sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "پوشه‌ای که باید جستجو شود را انتخاب کنید"
            };
            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                var exists = false;
                foreach (var item in _lstRoots.Items)
                {
                    if (item?.ToString() == fbd.SelectedPath) { exists = true; break; }
                }
                if (!exists)
                {
                    var idx = _lstRoots.Items.Add(fbd.SelectedPath);
                    _lstRoots.SetItemChecked(idx, true);
                }
            }
        }

        private List<string> GetSelectedRoots()
        {
            var roots = new List<string>();
            foreach (var item in _lstRoots.CheckedItems)
            {
                if (item != null) roots.Add(item.ToString() ?? "");
            }
            return roots;
        }

        private List<DiskFormat> GetSelectedFormats()
        {
            var formats = new List<DiskFormat>();
            var all = DiskFormatRegistry.All;
            for (int i = 0; i < _lstFormats.Items.Count; i++)
            {
                if (_lstFormats.GetItemChecked(i)) formats.Add(all[i]);
            }
            return formats;
        }

        private async void BtnScan_Click(object? sender, EventArgs e)
        {
            var roots = GetSelectedRoots();
            if (roots.Count == 0)
            {
                _lblStatus.Text = "دست‌کم یک مکان جستجو انتخاب کنید.";
                return;
            }

            var formats = GetSelectedFormats();
            if (formats.Count == 0)
            {
                _lblStatus.Text = "دست‌کم یک فرمت انتخاب کنید.";
                return;
            }

            var minSize = (long)_numMinSize.Value * 1024;

            _cts = new CancellationTokenSource();
            _btnScan.Enabled = false;
            _btnCancelScan.Enabled = true;
            _grid.Rows.Clear();
            Found = new List<DatabaseInfo>();
            _bar.Value = 0;
            _bar.Style = ProgressBarStyle.Continuous;

            var scanner = new DiskScanner();
            try
            {
                var result = await Task.Run(() => scanner.Scan(roots, formats, minSize, OnProgress, _cts.Token));

                if (_cts.IsCancellationRequested)
                {
                    _lblStatus.Text = $"اسکن متوقف شد - {result.Count} مورد تاکنون یافت شد.";
                }
                else
                {
                    _lblStatus.Text = $"اسکن کامل شد - {result.Count} مورد یافت شد.";
                    _lblStatus.ForeColor = result.Count > 0 ? Color.FromArgb(76, 175, 80) : Color.Gray;
                }

                Found = result;
                FillGrid(result);
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"خطا: {ex.Message}";
                _lblStatus.ForeColor = Color.FromArgb(211, 47, 47);
            }
            finally
            {
                _btnScan.Enabled = true;
                _btnCancelScan.Enabled = false;
                _bar.Value = _bar.Maximum;
            }
        }

        private void OnProgress(DiskScanner.ScanProgress p)
        {
            try
            {
                BeginInvoke(new Action(() =>
                {
                    if (p.DirectoriesScanned > 0)
                    {
                        _bar.Maximum = Math.Max(_bar.Maximum, p.DirectoriesScanned + 100);
                        _bar.Value = Math.Min(_bar.Value + 1, _bar.Maximum);
                    }
                    else
                    {
                        _bar.Maximum = 100;
                    }

                    if (!string.IsNullOrEmpty(p.CurrentDirectory))
                    {
                        _lblStatus.Text = $"در حال بررسی: {p.CurrentDirectory}";
                    }
                    else
                    {
                        _lblStatus.Text = $"فایل‌های بررسی‌شده: {p.FilesScanned} ...";
                    }
                }));
            }
            catch { }
        }

        private void FillGrid(List<DatabaseInfo> results)
        {
            _grid.Rows.Clear();
            foreach (var r in results)
            {
                var row = new DataGridViewRow();
                row.CreateCells(_grid);
                row.Cells[0].Value = true;
                row.Cells[1].Value = r.LocalPath;
                row.Cells[2].Value = r.FormatName;
                row.Cells[3].Value = r.Name;
                row.Cells[4].Value = DatabaseFileLocator.FormatSize(r.FileSize);
                row.Cells[5].Value = r.FileModified.ToString("yyyy-MM-dd HH:mm");
                row.Cells[6].Value = r.IsBackup ? "بله" : "";
                row.Tag = r;
                if (r.IsBackup) row.DefaultCellStyle.BackColor = Color.FromArgb(232, 245, 233);
                _grid.Rows.Add(row);
            }
        }

        private List<DatabaseInfo> GetSelectedResults()
        {
            var list = new List<DatabaseInfo>();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.Cells[0].Value is bool b && b && row.Tag is DatabaseInfo di)
                    list.Add(di);
            }
            return list;
        }

        private void BtnCopy_Click(object? sender, EventArgs e)
        {
            var selected = GetSelectedResults();
            if (selected.Count == 0)
            {
                _lblStatus.Text = "ردیف‌هایی که می‌خواهید کپی شوند را تیک بزنید.";
                return;
            }

            var form = new DatabaseCopyForm(selected);
            form.ShowDialog(this);
        }
    }
}