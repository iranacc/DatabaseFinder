# Database Finder

Windows utility to detect databases currently running on your system.

> **فارسی:** [مطالعه نسخه فارسی README](README.fa.md) / [Persian README]

## Detection
- SQL Server
- MySQL
- MariaDB
- PostgreSQL
- Oracle
- MongoDB
- Redis
- Elasticsearch
- CouchDB

## What's new in v1.8.7

- **Backup without sa password:** when SQL login fails, "Copy files" falls back to physical MDF/LDF acquisition from registry/service paths via VSS (no password needed); attach the files later in your lab.
- **Find saved password (manual):** "Find saved password..." button scans vendor config folders (Mahak/Holoo/Parsian...) on demand only — never automatic.
- **Emergency sysadmin (manual):** red "Emergency sysadmin..." button (Administrator only, double-confirmed) grants the current Windows user sysadmin via single-user mode; every step is logged for the Art.181 case file.

## What's new in v1.8.6

- **In-app self-update:** on startup the app reads the latest release from GitHub (iranacc/DatabaseFinder) and compares it with its own version. When a newer one exists, an "Update" link shows in the header and a "Check for updates" item is added to the tray menu. Clicking it downloads the update with a live percentage, verifies the file's SHA-256 against the release `SHA256SUMS.txt`, then swaps the executable and restarts automatically. The matching variant is downloaded on its own (Light → Light, standalone → standalone).
- **Honest title bar:** the window title now shows the actual version of the running executable instead of a fixed number.

## What's new in v1.8.5

- **Fixed backup destination bug:** the backup plan was built only once when the window opened; changing the destination silently sent the files to the default folder (`C:\DatabaseFinder\Backup`) while the log and manifest pointed at the new (empty) folder. The plan is now rebuilt whenever the destination changes (checked databases are preserved).
- **SQL Server backup options:** compression (COMPRESSION), verify on completion (VERIFYONLY) and checksum before writing to media (CHECKSUM) — SSMS-style.
- **Rich, prettier manifest:** `manifest.txt` gained a framed layout headed by a 181 "stamp", an execution summary, per-database cards with options/timing/status/compression ratio, a file-hash table and a `sha256sum -c manifest.txt`-compatible block; `manifest.md` (GitHub-ready) and `manifest.json` (structured) are also produced, with live server versions and the manifest's own SHA-256.
- **Article 181 stamp:** a large 181 logo (Article 181 of the Iranian Direct Tax Law) with the caption `TAX 181 ARTICLE . MSAM Group` heads every manifest, marking it as machine-readable extracted data.
- **Database list:** double-click the "# Databases" column to open the database names of that service.

## What's new in v1.8.4

- Network range scanning: scan an IP range, a local subnet (advertised interfaces listed in a dropdown) or a CIDR block with bounded parallelism and a per-host timeout; every finding is resolved with a SQL Browser (SSRP, UDP 1434) probe.
- "Open ports only" network filter (open ports by default; a check box shows every host).
- SQL Server discovery without SSMS via the SSRP probe.
- Windows Authentication in Run Query (Integrated Security), with automatic machine-name resolution for IP hosts.
- A "# Databases" column on the main page showing the real database count of each online service (via saved profile or Windows auth).
- The Close (X) button now really exits; minimizing still goes to the tray. "Run Query" works on any selected scan result row.

## What's new in v1.8.3

- Fixed single-file packaging: all native libraries (including the SQL Server connection library) are now bundled inside the executable, so no separate files are needed next to the exe and SQL Server file discovery works without errors. The packaging options are set in the project file for future releases.

## What's new in v1.8.2

- Fixed the v1.8.1 regression where detection silently stalled: a banner read could block forever on servers that don't send an unsolicited greeting (SQL Server), leaving detection permanently "busy" so every later scan did nothing. The version probe now has a hard 5-second cap and re-detection always restarts.
- Offline scan now recognizes backups of Iranian accounting packages: Mahak (name patterns `Backup-mahak…`, `Auto-mahak…`, `BeforeUpdate…`, `FullBackup-…`, `ver…`), Parsian (`PARSIAN.BACK` folder) and Holoo (`Holoo.Bak` folder). These folders were also added to the quick-scan roots.
- Disk matching priority is now folder path → file-name pattern → extension.

## What's new in v1.8.1

- Port detection performance: the port→PID map is now built with a single netstat run instead of one per port.
- Version probing moved off the UI thread with a 5-second connect timeout; the interface no longer freezes on slow networks and rows update as tests finish.
- Version results are matched to grid rows by index instead of name+port.
- Fewer MySQL false positives (only mysqld/mysql processes are detected).
- A technical error log for swallowed failures in settings, profiles, detection, the manifest and Shadow Copy (`%AppData%\DatabaseFinder\debug.log`).
- Added a solution file so all projects build in one command.

## What's new in v1.8

