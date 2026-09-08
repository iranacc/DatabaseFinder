using System.Data;
using System.Diagnostics;

namespace DatabaseFinder
{
    public partial class DiskScanForm : AppForm
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
        private ContextMenuStrip? _ctxResults;

        private CancellationTokenSource? _cts;

        public DiskScanForm()
        {
            Text = L.Text("S157");
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
                Text = L.Text("S158"),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                AutoSize = true,
                Location = new Point(12, 8)
            };
            Controls.Add(lblTitle);

            // ---- ناحیه ریشه‌ها ----
            var grpRoots = new GroupBox
            {
                Text = L.Text("S159"),
                Location = new Point(12, 42),
                Size = new Size(330, 230)
            };

            _rbQuick = new RadioButton
            {
                Text = L.Text("S160"),
                Checked = true,
                Location = new Point(12, 22),
                AutoSize = true
            };
            _rbFull = new RadioButton
            {
                Text = L.Text("S161"),
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
                Text = L.Text("S162"),
                Location = new Point(6, 172),
                Size = new Size(318, 26),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAddRoot.Click += BtnAddRoot_Click;

            var btnAllRoots = new Button
            {
                Text = L.Text("S034"),
                Location = new Point(6, 202),
                Size = new Size(154, 24),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAllRoots.Click += (s, e) => SetRootsChecked(true);

            var btnNoneRoots = new Button
            {
                Text = L.Text("S163"),
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
                Text = L.Text("S164"),
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
                Text = L.Text("S034"),
                Location = new Point(6, 190),
                Size = new Size(116, 28),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAllFmt.Click += (s, e) => SetFormatsChecked(true);

            var btnNoneFmt = new Button
            {
                Text = L.Text("S163"),
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
                Text = L.Text("S165"),
                Location = new Point(612, 42),
                Size = new Size(276, 230)
            };

            var lblMin = new Label { Text = L.Text("S166"), Location = new Point(12, 28), AutoSize = true };
            _numMinSize = new NumericUpDown
            {
                Location = new Point(140, 24),
                Size = new Size(100, 27),
                Minimum = 0,
                Maximum = 1048576,
                Value = 3,
                Increment = 1
            };
            var lblMinUnit = new Label { Text = L.Text("S167"), Location = new Point(12, 56), AutoSize = true, ForeColor = Color.Gray };

            var lblNote = new Label
            {
                Text = L.Text("S168"),
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
                Text = L.Text("S169"),
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
                Text = L.Text("S170"),
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
                Text = L.Text("S171"),
                Location = new Point(12, 356),
                AutoSize = true
            };
            Controls.Add(lblResults);

            var btnAllResults = new Button
            {
                Text = L.Text("S034"),
                Location = new Point(660, 350),
                Size = new Size(104, 26),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnAllResults.Click += (s, e) => SetResultsChecked(true);
            Controls.Add(btnAllResults);

            var btnNoneResults = new Button
            {
                Text = L.Text("S163"),
                Location = new Point(772, 350),
                Size = new Size(104, 26),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnNoneResults.Click += (s, e) => SetResultsChecked(false);
            Controls.Add(btnNoneResults);

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

            _grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = L.Text("S172"), Name = "sel", Width = 45 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L.Text("S173"), Name = "path", Width = 260, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L.Text("S174"), Name = "format", Width = 110, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L.Text("S175"), Name = "ext", Width = 90, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L.Text("S176"), Name = "name", Width = 110, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L.Text("S177"), Name = "size", Width = 80, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L.Text("S178"), Name = "date", Width = 100, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L.Text("S179"), Name = "backup", Width = 60, ReadOnly = true });

            _grid.CellMouseClick += Grid_CellMouseClick;
            Controls.Add(_grid);

            _ctxResults = new ContextMenuStrip();
            _ctxResults.Items.Add(L.Text("S180"), null, (s, e) => RunPathAction(OpenFolder));
            _ctxResults.Items.Add(L.Text("S181"), null, (s, e) => RunPathAction(OpenFile));
            _ctxResults.Items.Add(L.Text("S182"), null, (s, e) => RunPathAction(CopyPath));
            _ctxResults.Items.Add(L.Text("S183"), null, (s, e) => RunPathAction(CopyFileToFolder));
            _ctxResults.Opening += (s, e) =>
            {
                var pt = _grid.PointToClient(Cursor.Position);
                var h = _grid.HitTest(pt.X, pt.Y);
                if (h.RowIndex < 0 || h.RowIndex >= _grid.Rows.Count)
                {
                    e.Cancel = true;
                    return;
                }
                _ctxResults.Tag = _grid.Rows[h.RowIndex].Cells[1].Value as string;
            };
            _grid.ContextMenuStrip = _ctxResults;

            // ---- دکمه‌های پایین ----
            _btnCopy = new Button
            {
                Text = L.Text("S184"),
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
                Text = L.Text("S185"),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(220, 642),
                Size = new Size(220, 40),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnMerge.Click += (s, e) =>
            {
                _grid.EndEdit();
                var selected = _grid.Rows.Cast<DataGridViewRow>()
                    .Where(row => Convert.ToBoolean(row.Cells[0].Value ?? false))
                    .Select(row => row.Tag).OfType<DatabaseInfo>().ToList();
                if (selected.Count == 0) { _lblStatus.Text = L.Text("S198"); return; }
                Found = selected;
                DialogResult = DialogResult.OK;
            };
            Controls.Add(_btnMerge);

            var btnClose = new Button
            {
                Text = L.Text("S095"),
                Location = new Point(792, 642),
                Size = new Size(96, 40),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnClose.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.Add(btnClose);

            ReloadRoots();
            SetFormatsChecked(true);
            BuildSearchLayout(grpRoots, grpFormats, grpOpt, btnAddRoot, btnAllRoots, btnNoneRoots, lblResults, btnAllResults, btnNoneResults, btnClose);

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
                Description = L.Text("S186")
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
            return _selectedFormats;
        }

        private async void BtnScan_Click(object? sender, EventArgs e)
        {
            var roots = GetSelectedRoots();
            if (roots.Count == 0)
            {
                _lblStatus.Text = L.Text("S187");
                return;
            }

            var formats = GetSelectedFormats();
            if (formats.Count == 0)
            {
                _lblStatus.Text = L.Text("S188");
                return;
            }

            var minSize = (long)_numMinSize.Value * 1024 * 1024;

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
                    _lblStatus.Text = L.Format("S189", result.Count);
                }
                else
                {
                    _lblStatus.Text = L.Format("S190", result.Count);
                    _lblStatus.ForeColor = result.Count > 0 ? Color.FromArgb(76, 175, 80) : Color.Gray;
                }

                Found = result;
                FillGrid(result);
            }
            catch (Exception ex)
            {
                _lblStatus.Text = L.Format("S055", ex.Message);
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
                        _lblStatus.Text = L.Format("S191", p.CurrentDirectory);
                    }
                    else
                    {
                        _lblStatus.Text = L.Format("S192", p.FilesScanned);
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
                row.Cells[3].Value = Path.GetExtension(r.LocalPath ?? "").TrimStart('.').ToLowerInvariant();
                row.Cells[4].Value = r.Name;
                row.Cells[5].Value = DatabaseFileLocator.FormatSize(r.FileSize);
                row.Cells[6].Value = r.FileModified.ToString("yyyy-MM-dd HH:mm");
                row.Cells[7].Value = r.IsBackup ? L.Text("S193") : "";
                row.Tag = r;
                if (r.IsBackup) row.DefaultCellStyle.BackColor = Color.FromArgb(232, 245, 233);
                _grid.Rows.Add(row);
            }
        }

        private void SetResultsChecked(bool check)
        {
            foreach (DataGridViewRow row in _grid.Rows)
            {
                row.Cells[0].Value = check;
            }
        }

        private void Grid_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (e.ColumnIndex == 0) return; // کلیک مستقیم روی خود تیک، خودش توگل می‌کند
            var cell = _grid.Rows[e.RowIndex].Cells[0];
            cell.Value = !(cell.Value as bool? ?? false);
        }

        private void RunPathAction(Action<string> action)
        {
            var path = _ctxResults?.Tag as string;
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                action(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(L.Text("S194") + ex.Message, L.Text("S195"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenFolder(string path)
        {
            Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + path + "\"") { UseShellExecute = true });
        }

        private void OpenFile(string path)
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        private void CopyPath(string path)
        {
            Clipboard.SetText(path);
        }

        private void CopyFileToFolder(string path)
        {
            using var dlg = new FolderBrowserDialog { Description = L.Text("S196") };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            var dest = Path.Combine(dlg.SelectedPath, Path.GetFileName(path));
            File.Copy(path, dest, overwrite: true);
            MessageBox.Show(L.Text("S197") + dest, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                _lblStatus.Text = L.Text("S198");
                return;
            }

            var form = new DatabaseCopyForm(selected);
            form.ShowDialog(this);
        }
    }
}
