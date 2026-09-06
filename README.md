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

## Features (v1.2)
- Detection via 3 methods: Windows services, processes, open ports
- Full details for each database (version, address, service/process status)
- Real connection & SQL query execution (MySQL, PostgreSQL, SQL Server, SQLite, Redis)
- Remote host scanning (detect databases running on other machines over the network)
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
Git tags are used for releases (e.g. `v1.2.0`).

## Requirements
- Windows 10/11
- .NET 8 (only for the Light version; the standalone version includes everything)