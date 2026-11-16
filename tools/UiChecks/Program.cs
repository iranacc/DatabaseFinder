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
            Check(mainGrid.Rows[0].Cells["colCheck"].GetContentBounds(0).Width>0,language+" main grid checkbox glyph has render bounds");
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
            var diskSelCol=diskGrid.Columns.Cast<DataGridViewColumn>().First(c=>c.Name=="sel");
            Check(diskSelCol is DataGridViewCheckBoxColumn && !diskSelCol.ReadOnly && !diskGrid.ReadOnly,language+" offline results checkbox stays editable");
            Check(diskGrid.Rows[0].Cells[0].GetContentBounds(0).Width>0,language+" offline checkbox glyph has render bounds");
            var testItems=new List<DatabaseInfo>{new(){Name="Big",FileSize=1288490188L,LocalPath="C:\\a.bak"},new(){Name="Small",FileSize=10590617L,LocalPath="C:\\b.bak"},new(){Name="Medium",FileSize=112302489L,LocalPath="C:\\c.bak"}};
            typeof(DiskScanForm).GetMethod("FillGrid",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(disk,new object[]{testItems});
            diskGrid.Sort(diskGrid.Columns["size"],System.ComponentModel.ListSortDirection.Ascending);
            Check(((DatabaseInfo)diskGrid.Rows[0].Tag!).Name=="Small",language+" disk sort size asc 1st is Small");
            Check(((DatabaseInfo)diskGrid.Rows[1].Tag!).Name=="Medium",language+" disk sort size asc 2nd is Medium");
            Check(((DatabaseInfo)diskGrid.Rows[2].Tag!).Name=="Big",language+" disk sort size asc 3rd is Big");
            diskGrid.Sort(diskGrid.Columns["size"],System.ComponentModel.ListSortDirection.Descending);
            Check(((DatabaseInfo)diskGrid.Rows[0].Tag!).Name=="Big",language+" disk sort size desc 1st is Big");
            Check(((DatabaseInfo)diskGrid.Rows[1].Tag!).Name=="Medium",language+" disk sort size desc 2nd is Medium");
            Check(((DatabaseInfo)diskGrid.Rows[2].Tag!).Name=="Small",language+" disk sort size desc 3rd is Small");
            // Smart Selection & Normalization Tests
            Check(DiskScanForm.GetDatabaseFamilyKey(new DatabaseInfo{LocalPath=@"C:\Backups\Sepidar_AutoBackup_1402_12_29_23_00.bak"})=="sepidar", "FamilyKey Sepidar 1402");
            Check(DiskScanForm.GetDatabaseFamilyKey(new DatabaseInfo{LocalPath=@"C:\Backups\Sepidar_AutoBackup_1403_01_01_23_00.bak"})=="sepidar", "FamilyKey Sepidar 1403");
            Check(DiskScanForm.GetDatabaseFamilyKey(new DatabaseInfo{LocalPath=@"D:\Data\Tadbir_Data_2023-10-15_12-30-00.bak"})=="tadbir_data", "FamilyKey Tadbir 2023");
            Check(DiskScanForm.GetDatabaseFamilyKey(new DatabaseInfo{LocalPath=@"D:\Data\Tadbir_Data_2024-05-01_10-00-00.bak"})=="tadbir_data", "FamilyKey Tadbir 2024");
            Check(DiskScanForm.GetDatabaseFamilyKey(new DatabaseInfo{LocalPath=@"C:\Holoo\Holoo_Backup_99.bak"})=="holoo", "FamilyKey Holoo 99");

            var smartTestItems = new List<DatabaseInfo>
            {
                new() { Name="Sepidar_Old", LocalPath=@"C:\Backups\Sepidar_AutoBackup_1402_12_29_23_00.bak", IsBackup=true, FileModified=new DateTime(2024, 3, 19), FileSize=1000 },
                new() { Name="Sepidar_New", LocalPath=@"C:\Backups\Sepidar_AutoBackup_1403_06_31_23_00.bak", IsBackup=true, FileModified=new DateTime(2024, 9, 21), FileSize=1200 },
                new() { Name="Tadbir_Old", LocalPath=@"C:\Backups\Tadbir_Data_2023-10-15_12-30-00.bak", IsBackup=true, FileModified=new DateTime(2023, 10, 15), FileSize=2000 },
                new() { Name="Tadbir_New", LocalPath=@"C:\Backups\Tadbir_Data_2024-05-01_10-00-00.bak", IsBackup=true, FileModified=new DateTime(2024, 5, 1), FileSize=2200 }
            };
            typeof(DiskScanForm).GetMethod("FillGrid", BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(disk, new object[]{smartTestItems});
            typeof(DiskScanForm).GetMethod("SelectLatestPerDatabase", BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(disk, null);

            var smartSelected = (List<DatabaseInfo>)typeof(DiskScanForm).GetMethod("GetSelectedResults", BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(disk, null)!;
            Check(smartSelected.Count == 2, language + " smart select reduced 4 backups to 2 latest");
            Check(smartSelected.Any(x => x.Name == "Sepidar_New") && smartSelected.Any(x => x.Name == "Tadbir_New"), language + " smart select chose the newest files");
            Check(!smartSelected.Any(x => x.Name == "Sepidar_Old") && !smartSelected.Any(x => x.Name == "Tadbir_Old"), language + " smart select unpicked the older files");

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
            var copyItem = new DatabaseCopyItem
            {
                Server = new DatabaseInfo { Type = DatabaseType.SQLServer, Host = "localhost", Port = 1433, IsOnline = true },
                DatabaseName = "TestDB",
                FolderName = "SQLServer_TestDB"
            };
            copyItem.Files.Add(new FileCopyItem { DisplayName = "TestDB.mdf", Size = 1024, RelativePath = "TestDB.mdf" });
            copyItem.Files.Add(new FileCopyItem { DisplayName = "TestDB_log.ldf", Size = 1024, RelativePath = "TestDB_log.ldf" });
            typeof(DatabaseCopyForm).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(copy, new List<DatabaseCopyItem> { copyItem });
            var checkedSet = (HashSet<DatabaseCopyItem>)typeof(DatabaseCopyForm).GetField("_checkedItems", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(copy)!;
            checkedSet.Add(copyItem);
            typeof(DatabaseCopyForm).GetMethod("FillTree", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(copy, null);
            var copyTree = Descendants(copy).OfType<TreeView>().First();
            var dbTreeNode = copyTree.Nodes[0].Nodes[0];
            Check(dbTreeNode.Checked, language + " db node checked");
            Check(dbTreeNode.Nodes.Count == 2, language + " db has 2 file nodes");
            Check(dbTreeNode.Nodes[0].Checked && dbTreeNode.Nodes[1].Checked, language + " file nodes initially checked");
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
        var mtfBak = Path.Combine(fixture, "real_sql.bak");
        var mtfBytes = new byte[1024];
        System.Text.Encoding.ASCII.GetBytes("TAPE").CopyTo(mtfBytes, 0);
        File.WriteAllBytes(mtfBak, mtfBytes);
        Check(FileSignatures.IsSqlServerFile(mtfBak), "MTF TAPE .bak detected as real SQL Server backup");
        var fakeBak = Path.Combine(fixture, "acrobat.dll.bak");
        File.WriteAllBytes(fakeBak, new byte[] { 0x4D, 0x5A, 0x90, 0x00 });
        Check(!FileSignatures.IsSqlServerFile(fakeBak), "Fake .bak (dll header) rejected");

        // ---- تست‌های Signature Carving و کشف فایل‌های تغییرنام‌یافته یا پنهان‌شده ----
        var carveFixture = Path.Combine(output, "carve_fixtures");
        if (Directory.Exists(carveFixture)) Directory.Delete(carveFixture, true);
        Directory.CreateDirectory(carveFixture);

        var disguisedSqlite = Path.Combine(carveFixture, "report_cache.tmp");
        File.WriteAllBytes(disguisedSqlite, System.Text.Encoding.ASCII.GetBytes("SQLite format 3\0SampleTableData"));
        Check(DatabaseCarver.TryCarve(disguisedSqlite, new FileInfo(disguisedSqlite).Length, out var carvedSqlite) && carvedSqlite.IsDisguised && carvedSqlite.TrueExtension == ".sqlite", "DatabaseCarver detects disguised SQLite .tmp file");

        var disguisedMdf = Path.Combine(carveFixture, "archive_data.dat");
        var mdfBytes = new byte[8192];
        mdfBytes[0] = 0x01; mdfBytes[1] = 0x0F; mdfBytes[4] = 0x08; mdfBytes[5] = 0x02;
        File.WriteAllBytes(disguisedMdf, mdfBytes);
        Check(DatabaseCarver.TryCarve(disguisedMdf, 8192, out var carvedMdf) && carvedMdf.IsDisguised && carvedMdf.TrueExtension == ".mdf", "DatabaseCarver detects disguised SQL Server MDF .dat file");

        var disguisedBak = Path.Combine(carveFixture, "backup_1402.old");
        File.WriteAllBytes(disguisedBak, System.Text.Encoding.ASCII.GetBytes("MSSQLBAK_TEST_DATA_PADDING"));
        Check(DatabaseCarver.TryCarve(disguisedBak, 26, out var carvedBak) && carvedBak.IsDisguised && carvedBak.TrueExtension == ".bak", "DatabaseCarver detects disguised SQL Server compressed backup .old file");

        var scanDisguised = new DiskScanner().Scan(new[] { carveFixture }, DiskFormatRegistry.All, 10, enableSignatureCarving: true, null, CancellationToken.None);
        Check(scanDisguised.Any(x => x.IsDisguised && x.LocalPath == disguisedMdf), "DiskScanner uncovers disguised MDF file via signature carving");
        Check(scanDisguised.Any(x => x.IsDisguised && x.LocalPath == disguisedSqlite), "DiskScanner uncovers disguised SQLite file via signature carving");
        Check(scanDisguised.Any(x => x.IsDisguised && x.LocalPath == disguisedBak), "DiskScanner uncovers disguised BAK file via signature carving");

        // ---- تست‌های SmartPathLocator و پویش مسیرهای هوشمند و اکسترنال ----
        var smartRoots = SmartPathLocator.GetDiscoveredRoots(ScanMode.Smart);
        Check(smartRoots.Count > 0, "SmartPathLocator finds smart roots");
        Check(smartRoots.Any(r => r.Category == RootCategory.FixedDrive || r.Category == RootCategory.UserDocuments || r.Category == RootCategory.DatabaseData || r.Category == RootCategory.AccountingSoftware), "SmartPathLocator categorizes smart roots");

        var fullRoots = SmartPathLocator.GetDiscoveredRoots(ScanMode.Full);
        Check(fullRoots.Count > 0, "SmartPathLocator finds full drive roots");
        Check(fullRoots.All(r => r.Category == RootCategory.FixedDrive || r.Category == RootCategory.ExternalDrive || r.Category == RootCategory.NetworkDrive), "Full mode contains drive roots");

        var extRoots = SmartPathLocator.GetDiscoveredRoots(ScanMode.ExternalOnly);
        Check(extRoots.All(r => r.Category == RootCategory.ExternalDrive), "ExternalOnly mode contains only external drives");

        var defaultSmart = DiskScanner.GetDefaultRoots(quickOnly: true);
        Check(defaultSmart.Count > 0, "DiskScanner.GetDefaultRoots smart mode returns roots");
        var defaultFull = DiskScanner.GetDefaultRoots(quickOnly: false);
        Check(defaultFull.Count > 0, "DiskScanner.GetDefaultRoots full mode returns roots");

        // ---- تست‌های Archive Peeking و پویش درون فایل‌های فشرده بدون Extract ----
        var arcFixture = Path.Combine(output, "archive_fixtures");
        if (Directory.Exists(arcFixture)) Directory.Delete(arcFixture, true);
        Directory.CreateDirectory(arcFixture);

        var testZip = Path.Combine(arcFixture, "Financial_Backups_1403.zip");
        using (var zipFs = new FileStream(testZip, FileMode.Create))
        using (var za = new System.IO.Compression.ZipArchive(zipFs, System.IO.Compression.ZipArchiveMode.Create))
        {
            var bakEntry = za.CreateEntry("Sepidar_1403_01_01.bak");
            using (var es = bakEntry.Open())
            {
                byte[] mssqlBytes = System.Text.Encoding.ASCII.GetBytes("MSSQLBAK_SEPIDAR_SAMPLE_DATA_BYTES_PADDING_HEADER_DATA_1403");
                es.Write(mssqlBytes, 0, mssqlBytes.Length);
            }

            var sqliteEntry = za.CreateEntry("company_cache.sqlite");
            using (var es = sqliteEntry.Open())
            {
                byte[] sqBytes = System.Text.Encoding.ASCII.GetBytes("SQLite format 3\0SampleDbData");
                es.Write(sqBytes, 0, sqBytes.Length);
            }

            var txtEntry = za.CreateEntry("readme.txt");
            using (var es = txtEntry.Open())
            {
                byte[] txtBytes = System.Text.Encoding.ASCII.GetBytes("Non database notes");
                es.Write(txtBytes, 0, txtBytes.Length);
            }
        }

        Check(ArchivePeeker.IsSupportedArchive(testZip), "ArchivePeeker recognizes .zip archive");
        var peeked = ArchivePeeker.PeekArchive(testZip, minSizeBytes: 10);
        Check(peeked.Count == 2, "ArchivePeeker discovers 2 databases inside ZIP, ignoring text file");
        Check(peeked.Any(x => x.FileName == "Sepidar_1403_01_01.bak" && x.Type == DatabaseType.SQLServer && x.IsBackup), "ArchivePeeker identifies inner SQL Server .bak");
        Check(peeked.Any(x => x.FileName == "company_cache.sqlite" && x.Type == DatabaseType.SQLite), "ArchivePeeker identifies inner SQLite .sqlite");

        var extractDest = Path.Combine(arcFixture, "extracted_sepidar.bak");
        bool extractedOk = ArchivePeeker.ExtractEntryWithProgress(testZip, "Sepidar_1403_01_01.bak", extractDest);
        Check(extractedOk && File.Exists(extractDest) && new FileInfo(extractDest).Length > 0, "ArchivePeeker extracts single entry on demand without full extraction");

        var scanArchives = new DiskScanner().Scan(new[] { arcFixture }, DiskFormatRegistry.All, 10, enableSignatureCarving: false, enableArchivePeeking: true, null, CancellationToken.None);
        Check(scanArchives.Any(x => x.IsInsideArchive && x.ArchiveContainerPath == testZip && x.ArchiveEntryPath == "Sepidar_1403_01_01.bak"), "DiskScanner discovers inner .bak from ZIP archive with IsInsideArchive");
        Check(scanArchives.Any(x => x.IsInsideArchive && x.ArchiveContainerPath == testZip && x.ArchiveEntryPath == "company_cache.sqlite"), "DiskScanner discovers inner SQLite from ZIP archive with IsInsideArchive");

        // ---- تست‌های Duplicate Detection و شناسایی هوشمند فایل‌های تکراری ----
        var dupFixture = Path.Combine(output, "dup_fixtures");
        if (Directory.Exists(dupFixture)) Directory.Delete(dupFixture, true);
        Directory.CreateDirectory(dupFixture);

        var dupPrimaryPath = Path.Combine(dupFixture, "Sepidar_1402_Master.bak");
        var dupCopy1Path = Path.Combine(dupFixture, "Sepidar_1402_Copy_Desktop.bak");
        var dupCopy2Path = Path.Combine(dupFixture, "Sepidar_1402_Copy_Temp.bak");
        var diffContentSameSize = Path.Combine(dupFixture, "Other_DB_SameSize.bak");
        var diffSizePath = Path.Combine(dupFixture, "Small_DB.bak");

        var dupBytes = new byte[65536];
        for (int i = 0; i < dupBytes.Length; i++) dupBytes[i] = (byte)(i % 251);
        File.WriteAllBytes(dupPrimaryPath, dupBytes);
        File.WriteAllBytes(dupCopy1Path, dupBytes);
        File.WriteAllBytes(dupCopy2Path, dupBytes);

        var diffBytes = new byte[65536];
        for (int i = 0; i < diffBytes.Length; i++) diffBytes[i] = (byte)((i + 42) % 251);
        File.WriteAllBytes(diffContentSameSize, diffBytes);

        File.WriteAllBytes(diffSizePath, new byte[1024]);

        var dupDbInfos = new List<DatabaseInfo>
        {
            new() { Name = "Sepidar_1402_Master", LocalPath = dupPrimaryPath, FileSize = dupBytes.Length, FileModified = DateTime.Now },
            new() { Name = "Sepidar_1402_Copy_Desktop", LocalPath = dupCopy1Path, FileSize = dupBytes.Length, FileModified = DateTime.Now.AddMinutes(-5) },
            new() { Name = "Sepidar_1402_Copy_Temp", LocalPath = dupCopy2Path, FileSize = dupBytes.Length, FileModified = DateTime.Now.AddMinutes(-10) },
            new() { Name = "Other_DB_SameSize", LocalPath = diffContentSameSize, FileSize = diffBytes.Length, FileModified = DateTime.Now },
            new() { Name = "Small_DB", LocalPath = diffSizePath, FileSize = 1024, FileModified = DateTime.Now }
        };

        var dupResult = DuplicateDetector.Analyze(dupDbInfos);
        Check(dupResult.TotalDuplicates == 2, "DuplicateDetector flags exactly 2 redundant copies");
        Check(dupResult.DuplicateGroupsCount == 1, "DuplicateDetector creates exactly 1 duplicate group");
        Check(dupResult.WastedBytes == (dupBytes.Length * 2), "DuplicateDetector calculates wasted duplicate bytes correctly");

        var master = dupDbInfos.Single(x => x.LocalPath == dupPrimaryPath);
        var copy1 = dupDbInfos.Single(x => x.LocalPath == dupCopy1Path);
        var copy2 = dupDbInfos.Single(x => x.LocalPath == dupCopy2Path);
        var nonDup1 = dupDbInfos.Single(x => x.LocalPath == diffContentSameSize);
        var nonDup2 = dupDbInfos.Single(x => x.LocalPath == diffSizePath);

        Check(master.IsPrimaryDuplicate && !master.IsDuplicate, "Master copy is marked as primary keeper and not marked redundant duplicate");
        Check(copy1.IsDuplicate && !copy1.IsPrimaryDuplicate, "Copy 1 is marked as redundant duplicate");
        Check(copy2.IsDuplicate && !copy2.IsPrimaryDuplicate, "Copy 2 is marked as redundant duplicate");
        Check(!nonDup1.IsDuplicate && !nonDup1.IsPrimaryDuplicate, "File with same size but different content is not marked duplicate");
        Check(!nonDup2.IsDuplicate && !nonDup2.IsPrimaryDuplicate, "File with different size is not marked duplicate");
        Check(master.DuplicateGroupId > 0 && master.DuplicateGroupId == copy1.DuplicateGroupId && master.DuplicateGroupId == copy2.DuplicateGroupId, "All copies share the same DuplicateGroupId");
        Check(copy1.DuplicateSiblingPaths != null && copy1.DuplicateSiblingPaths.Contains(dupPrimaryPath), "Duplicate siblings list contains other copies");

        using (var testDiskForm = new DiskScanForm())
        {
            testDiskForm.Show();
            Application.DoEvents();
            typeof(DiskScanForm).GetMethod("FillGrid", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(testDiskForm, new object[] { dupDbInfos });
            var selCol = (List<DatabaseInfo>)typeof(DiskScanForm).GetMethod("GetSelectedResults", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(testDiskForm, null)!;
            Check(!selCol.Any(x => x.IsDuplicate), "FillGrid automatically deselects redundant duplicate copies by default");
            Check(selCol.Any(x => x.LocalPath == dupPrimaryPath), "FillGrid keeps primary master copy selected by default");

            typeof(DiskScanForm).GetMethod("SelectDuplicatesOnly", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(testDiskForm, null);
            var dupOnlySel = (List<DatabaseInfo>)typeof(DiskScanForm).GetMethod("GetSelectedResults", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(testDiskForm, null)!;
            Check(dupOnlySel.All(x => x.IsDuplicate || x.IsPrimaryDuplicate) && dupOnlySel.Count == 3, "SelectDuplicatesOnly selects all 3 members of the duplicate group");

            typeof(DiskScanForm).GetMethod("DeselectDuplicates", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(testDiskForm, null);
            var dedupSel = (List<DatabaseInfo>)typeof(DiskScanForm).GetMethod("GetSelectedResults", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(testDiskForm, null)!;
            Check(!dedupSel.Any(x => x.IsDuplicate), "DeselectDuplicates unchecks all redundant duplicate copies");

            typeof(DiskScanForm).GetMethod("SelectUniqueOnly", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(testDiskForm, null);
            var uniqueSel = (List<DatabaseInfo>)typeof(DiskScanForm).GetMethod("GetSelectedResults", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(testDiskForm, null)!;
            Check(!uniqueSel.Any(x => x.IsDuplicate), "SelectUniqueOnly excludes all redundant duplicate files");
            testDiskForm.Close();
        }

        // ---- تست‌های Shadow Copies و پویش در نسخه‌های قبلی ویندوز ----
        var sampleVssOutput = """
        vssadmin 1.1 - Volume Shadow Copy Service administrative command-line tool
        (C) Copyright 2001-2013 Microsoft Corp.

        Contents of shadow copy set ID: {b5757989-138d-4f18-a621-3e4b78fa81ec}
           Contained 1 shadow copies at creation time: 11/10/2026 2:30:15 PM
              Shadow Copy ID: {447bf5a2-3b09-40ee-bdf8-9b8fb7bceb16}
                 Original Volume: (C:)\\?\Volume{d30e5860-0000-0000-0000-100000000000}\
                 Shadow Copy Volume: \\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy1
                 Originating Machine: AUDIT-SRV01
                 Service Machine: AUDIT-SRV01
                 Provider: 'Microsoft Software Shadow Copy provider 1.0'
                 Type: ClientAccessibleWriters
                 Attributes: Persistent, Client-accessible, No auto release, Differential, Auto recovered

        Contents of shadow copy set ID: {c6868090-249e-4f29-b732-4f5c89fb92fd}
           Contained 1 shadow copies at creation time: 11/15/2026 9:15:00 AM
              Shadow Copy ID: {558ca6b3-4c10-51ff-cef9-ac9fc8cdf027}
                 Original Volume: (D:)\\?\Volume{e41f6971-0000-0000-0000-200000000000}\
                 Shadow Copy Device Name: \\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy2
                 Originating Machine: AUDIT-SRV01
        """;

        var parsedSnapshots = ShadowCopyFinder.ParseVssadminOutput(sampleVssOutput);
        Check(parsedSnapshots.Count == 2, "ShadowCopyFinder.ParseVssadminOutput extracts 2 snapshots");
        Check(parsedSnapshots[0].Id == "{447bf5a2-3b09-40ee-bdf8-9b8fb7bceb16}", "First snapshot ID parsed");
        Check(parsedSnapshots[0].DriveLetter == "C:", "First snapshot DriveLetter parsed (C:)");
        Check(parsedSnapshots[0].DeviceObject == @"\\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy1", "First snapshot DeviceObject parsed");
        Check(parsedSnapshots[0].OriginatingMachine == "AUDIT-SRV01", "First snapshot OriginatingMachine parsed");
        Check(parsedSnapshots[1].DriveLetter == "D:", "Second snapshot DriveLetter parsed (D:)");
        Check(parsedSnapshots[1].DeviceObject == @"\\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy2", "Second snapshot DeviceObject parsed");

        var testShadowMountDir = Path.Combine(output, "vss_mock_mount");
        if (Directory.Exists(testShadowMountDir)) Directory.Delete(testShadowMountDir, true);
        Directory.CreateDirectory(testShadowMountDir);

        var snapItem1 = new DatabaseInfo
        {
            Name = "Sepidar_Historical.bak",
            LocalPath = Path.Combine(testShadowMountDir, "Sepidar", "Sepidar_Historical.bak"),
            FileSize = 1048576,
            FileModified = new DateTime(2026, 11, 10, 14, 30, 0)
        };
        ShadowCopyFinder.AnnotateShadowCopyResult(snapItem1, testShadowMountDir, parsedSnapshots[0]);
        Check(snapItem1.IsFromShadowCopy, "AnnotateShadowCopyResult marks item as IsFromShadowCopy");
        Check(snapItem1.ShadowCopyId == parsedSnapshots[0].Id, "AnnotateShadowCopyResult sets ShadowCopyId");
        Check(snapItem1.LiveDiskEquivalentPath != null && snapItem1.LiveDiskEquivalentPath.StartsWith("C:\\Sepidar"), "AnnotateShadowCopyResult computes LiveDiskEquivalentPath");
        Check(snapItem1.IsDeletedFromCurrentDisk, "AnnotateShadowCopyResult detects file is missing/deleted from current live disk");

        var snapItemLiveExists = new DatabaseInfo
        {
            Name = "Existing_File.bak",
            LocalPath = Path.Combine(testShadowMountDir, "Existing_File.bak"),
            FileSize = 1024,
            FileModified = DateTime.Now
        };
        snapItemLiveExists.LocalPath = Path.Combine(testShadowMountDir, Path.GetFileName(dupPrimaryPath));
        var dummySnap = new ShadowCopySnapshot { Id = "{1111}", DriveLetter = Path.GetPathRoot(dupPrimaryPath)!.TrimEnd('\\'), DeviceObject = @"\\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy9" };
        var mountDirForLive = Path.GetDirectoryName(dupPrimaryPath)!;
        ShadowCopyFinder.AnnotateShadowCopyResult(snapItemLiveExists, mountDirForLive, dummySnap);
        Check(!snapItemLiveExists.IsDeletedFromCurrentDisk, "AnnotateShadowCopyResult detects file currently exists on live disk");

        using (var vssDiskForm = new DiskScanForm())
        {
            vssDiskForm.Show();
            Application.DoEvents();

            var vssTestItems = new List<DatabaseInfo> { snapItem1, snapItemLiveExists, master };
            typeof(DiskScanForm).GetMethod("FillGrid", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(vssDiskForm, new object[] { vssTestItems });

            typeof(DiskScanForm).GetMethod("SelectShadowCopiesOnly", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(vssDiskForm, null);
            var vssOnlySel = (List<DatabaseInfo>)typeof(DiskScanForm).GetMethod("GetSelectedResults", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(vssDiskForm, null)!;
            Check(vssOnlySel.All(x => x.IsFromShadowCopy) && vssOnlySel.Count == 2, "SelectShadowCopiesOnly selects all items from shadow copies");

            typeof(DiskScanForm).GetMethod("SelectDeletedFromDiskOnly", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(vssDiskForm, null);
            var deletedOnlySel = (List<DatabaseInfo>)typeof(DiskScanForm).GetMethod("GetSelectedResults", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(vssDiskForm, null)!;
            Check(deletedOnlySel.Count == 1 && deletedOnlySel[0].Name == "Sepidar_Historical.bak" && deletedOnlySel[0].IsDeletedFromCurrentDisk, "SelectDeletedFromDiskOnly selects only the file missing from live disk");

            vssDiskForm.Close();
        }
        Check(ManifestGenerator.ToolVersion == UpdateChecker.VersionString(UpdateChecker.CurrentVersion), "Manifest ToolVersion matches executing version");
        Check(JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(new AppSettings{Language="en"}))!.Language=="en","Language settings round trip");
        var copySrc = Path.Combine(fixture, "test_large.dat");
        var copyDst = Path.Combine(fixture, "test_large_dest.dat");
        var dummyData = new byte[1024 * 1024];
        using (var fs = File.OpenWrite(copySrc)) { for (int i = 0; i < 4; i++) fs.Write(dummyData, 0, dummyData.Length); }
        var copyReports = new List<int>();
        DatabaseFileLocator.CopyFileWithProgress(copySrc, copyDst, (copied, total) => copyReports.Add((int)((copied * 100) / total)));
        Check(copyReports.Count > 0 && copyReports.Last() == 100 && new FileInfo(copyDst).Length == new FileInfo(copySrc).Length, "CopyFileWithProgress streams progress and matches source bytes");
        File.Delete(copyDst);
        var sampleItems = new List<DatabaseCopyItem>
        {
            new() { DatabaseName = "Erfan_sg3", Files = new() { new() { DisplayName = "Erfan_sg3.mdf", Size = 12L * 1024 * 1024 * 1024, IsInUse = true } } },
            new() { DatabaseName = "tax", Files = new() { new() { DisplayName = "tax.mdf", Size = 720L * 1024 * 1024, IsInUse = false } } },
            new() { DatabaseName = "Tadbir_Test", Files = new() { new() { DisplayName = "Tadbir_Test.mdf", Size = 57L * 1024 * 1024, IsInUse = true } } },
            new() { DatabaseName = "Argess_Config", Files = new() { new() { DisplayName = "Argess_Config.mdf", Size = 172L * 1024 * 1024, IsInUse = false } }, Error = "Access error" }
        };
        var bySizeDesc = sampleItems.OrderByDescending(i => i.TotalSize).ToList();
        Check(bySizeDesc[0].DatabaseName == "Erfan_sg3" && bySizeDesc.Last().DatabaseName == "Tadbir_Test", "Sort by size descending puts 12GB first and 57MB last");
        var bySizeAsc = sampleItems.OrderBy(i => i.TotalSize).ToList();
        Check(bySizeAsc[0].DatabaseName == "Tadbir_Test" && bySizeAsc.Last().DatabaseName == "Erfan_sg3", "Sort by size ascending puts 57MB first and 12GB last");
        var byNameAsc = sampleItems.OrderBy(i => i.DatabaseName, StringComparer.OrdinalIgnoreCase).ToList();
        Check(byNameAsc[0].DatabaseName == "Argess_Config" && byNameAsc.Last().DatabaseName == "tax", "Sort by name A-Z orders correctly");
        var filterSearch = sampleItems.Where(i => i.DatabaseName.Contains("erfan", StringComparison.OrdinalIgnoreCase)).ToList();
        Check(filterSearch.Count == 1 && filterSearch[0].DatabaseName == "Erfan_sg3", "Text search filter correctly matches case-insensitively");
        var filterLarge = sampleItems.Where(i => i.TotalSize >= 1024L * 1024 * 1024).ToList();
        Check(filterLarge.Count == 1 && filterLarge[0].DatabaseName == "Erfan_sg3", "Filter large databases (>1GB) isolates multi-gigabyte databases");
        var filterInUse = sampleItems.Where(i => i.Files.Any(f => f.IsInUse)).ToList();
        Check(filterInUse.Count == 2 && filterInUse.All(i => i.Files.Any(f => f.IsInUse)), "Filter in-use databases returns only locked files");
        var groupSize = sampleItems.GroupBy(i => i.TotalSize >= 1024L * 1024 * 1024 ? "Large" : i.TotalSize >= 100L * 1024 * 1024 ? "Medium" : "Small").ToDictionary(g => g.Key, g => g.Count());
        Check(groupSize["Large"] == 1 && groupSize["Medium"] == 2 && groupSize["Small"] == 1, "Categorize by size distributes into Large, Medium and Small groups");
        var sampleBackups = new List<DatabaseBackupItem>
        {
            new() { DatabaseName = "Erfan_sg3", Method = "BACKUP DATABASE", Server = new() { IsOnline = true } },
            new() { DatabaseName = "Bimeh96", Method = "BACKUP DATABASE", Server = new() { IsOnline = true } },
            new() { DatabaseName = "detector_181", Method = "pg_dump", Server = new() { IsOnline = false }, Error = "Connection refused" },
            new() { DatabaseName = "tax", Method = "mysqldump", Server = new() { IsOnline = true } }
        };
        var backupByName = sampleBackups.OrderBy(i => i.DatabaseName, StringComparer.OrdinalIgnoreCase).ToList();
        Check(backupByName[0].DatabaseName == "Bimeh96" && backupByName.Last().DatabaseName == "tax", "Backup sort by name A-Z orders correctly");
        var backupByMethod = sampleBackups.OrderBy(i => i.Method, StringComparer.OrdinalIgnoreCase).ToList();
        Check(backupByMethod[0].Method == "BACKUP DATABASE" && backupByMethod.Last().Method == "pg_dump", "Backup sort by method orders correctly");
        var backupSearch = sampleBackups.Where(i => i.DatabaseName.Contains("erfan", StringComparison.OrdinalIgnoreCase)).ToList();
        Check(backupSearch.Count == 1 && backupSearch[0].DatabaseName == "Erfan_sg3", "Backup text search matches database name");
        var backupHealthy = sampleBackups.Where(i => i.Server.IsOnline && string.IsNullOrEmpty(i.Error)).ToList();
        Check(backupHealthy.Count == 3 && !backupHealthy.Any(i => !string.IsNullOrEmpty(i.Error)), "Backup healthy filter filters out errors");
        var backupGroupMethod = sampleBackups.GroupBy(i => i.Method).ToDictionary(g => g.Key, g => g.Count());
        Check(backupGroupMethod["BACKUP DATABASE"] == 2 && backupGroupMethod["pg_dump"] == 1 && backupGroupMethod["mysqldump"] == 1, "Backup group by method categorizes properly");
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

        // ---------- Formal Reporting & Tax Audit Checks (Section 4) ----------
        Check(JalaliDateHelper.GetMonthName(1) == "فروردین", "Jalali month 1 is Farvardin");
        Check(JalaliDateHelper.GetMonthName(12) == "اسفند", "Jalali month 12 is Esfand");
        Check(JalaliDateHelper.FormatShort(new DateTime(2026, 3, 21)) == "1405/01/01", "Gregorian 2026-03-21 converts to 1405/01/01");
        Check(JalaliDateHelper.ToPersianDigits("12345") == "۱۲۳۴۵", "ToPersianDigits converts ASCII digits to Persian Unicode");

        var forensic = ForensicHostInfo.Collect();
        Check(!string.IsNullOrWhiteSpace(forensic.MachineName), "Forensic host collector retrieves MachineName");
        Check(!string.IsNullOrWhiteSpace(forensic.OsDescription), "Forensic host collector retrieves OsDescription");

        var testCase = new CaseInfo
        {
            TaxpayerName = "شرکت آزمایشی تجارت نوین",
            NationalId = "10103456789",
            CaseNumber = "TAX-181-405",
            Warrant = "W-9901",
            Officer = "سهراب سپهری",
            TaxpayerRepresentative = "نیما یوشیج",
            Location = "تهران",
            Notes = "تست ممیزی"
        };
        Check(!testCase.IsEmpty && testCase.TaxpayerName == "شرکت آزمایشی تجارت نوین", "CaseInfo retains taxpayer and audit particulars");

        var repTestDir = Path.Combine(output, "report_test");
        if (Directory.Exists(repTestDir)) Directory.Delete(repTestDir, true);
        Directory.CreateDirectory(repTestDir);
        var testManifestJson = Path.Combine(repTestDir, "manifest.json");
        var testJsonContent = """
        {
          "taxNotice": "TAX 181 ARTICLE . MSAM Group",
          "tool": "Database Finder",
          "version": "1.11.0",
          "machine": "AUDIT-SRV01",
          "user": "AuditorUser",
          "os": "Microsoft Windows 11 Pro 64-bit",
          "root": "D:\\181_Evidence\\Case_9901",
          "createdUtc": "2026-11-10T20:17:01Z",
          "caseFile": {
            "TaxpayerName": "شرکت صنایع پارس نوین",
            "NationalId": "14009876543",
            "Warrant": "181/1405/8821",
            "CaseNumber": "TAX-1405-9982",
            "Officer": "محسن حسینی",
            "TaxpayerRepresentative": "علی رضایی",
            "Location": "تهران، شهرک صنعتی شمس آباد",
            "Notes": "استخراج کامل دیتابیس‌های مالیاتی بدون وقفه در کارخانه"
          },
          "summary": { "databases": 2, "succeeded": 2, "failed": 0, "files": 2, "totalBytes": 10485760 },
          "databases": [
            { "Name": "Holoo_DB", "Engine": "SQL Server", "ServerVersion": "15.0.2000", "Method": "VSS Shadow Copy", "Bytes": 5242880, "Status": "ok" },
            { "Name": "Tadbir_Main", "Engine": "SQL Server", "ServerVersion": "15.0.2000", "Method": "VSS Shadow Copy", "Bytes": 5242880, "Status": "ok" }
          ],
          "acquisition": [
            { "RelPath": "Holoo_DB\\Holoo.mdf", "SourceHash": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", "DestHash": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", "Verified": true, "Bytes": 5242880, "Match": "MATCH" }
          ],
          "selfSha256": "4b227777d4dd1fc61c6f884f48641d02b4d121d3fd328cb08b5531fcacdabf8a"
        }
        """;
        File.WriteAllText(testManifestJson, testJsonContent);
        var genResult = ReportGenerator.GenerateFromManifest(testManifestJson);
        Check(File.Exists(genResult), "ReportGenerator produces valid output file");
        Check(File.Exists(ReportGenerator.LastGeneratedHtmlPath!), "ReportGenerator produces HTML report");
        var htmlContent = File.ReadAllText(ReportGenerator.LastGeneratedHtmlPath!);
        Check(htmlContent.Contains("صورت‌جلسه بازرسی و برداشت اطلاعات سامانه‌های رایانه‌ای"), "HTML minute contains Article 181 heading");
        Check(htmlContent.Contains("شرکت صنایع پارس نوین"), "HTML minute contains taxpayer company name");
        Check(htmlContent.Contains("14009876543"), "HTML minute contains national ID");
        Check(htmlContent.Contains("تطابق کامل (MATCH)"), "HTML minute contains 100% cryptographic match badge");
        Check(htmlContent.Contains("ساعت"), "HTML minute contains Jalali formal time");
        Check(htmlContent.Contains("AUDIT-SRV01"), "HTML minute contains host forensic name");

        if (PdfExporter.IsPdfSupported)
        {
            Check(!string.IsNullOrEmpty(ReportGenerator.LastGeneratedPdfPath) && File.Exists(ReportGenerator.LastGeneratedPdfPath), "Headless PDF generation creates valid PDF file");
            Check(new FileInfo(ReportGenerator.LastGeneratedPdfPath!).Length > 1000, "Generated PDF minute has non-trivial file size");
            Check(genResult.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase), "ReportGenerator returns PDF path when PDF engine is available");
        }
        Check(htmlContent.Contains("report-layout-table"), "HTML minute uses outer table layout for collision-free repeated footer");
        Console.WriteLine($"PASS: {checks} checks. Renders: {output}");
    }
    static IEnumerable<Control> Descendants(Control control){foreach(Control child in control.Controls){yield return child;foreach(var descendant in Descendants(child))yield return descendant;}}
    static void Render(Form form,string file){form.CreateControl();form.PerformLayout();using var bitmap=new Bitmap(form.Width,form.Height);form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(file);}
    static void Check(bool condition,string label){if(!condition)throw new Exception("FAIL: "+label);checks++;}
}
