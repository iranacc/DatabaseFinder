using DatabaseFinder;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

internal static class Program
{
    private static int checks;
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var output=Path.GetFullPath(args.FirstOrDefault() ?? "ui-checks");Directory.CreateDirectory(output);
        var fa=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText("DatabaseFinder/Strings.fa.json"))!;
        var en=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText("DatabaseFinder/Strings.en.json"))!;
        Check(fa.Keys.Order().SequenceEqual(en.Keys.Order()),"Resource keys match");
        foreach(var (key,value) in fa)Check(Regex.Matches(value,@"\{#\}").Count==Regex.Matches(en[key],@"\{#\}").Count,"Placeholders "+key);
        var results=new List<DatabaseInfo>{new(){Name="SQL Server",Type=DatabaseType.SQLServer,ServiceName="MSSQLSERVER",Host="localhost",Port=1433,Version="2022",IsOnline=true,IsRunningAsService=true},new(){Name="Accounting",Type=DatabaseType.SQLite,IsOnline=false,LocalPath=@"D:\Accounting\داده‌ها\accounts.sqlite",FileSize=4194304,FormatName="SQLite"}};
        foreach(var language in new[]{"fa","en"})
        {
            L.Language=language;
            using var main=new Form1(new MainViewState(results,new HashSet<int>{1},0));main.Show();Application.DoEvents();
            Check(main.CaptureView().Checked.SetEquals(new[]{1}),language+" restored selection");
            Check(main.CaptureView().Results[1].LocalPath==results[1].LocalPath,language+" unchanged path");
            Render(main,Path.Combine(output,"main-"+language+".png"));
            using var disk=new DiskScanForm();disk.Show();Application.DoEvents();
            typeof(DiskScanForm).GetMethod("FillGrid",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(disk,new object[]{new List<DatabaseInfo>{results[1]}});
            var diskGrid=Descendants(disk).OfType<DataGridView>().Single();
            typeof(DiskScanForm).GetMethod("SetResultsChecked",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(disk,new object[]{false});Check(!(bool)diskGrid.Rows[0].Cells[0].Value!,language+" disk clear selection");
            typeof(DiskScanForm).GetMethod("SetResultsChecked",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(disk,new object[]{true});Check((bool)diskGrid.Rows[0].Cells[0].Value!,language+" disk select all");
            Render(disk,Path.Combine(output,"disk-"+language+".png"));
            using var picker=new FormatPickerForm(DiskFormatRegistry.All);picker.Show();Application.DoEvents();
            Check(picker.SelectedFormats.Count==11,language+" all 11 format groups available");
            var tree=Descendants(picker).OfType<TreeView>().Single();
            tree.Nodes[0].Nodes[1].Checked=false;
            Check(picker.SelectedFormats[0].Extensions.SequenceEqual(new[]{".mdf",".ndf"}),language+" individual extension deselect");
            var search=Descendants(picker).OfType<TextBox>().Single();search.Text="SQLite";Application.DoEvents();
            Check(tree.Nodes.Count==1,language+" search filters groups");
            search.Text="";Application.DoEvents();Check(!tree.Nodes[0].Nodes[1].Checked,language+" filter preserves hidden selection");
            tree.Nodes[0].Checked=true;Check(picker.SelectedFormats[0].Extensions.Length==3,language+" group selects all extensions");
            Render(picker,Path.Combine(output,"formats-"+language+".png"));
            // Empty server lists ensure dialog loading cannot connect to real databases.
            using var settings=new SettingsForm(new AppSettings());settings.Show();Application.DoEvents();Render(settings,Path.Combine(output,"settings-"+language+".png"));
            using var copy=new DatabaseCopyForm(new List<DatabaseInfo>());copy.Show();Application.DoEvents();Render(copy,Path.Combine(output,"copy-"+language+".png"));
            using var backup=new DatabaseBackupForm(new List<DatabaseInfo>());backup.Show();Application.DoEvents();Render(backup,Path.Combine(output,"backup-"+language+".png"));
            using var remote=new RemoteScannerForm();remote.Show();Application.DoEvents();Render(remote,Path.Combine(output,"remote-"+language+".png"));
            using var details=new DatabaseDetailForm(results[0]);details.Show();Application.DoEvents();Render(details,Path.Combine(output,"details-"+language+".png"));
            using var query=new QueryRunnerForm(results[1]);query.Show();Application.DoEvents();Render(query,Path.Combine(output,"query-"+language+".png"));
            if(language=="en") foreach(var form in new Form[]{main,disk,picker,settings,copy,backup,remote,details,query}) foreach(var c in Descendants(form).Where(c=>c is Button or Label or GroupBox))Check(!Regex.IsMatch(c.Text,"[\u0600-\u06ff]"),"English control "+c.Text);
            main.Size=main.MinimumSize;Application.DoEvents();Render(main,Path.Combine(output,"main-small-"+language+".png"));
            disk.Size=disk.MinimumSize;Application.DoEvents();Render(disk,Path.Combine(output,"disk-small-"+language+".png"));
            var second=new DatabaseInfo{Type=DatabaseType.SQLite,Name="SelectedOnly",IsOnline=false,LocalPath=@"D:\Fixture\selected.sqlite"};
            typeof(DiskScanForm).GetMethod("FillGrid",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(disk,new object[]{new List<DatabaseInfo>{results[1],second}});
            diskGrid.Rows[0].Cells[0].Value=false;
            var merge=(Button)typeof(DiskScanForm).GetField("_btnMerge",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(disk)!;merge.PerformClick();
            Check(disk.Found.Count==1 && disk.Found[0].Name=="SelectedOnly",language+" merge includes checked rows only");
            picker.Close();disk.Close();main.CloseForLanguageChange();
        }
        L.Language="en";
        Check(L.Format("S055","نام {#} C:\\Data") == "Error: نام {#} C:\\Data","Interpolation arguments remain untouched");
        Check(L.DisplayFormat("SQL Server (بکاپ)")=="SQL Server (backup)","Restored format label follows UI language");
        var fixture=Path.Combine(output,"fixtures");Directory.CreateDirectory(fixture);
        var sqlite=Path.Combine(fixture,"accounts.sqlite");var bytes=new byte[4096];System.Text.Encoding.ASCII.GetBytes("SQLite format 3\0").CopyTo(bytes,0);File.WriteAllBytes(sqlite,bytes);
        var selected=DiskFormatRegistry.All.Single(f=>f.Name=="SQLite");
        var scan=new DiskScanner();
        var found=scan.Scan(new[]{fixture},new[]{selected},0,null,CancellationToken.None);
        Check(found.Count==1 && found[0].LocalPath==sqlite,"Selected SQLite extension found");
        selected.Extensions=new[]{".db"};
        Check(scan.Scan(new[]{fixture},new[]{selected},0,null,CancellationToken.None).Count==0,"Deselected extension excluded from scan");
        Check(JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(new AppSettings{Language="en"}))!.Language=="en","Language settings round trip");
        L.Language="fa";
        var saved="";
        var sessionType=typeof(Form1).Assembly.GetType("DatabaseFinder.AppSession")!;
        using var session=(ApplicationContext)Activator.CreateInstance(sessionType,new object?[]{new MainViewState(results,new HashSet<int>{1},0),(Action<string>)(value=>saved=value)})!;
        foreach(var lang in new[]{"en","fa"}){
            var old=(Form1)session.MainForm!;
            var languageBox=Descendants(old).OfType<ComboBox>().Single(c=>c.AccessibleName=="Language / زبان");
            languageBox.SelectedIndex=lang=="en"?1:0;Application.DoEvents();
            var current=(Form1)session.MainForm!;
            Check(!ReferenceEquals(old,current) && !current.IsDisposed,"Language change retains live application context");
            Check(current.CaptureView().Checked.SetEquals(new[]{1}),"Language switch preserves checked rows");
            Check(saved==lang && L.Language==lang,"Language choice persisted");
        }
        ((Form1)session.MainForm!).CloseForLanguageChange();
        Console.WriteLine($"PASS: {checks} checks. Renders: {output}");
    }
    static IEnumerable<Control> Descendants(Control control){foreach(Control child in control.Controls){yield return child;foreach(var descendant in Descendants(child))yield return descendant;}}
    static void Render(Form form,string file){form.CreateControl();form.PerformLayout();using var bitmap=new Bitmap(form.Width,form.Height);form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(file);}
    static void Check(bool condition,string label){if(!condition)throw new Exception("FAIL: "+label);checks++;}
}
