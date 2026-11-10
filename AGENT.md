# AGENT.md - راهنمای جامع معماری و دانش فنی پروژه Database Finder

> **توجه حیاتی و دستورالعمل اجباری برای تمامی عامل‌های هوش مصنوعی (AI Agents) و توسعه‌دهندگان:**
> این سند حاوی تمام آموخته‌ها، معماری فنی، تصمیمات طراحی و باگ‌های شناسایی‌شده در پروژه است.
> **قانون الزام‌آور:** هر هوش مصنوعی یا توسعه‌دهنده‌ای که تغییری در این مخزن ایجاد می‌کند، ویژگی جدیدی می‌افزاید، باگی را رفع می‌کند یا نکته فنی مهمی را کشف می‌کند، **موظف است بلافاصله این فایل را به‌روزرسانی کرده** و گزارش تغییرات خود را در انتهای این فایل ثبت نماید تا از اتلاف وقت عامل‌های بعدی برای کشف مجدد پروژه جلوگیری شود.

---

## ۱. هویت و دامنه کاربرد پروژه (Domain & Purpose)
- **نام نرم‌افزار:** Database Finder
- **هدف اصلی:** کشف، شناسایی، کپی‌برداری فیزیکی، تهیه پشتیبان (Backup) و مستندسازی قانونی پایگاه‌های داده در سیستم‌های ویندوزی در مأموریت‌های میدانی و بازرسی‌های مالیاتی (به‌ویژه **ماده ۱۸۱ قانون مالیات‌های مستقیم**).
- **مخزن رسمی گیت‌هاب:** `https://github.com/iranacc/DatabaseFinder`
- **حساب سازنده/نگهدارنده:** `iranacc`
- **ویژگی مأموریتی:** ابزار برای اجرا روی سیستم مودی یا سیستم‌های آزمایشگاهی بازرسی طراحی شده و باید کمترین ردپا (Artifact) را بر جای بگذارد (پشتیبانی از حالت Portable با قرار دادن فایل `portable.txt` کنار برنامه).

---

## ۲. پشته فناوری (Technology Stack)
- **زبان و فریم‌ورک:** C# 12 روی `.NET 8.0-windows` (Windows Forms)
- **نوع اجرا:** مستقل (Self-Contained Single-File) بدون نیاز به نصب پیش‌نیاز در نسخه اصلی، و نسخه سبک (Light) متکی به دات‌نت ۸ محلی.
- **کتابخانه‌های کلیدی استفاده شده:**
  - `Microsoft.Data.SqlClient` (v5.2.1): اتصال مستقیم به SQL Server
  - `MySqlConnector` (v2.3.7): اتصال به MySQL و MariaDB
  - `Npgsql` (v8.0.4): اتصال به PostgreSQL
  - `Microsoft.Data.Sqlite` (v8.0.8): کار با فایل‌های دیتابیس SQLite
  - `StackExchange.Redis` (v2.8.16): اتصال و کوئری ردیس
  - `System.Management` (v8.0.0): کوئری‌های WMI برای سرویس‌ها و VSS
  - `System.ServiceProcess.ServiceController` (v8.0.0): مدیریت سرویس‌های ویندوز

---

