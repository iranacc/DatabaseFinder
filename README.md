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

## Features (v1.4)
- Detection via 3 methods: Windows services, processes, open ports
- **Scan mode selector** — choose between online (running), offline (hard drive), or both
- **Offline hard-disk scan** — finds database files that are NOT running (stopped services, deleted-from-service databases, and backups):
  - Format selection checkboxes: SQL Server (.mdf/.ldf/.ndf), SQL Server backups (.bak), MySQL InnoDB/MyISAM, SQLite, Access, FoxPro/dBase (common in Iranian accounting software), Firebird, MongoDB WiredTiger, Redis, generic backup archives
  - Quick scan (common folders) or full scan (all fixed drives), minimum file-size filter, per-root selection, custom folder, cancel with progress
  - Results list with format, guessed DB name, size, last-modified, and backup flag
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
  - Locked in-use files are reported with a hint to stop the DB service or run as Administrator
- Connection test with version detection
- Customizable settings (custom ports, auto refresh)
- Saved database profiles
- System tray icon with quick menu

## Installation & Running
### Pre-built executable (no .NET required)
Download `DatabaseFinder.exe` from the [Releases](https://github.com/iranacc/DatabaseFinder/releases) page.

### Build from source
```bash
dotnet build DatabaseFinder/DatabaseFinder.csproj
```

### Publish standalone executable
```bash
dotnet publish DatabaseFinder/DatabaseFinder.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o Release
```

## Versioning
Git tags are used for releases (e.g. `v1.4.0`).

## Requirements
- Windows 10/11
- .NET 8 (only for the Light version; the standalone version includes everything)