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

## قابلیت‌ها (نسخه ۱.۲)
- شناسایی از ۳ روش: سرویس ویندوز، پروسس، پورت‌های باز
- جزئیات کامل هر دیتابیس (نسخه، آدرس، وضعیت سرویس/پروسس)
- اتصال واقعی و اجرای کوئری (MySQL، PostgreSQL، SQL Server، SQLite، Redis)
- اسکن سیستم‌های راه دور (شناسایی دیتابیس‌های در حال اجرا روی سایر سیستم‌های شبکه)
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
تگ‌ها برای نسخه‌های مختلف استفاده می‌شوند (مثال: `v1.2.0`).