## ۳. ساختار پروژه‌ها در مخزن (Solution Structure)
```text
D:\181 Project\Database Finder\
├── DatabaseFinder.sln                  # فایل راه‌حل اصلی
├── DatabaseFinder\                     # پروژه اصلی ویندوز فرمز
│   ├── Program.cs                      # نقطه ورود و راه‌اندازی تم/زبان
│   ├── Form1.cs / .Designer / .Layout  # فرم اصلی برنامه (داشبورد کشف)
│   ├── DatabaseDetector.cs             # موتور کشف آنلاین (سرویس، پروسس، پورت)
│   ├── DiskScanner.cs                  # موتور اسکن عمیق هارددیسک (آفلاین)
│   ├── FileSignatures.cs               # بررسی امضای بایتی فایل‌ها (Magic Bytes)
│   ├── DatabaseFileLocator.cs          # مکان‌یابی فایل‌های دیتابیس در دیسک
│   ├── VolumeShadowCopy.cs             # مدیریت VSS جهت کپی فایل‌های قفل‌شده
│   ├── DatabaseBackuper.cs             # بکاپ‌گیر نیتیو و ابزارهای خط فرمان
│   ├── DatabaseBackupForm.cs           # فرم بکاپ‌گیری
│   ├── DatabaseCopyForm.cs             # فرم کپی فیزیکی فایل‌ها
│   ├── SqlServerNoAuth.cs              # متدهای اضطراری sysadmin و اسکریپت لب
│   ├── DatabaseManifest.cs             # تولیدکننده مانیفست رسمی و چکسام SHA-256
│   ├── RemoteScanner.cs / SqlBrowser.cs# اسکن رنج‌های شبکه و پروتکل SSRP
│   ├── QueryRunnerForm.cs              # پنجره تست و اجرای کوئری SQL/Redis
│   ├── L.cs / Strings.*.json           # سیستم چندزبانه (فارسی و انگلیسی)
│   ├── UpdateChecker.cs                # بررسی، دانلود و اعتبارسنجی آپدیت گیت‌هاب
│   └── AppPaths.cs / AppSettings.cs    # مسیرهای پرتابل و تنظیمات کاربر
├── tools\
│   ├── UiChecks\                       # پروژه تست یکپارچه، ترجمه‌ها و رندر فرم‌ها
│   └── Localize\                       # ابزار استخراج و نگهداری کلیدهای زبان
├── Release\                            # خروجی بیلد آماده نسخه‌های پایدار
└── docs\                               # راهنمای تصویری و کاربری
```

---

## ۴. تشریح سازوکارهای کلیدی (Key Mechanisms)

### ۴.۱. کشف آنلاین (`DatabaseDetector.cs`)
سیستم از سه لایه همپوشان برای شناسایی پایگاه داده استفاده می‌کند:
1. **سرویس‌های در حال اجرا (`Win32_Service`)**: سرویس‌های منطبق بر نام‌های دیتابیسی.
2. **پروسس‌های فعال (`Process.GetProcesses()`)**: شناسایی با نام پروسس‌ها مانند `sqlservr`, `mysqld`, `postgres`, `mongod`, `redis-server`.
3. **پورت‌های شنیداری (`GetPortOwners`)**: با یک بار اجرای `netstat -ano` مپ پورت به PID را استخراج کرده و سرویس‌های در حال گوش دادن را شناسایی می‌کند.
4. **سرویس‌های متوقف شده (`DetectStoppedSqlInstances`)**: شناسایی SQL Server‌های خاموش از طریق رجیستری (`SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL`) و سرویس‌های Stopped ویندوز.

### ۴.۲. کپی فایل‌های قفل‌شده با VSS (`VolumeShadowCopy.cs`)
وقتی دیتابیس در حال اجرا است، فایل‌های `.mdf` و `.ldf` توسط ویندوز قفل انحصاری (Exclusive Lock) هستند.
- ابزار یک اسنپ‌شات VSS از درایو مربوطه از طریق WMI کلاس `Win32_ShadowCopy` می‌سازد.
- مسیر فایل را به مسیر ریشه شدو مپ می‌کند (`\\?\GLOBALROOT\Device\HarddiskVolumeShadowCopyX\...`).
- فایل‌ها را مستقیماً از شدو کپی می‌کند بدون نیاز به متوقف کردن سرویس‌های عملیاتی مودی.
- در پایان در بلوک `finally` اسنپ‌شات شدو را پاکسازی می‌کند.