- Redesigned desktop interface with sidebar navigation, consistent colors, a selection toolbar and a path/details panel.
- Persian / English language switch in the main window. The language is saved, layout direction changes, and existing results and checked rows are preserved.
- Searchable format picker covering all 11 groups and 28 extensions. Expand a group to select individual extensions; filtering the list preserves selections.
- Responsive file search, copy, backup, settings, query, remote scan, profile and detail windows.
- Application messages and operational logs use embedded translation resources. File paths, database names, SQL queries and structured manifest keys are not translated.

## Features (v1.8)
- Detection via 3 methods: Windows services, processes, open ports
- **Scan mode selector** — choose between online (running), offline (hard drive), or both
- **Offline hard-disk scan** — finds database files that are NOT running (stopped services, deleted-from-service databases, and backups):
  - Format selection checkboxes: SQL Server (.mdf/.ldf/.ndf), SQL Server backups (.bak), MySQL InnoDB/MyISAM, SQLite, Access, FoxPro/dBase (common in Iranian accounting software), Firebird, MongoDB WiredTiger, Redis, generic backup archives
  - Quick scan (common folders) or full scan (all fixed drives), minimum file-size filter, per-root selection, custom folder, cancel with progress
  - **Select-all / uncheck-all buttons** for both the locations list and the formats list
  - **Content (magic-byte) validation** — files are validated by their content in addition to the extension, so files like `Acrobat.dll.bak` or `Photoshop.exe.bak` are no longer reported as SQL backups
  - Results list with format, **file format/extension**, guessed DB name, size, last-modified, and backup flag
  - **Select/deselect results** — clicking anywhere on a row toggles its checkbox; Select-all / none buttons above the results grid are provided
  - **Right-click context menu on results** — open file's folder, open file, copy full path, copy file to a chosen folder
- Minimum file size setting in MB (default 3 MB)
- Selecting "Offline only" automatically opens the hard-disk scan page
- Multi-size application icon in the taskbar and title bar
  - Offline results can be copied directly or merged into the main grid for the copy feature
- Full details for each database (version, address, service/process status)
- Real connection & SQL query execution (MySQL, PostgreSQL, SQL Server, SQLite, Redis)
- Remote host scanning (detect databases running on other machines over the network)
- Copy database data files (online + offline) into separate per-database folders with original filenames
  - SQL Server: file list from `sys.databases` + `master_files` (system/tempdb excluded)
  - MySQL/MariaDB: per-database folders from the server data directory
  - PostgreSQL: per-database directories from `base/` (by OID)
  - Redis: `dump.rdb` / `appendonly` files from `CONFIG GET dir`
  - MongoDB: data directory discovery (`\data\db`)
  - Manual folder selection fallback
  - **Handling for locked in-use files** — selectable in the copy dialog:
    - **VSS Shadow Copy without stopping the service (recommended):** locked files are copied from a volume snapshot while the DB service stays running (requires Administrator)
    - **Auto stop/start of the DB service:** the relevant services (sqlservr/mysqld/postgres) are stopped for the copy and guaranteed to restart afterwards
    - **Report-only:** locked files are reported as errors (previous behavior)
- **Logical backup of running databases** — creates native backups into a separate per-database folder:
  - SQL Server: `BACKUP DATABASE ... TO DISK` (with compression)
  - MySQL/MariaDB: `mysqldump` with automatic binary discovery
  - PostgreSQL: `pg_dump` (custom format) with automatic binary discovery
  - Redis: runs `SAVE` and copies `dump.rdb`
  - MongoDB: `mongodump` (when MongoDB Database Tools are installed)
- **SHA-256 manifest (for the case file)** — after any file copy or backup, three files are auto-generated in the destination folder: `manifest.txt` (a framed, printable report with a 181 stamp, execution summary, per-database cards, a `sha256sum -c`-compatible block and the manifest's own hash), `manifest.md` (GitHub-ready) and `manifest.json` (structured)
- Connection test with version detection
- Customizable settings (custom ports, auto refresh)
- Saved database profiles
- System tray icon with quick menu

## Installation & Running
### Pre-built executable
Download either build from the [Releases](https://github.com/iranacc/DatabaseFinder/releases) page:

- **DatabaseFinder-Light.exe:** requires .NET 8 Desktop Runtime for Windows x64.
- **DatabaseFinder.exe:** standalone Windows x64 build with the .NET runtime included; no separate .NET installation required.

### Build from source
```bash
dotnet build DatabaseFinder/DatabaseFinder.csproj
```

### Publish standalone executable
```bash
dotnet publish DatabaseFinder/DatabaseFinder.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o Release
```

## Versioning
Git tags are used for releases (e.g. `v1.8.0`).

## Validation

Run `dotnet run --project tools/UiChecks -c Release -- ui-checks` on Windows with the .NET 8 SDK. The checks cover translation completeness, placeholders, both UI languages, language switching with preserved selections, format filtering, individual extension selection, and disk scanning against synthetic files. They also render forms for visual review. They do not run real database backups or restore operations.

## Requirements
- Windows 10/11
- .NET 8 (only for the Light version; the standalone version includes everything)
