using System.Diagnostics;

namespace DatabaseFinder
{
    public class DatabaseCopyForm : Form
    {
        private readonly List<DatabaseInfo> _servers;
        private List<DatabaseCopyItem> _items = new();
        private readonly TextBox _txtDest;
        private readonly TreeView _tree;
        private readonly TextBox _txtLog;
        private readonly Button _btnCopy;
        private readonly Button _btnManualPath;
        private readonly Button _btnManifest;
        private readonly Button _btnOpen;
        private readonly Label _lblStatus;
        private readonly RadioButton _rdoVss;
        private readonly RadioButton _rdoStopStart;
        private readonly RadioButton _rdoReportOnly;
        private string _manifestPath = "";

        public DatabaseCopyForm(List<DatabaseInfo> servers)
        {
            _servers = servers;

            Text = "کپی فایل‌های دیتابیس";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(760, 620);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(680, 520);

            var lblTitle = new Label
            {
                Text = "کپی فایل‌های فیزیکی دیتابیس",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                AutoSize = true,
                Location = new Point(12, 8)
            };
            Controls.Add(lblTitle);

            var grpDest = new GroupBox
            {
                Text = "مسیر مقصد",
                Location = new Point(12, 42),
                Size = new Size(736, 60)
            };

            _txtDest = new TextBox
            {
                Location = new Point(12, 24),
                Size = new Size(560, 27),
                Text = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "DatabaseFilesBackup")
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
            Controls.Add(grpDest);

            var lblItems = new Label
            {
                Text = "دیتابیس‌های انتخابی (تیک بزنید):",
                Location = new Point(12, 112),
                AutoSize = true
            };
            Controls.Add(lblItems);

            _tree = new TreeView
            {
                Location = new Point(12, 136),
                Size = new Size(480, 330),
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
                Location = new Point(500, 136),
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
                Location = new Point(628, 136),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClearAll.Click += (s, e) => SetAllChecked(false);
            Controls.Add(btnClearAll);

            _btnManualPath = new Button
            {
                Text = "انتخاب پوشه دستی...",
                Location = new Point(500, 172),
                Size = new Size(248, 30),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnManualPath.Click += BtnManualPath_Click;
            Controls.Add(_btnManualPath);

            _btnManifest = new Button
            {
                Text = "مانیفست SHA-256",
                Location = new Point(500, 208),
                Size = new Size(248, 30),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnManifest.Click += BtnManifest_Click;
            Controls.Add(_btnManifest);

            _btnOpen = new Button
            {
                Text = "باز کردن پوشه",
                Location = new Point(500, 244),
                Size = new Size(248, 30),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnOpen.Click += BtnOpen_Click;
            Controls.Add(_btnOpen);

            var grpLocked = new GroupBox
            {
                Text = "فایل‌های قفل‌شده (سرویس فعال):",
                Location = new Point(500, 286),
                Size = new Size(248, 182),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            _rdoVss = new RadioButton
            {
                Text = "Shadow Copy بدون توقف سرویس (پیشنهادی)",
                Location = new Point(10, 26),
                Size = new Size(228, 34),
                Checked = true,
                Font = new Font("Segoe UI", 8.8F)
            };
            grpLocked.Controls.Add(_rdoVss);

            var lblVssHint = new Label
            {
                Text = "با VSS هم‌زمان با سرویس در حال اجرا؛ نیاز به Administrator.",
                Location = new Point(28, 58),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 7.8F)
            };
            grpLocked.Controls.Add(lblVssHint);

            _rdoStopStart = new RadioButton
            {
                Text = "توقف و شروع مجدد خودکار سرویس SQL",
                Location = new Point(10, 86),
                Size = new Size(228, 34),
                Font = new Font("Segoe UI", 8.8F)
            };
            grpLocked.Controls.Add(_rdoStopStart);

            var lblStopHint = new Label
            {
                Text = "سرویس دیتابیس موقتاً متوقف و پس از کپی راه‌اندازی می‌شود.",
                Location = new Point(28, 118),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 7.8F)
            };
            grpLocked.Controls.Add(lblStopHint);

            _rdoReportOnly = new RadioButton
            {
                Text = "فقط گزارش خطا",
                Location = new Point(10, 148),
                Size = new Size(180, 24),
                Font = new Font("Segoe UI", 8.8F)
            };
            grpLocked.Controls.Add(_rdoReportOnly);
            Controls.Add(grpLocked);

            var lblLog = new Label
            {
                Text = "گزارش کپی:",
                Location = new Point(12, 476),
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            Controls.Add(lblLog);

            _txtLog = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Location = new Point(12, 500),
                Size = new Size(736, 76),
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9F),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_txtLog);

            _btnCopy = new Button
            {
                Text = "شروع کپی",
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(12, 576),
                Size = new Size(140, 34),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnCopy.Click += BtnCopy_Click;
            Controls.Add(_btnCopy);

            _lblStatus = new Label
            {
                AutoSize = true,
                Location = new Point(170, 584),
                ForeColor = Color.Gray,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            Controls.Add(_lblStatus);

            Load += DatabaseCopyForm_Load;
        }

        private async void DatabaseCopyForm_Load(object? sender, EventArgs e)
        {
            _btnCopy.Enabled = false;
            _lblStatus.Text = "در حال شناسایی فایل‌ها...";
            AppendLog("در حال اتصال به دیتابیس‌ها و شناسایی فایل‌های فیزیکی...");

            var result = await Task.Run(() =>
                DatabaseFileLocator.BuildPlan(_servers, message => AppendLogSafe(message)));

            _items = result;
            _tree.Nodes.Clear();

            foreach (var group in _items.GroupBy(i =>
                i.Server.IsOnline
                    ? $"{i.Server.TypeDisplayName}@{i.Server.Host}:{i.Server.Port}"
                    : $"آفلاین - {i.Server.FormatName}"))
            {
                var serverNode = new TreeNode(group.Key)
                {
                    Checked = true
                };
                foreach (var item in group)
                {
                    var sizeText = item.Files.Count > 0
                        ? $" ({item.Files.Count} فایل - {DatabaseFileLocator.FormatSize(item.TotalSize)})"
                        : "";
                    var locationText = !item.Server.IsOnline && item.Server.LocalPath != null
                        ? "  [" + System.IO.Path.GetDirectoryName(item.Server.LocalPath) + "]"
                        : "";
                    var node = new TreeNode($"{item.DatabaseName}{sizeText}{locationText}")
                    {
                        Tag = item,
                        Checked = true
                    };
                    if (!string.IsNullOrEmpty(item.Error))
                    {
                        node.Text += "  ⚠ " + ShortError(item.Error);
                        node.ForeColor = Color.FromArgb(211, 47, 47);
                    }
                    else if (item.UseManualPath)
                    {
                        node.Text += "  (مسیر دستی)";
                        node.ForeColor = Color.FromArgb(255, 152, 0);
                    }
                    serverNode.Nodes.Add(node);
                }
                _tree.Nodes.Add(serverNode);
            }

            _tree.ExpandAll();
            _btnCopy.Enabled = true;
            _lblStatus.Text = $"{_items.Count} دیتابیس شناسایی شد. موارد خطادار با ⚠ مشخص شده‌اند.";
        }

        private static string HandlingText(LockedFileHandling h)
        {
            switch (h)
            {
                case LockedFileHandling.Vss: return "Shadow Copy (VSS) — بدون توقف سرویس";
                case LockedFileHandling.StopServices: return "توقف و شروع مجدد سرویس دیتابیس";
                default: return "فقط گزارش خطا";
            }
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

        private List<DatabaseCopyItem> GetCheckedItems()
        {
            var selected = new List<DatabaseCopyItem>();
            foreach (TreeNode node in _tree.Nodes)
            {
                foreach (TreeNode child in node.Nodes)
                {
                    if (child.Checked && child.Tag is DatabaseCopyItem item)
                        selected.Add(item);
                }
            }
            return selected;
        }

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "پوشه مقصد را انتخاب کنید",
                SelectedPath = _txtDest.Text
            };
            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                _txtDest.Text = fbd.SelectedPath;
            }
        }

        private void BtnManualPath_Click(object? sender, EventArgs e)
        {
            if (_tree.SelectedNode?.Tag is DatabaseCopyItem item)
            {
                if (item.Files.Count > 0 && string.IsNullOrEmpty(item.Error))
                {
                    _lblStatus.Text = "این دیتابیس فایل‌های شناسایی‌شده دارد؛ نیازی به مسیر دستی نیست.";
                    return;
                }

                using var fbd = new FolderBrowserDialog
                {
                    Description = $"پوشه حاوی فایل‌های دیتابیس {item.DatabaseName} را انتخاب کنید"
                };
                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    item.ManualPath = fbd.SelectedPath;
                    item.UseManualPath = true;
                    item.Error = null;
                    _tree.SelectedNode.Text = $"{item.DatabaseName}  (پوشه دستی: {fbd.SelectedPath})";
                    _tree.SelectedNode.ForeColor = Color.FromArgb(255, 152, 0);
                    _lblStatus.Text = "پوشه دستی تنظیم شد.";
                }
            }
            else
            {
                _lblStatus.Text = "ابتدا یک دیتابیس را از درخت انتخاب کنید.";
            }
        }

        private async void BtnCopy_Click(object? sender, EventArgs e)
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

            _btnCopy.Enabled = false;
            _btnCopy.Text = "در حال کپی...";
            _btnManifest.Enabled = false;
            _btnOpen.Enabled = false;
            _txtLog.Clear();
            AppendLog($"مقصد: {destRoot}");
            AppendLog($"تعداد دیتابیس‌های انتخابی: {items.Count}");
            var lockedHandling = _rdoVss.Checked ? LockedFileHandling.Vss
                : _rdoStopStart.Checked ? LockedFileHandling.StopServices
                : LockedFileHandling.ReportOnly;
            AppendLog($"روش فایل‌های قفل‌شده: {HandlingText(lockedHandling)}");
            AppendLog("");

            try
            {
                var result = await Task.Run(() =>
                    DatabaseFileLocator.ExecuteCopy(items, destRoot, lockedHandling, message => AppendLogSafe(message)));

                AppendLog("");
                AppendLog("--------------------------");
                AppendLog($"فایل‌های کپی‌شده: {result.FilesCopied} | حجم: {DatabaseFileLocator.FormatSize(result.BytesCopied)} | ناموفق: {result.Failed}");

                if (result.FilesCopied > 0)
                {
                    AppendLog("در حال ساخت مانیفست SHA-256 ...");
                    _manifestPath = await Task.Run(() => ManifestGenerator.Generate(destRoot));
                    AppendLog($"مانیفست: {_manifestPath}");
                    _btnManifest.Enabled = true;
                    _btnOpen.Enabled = true;
                }

                _lblStatus.Text = $"پایان کپی: {result.FilesCopied} فایل کپی شد، {result.Failed} مورد ناموفق.";
                _lblStatus.ForeColor = result.Failed > 0 ? Color.FromArgb(211, 47, 47) : Color.FromArgb(76, 175, 80);
            }
            catch (Exception ex)
            {
                AppendLog($"خطا: {ex.Message}");
                _lblStatus.Text = "خطا در کپی.";
                _lblStatus.ForeColor = Color.FromArgb(211, 47, 47);
            }
            finally
            {
                _btnCopy.Enabled = true;
                _btnCopy.Text = "شروع کپی";
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