### ۴.۳. اسکن عمیق آفلاین هارددیسک (`DiskScanner.cs` و `FileSignatures.cs`)
- درایوهای Fixed را برای انواع دیتابیس‌ها و نرم‌افزارهای حسابداری اسکن می‌کند.
- پوشه‌های خاص حسابداری ایرانی مانند `parsian.back`, `holoo.bak`, `mahak` را به عنوان ریشه‌های سریع هدف می‌گیرد.
- از اعتبارسنجی بایتی (Magic Bytes) استفاده می‌کند تا فایل‌های غیرمرتبط با پسوند مشابه (مثل `.bak` برنامه‌های دیگر) وارد لیست نشوند.

### ۴.۴. تولید مانیفست و صورت‌جلسه بازرسی (`DatabaseManifest.cs`)
پس از هر عملیات کپی یا بکاپ، ۳ فایل در مقصد ایجاد می‌شود:
1. `manifest.txt`: گزارش قاب‌بندی‌شده همراه با استامپ ASCII ۱۸۱، جدول هش SHA-256 و بلوک سازگار با دستور لینوکسی `sha256sum -c`.
2. `manifest.md`: نسخه مارک‌داون مناسب نمایش در سامانه‌ها.
3. `manifest.json`: خروجی ساختاریافته ماشین‌خوان حاوی اطلاعات سرور، زمان‌ها، بایت‌های دریافتی و خطاها.

---

## ۵. دستورات ساخت و تست (Build & Verification Commands)

### کامپایل کلی پروژه:
```powershell
dotnet build "d:\181 Project\Database Finder\DatabaseFinder.sln"
```

### اجرای تست‌های خودکار (UI & Functional Checks):
**الزامی قبل از هر ریلیز یا کامیت:**
```powershell
dotnet run --project "d:\181 Project\Database Finder\tools\UiChecks\UiChecks.csproj"
```
*خروجی موفقیت باید عبارت `PASS: 683 checks` (یا بیشتر) باشد.*

### انتشار نسخه نهایی تک‌فایلی (Standalone):
```powershell
dotnet publish "d:\181 Project\Database Finder\DatabaseFinder\DatabaseFinder.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o Release
```

### انتشار نسخه سبک (Light):
```powershell
dotnet publish "d:\181 Project\Database Finder\DatabaseFinder\DatabaseFinder.csproj" -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o Release-Light
```

---

## ۶. فهرست آسیب‌پذیری‌ها و باگ‌های شناسایی‌شده (Known Issues & Vulnerabilities)

هنگام کار روی پروژه، به این اشکالات توجه ویژه داشته باشید و در اولویت اصلاح قرار دهید:

