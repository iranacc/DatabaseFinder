using System.Diagnostics;

namespace DatabaseFinder
{
    public class DatabaseBackupForm : Form
    {
        private readonly List<DatabaseInfo> _servers;
        private List<DatabaseBackupItem> _items = new();
        private readonly TextBox _txtDest;
        private readonly TreeView _tree;
        private readonly TextBox _txtLog;
        private readonly Button _btnStart;
        private readonly Button _btnManifest;
        private readonly Button _btnOpen;
        private readonly Label _lblStatus;
        private string _manifestPath = "";

        public DatabaseBackupForm(List<DatabaseInfo> servers)
        {
            _servers = servers;

            Text = "بکاپ منطقی دیتابیس (نسخه پشتیبان)";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(780, 660);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(700, 560);

            var lblTitle = new Label
            {
                Text = "بکاپ منطقی دیتابیس‌های در حال اجرا",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                AutoSize = true,
                Location = new Point(12, 8)
            };
            Controls.Add(lblTitle);

            var grpDest = new GroupBox
            {
                Text = "مسیر مقصد نسخه پشتیبان",
                Location = new Point(12, 42),
                Size = new Size(756, 82)
            };

            _txtDest = new TextBox
            {
                Location = new Point(12, 24),
                Size = new Size(560, 27),
                Text = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "DatabaseFinder", "Backup")
            };
            grpDest.Controls.Add(_txtDest);

            var btnBrowse = new Button
            {
                Text = "بگرد...",
                Location = new Point(580, 23),
                Size = new Size(100, 28),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnBrowse.Click += BtnBrowse_Click;
            grpDest.Controls.Add(btnBrowse);

            var lblHint = new Label
            {
                Text = "توجه: SQL Server با هویت سرویس خود فایل بکاپ را می‌نویسد؛ مسیری انتخاب کنید که آن سرویس دسترسی نوشتن داشته باشد.",
                Location = new Point(12, 56),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8.5F)
            };
            grpDest.Controls.Add(lblHint);
            Controls.Add(grpDest);

            var lblItems = new Label
            {
                Text = "دیتابیس‌های انتخابی (تیک بزنید):",
                Location = new Point(12, 134),
                AutoSize = true
            };
            Controls.Add(lblItems);

            _tree = new TreeView
            {
                Location = new Point(12, 158),
                Size = new Size(500, 310),
                CheckBoxes = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9.5F)
            };
            _tree.AfterCheck += (s, e) =>
            {
                var node = e.Node;
                if (node != null && node.Nodes != null)
                    foreach (TreeNode child in node.Nodes) child.Checked = node.Checked;
            };
            Controls.Add(_tree);

