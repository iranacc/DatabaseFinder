namespace DatabaseFinder;

/// <summary>Stable index/extension selection, independent of translated display names and search filtering.</summary>
public class FormatPickerForm : AppForm
{
    private readonly List<DiskFormat> _formats = DiskFormatRegistry.All;
    private readonly HashSet<string> _checked = new(StringComparer.OrdinalIgnoreCase);
    private readonly TreeView _tree = new() { Dock = DockStyle.Fill, CheckBoxes = true, HideSelection = false };
    private readonly TextBox _search = new() { Dock = DockStyle.Fill };
    private readonly Label _count = new() { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(6) };
    private bool _updating;
    protected override bool ModernLayout => true;
    public List<DiskFormat> SelectedFormats => _formats.Select((f, i) => new DiskFormat { Name = f.Name, Type = f.Type, IsBackup = f.IsBackup, Description = f.Description, ContentValidator = f.ContentValidator, NamePatterns = f.NamePatterns, FolderNames = f.FolderNames, Extensions = f.Extensions.Where(e => _checked.Contains(Key(i, e))).ToArray() }).Where(f => f.Extensions.Length > 0).ToList();
    private static string Key(int index, string extension) => index + ":" + extension;
    public FormatPickerForm(List<DiskFormat> selection)
    {
        Text = L.Text("S164"); ClientSize = new Size(660, 640); MinimumSize = new Size(540, 480); StartPosition = FormStartPosition.CenterParent; Font = new Font("Segoe UI", 10);
        for (var i = 0; i < _formats.Count; i++) foreach (var ext in _formats[i].Extensions) if (selection.Any(s => s.Name == _formats[i].Name && s.Extensions.Contains(ext))) _checked.Add(Key(i, ext));
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 5 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _search.PlaceholderText = L.Text("FormatSearch"); _search.AccessibleName = _search.PlaceholderText;
        _search.TextChanged += (_, _) => Populate();
        layout.Controls.Add(_search, 0, 0);
        layout.Controls.Add(UiTheme.Flow(UiTheme.Button(L.Text("S034"), (_, _) => SetAll(true)), UiTheme.Button(L.Text("S163"), (_, _) => SetAll(false))), 0, 1);
        layout.Controls.Add(_tree, 0, 2); layout.Controls.Add(_count, 0, 3);
        var ok = UiTheme.Button(L.Text("ApplySelection"), (_, _) => DialogResult = DialogResult.OK, true);
        var cancel = UiTheme.Button(L.Text("S307"), (_, _) => DialogResult = DialogResult.Cancel);
        AcceptButton = ok; CancelButton = cancel; layout.Controls.Add(UiTheme.Flow(ok, cancel), 0, 4); Controls.Add(layout);
        _tree.AfterCheck += (_, e) => { if (_updating || e.Node == null) return; _updating = true; try { if (e.Node.Tag is int index) { foreach (var ext in _formats[index].Extensions) SetKey(Key(index, ext), e.Node.Checked); foreach (TreeNode child in e.Node.Nodes) child.Checked = e.Node.Checked; } else if (e.Node.Tag is string key) { SetKey(key, e.Node.Checked); var parent = e.Node.Parent!; parent.Checked = parent.Nodes.Cast<TreeNode>().All(n => n.Checked); } UpdateCount(); } finally { _updating = false; } };
        Populate();
    }
    private void SetKey(string key, bool value) { if (value) _checked.Add(key); else _checked.Remove(key); }
    private void SetAll(bool value) { for (var i = 0; i < _formats.Count; i++) foreach (var ext in _formats[i].Extensions) SetKey(Key(i, ext), value); Populate(); }
    private void UpdateCount()
    {
        _count.Text = L.Format("ExtensionCount", _checked.Count);
        foreach (TreeNode node in _tree.Nodes) { var i = (int)node.Tag!; var n = _formats[i].Extensions.Count(e => _checked.Contains(Key(i, e))); node.Text = $"{_formats[i].Name}  ({n}/{_formats[i].Extensions.Length})"; }
    }
    private void Populate()
    {
        _updating = true; _tree.BeginUpdate(); try { _tree.Nodes.Clear(); for (var i = 0; i < _formats.Count; i++) { var f = _formats[i]; if (!string.IsNullOrWhiteSpace(_search.Text) && !($"{f.Name} {f.Description} {string.Join(" ", f.Extensions)}").Contains(_search.Text.Trim(), StringComparison.OrdinalIgnoreCase)) continue; var node = new TreeNode(f.Name) { Tag = i, Checked = f.Extensions.All(e => _checked.Contains(Key(i, e))) }; foreach (var ext in f.Extensions) node.Nodes.Add(new TreeNode(ext) { Tag = Key(i, ext), Checked = _checked.Contains(Key(i, ext)) }); _tree.Nodes.Add(node); if (_search.Text.Length > 0) node.Expand(); } UpdateCount(); } finally { _tree.EndUpdate(); _updating = false; }
    }
}