| اولویت | عنوان اشکال | محل در کد | شرح فنی و پیامد |
| :---: | :--- | :--- | :--- |
| **P0** | **امضای اشتباه فایل‌های بکاپ SQL Server** | `FileSignatures.cs:16` | شرط `if (ext == ".bak") return StartsWithBytes(path, Encoding.ASCII.GetBytes("MSSQLBAK"));` کاملاً اشتباه است. فرمت MTF مایکروسافت هرگز با `MSSQLBAK` شروع نمی‌شود؛ بلکه بایت‌های اول آن امضای `TAPE` (`0x54, 0x41, 0x50, 0x45`) است. این باعث رد شدن تمام فایل‌های بکاپ واقعی در اسکن دیسک می‌شود! |
| **P0** | **بن‌بست در ریدایرکت پروسس‌ها (Process Deadlock)** | `SqlServerNoAuth.cs:543-545` | اجرای همگام `stdout.ReadToEnd()` و سپس `stderr.ReadToEnd()` روی پروسسی که هر دو خروجی آن ریدایرکت شده‌اند، در صورت پر شدن بافر پایپ ویندوز منجر به Deadlock قطعی می‌شود. |
| **P1** | **فریز شدن رابط کاربری حین Detect** | `Form1.cs:641` | فراخوانی سنکرون `_detector.Detect()` در ترد UI پنجره برنامه را به مدت ۲ تا ۵ ثانیه فریز می‌کند. باید در `Task.Run` اجرا شود. |
| **P1** | **حلقه نامحدود Junction Points در اسکن دیسک** | `DiskScanner.cs:310-322` | متد `WalkDir` ویژگی `FileAttributes.ReparsePoint` را نادیده می‌گیرد و پوشه‌هایی مثل `Application Data` باعث چرخه تکراری و خطای `PathTooLongException` می‌شوند. |
| **P1** | **ناهماهنگی ایندکس گرید در بروزرسانی ناهمگام** | `Form1.cs:671, 697` | بروزرسانی شمارش دیتابیس‌ها و نسخه با استفاده از `results.IndexOf(db)` روی ردیف‌های `dgvDatabases.Rows[idx]`. با سورت کردن ستون توسط کاربر، اطلاعات در ردیف دیتابیس اشتباهی درج می‌شود. باید با تطبیق `row.Tag == db` بروز شود. |
| **P2** | **ذخیره کلمه عبور به صورت متن خام** | `AppSettings.cs:59-96` | پسوردهای اتصالات دیتابیس در `profiles.json` بدون رمزنگاری ذخیره می‌شوند. باید از Windows DPAPI (`ProtectedData.Protect`) استفاده شود. |
| **P2** | **ریسک کمبود حافظه در اجرای کوئری** | `QueryRunnerForm.cs:485-488` | متد `dt.Load(reader)` روی جداول چند میلیونی رکورد بدون سقف لود می‌شود و برنامه با خطای `OutOfMemoryException` کرش می‌کند. |
| **P2** | **عدم امکان لغو (Cancellation) در کپی و بکاپ** | `DatabaseFileLocator.cs` / `DatabaseBackuper.cs` | نبود `CancellationToken` و دکمه Cancel، کاربر را مجبور به بستن اجباری نرم‌افزار می‌کند. |
| **P2** | **نسخه هاردکد شده در مانیفست** | `DatabaseManifest.cs:74` | متغیر `ToolVersion = "1.8.15"` ثابت است و با آپدیت نسخه اسمبلی در `.csproj` همگام نمی‌شود. |

---

## ۷. دفترچه ثبت رویدادها و تغییرات توسط عامل‌ها (Agent Audit & Modification Log)

> **دستورالعمل:** هرگونه تغییر یا بررسی باید با فرمت زیر به این بخش اضافه شود:
> ```markdown
> ### [تاریخ و ساعت] - [شناسه عامل یا نام توسعه‌دهنده]
> - **اقدام انجام‌شده:** شرح تغییر
> - **فایل‌های تحت تأثیر:** لیست فایل‌ها
> - **وضعیت تست:** نتیجه بیلد و UiChecks
> - **نکات تکمیلی برای عامل‌های بعدی:** ...
> ```

### 2026-10-14 01:46 - Antigravity Agent
- **اقدام انجام‌شده:** ایجاد اولیه سند `AGENT.md` و مستندسازی تمامی معماری، وابستگی‌ها، دستورات ساخت، جزئیات پروژه‌ها و باگ‌های استخراج‌شده در بازبینی فنی ارشد.
- **وضعیت سلامت پروژه:** بیلد `0 Error, 0 Warning` و پاس شدن کامل تمامی ۶۸۳ تست `UiChecks`.
- **نکات برای عامل‌های بعدی:** پیش از شروع به کار روی اصلاح باگ‌های P0 و P1، این فایل را به عنوان مرجع اصلی حقیقت در نظر بگیرید و پس از اصلاح، توضیحات و وضعیت جدید را در این لاگ ثبت کنید.

