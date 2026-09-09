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
        UpdateChecker.Enabled=false;
        var upJson=@"{""tag_name"":""v9.9.9"",""body"":""test"",""assets"":[{""name"":""DatabaseFinder-Light.exe"",""browser_download_url"":""https://x/light.exe""},{""name"":""DatabaseFinder.exe"",""browser_download_url"":""https://x/full.exe""}]}";
        var upFull=UpdateChecker.Parse(upJson,"DatabaseFinder.exe");
        Check(upFull is { IsNewer:true } && upFull.AssetName=="DatabaseFinder.exe" && upFull.AssetUrl.StartsWith("https://x/"),"update parse picks the running exe asset");
        var upLight=UpdateChecker.Parse(upJson,"DatabaseFinder-Light.exe");
        Check(upLight is { AssetName:"DatabaseFinder-Light.exe" },"update light build picks its own asset");
        var currentTag="v"+UpdateChecker.VersionString(UpdateChecker.CurrentVersion);
        var upEqualJson="{\"tag_name\":\""+currentTag+"\",\"assets\":[{\"name\":\"DatabaseFinder.exe\",\"browser_download_url\":\"u\"}]}";
        var upEqual=UpdateChecker.Parse(upEqualJson,"DatabaseFinder.exe");
        Check(upEqual is { IsNewer:false },"update same version is not newer");
        Check(UpdateChecker.Parse("{broken","DatabaseFinder.exe") is null,"update malformed json fails softly");
        var upEmpty=UpdateChecker.Parse(@"{""tag_name"":""v9.9.9"",""assets"":[]}","DatabaseFinder.exe");
        Check(upEmpty is null,"update without exe asset yields nothing");
        var sumsTxt="f4b53d3562fcf9c6ed98faa86fe7b7bc0fdb920cd6b0fda880bcc0034887a3af  DatabaseFinder.exe\n034bd136ce553b78a854a87b5470a22b4231612389c47deb42138340109455d3*DatabaseFinder-Light.exe\n";
        Check(UpdateChecker.ParseChecksum(sumsTxt,"DatabaseFinder.exe")=="f4b53d3562fcf9c6ed98faa86fe7b7bc0fdb920cd6b0fda880bcc0034887a3af","checksum line parsed for standalone build");
        Check(UpdateChecker.ParseChecksum(sumsTxt,"DatabaseFinder-Light.exe")=="034bd136ce553b78a854a87b5470a22b4231612389c47deb42138340109455d3","binary-mode marker parsed for light build");
        Check(UpdateChecker.ParseChecksum(sumsTxt,"Missing.exe") is null,"unknown asset yields no checksum");
        Check(UpdateChecker.ParseChecksum(new string('g',64)+"  a.exe","a.exe") is null,"non-hex checksum line rejected");
        foreach(var language in new[]{"fa","en"})
        {
            L.Language=language;
            using var main=new Form1(new MainViewState(results,new HashSet<int>{1},0));main.Show();Application.DoEvents();
            Check(main.CaptureView().Checked.SetEquals(new[]{1}),language+" restored selection");
            Check(main.CaptureView().Results[1].LocalPath==results[1].LocalPath,language+" unchanged path");
            Check(main.Text.Contains(UpdateChecker.VersionString(UpdateChecker.CurrentVersion)),language+" main title carries version");
            Check(!Descendants(main).OfType<LinkLabel>().Single().Visible,language+" update link hidden until a newer release is published");
            var mainGrid=Descendants(main).OfType<DataGridView>().First();
            Check(mainGrid.Rows[0].Cells["colDbCount"].Value is string s1 && s1=="-",language+" online count placeholder without live measure");
            Check(mainGrid.Rows[1].Cells["colDbCount"].Value is string s2 && s2=="-",language+" offline database shows no service count");
            results[0].DatabaseCount=5;typeof(Form1).GetMethod("MergeOfflineResults",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(main,new object[]{new List<DatabaseInfo>()});
            Check(mainGrid.Rows[0].Cells["colDbCount"].Value is string s3 && s3=="5",language+" counted databases reflected after measure");
            results[0].DatabaseCount=null;
            Render(main,Path.Combine(output,"main-"+language+".png"));
            results[0].DatabaseNames=new List<string>{"Northwind","Accounts"};
            using var dbl=new DatabaseListForm(results[0]);dbl.Show();Application.DoEvents();
            var dbListBox=Descendants(dbl).OfType<ListBox>().Single();
            Check(dbListBox.Items.Count==2,language+" database list shows names");
            using var emptyDb=new DatabaseListForm(new DatabaseInfo{Type=DatabaseType.SQLServer,Host="localhost",Port=1433,DatabaseNames=new List<string>()});emptyDb.Show();Application.DoEvents();
            Check(Descendants(emptyDb).OfType<ListBox>().Single().Items.Count==0 && Descendants(emptyDb).OfType<Label>().Any(l=>l.Text==L.Text("S331")),language+" empty database list message");
            results[0].DatabaseNames=null;
            Render(dbl,Path.Combine(output,"dblist-"+language+".png"));
            using var disk=new DiskScanForm();disk.Show();Application.DoEvents();
            typeof(DiskScanForm).GetMethod("FillGrid",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(disk,new object[]{new List<DatabaseInfo>{results[1]}});
            var diskGrid=Descendants(disk).OfType<DataGridView>().Single();
            typeof(DiskScanForm).GetMethod("SetResultsChecked",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(disk,new object[]{false});Check(!(bool)diskGrid.Rows[0].Cells[0].Value!,language+" disk clear selection");
            typeof(DiskScanForm).GetMethod("SetResultsChecked",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(disk,new object[]{true});Check((bool)diskGrid.Rows[0].Cells[0].Value!,language+" disk select all");
            Render(disk,Path.Combine(output,"disk-"+language+".png"));
            using var picker=new FormatPickerForm(DiskFormatRegistry.All);picker.Show();Application.DoEvents();
            Check(picker.SelectedFormats.Count==14,language+" all 14 format groups available");
            Check(picker.SelectedFormats.Any(f=>f.Name==L.Text("S212")&&f.NamePatterns.Length==4&&f.FolderNames.Length==0),language+" name-pattern format carried through picker");
            Check(picker.SelectedFormats.Any(f=>f.Name==L.Text("S213")&&f.FolderNames.Length==1&&f.NamePatterns.Length==0),language+" folder-marker format carried through picker");
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
            var backupOpts=Descendants(backup).OfType<CheckBox>().ToList();
            Check(backupOpts.Any(c=>c.Text==L.Text("S333")),language+" compress option shown");
            Check(backupOpts.Any(c=>c.Text==L.Text("S334")),language+" verify option shown");
            Check(backupOpts.Any(c=>c.Text==L.Text("S335")),language+" checksum option shown");
            Check(backupOpts.First(c=>c.Text==L.Text("S333")).Checked,language+" compress default on");
            Check(backupOpts.First(c=>c.Text==L.Text("S334")).Checked,language+" verify default on");
            Check(!backupOpts.First(c=>c.Text==L.Text("S335")).Checked,language+" checksum default off");
            using var remote=new RemoteScannerForm();remote.Show();Application.DoEvents();Render(remote,Path.Combine(output,"remote-"+language+".png"));
            using var details=new DatabaseDetailForm(results[0]);details.Show();Application.DoEvents();Render(details,Path.Combine(output,"details-"+language+".png"));
            using var query=new QueryRunnerForm(results[1]);query.Show();Application.DoEvents();Render(query,Path.Combine(output,"query-"+language+".png"));
            var qWin=Descendants(query).OfType<CheckBox>().FirstOrDefault(c=>c.Text==L.Text("S328"));
            Check(qWin is { Visible:false },language+" non-SQL hides windows auth");
            using var qsql=new QueryRunnerForm(results[0]);qsql.Show();Application.DoEvents();
            var qWinSql=Descendants(qsql).OfType<CheckBox>().FirstOrDefault(c=>c.Text==L.Text("S328"));
            Check(qWinSql is { Visible:true },language+" SQL Server shows windows auth");
            if(language=="en") foreach(var form in new Form[]{main,disk,picker,settings,copy,backup,remote,details,query,dbl,emptyDb}) foreach(var c in Descendants(form).Where(c=>c is Button or Label or GroupBox))Check(!Regex.IsMatch(c.Text,"[\u0600-\u06ff]"),"English control "+c.Text);
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
        // Regression: SQL Server accepts TCP but never sends a banner. The async version probe must
        // time out instead of hanging forever (which would permanently busy-lock re-detection).
        var silentListener=new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback,0);silentListener.Start();
        var silentPort=((System.Net.IPEndPoint)silentListener.LocalEndpoint!).Port;
        var silentAccept=Task.Run(()=>silentListener.AcceptTcpClient());
        var silentProbe=Task.Run(()=>DatabaseTester.TestConnectionAsync(new DatabaseInfo{Type=DatabaseType.SQLServer,Port=silentPort}));
        Check(silentProbe.Wait(TimeSpan.FromSeconds(8)),"async probe returns for silent server (SQL Server)");
        Check(silentProbe.Result.Success,"async probe keeps success on silent banner");
        silentListener.Stop();
        (silentAccept.IsCompleted&&silentAccept.Result!=null?silentAccept.Result:null)?.Dispose();
        Check(NetworkRanges.Expand("192.168.1.5").Count==1,"single IP expands to one host");
        Check(NetworkRanges.Expand("192.168.1.1-192.168.1.254").Count==254,"full dash range expands");
        Check(NetworkRanges.Expand("192.168.1.100-254").Count==155,"last-octet dash range expands");
        Check(NetworkRanges.Expand("192.168.1.0/24").Count==254,"CIDR /24 expands excluding network and broadcast");
        Check(NetworkRanges.Expand("192.168.1.0/15").Count==0,"oversized CIDR rejected");
        var mfDir=Path.Combine(output,"manifest-fixture");Directory.CreateDirectory(mfDir);
        var mfFile=Path.Combine(mfDir,"SQL Server_ANBAR","ANBAR.bak");Directory.CreateDirectory(Path.GetDirectoryName(mfFile)!);File.WriteAllBytes(mfFile,new byte[4096]);
        var mfItem=new DatabaseBackupItem{Server=new DatabaseInfo{Type=DatabaseType.SQLServer,Host="localhost",Port=1433,ServiceName="MSSQLSERVER"},DatabaseName="ANBAR",FolderName="SQL Server_ANBAR",Method="BACKUP DATABASE",Chain="full",Compress=true,Verify=true,Checksum=false,StartedUtc=DateTime.UtcNow.AddSeconds(-5),EndedUtc=DateTime.UtcNow,Done=true,BytesProduced=4096};
        mfItem.OutputFiles.Add(mfFile);
        var mfTxt=ManifestGenerator.Generate(mfDir,new[]{mfItem});
        var mfTxtContent=File.ReadAllText(mfTxt);
        var mfJson=JsonDocument.Parse(File.ReadAllText(mfTxt.Replace("manifest.txt","manifest.json")));
        Check(mfJson.RootElement.GetProperty("databases").GetArrayLength()==1,"manifest lists databases");
        Check(mfJson.RootElement.GetProperty("servers").GetArrayLength()==1,"manifest lists servers");
        Check(mfJson.RootElement.GetProperty("databases")[0].GetProperty("Chain").GetString()=="full","manifest records chain type");
        Check(mfJson.RootElement.GetProperty("databases")[0].GetProperty("Files").GetArrayLength()==1 && mfJson.RootElement.GetProperty("databases")[0].GetProperty("Files")[0].GetProperty("Size").GetInt64()==4096,"manifest records per-database files");
        Check(mfJson.RootElement.GetProperty("summary").GetProperty("succeeded").GetInt32()==1,"manifest summary counts success");
        Check(mfJson.RootElement.GetProperty("selfSha256").GetString()!.Length==64,"manifest contains self sha256");
        Check(mfJson.RootElement.GetProperty("schema").GetString()==ManifestGenerator.ToolVersion,"manifest carries schema version");
        Check(mfJson.RootElement.GetProperty("taxNotice").GetString()=="TAX 181 ARTICLE . MSAM Group","manifest carries tax notice");
        Check(mfTxtContent.Contains("TAX 181 ARTICLE . MSAM Group"),"txt manifest carries article 181 stamp");
        Check(File.ReadAllText(Path.Combine(mfDir,"manifest.md")).Contains("<pre>"),"markdown wraps article 181 stamp in pre");
        Check(mfTxtContent.Contains("Manifest SHA-256: "),"txt manifest contains self sha256 line");
        Check(!mfTxtContent.Contains("\r"),"manifest txt uses LF line endings");
        Check(mfTxtContent.Contains("sha256sum block"),"manifest txt includes sha256sum block");
        Check(mfTxtContent.Contains("┌") && mfTxtContent.Contains("└"),"manifest txt uses framed layout");
        Check(File.Exists(Path.Combine(mfDir,"manifest.md")) && File.ReadAllText(Path.Combine(mfDir,"manifest.md")).Contains("## Databases"),"manifest markdown is generated");
        File.Delete(Path.Combine(mfDir,"manifest.txt"));File.Delete(Path.Combine(mfDir,"manifest.json"));File.Delete(Path.Combine(mfDir,"manifest.md"));
        Directory.Delete(mfDir,true);
        Check(NetworkRanges.Expand("999.9.9.9").Count==0,"invalid range rejected");
        var localNets=NetworkRanges.GetLocalRanges();
        Check(localNets.GetType()==typeof(List<string>),"local network enumeration returns a list");
        Check(localNets.All(n=>NetworkRanges.Expand(n).Count>0),"every detected local range is parseable");
        Check(RemoteScanner.KnownPorts.Length==9,"remote scanner covers all 9 known database ports");
        var ssrpPayload = "ServerName;PC1;InstanceName;MSSQLSERVER;IsClustered;No;Version;15.0.2000.5;tcp;1433;np;\\\\PC1\\pipe\\sql\\query;;ServerName;PC1;InstanceName;SQLEXPRESS;IsClustered;No;Version;13.0.5026.0;tcp;51437;np;\\\\PC1\\pipe\\MSSQL$SQLEXPRESS\\sql\\query;;";
        var ssrpBytes = new byte[ssrpPayload.Length + 3];
        ssrpBytes[0] = 0x05;
        ssrpBytes[1] = (byte)(ssrpPayload.Length & 0xFF);
        ssrpBytes[2] = (byte)(ssrpPayload.Length >> 8);
        for (int i = 0; i < ssrpPayload.Length; i++) ssrpBytes[3 + i] = (byte)ssrpPayload[i];
        var ssrp = new List<SqlBrowserInstance>();
        SqlBrowser.Parse(ssrpBytes, "192.168.1.10", ssrp);
        Check(ssrp.Count == 2, "SQL Browser payload parses two instances");
        Check(ssrp[0].TcpPort == 1433 && ssrp[0].InstanceName == "MSSQLSERVER", "default instance parsed (tcp 1433)");
        Check(ssrp[1].TcpPort == 51437 && ssrp[1].InstanceName == "SQLEXPRESS", "named instance dynamic port parsed (51437)");
        Check(ssrp[1].Version == "13.0.5026.0", "named instance version parsed");
        Check(ssrp[1].ServerName == "PC1", "browser server name parsed");
        var ssrpBad = new List<SqlBrowserInstance>();
        SqlBrowser.Parse(new byte[] { 0x01, 0x02, 0x03 }, "192.168.1.10", ssrpBad);
        Check(ssrpBad.Count == 0, "malformed SQL Browser payload yields nothing");
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
