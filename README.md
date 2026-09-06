# Database Finder

ابزار ویندوزی برای پیدا کردن دیتابیس‌های در حال اجرا روی سیستم.

قابلیت تشخیص انواع دیتابیس زیر را دارد:
- SQL Server
- MySQL
- MariaDB
- PostgreSQL
- Oracle
- MongoDB
- Redis
- Elasticsearch
- CouchDB

## روش‌های تشخیص
۱. سرویس‌های در حال اجرا (از طریق WMI)
۲. پروسس‌های سیستم
۳. پورت‌های باز شده

## نحوه اجرا
### نسخه آماده (بدون نیاز به نصب .NET)
فایل `DatabaseFinder.exe` از پوشه `Release` اجرا کنید.

### بیلد از سورس
```bash
dotnet build DatabaseFinder/DatabaseFinder.csproj
```

## ساختن نسخه اجرایی مستقل
```bash
dotnet publish DatabaseFinder/DatabaseFinder.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o Release
```

## ورژن‌بندی
تگ‌ها برای نسخه‌های مختلف استفاده می‌شوند (مثال: `v1.0.0`).