### 2026-10-14 01:56 - Antigravity Agent
- **اقدام انجام‌شده:** برطرف‌سازی کامل تمامی باگ‌های اولویت‌دار (P0 تا P2) به شرح زیر:
  1. **P0 (FileSignatures.cs):** اصلاح اعتبارسنجی بکاپ‌های SQL Server از رشته ناموجود `MSSQLBAK` به استاندارد رسمی مایکروسافت (امضای MTF TAPE در آفست ۰ و ۵۱۲) و افزودن تست رگرسیون.
  2. **P0 (SqlServerNoAuth.cs):** رفع Deadlock فرآیند در ریدایرکت همزمان Stdout و Stderr با خواندن ناهمگام (`ReadToEndAsync`) و تایم‌اوت هوشمند.
  3. **P1 (Form1.cs):** رفع کامل فریز شدن رابط کاربری با اجرای `_detector.Detect()` در ترد پس‌زمینه (`await Task.Run`).
  4. **P1 (DiskScanner.cs):** جلوگیری از ورود به حلقه‌های بی‌پایان Junction Points با بررسی صریح `FileAttributes.ReparsePoint` و محدودسازی عمق بازگشت (Depth <= 24).
  5. **P1 (Form1.cs):** اصلاح باگ ناهماهنگی ایندکس گرید در هنگام بروزرسانی شمارش دیتابیس‌ها و نسخه؛ جایگزینی تطبیق اندیس با تطبیق رفرنسی `row.Tag == db`.
  6. **P2 (DatabaseManifest.cs):** داینامیک‌سازی مقدار `ToolVersion` بر اساس نسخه جاری اسمبلی اجرایی به جای رشته هاردکد شده.
  7. **P2 (QueryRunnerForm.cs):** اعمال سقف حداکثر ۵,۰۰۰ سطر در بارگذاری جدول برای جلوگیری از کرش `OutOfMemoryException`.
  8. **P2 (AppSettings.cs):** رمزنگاری امن پسوردهای ذخیره‌شده در `profiles.json` با پروتکل ویندوزی DPAPI (`ProtectedData.Protect/Unprotect`).
  9. **P2 (AppLog.cs):** پیاده‌سازی مکانیزم چرخش لاگ‌ها (Rollover) در سقف ۵ مگابایت به منظور جلوگیری از پر شدن دیسک.
  10. **P2 (DatabaseDetector.cs):** بهینه‌سازی کوئری WMI سرویس‌ها (`SELECT Name, DisplayName`) و آزادسازی صریح هندل‌های سیستم‌عامل (`Dispose` روی `ManagementObject` و `Process`).
- **فایل‌های تحت تأثیر:**
  - `DatabaseFinder/FileSignatures.cs`
  - `DatabaseFinder/SqlServerNoAuth.cs`
  - `DatabaseFinder/Form1.cs`
  - `DatabaseFinder/DiskScanner.cs`
  - `DatabaseFinder/DatabaseManifest.cs`
  - `DatabaseFinder/QueryRunnerForm.cs`
  - `DatabaseFinder/AppSettings.cs`
  - `DatabaseFinder/AppLog.cs`
  - `DatabaseFinder/DatabaseDetector.cs`
  - `tools/UiChecks/Program.cs`
- **وضعیت تست:**
  - بیلد کامپایل با موفقیت و بدون هیچ‌گونه خطا یا وارنینگ (`0 Warning, 0 Error`).
  - تمام **۶۸۶ تست خودکار `UiChecks`** با موفقیت پاس شدند (`PASS: 686 checks`).

### 2026-11-03 11:42 - Antigravity Agent
- **اقدام انجام‌شده:** اصلاح متن دکمه انتخاب مسیر فایل‌های مقصد (Browse) در زبان فارسی (`S031`). ترجمه نادرست «بگرد...» به عبارت استاندارد و گویای «انتخاب مسیر...» اصلاح شد تا از سردرگمی کاربر جلوگیری شود.
- **فایل‌های تحت تأثیر:**
  - `DatabaseFinder/Strings.fa.json`
  - `tools/Localize/fa-inventory.json`
- **وضعیت تست:**
  - کامپایل موفق (`0 Warning, 0 Error`).
  - تست‌های ۶۸۶ گانه `UiChecks` با موفقیت پاس شدند.