            var btnSelectAll = new Button
            {
                Text = "انتخاب همه",
                Location = new Point(520, 158),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnSelectAll.Click += (s, e) => SetAllChecked(true);
            Controls.Add(btnSelectAll);

            var btnClearAll = new Button
            {
                Text = "حذف انتخاب",
                Location = new Point(648, 158),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClearAll.Click += (s, e) => SetAllChecked(false);
            Controls.Add(btnClearAll);

            var btnExpand = new Button
            {
                Text = "باز/بسته کردن",
                Location = new Point(520, 194),
                Size = new Size(248, 30),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnExpand.Click += (s, e) => _tree.ExpandAll();
            Controls.Add(btnExpand);

            var lblLog = new Label
            {
                Text = "گزارش بکاپ:",
                Location = new Point(12, 478),
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            Controls.Add(lblLog);

            _txtLog = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Location = new Point(12, 502),
                Size = new Size(756, 100),
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9F),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_txtLog);

            _btnStart = new Button
            {
                Text = "شروع بکاپ",
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(12, 614),
                Size = new Size(150, 36),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnStart.Click += BtnStart_Click;
            Controls.Add(_btnStart);

            _btnManifest = new Button
            {
                Text = "ایجاد مانیفست SHA-256",
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(172, 614),
                Size = new Size(180, 36),
                Enabled = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnManifest.Click += BtnManifest_Click;
            Controls.Add(_btnManifest);

            _btnOpen = new Button
            {
                Text = "باز کردن پوشه",
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(362, 614),
                Size = new Size(150, 36),
                Enabled = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnOpen.Click += BtnOpen_Click;
            Controls.Add(_btnOpen);

            _lblStatus = new Label
            {
                AutoSize = true,
                Location = new Point(12, 596),
                ForeColor = Color.Gray,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            Controls.Add(_lblStatus);

            Load += DatabaseBackupForm_Load;
        }

        private async void DatabaseBackupForm_Load(object? sender, EventArgs e)
        {
            _btnStart.Enabled = false;
            _lblStatus.Text = "در حال آماده‌سازی (اتصال به دیتابیس‌ها)...";
            AppendLog("در حال اتصال به دیتابیس‌های در حال اجرا و آماده‌سازی دستور بکاپ...");

            var result = await Task.Run(() =>
                DatabaseBackuper.BuildBackupPlan(_servers, _txtDest.Text, message => AppendLogSafe(message)));

            _items = result;
            _tree.Nodes.Clear();

            foreach (var group in _items.GroupBy(i => i.Server.TypeDisplayName))
            {
                var serverNode = new TreeNode(group.Key) { Checked = true };
                foreach (var item in group)
                {
                    var node = new TreeNode(TextFor(item))
                    {
                        Tag = item,
                        Checked = !string.IsNullOrEmpty(item.Error) || item.Server.IsOnline
                    };
                    if (!string.IsNullOrEmpty(item.Error))
                    {
                        node.Text += "  ⚠ " + ShortError(item.Error);
                        node.ForeColor = Color.FromArgb(211, 47, 47);
                        node.Checked = false;
                    }
                    serverNode.Nodes.Add(node);
                }
                _tree.Nodes.Add(serverNode);
            }

            _tree.ExpandAll();
            _btnStart.Enabled = true;
            _lblStatus.Text = $"{_items.Count} دیتابیس آماده شد. موارد خطادار با ⚠ مشخص شده‌اند.";
        }

        private static string TextFor(DatabaseBackupItem item)
        {
            var method = string.IsNullOrEmpty(item.Method) ? "" : $"  ({item.Method})";
            return $"{item.DatabaseName}{method}";
        }

        private static string ShortError(string error)
        {
            if (string.IsNullOrEmpty(error)) return "";
            return error.Length > 60 ? error.Substring(0, 60) + "..." : error;
        }

        private void AppendLog(string message)
        {
            if (_txtLog.IsDisposed) return;
            _txtLog.AppendText(message + Environment.NewLine);
        }

        private void AppendLogSafe(string message)
        {
            try
            {
                if (_txtLog.IsDisposed) return;
                if (_txtLog.InvokeRequired)
                {
                    _txtLog.BeginInvoke(new Action(() => AppendLog(message)));
                }
                else
                {
                    AppendLog(message);
                }
            }
            catch { }
        }

        private void SetAllChecked(bool check)
        {
            foreach (TreeNode node in _tree.Nodes)
            {
                node.Checked = check;
                foreach (TreeNode child in node.Nodes) child.Checked = check;
            }
        }

        private List<DatabaseBackupItem> GetCheckedItems()
        {
            var selected = new List<DatabaseBackupItem>();
            foreach (TreeNode node in _tree.Nodes)
            {
                foreach (TreeNode child in node.Nodes)
                {
                    if (child.Checked && child.Tag is DatabaseBackupItem item)
                        selected.Add(item);
                }
            }
            return selected;
        }

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "پوشه مقصد نسخه پشتیبان را انتخاب کنید",
                SelectedPath = _txtDest.Text
            };
            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                _txtDest.Text = fbd.SelectedPath;
            }
        }

        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            var items = GetCheckedItems();
            if (items.Count == 0)
            {
                _lblStatus.Text = "دست‌کم یک دیتابیس را تیک بزنید.";
                return;
            }

            var destRoot = _txtDest.Text.Trim();
            if (string.IsNullOrEmpty(destRoot))
            {
                _lblStatus.Text = "مسیر مقصد را انتخاب کنید.";
                return;
            }

            _btnStart.Enabled = false;
            _btnStart.Text = "در حال بکاپ...";
            _btnManifest.Enabled = false;
            _btnOpen.Enabled = false;
            _txtLog.Clear();
            AppendLog($"مقصد: {destRoot}");
            AppendLog($"تعداد دیتابیس‌های انتخابی: {items.Count}");
            AppendLog("");

            try
            {
                await Task.Run(() =>
                    DatabaseBackuper.ExecuteBackup(items, message => AppendLogSafe(message)));

                var ok = items.Count(i => i.Done);
                var failed = items.Count(i => i.Failed);

                AppendLog("");
                AppendLog("--------------------------");
                AppendLog($"انجام شد: {ok} | ناموفق: {failed} | حجم کل بکاپ: {DatabaseFileLocator.FormatSize(items.Sum(i => i.BytesProduced))}");

                if (ok > 0)
                {
                    AppendLog("در حال ساخت مانیفست SHA-256 ...");
                    _manifestPath = await Task.Run(() => ManifestGenerator.Generate(destRoot));
                    AppendLog($"مانیفست: {_manifestPath}");
                    _btnManifest.Enabled = true;
                    _btnOpen.Enabled = true;
                }

                _lblStatus.Text = failed > 0
                    ? $"بکاپ تمام شد: {ok} موفق، {failed} ناموفق."
                    : $"بکاپ کامل انجام شد: {ok} دیتابیس پشتیبان گرفته شد.";
                _lblStatus.ForeColor = failed > 0 ? Color.FromArgb(211, 47, 47) : Color.FromArgb(76, 175, 80);
            }
            catch (Exception ex)
            {
                AppendLog($"خطا: {ex.Message}");
                _lblStatus.Text = "خطا در انجام بکاپ.";
                _lblStatus.ForeColor = Color.FromArgb(211, 47, 47);
            }
            finally
            {
                _btnStart.Enabled = true;
                _btnStart.Text = "شروع بکاپ";
            }
        }

        private async void BtnManifest_Click(object? sender, EventArgs e)
        {
            _btnManifest.Enabled = false;
            AppendLog("در حال ساخت مانیفست SHA-256 ...");
            try
            {
                _manifestPath = await Task.Run(() => ManifestGenerator.Generate(_txtDest.Text.Trim()));
                AppendLog($"مانیفست: {_manifestPath}");
                _lblStatus.Text = "مانیفست ساخته شد.";
            }
            catch (Exception ex)
            {
                AppendLog($"خطا: {ex.Message}");
            }
            finally
            {
                _btnManifest.Enabled = true;
            }
        }

        private void BtnOpen_Click(object? sender, EventArgs e)
        {
            try
            {
                var target = _txtDest.Text.Trim();
                if (!string.IsNullOrEmpty(_manifestPath) && File.Exists(_manifestPath))
                    target = Path.GetDirectoryName(_manifestPath) ?? target;
                Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"خطا در باز کردن پوشه: {ex.Message}";
            }
        }
    }
}