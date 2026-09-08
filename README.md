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
- **SHA-256 manifest (for the case file)** — after any file copy or backup, `manifest.txt` (hash + path, printable) and `manifest.json` (structured) are auto-generated in the destination folder so every captured file is verifiable
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