### 2026-11-03 12:25 - Antigravity Agent
- **اقدام انجام‌شده:** افزودن دکمه «جست‌وجوی پوشه انتخابی...» (`_btnSweepFolder`) در پنجره کپی فایل‌ها (`DatabaseCopyForm`) در کنار دکمه «جست‌وجوی عمیق درایوها...» (`_btnDeepSweep`). این قابلیت به کاربر اجازه می‌دهد به جای اسکن طولانی کل درایوهای ویندوز، تنها یک مسیر مشخص را انتخاب کند تا فایل‌های دیتابیس SQL Server در آن مسیر شناسایی و به ساختار درخت اضافه شوند.
- **فایل‌های تحت تأثیر:**
  - `DatabaseFinder/DatabaseCopyForm.cs`: افزودن دکمه `_btnSweepFolder`، هندلر `BtnSweepFolder_Click` و اتصال به `_planControls` و `OperationLayout`.
  - `DatabaseFinder/DatabaseFileLocator.cs`: افزودن متد `SweepSqlServerFolder`.
  - `DatabaseFinder/SqlServerNoAuth.cs`: افزودن متد `SweepPathForDataFiles` و محافظت در برابر ReparsePoint در پیمایش زیرپوشه‌ها.
  - `DatabaseFinder/Strings.fa.json` و `DatabaseFinder/Strings.en.json`: افزودن کلیدهای `S427` و `S428`.
- **وضعیت تست:**
  - کامپایل موفق و انتشار باینری‌های جدید در پوشه `Release`.
  - پاس شدن کامل تمامی ۶۸۹ تست خودکار در `UiChecks`.

### 2026-11-03 14:26 - Antigravity Agent
- **اقدام انجام‌شده:** مدرن‌سازی کامل رابط کاربری (UI/UX) — مسیر ۱: بازطراحی نیتیو بدون کتابخانه خارجی.
- **تغییرات کلیدی:**
  1. **فونت وزیرمتن (Vazirmatn):** دانلود و Embed فونت فارسی `Vazirmatn-Regular.ttf` و `Vazirmatn-Bold.ttf` به عنوان Embedded Resource. ایجاد `UiFont.cs` با `PrivateFontCollection` و سیستم کش هوشمند. در زبان فارسی فونت Vazirmatn بارگذاری می‌شود و در انگلیسی `Segoe UI Variable Text` با Fallback به `Segoe UI`.
  2. **کارت‌های گردگوشه (ModernCard):** ایجاد `ModernCard.cs` — پنل سفارشی با `GraphicsPath` و گوشه‌های گرد (CornerRadius=8)، حاشیه ظریف و DoubleBuffered.
  3. **پالت رنگی مدرن (Slate/Indigo):** بازنویسی کامل `UiTheme` با رنگ‌های هماهنگ Tailwind CSS (Slate-50→900, Blue-500→700) به جای رنگ‌های Material قدیمی. اضافه شدن `AccentHover`, `SurfaceAlt`, `Success`, `Warning`, `Danger`.
  4. **ترمینال لاگ تیره:** بخش لاگ در فرم‌های عملیاتی با پس‌زمینه Slate-900 تیره (`#182030`) و متن روشن، شامل دکمه «کپی لاگ» شناور.
  5. **بازنویسی OperationLayout.cs:** چیدمان کاملاً جدید: کارت بالایی (عنوان + مسیر)، بخش میانی دوستونه (درخت + پنل ابزار)، کارت لاگ ترمینالی و کارت پایینی اکشن‌ها.
  6. **بروزرسانی Form1.Layout.cs:** کارت‌های آماری KPI با `ModernCard` به جای `Panel` ساده. جایگزینی تمامی `Color.White` با `UiTheme.Surface`.
  7. **جایگزینی فونت در ۱۳ فایل:** تمامی `new Font("Segoe UI", ...)` به `UiFont.Make(...)` تبدیل شد: `CaseInfoForm`, `DatabaseBackupForm`, `DatabaseCopyForm`, `DatabaseDetailForm`, `DatabaseListForm`, `DiskScanForm`, `DiskScanForm.Layout`, `FormatPickerForm`, `FormLayout`, `ProfilesForm`, `QueryRunnerForm`, `RemoteScannerForm`, `SettingsForm`.
