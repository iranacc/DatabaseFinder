# Database Finder

ابزار ویندوزی برای پیدا کردن دیتابیس‌های در حال اجرا روی سیستم.

> **English:** [Read English README](../README.md)

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

## قابلیت‌ها (نسخه ۱.۳)
- شناسایی از ۳ روش: سرویس ویندوز، پروسس، پورت‌های باز
- جزئیات کامل هر دیتابیس (نسخه، آدرس، وضعیت سرویس/پروسس)
- اتصال واقعی و اجرای کوئری (MySQL، PostgreSQL، SQL Server، SQLite، Redis)
- اسکن سیستم‌های راه دور (شناسایی دیتابیس‌های در حال اجرا روی سایر سیستم‌های شبکه)
- **کپی فایل‌های دیتابیس** — دیتابیس‌های یافت‌شده را با تیک انتخاب کنید، پوشه مقصد را تعیین کنید و فایل‌های فیزیکی هر دیتابیس در یک پوشه جداگانه با نام اصلی فایل‌ها کپی می‌شوند (مثلاً فایل‌های `.mdf`/`.ldf` سرویس‌دهنده SQL)
  - SQL Server: لیست فایل‌ها از `sys.databases` و `master_files` (بدون دیتابیس‌های سیستمی/tempdb)
  - MySQL/MariaDB: پوشه جداگانه هر دیتابیس از دایرکتوری داده سرور
  - PostgreSQL: دایرکتوری‌های هر دیتابیس از `base/` (بر اساس OID)
  - Redis: فایل‌های `dump.rdb` / `appendonly` از `CONFIG GET dir`
  - MongoDB: شناسایی خودکار دایرکتوری داده (`\data\db`)
  - انتخاب دستی پوشه به‌عنوان جایگزین برای دیتابیس‌هایی که فایل‌شان خودکار شناسایی نمی‌شود
  - فایل‌های قفل‌شده با راهنمایی «توقف سرویس دیتابیس یا اجرا به‌عنوان Administrator» گزارش می‌شوند
- تست اتصال مستقیم به دیتابیس
- تنظیمات قابل شخصی‌سازی (پورت‌های سفارشی، به‌روزرسانی خودکار)
- پروفایل دیتابیس‌های ذخیره‌شده
- آیکون در سینی سیستم با منوی سریع

## نحوه اجرا
### نسخه آماده (بدون نیاز به نصب .NET)
از صفحه [Releases](https://github.com/iranacc/DatabaseFinder/releases) فایل `DatabaseFinder.exe` را دانلود و اجرا کنید.

### بیلد از سورس
```bash
dotnet build DatabaseFinder/DatabaseFinder.csproj
```

## ساختن نسخه اجرایی مستقل
```bash
dotnet publish DatabaseFinder/DatabaseFinder.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o Release
```

## ورژن‌بندی
تگ‌ها برای نسخه‌های مختلف استفاده می‌شوند (مثال: `v1.3.0`).