- **فایل‌های جدید:**
  - `DatabaseFinder/UiFont.cs`
  - `DatabaseFinder/ModernCard.cs`
  - `DatabaseFinder/Resources/Fonts/Vazirmatn-Regular.ttf`
  - `DatabaseFinder/Resources/Fonts/Vazirmatn-Bold.ttf`
- **فایل‌های تحت تأثیر:** ۱۷ فایل (21 files changed, 782 insertions, 123 deletions)
- **نسخه:** بامپ شده به `1.9.0`
- **وضعیت تست:** تمامی **۶۹۱ تست خودکار `UiChecks`** با موفقیت پاس شدند.
- **کامیت:** `910acab` — پوش شده به `origin/master`.

### 2026-11-10 02:29 - Antigravity Agent
- **اقدام انجام‌شده:** پیاده‌سازی نمایش پیشرفت لحظه‌ای (درصد و بایت) برای کپی فایل‌های دیتابیس (به ویژه فایل‌های چند گیگابایتی) در لاگ و نوار وضعیت.
- **تغییرات کلیدی:**
  1. **موتور کپی استریمی (`DatabaseFileLocator.CopyFileWithProgress`):** جایگزینی فراخوانی بلوکه‌کننده `File.Copy` با استریم ناهمگام مجهز به بافر ۱ مگابایتی برای حداکثر سرعت I/O و کال‌بک هوشمند پیشرفت (محدود به ۵ گزارش در ثانیه برای جلوگیری از بار پردازشی ترد UI). حفظ متادیتای LastWriteTimeUtc فایل‌ها.
  2. **پشتیبانی VSS و پوشه دستی:** اتصال `CopyFileWithProgress` به متد `TryCopyViaShadow` و `CopyDirectory` جهت پوشش کامل کپی فایل‌های قفل‌شده و کپی دستی.
  3. **بروزرسانی زنده لاگ بدون اسپم سطرها (`DatabaseCopyForm.ReportFileProgress`):** مدیریت سطر فعال با متغیر `_activeProgressLineStart` تا درصد کپی و حجم منتقل‌شده در همان سطر به صورت زنده و نرم جایگزین شود و پس از اتمام با پیام نهایی تیک‌خورده (`✓`) تثبیت گردد.
  4. **نمایش مرحله اعتبارسنجی هش SHA-256:** اطلاع‌رسانی به کاربر حین محاسبه هش فایل‌های بزرگ تا ابهامی از بابت فریز بودن برنامه وجود نداشته باشد.
  5. **کلیدهای محلی‌سازی جدید:** اضافه شدن `S429` (در حال کپی) و `S430` (اعتبارسنجی هش SHA-256) به هر دو زبان فارسی و انگلیسی با حفظ تقارن پارامترها.
  6. **تست خودکار:** اضافه شدن تست استریم پیشرفت به مجموعه آزمون‌های `UiChecks` (تعداد تست‌ها به ۶۹۴ افزایش یافت).
- **فایل‌های تحت تأثیر:**
  - `DatabaseFinder/DatabaseFileLocator.cs`
  - `DatabaseFinder/DatabaseCopyForm.cs`
  - `DatabaseFinder/Strings.fa.json`
  - `DatabaseFinder/Strings.en.json`
  - `DatabaseFinder/DatabaseFinder.csproj` (نسخه 1.9.1)
  - `tools/UiChecks/Program.cs`
  - `README.md` و `README.fa.md`
  - `SHA256SUMS.txt`
  - `docs/releases/v1.9.1.md`
- **وضعیت تست:** تمام **۶۹۴ تست خودکار `UiChecks`** با موفقیت ۱۰۰٪ پاس شدند (`PASS: 694 checks`).
- **نسخه:** `1.9.1`
