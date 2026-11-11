using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace DatabaseFinder
{
    /// <summary>
    /// موتور تولید صورت‌جلسه رسمی و گزارش‌های حقوقی بازرسی (موضوع ماده ۱۸۱ قانون مالیات‌های مستقیم).
    /// تولید اسناد با فرمت رسمی HTML (راست‌به‌چپ) و صدور مستقیم نسخه استاندارد PDF چاپی (A4).
    /// </summary>
    public static class ReportGenerator
    {
        public static string? LastGeneratedPdfPath { get; private set; }
        public static string? LastGeneratedHtmlPath { get; private set; }

        /// <summary>
        /// تولید صورت‌جلسه رسمی بازرسی از روی manifest.json در قالب‌های استاندارد HTML و PDF.
        /// در صورت امکان، فایل PDF تولید و مسیر آن بازگردانده می‌شود؛ در غیر این‌صورت مسیر فایل HTML برگشت داده خواهد شد.
        /// </summary>
        public static string GenerateFromManifest(string manifestJsonPath)
        {
            using var stream = File.OpenRead(manifestJsonPath);
            using var doc = JsonDocument.Parse(stream);
            var r = doc.RootElement;

            string S(string name) => r.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString() ?? "" : "";

            var createdUtcStr = S("createdUtc");
            var createdUtc = DateTime.TryParse(createdUtcStr, out var dtu) ? dtu : DateTime.UtcNow;
            var createdLocal = createdUtc.ToLocalTime();
            var jalaliFormal = JalaliDateHelper.FormatFormal(createdLocal, usePersianDigits: true);
            var jalaliShort = JalaliDateHelper.FormatShort(createdLocal);
            var timeShort = createdLocal.ToString("HH:mm:ss");

            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html lang=\"fa\" dir=\"rtl\"><head><meta charset=\"utf-8\">");
            sb.Append("<title>صورت‌جلسه بازرسی و برداشت اطلاعات سامانه‌های رایانه‌ای (ماده ۱۸۱ ق.م.م)</title>");
            sb.Append("<style>");
            sb.Append(@"
                @page { size: A4 portrait; margin: 12mm 12mm 14mm 12mm; }
                * { box-sizing: border-box; }
                body {
                    font-family: 'Vazirmatn', Tahoma, Arial, sans-serif;
                    margin: 0;
                    padding: 24px;
                    color: #1e293b;
                    background-color: #ffffff;
                    font-size: 11.5px;
                    line-height: 1.6;
                    direction: rtl;
                }
                .gov-card {
                    border: 2px solid #0f172a;
                    padding: 16px 20px;
                    margin-bottom: 16px;
                    background: #ffffff;
                }
                .gov-header {
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    border-bottom: 2px solid #0f172a;
                    padding-bottom: 14px;
                    margin-bottom: 14px;
                }
                .header-side { width: 28%; font-size: 10.5px; line-height: 1.7; }
                .header-side-left { text-align: left; direction: ltr; }
                .header-center { width: 44%; text-align: center; }
                .emblem-svg { width: 52px; height: 52px; margin-bottom: 4px; display: inline-block; }
                .state-title { font-size: 13px; font-weight: bold; color: #0f172a; margin: 0; }
                .ministry-title { font-size: 11px; color: #334155; margin: 2px 0 0 0; }
                .tax-title { font-size: 12px; font-weight: bold; color: #0284c7; margin: 2px 0 0 0; }
                .doc-main-title { font-size: 14.5px; font-weight: bold; color: #0f172a; margin: 6px 0 2px 0; }
                .doc-sub-title { font-size: 10.5px; font-weight: bold; color: #64748b; margin: 0; }
                .badge-181 {
                    display: inline-block;
                    background: #0f172a;
                    color: #ffffff;
                    padding: 2px 10px;
                    border-radius: 4px;
                    font-weight: bold;
                    font-size: 10px;
                    margin-top: 4px;
                }
                .sec-title {
                    font-size: 12px;
                    font-weight: bold;
                    background: #f1f5f9;
                    border-right: 4px solid #0284c7;
                    padding: 4px 10px;
                    margin: 16px 0 8px 0;
                    color: #0f172a;
                }
                table.tbl-formal {
                    width: 100%;
                    border-collapse: collapse;
                    margin: 8px 0 14px 0;
                    font-size: 11px;
                }
                table.tbl-formal th, table.tbl-formal td {
                    border: 1px solid #cbd5e1;
                    padding: 6px 8px;
                    text-align: right;
                    vertical-align: middle;
                }
                table.tbl-formal th {
                    background: #f8fafc;
                    color: #1e293b;
                    font-weight: bold;
                }
                .lbl-col { width: 150px; background: #f8fafc; font-weight: bold; color: #334155; }
                .ltr { direction: ltr; text-align: left; font-family: Consolas, monospace; }
                .hash-code { font-family: Consolas, 'Courier New', monospace; font-size: 9.5px; word-break: break-all; }
                .badge-match {
                    display: inline-block;
                    padding: 2px 8px;
                    background: #dcfce7;
                    color: #166534;
                    border: 1px solid #86efac;
                    border-radius: 3px;
                    font-weight: bold;
                    font-size: 10px;
                }
                .badge-diff {
                    display: inline-block;
                    padding: 2px 8px;
                    background: #fee2e2;
                    color: #991b1b;
                    border: 1px solid #fca5a5;
                    border-radius: 3px;
                    font-weight: bold;
                    font-size: 10px;
                }
                .badge-ok { color: #166534; font-weight: bold; }
                .badge-bad { color: #991b1b; font-weight: bold; }
                .legal-text {
                    background: #f8fafc;
                    border: 1px solid #e2e8f0;
                    border-radius: 4px;
                    padding: 10px 14px;
                    font-size: 11px;
                    text-align: justify;
                    color: #334155;
                    margin: 12px 0;
                    line-height: 1.8;
                }
                .notes-box {
                    border: 1px dashed #94a3b8;
                    min-height: 48px;
                    padding: 6px 10px;
                    background: #ffffff;
                    margin-top: 6px;
                    font-size: 11px;
                }
                .sig-grid {
                    display: flex;
                    justify-content: space-between;
                    gap: 12px;
                    margin-top: 36px;
                    page-break-inside: avoid;
                }
                .sig-box {
                    flex: 1;
                    border: 1px solid #94a3b8;
                    padding: 8px 10px;
                    text-align: center;
                    min-height: 110px;
                    background: #ffffff;
                }
                .sig-box-title { font-weight: bold; font-size: 11px; color: #0f172a; margin-bottom: 6px; }
                .sig-box-sub { font-size: 9.5px; color: #64748b; }
                .sig-space { height: 45px; }
                .doc-footer {
                    margin-top: 24px;
                    padding-top: 8px;
                    border-top: 1px solid #cbd5e1;
                    display: flex;
                    justify-content: space-between;
                    font-size: 9.5px;
                    color: #64748b;
                }
                @media print {
                    body { padding: 0; font-size: 10.5px; }
                    .gov-card { border: 1.5px solid #000; padding: 12px; }
                    table.tbl-formal th, table.tbl-formal td { border: 1px solid #475569; padding: 4px 6px; }
                    .sig-grid { margin-top: 24px; }
                    .page-break { page-break-before: always; }
                }
            ");
            sb.Append("</style></head><body>");

            sb.Append("<div class=\"gov-card\">");

            // ---------- سربرگ رسمی دولتی ----------
            sb.Append("<div class=\"gov-header\">");
            sb.Append("<div class=\"header-side\">");
            sb.Append("<div><strong>تاریخ تنظیم:</strong> ").Append(E(jalaliShort)).Append("</div>");
            sb.Append("<div><strong>ساعت تنظیم:</strong> ").Append(E(timeShort)).Append("</div>");
            if (r.TryGetProperty("caseFile", out var cObj) && cObj.ValueKind == JsonValueKind.Object)
            {
                var wn = G(cObj, "Warrant");
                var cn = G(cObj, "CaseNumber");
                if (!string.IsNullOrEmpty(wn))
                    sb.Append("<div><strong>شماره حکم/مجوز:</strong> ").Append(E(wn)).Append("</div>");
                if (!string.IsNullOrEmpty(cn))
                    sb.Append("<div><strong>شماره پرونده:</strong> ").Append(E(cn)).Append("</div>");
            }
            sb.Append("<div><strong>طبقه‌بندی:</strong> محرمانه (مالیاتی)</div>");
            sb.Append("</div>");

            sb.Append("<div class=\"header-center\">");
            // آیکون رسمی الله و نماد جمهوری اسلامی ایران
            sb.Append("<div class=\"state-title\">جمهوری اسلامی ایران</div>");
            sb.Append("<div class=\"ministry-title\">وزارت امور اقتصادی و دارایی</div>");
            sb.Append("<div class=\"tax-title\">سازمان امور مالیاتی کشور</div>");
            sb.Append("<div class=\"doc-main-title\">صورت‌جلسه بازرسی و برداشت اطلاعات سامانه‌های رایانه‌ای</div>");
            sb.Append("<div class=\"doc-sub-title\">(موضوع ماده ۱۸۱ قانون مالیات‌های مستقیم مصوب ۱۳۶۶ و اصلاحات بعدی)</div>");
            sb.Append("<span class=\"badge-181\">ماده ۱۸۱ ق.م.م</span>");
            sb.Append("</div>");

            sb.Append("<div class=\"header-side header-side-left\">");
            sb.Append("<div><strong>Tool:</strong> Database Finder ").Append(E(S("version"))).Append("</div>");
            sb.Append("<div><strong>Host:</strong> ").Append(E(S("machine"))).Append("</div>");
            sb.Append("<div><strong>User:</strong> ").Append(E(S("user"))).Append("</div>");
            sb.Append("<div><strong>UTC:</strong> ").Append(E(createdUtc.ToString("yyyy-MM-dd HH:mm"))).Append("</div>");
            sb.Append("</div>");
            sb.Append("</div>"); // gov-header

            // ---------- بخش ۱: مشخصات پرونده و مؤدی ----------
            sb.Append("<div class=\"sec-title\">۱. مشخصات مأموریت بازرسی و مؤدی مالیاتی</div>");
            sb.Append("<table class=\"tbl-formal\">");
            if (r.TryGetProperty("caseFile", out var c) && c.ValueKind == JsonValueKind.Object)
            {
                var tp = G(c, "TaxpayerName");
                var nid = G(c, "NationalId");
                var off = G(c, "Officer");
                var rep = G(c, "TaxpayerRepresentative");
                var loc = G(c, "Location");
                var war = G(c, "Warrant");
                var cNum = G(c, "CaseNumber");
                var nts = G(c, "Notes");

                sb.Append("<tr><td class=\"lbl-col\">نام شخص / شرکت (مؤدی):</td><td>")
                    .Append(string.IsNullOrEmpty(tp) ? "—" : E(tp))
                    .Append("</td><td class=\"lbl-col\">شناسه ملی / کد اقتصادی:</td><td class=\"ltr\">")
                    .Append(string.IsNullOrEmpty(nid) ? "—" : E(nid)).Append("</td></tr>");

                sb.Append("<tr><td class=\"lbl-col\">شماره حکم / مجوز دادستانی:</td><td class=\"ltr\">")
                    .Append(string.IsNullOrEmpty(war) ? "—" : E(war))
                    .Append("</td><td class=\"lbl-col\">کلاسه پرونده مالیاتی:</td><td class=\"ltr\">")
                    .Append(string.IsNullOrEmpty(cNum) ? "—" : E(cNum)).Append("</td></tr>");

                sb.Append("<tr><td class=\"lbl-col\">سرپرست / مأموران بازرسی:</td><td>")
                    .Append(string.IsNullOrEmpty(off) ? "—" : E(off))
                    .Append("</td><td class=\"lbl-col\">نماینده مؤدی در محل:</td><td>")
                    .Append(string.IsNullOrEmpty(rep) ? "—" : E(rep)).Append("</td></tr>");

                if (!string.IsNullOrEmpty(loc))
                {
                    sb.Append("<tr><td class=\"lbl-col\">نشانی محل بازرسی:</td><td colspan=\"3\">")
                        .Append(E(loc)).Append("</td></tr>");
                }
                if (!string.IsNullOrEmpty(nts))
                {
                    sb.Append("<tr><td class=\"lbl-col\">ملاحظات پرونده:</td><td colspan=\"3\">")
                        .Append(E(nts)).Append("</td></tr>");
                }
            }
            else
            {
                sb.Append("<tr><td class=\"lbl-col\">وضعیت ثبت مشخصات پرونده:</td><td colspan=\"3\">توسط کاربر ثبت نشده است (استفاده از هویت سخت‌افزاری و سیستمی).</td></tr>");
            }
            sb.Append("</table>");

            // ---------- بخش ۲: شناسنامه دیجیتال و فنی سیستم میزبان (Digital Forensics) ----------
            sb.Append("<div class=\"sec-title\">۲. شناسنامه دیجیتال و شواهد سخت‌افزاری سیستم میزبان (Host Forensics)</div>");
            sb.Append("<table class=\"tbl-formal\">");

            // خواندن فیلد forensicHost از JSON در صورت وجود، یا فراخوانی زنده
            var machine = S("machine");
            var user = S("user");
            var osDesc = S("os");
            var domain = Environment.UserDomainName;
            var isAdmin = "Yes";
            var netSummary = new List<string>();

            if (r.TryGetProperty("forensicHost", out var fh) && fh.ValueKind == JsonValueKind.Object)
            {
                if (fh.TryGetProperty("DomainName", out var dProp)) domain = dProp.GetString() ?? domain;
                if (fh.TryGetProperty("IsAdministrator", out var aProp)) isAdmin = aProp.GetBoolean() ? "مدیر سیستم (Administrator)" : "کاربر استاندارد";
                if (fh.TryGetProperty("NetworkAdapters", out var naArr) && naArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var a in naArr.EnumerateArray())
                    {
                        var nName = G(a, "Name");
                        var nMac = G(a, "MacAddress");
                        var ips = new List<string>();
                        if (a.TryGetProperty("IpAddresses", out var ipsArr) && ipsArr.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var ip in ipsArr.EnumerateArray()) ips.Add(ip.GetString() ?? "");
                        }
                        netSummary.Add($"[{nName}] MAC: {nMac} | IP: {string.Join(", ", ips)}");
                    }
                }
            }
            else
            {
                var live = ForensicHostInfo.Collect();
                domain = live.DomainName;
                isAdmin = live.IsAdministrator ? "مدیر سیستم (Administrator)" : "کاربر استاندارد";
                foreach (var a in live.NetworkAdapters)
                    netSummary.Add($"[{a.Name}] MAC: {a.MacAddress} | IP: {string.Join(", ", a.IpAddresses)}");
            }

            sb.Append("<tr><td class=\"lbl-col\">نام رایانه (Host Name):</td><td class=\"ltr\">").Append(E(machine))
                .Append("</td><td class=\"lbl-col\">دامنه / گروه کاری:</td><td class=\"ltr\">").Append(E(domain)).Append("</td></tr>");

            sb.Append("<tr><td class=\"lbl-col\">کاربر ویندوز (Logged-in User):</td><td class=\"ltr\">").Append(E(user))
                .Append("</td><td class=\"lbl-col\">سطح دسترسی سامانه:</td><td>").Append(E(isAdmin)).Append("</td></tr>");

            sb.Append("<tr><td class=\"lbl-col\">سیستم‌عامل و نگارش:</td><td class=\"ltr\" colspan=\"3\">").Append(E(osDesc)).Append("</td></tr>");

            if (netSummary.Count > 0)
            {
                sb.Append("<tr><td class=\"lbl-col\">کارت‌های شبکه و شناسه MAC:</td><td colspan=\"3\" class=\"ltr\" style=\"font-size:10px;\">");
                foreach (var net in netSummary)
                    sb.Append("<div>").Append(E(net)).Append("</div>");
                sb.Append("</td></tr>");
            }

            sb.Append("<tr><td class=\"lbl-col\">تاریخ و زمان استخراج داده:</td><td colspan=\"3\">")
                .Append(E(jalaliFormal))
                .Append(" <span class=\"ltr\" style=\"color:#64748b;\">(").Append(E(createdUtc.ToString("yyyy-MM-dd HH:mm:ss"))).Append(" UTC)</span>")
                .Append("</td></tr>");

            sb.Append("<tr><td class=\"lbl-col\">مسیر ذخیره‌سازی مأموریت:</td><td colspan=\"3\" class=\"ltr\">").Append(E(S("root"))).Append("</td></tr>");
            sb.Append("</table>");

            // ---------- بخش ۳: صورت وضعیت پایگاه‌های داده کشف‌شده ----------
            sb.Append("<div class=\"sec-title\">۳. صورت وضعیت پایگاه‌های داده و سامانه‌های اطلاعاتی کشف‌شده</div>");
            if (r.TryGetProperty("databases", out var dbs) && dbs.ValueKind == JsonValueKind.Array && dbs.GetArrayLength() > 0)
            {
                sb.Append("<table class=\"tbl-formal\">");
                sb.Append("<tr><th style=\"width:35px;\">ردیف</th><th>نام پایگاه داده</th><th>موتور دیتابیس</th><th>روش برداشت</th><th>حجم داده</th><th>وضعیت استخراج</th></tr>");
                int idx = 1;
                foreach (var d in dbs.EnumerateArray())
                {
                    var st = G(d, "Status");
                    var isOk = st == "ok";
                    var statusBadge = isOk
                        ? "<span class=\"badge-ok\">موفقیت‌آمیز ✓</span>"
                        : $"<span class=\"badge-bad\">خطا: {E(G(d, "Error"))}</span>";

                    long bytes = 0;
                    if (d.TryGetProperty("Bytes", out var bProp) && bProp.TryGetInt64(out var bVal))
                        bytes = bVal;

                    sb.Append("<tr><td style=\"text-align:center;\">").Append(idx++)
                        .Append("</td><td class=\"ltr\"><strong>").Append(E(G(d, "Name"))).Append("</strong></td>")
                        .Append("<td>").Append(E(G(d, "Engine"))).Append(string.IsNullOrEmpty(G(d, "ServerVersion")) ? "" : " <span class=\"ltr\" style=\"font-size:9.5px;color:#64748b;\">(" + E(G(d, "ServerVersion")) + ")</span>").Append("</td>")
                        .Append("<td>").Append(E(G(d, "Method"))).Append("</td>")
                        .Append("<td class=\"ltr\">").Append(E(ManifestGenerator.FormatBytes(bytes))).Append("</td>")
                        .Append("<td>").Append(statusBadge).Append("</td></tr>");
                }
                sb.Append("</table>");
            }
            else
            {
                sb.Append("<p>پایگاه داده‌ای در مانیفست ثبت نشده است.</p>");
            }

            // خلاصه آماری
            if (r.TryGetProperty("summary", out var s) && s.ValueKind == JsonValueKind.Object)
            {
                long totalBytes = 0;
                if (s.TryGetProperty("totalBytes", out var tbProp) && tbProp.TryGetInt64(out var tbVal))
                    totalBytes = tbVal;

                sb.Append("<div style=\"margin: 6px 0 14px 0; font-size:11px; background:#f8fafc; padding:6px 12px; border:1px solid #e2e8f0; border-radius:4px;\">");
                sb.Append("<strong>خلاصه مأموریت:</strong> ")
                    .Append($"تعداد کل پایگاه‌ها: {G(s, "databases")} مورد | ")
                    .Append($"موفق: {G(s, "succeeded")} | ")
                    .Append($"ناموفق: {G(s, "failed")} | ")
                    .Append($"تعداد فایل‌های استخراج‌شده: {G(s, "files")} | ")
                    .Append($"حجم کل استخراج‌شده: <span class=\"ltr\">{E(ManifestGenerator.FormatBytes(totalBytes))}</span>");
                sb.Append("</div>");
            }

            // ---------- بخش ۴: پیوست فنی تطابق چک‌سام‌ها و زنجیره توقیف شواهد دیجیتال ----------
            sb.Append("<div class=\"sec-title\">۴. پیوست فنی زنجیره توقیف شواهد دیجیتال و تطابق چک‌سام‌ها (Cryptographic Chain of Custody)</div>");
            sb.Append("<p style=\"font-size:10.5px; color:#475569; margin:4px 0 8px 0;\">");
            sb.Append("به منظور تضمین صحت و تمامیت شواهد دیجیتال، چک‌سام رمزنگاری SHA-256 کلیه فایل‌ها در لحظه خواندن از مبدأ محاسبه شده و با هش فایل نهایی در مقصد مقایسه گردید:");
            sb.Append("</p>");

            if (r.TryGetProperty("acquisition", out var ac) && ac.ValueKind == JsonValueKind.Array && ac.GetArrayLength() > 0)
            {
                sb.Append("<table class=\"tbl-formal\">");
                sb.Append("<tr><th style=\"width:30px;\">#</th><th>مسیر و نام فایل</th><th style=\"width:80px;\">نتیجه تطابق</th><th>چک‌سام مبدأ (Source SHA-256)</th><th>چک‌سام مقصد (Destination SHA-256)</th></tr>");
                int fIdx = 1;
                foreach (var x in ac.EnumerateArray())
                {
                    var m = G(x, "Match");
                    var isMatch = m == "MATCH";
                    var badge = isMatch
                        ? "<span class=\"badge-match\">عین اصل (MATCH) ✓</span>"
                        : "<span class=\"badge-diff\">مغایرت (DIFF) ⚠</span>";

                    sb.Append("<tr><td style=\"text-align:center;\">").Append(fIdx++)
                        .Append("</td><td class=\"ltr\" style=\"word-break:break-all; font-weight:bold;\">").Append(E(G(x, "RelPath")))
                        .Append("</td><td style=\"text-align:center;\">").Append(badge)
                        .Append("</td><td class=\"ltr hash-code\">").Append(E(G(x, "SourceHash")))
                        .Append("</td><td class=\"ltr hash-code\">").Append(E(G(x, "DestHash"))).Append("</td></tr>");
                }
                sb.Append("</table>");
            }
            else if (r.TryGetProperty("files", out var fList) && fList.ValueKind == JsonValueKind.Array && fList.GetArrayLength() > 0)
            {
                sb.Append("<table class=\"tbl-formal\">");
                sb.Append("<tr><th style=\"width:30px;\">#</th><th>مسیر و نام فایل</th><th>حجم</th><th>چک‌سام معتبر (SHA-256)</th></tr>");
                int fIdx = 1;
                foreach (var x in fList.EnumerateArray())
                {
                    long fSize = 0;
                    if (x.TryGetProperty("Size", out var szProp) && szProp.TryGetInt64(out var szVal))
                        fSize = szVal;

                    sb.Append("<tr><td style=\"text-align:center;\">").Append(fIdx++)
                        .Append("</td><td class=\"ltr\">").Append(E(G(x, "Path")))
                        .Append("</td><td class=\"ltr\">").Append(E(ManifestGenerator.FormatBytes(fSize)))
                        .Append("</td><td class=\"ltr hash-code\">").Append(E(G(x, "Sha256"))).Append("</td></tr>");
                }
                sb.Append("</table>");
            }

            // چک‌سام تجمیعی خود مانیفست
            var selfSha = S("selfSha256");
            if (!string.IsNullOrEmpty(selfSha))
            {
                sb.Append("<div style=\"font-size:10px; margin: 4px 0 12px 0; background:#f1f5f9; padding:6px 10px; border-radius:3px;\">");
                sb.Append("<strong>چک‌سام رمزنگاری مانیفست پرونده (Manifest SHA-256):</strong> ");
                sb.Append("<span class=\"ltr hash-code\" style=\"color:#0369a1;\">").Append(E(selfSha)).Append("</span>");
                sb.Append("</div>");
            }

            // ---------- بخش ۵: اقرارنامه و تعهدات حقوقی طرفین ----------
            sb.Append("<div class=\"sec-title\">۵. گواهی حسن اجرای بازرسی و اظهارات طرفین</div>");
            sb.Append("<div class=\"legal-text\">");
            sb.Append("در اجرای حکم بازرسی صادره فوق‌الذکر و به استناد مفاد <strong>ماده ۱۸۱ قانون مالیات‌های مستقیم مصوب اسفند ۱۳۶۶ با اصلاحات و الحاقات بعدی</strong>، هیأت بازرسی با حضور در محل استقرار تجهیزات و سامانه‌های رایانه‌ای مؤدی، اقدام به بازرسی، شناسایی و برداشت نسخه‌ای از پایگاه‌های داده و اطلاعات مالی فوق‌الذکر نمود. عملیات برداشت داده‌ها با بهره‌گیری از فناوری‌های استاندارد تصویربرداری دیجیتال و کپی سایه (Volume Shadow Copy)، بدون ایجاد هرگونه توقف یا وقفه در فعالیت‌های سیستم و بدون کوچکترین تغییر، حذف یا دستکاری در داده‌های سامانه‌های اصلی مؤدی انجام پذیرفت. صحت، سلامت و اصالت فایل‌های کپی‌شده بر مبنای جدول مقایسه هش‌های رمزنگاری SHA-256 در مبدأ و مقصد مورد تایید و گواهی است.");
            sb.Append("</div>");

            sb.Append("<div style=\"margin-top:8px;\"><strong>توضیحات و ملاحظات نماینده مؤدی / هیأت بازرسی:</strong></div>");
            sb.Append("<div class=\"notes-box\">");
            if (r.TryGetProperty("caseFile", out var c2) && c2.ValueKind == JsonValueKind.Object && !string.IsNullOrEmpty(G(c2, "Notes")))
                sb.Append(E(G(c2, "Notes")));
            else
                sb.Append("<span style=\"color:#94a3b8;\">ملاحظات خاصی ثبت نشده است / محل درج دست‌نویس در نسخه چاپی:</span>");
            sb.Append("</div>");

            // ---------- بخش ۶: کادرهای امضاء و مهر طرفین ----------
            sb.Append("<div class=\"sig-grid\">");

            sb.Append("<div class=\"sig-box\">");
            sb.Append("<div class=\"sig-box-title\">سرپرست / اعضای هیأت بازرسی ماده ۱۸۱</div>");
            sb.Append("<div class=\"sig-box-sub\">سازمان امور مالیاتی کشور</div>");
            sb.Append("<div class=\"sig-space\"></div>");
            sb.Append("<div>نام: .......................................</div>");
            sb.Append("<div>امضاء و تاریخ: ............................</div>");
            sb.Append("</div>");

            sb.Append("<div class=\"sig-box\">");
            sb.Append("<div class=\"sig-box-title\">کارشناس فناوری اطلاعات و استخراج داده</div>");
            sb.Append("<div class=\"sig-box-sub\">مجری فرآیند فنی مأموریت</div>");
            sb.Append("<div class=\"sig-space\"></div>");
            sb.Append("<div>نام: .......................................</div>");
            sb.Append("<div>امضاء و تاریخ: ............................</div>");
            sb.Append("</div>");

            sb.Append("<div class=\"sig-box\">");
            sb.Append("<div class=\"sig-box-title\">مؤدی / نماینده قانونی مؤدی</div>");
            sb.Append("<div class=\"sig-box-sub\">مدیرعامل / مدیر مالی / نماینده تام‌الاختیار</div>");
            sb.Append("<div class=\"sig-space\"></div>");
            sb.Append("<div>نام: .......................................</div>");
            sb.Append("<div>امضاء و اثر انگشت: .....................</div>");
            sb.Append("</div>");

            sb.Append("<div class=\"sig-box\">");
            sb.Append("<div class=\"sig-box-title\">ناظر دادستانی انتظامی مالیاتی / ضابطین</div>");
            sb.Append("<div class=\"sig-box-sub\">در صورت حضور در مأموریت</div>");
            sb.Append("<div class=\"sig-space\"></div>");
            sb.Append("<div>نام: .......................................</div>");
            sb.Append("<div>امضاء و مهر: ............................</div>");
            sb.Append("</div>");

            sb.Append("</div>"); // sig-grid

            // فوتر
            sb.Append("<div class=\"doc-footer\">");
            sb.Append("<div>سازمان امور مالیاتی کشور — مرکز مبارزه با فرار مالیاتی و پولشویی</div>");
            sb.Append("<div>سامانه استاندارد Database Finder — نسخه ").Append(E(S("version"))).Append("</div>");
            sb.Append("<div>شناسه استخراج: ").Append(E(jalaliShort)).Append(" / ").Append(E(timeShort)).Append("</div>");
            sb.Append("</div>");

            sb.Append("</div>"); // gov-card
            sb.Append("</body></html>");

            var baseDir = Path.GetDirectoryName(manifestJsonPath) ?? ".";
            var fileNameNoExt = Path.GetFileNameWithoutExtension(manifestJsonPath).Replace("manifest", "TaxAudit_Report_181");
            var htmlPath = Path.Combine(baseDir, fileNameNoExt + ".html");
            var pdfPath = Path.Combine(baseDir, fileNameNoExt + ".pdf");

            File.WriteAllText(htmlPath, sb.ToString(), new UTF8Encoding(false));
            LastGeneratedHtmlPath = htmlPath;

            // صدور مستقیم نسخه رسمی PDF در صورت وجود مرورگر Edge یا Chrome
            bool pdfSuccess = PdfExporter.ConvertHtmlToPdf(htmlPath, pdfPath);
            if (pdfSuccess && File.Exists(pdfPath))
            {
                LastGeneratedPdfPath = pdfPath;
                return pdfPath;
            }

            return htmlPath;
        }

        /// <summary>
        /// گزارش اسکن شبکه (سیستم‌های دارای پورت باز) در قالب‌های TXT, JSON, HTML و PDF.
        /// </summary>
        public static string GenerateRemote(string outputBase, string target, string ports, IReadOnlyList<RemoteScanResult> results)
        {
            var nowUtc = DateTime.UtcNow;
            var nowLocal = nowUtc.ToLocalTime();
            var jalaliFormal = JalaliDateHelper.FormatFormal(nowLocal);
            var jalaliShort = JalaliDateHelper.FormatShort(nowLocal);
            var appVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
            var osText = RuntimeInformation.OSDescription + " (" + RuntimeInformation.OSArchitecture + ")";
            var forensic = ForensicHostInfo.Collect();

            var openResults = results.Where(r => r.IsOpen)
                .OrderBy(r => r.Host, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.Port)
                .ToList();
            var summary = new
            {
                total = results.Count,
                open = openResults.Count,
                openHosts = openResults.Select(r => r.Host).Distinct().Count(),
                closed = results.Count - openResults.Count
            };

            // ---------- TXT ----------
            var body = new List<string> { ManifestGenerator.BuildTaxStamp181(includeCaption: false), "" };
            body.Add("گزارش اسکن شبکه — ماده ۱۸۱ قانون مالیات‌های مستقیم");
            body.Add("=".PadRight(64, '='));
            body.Add($"ابزار          : Database Finder {ManifestGenerator.ToolVersion}");
            body.Add($"هدف اسکن       : {target}");
            body.Add($"پورت‌ها         : {(string.IsNullOrEmpty(ports) ? "پورت‌های شناخته‌شده" : ports)}");
            body.Add($"سیستم           : {Environment.MachineName}");
            body.Add($"دامنه / ادمین   : {forensic.DomainName} (Admin: {(forensic.IsAdministrator ? "Yes" : "No")})");
            body.Add($"کاربر           : {Environment.UserName}");
            body.Add($"نوع سیستم       : {osText}");
            body.Add($"زمان (شمسی)     : {jalaliFormal}");
            body.Add($"زمان (UTC)      : {nowUtc:yyyy-MM-dd HH:mm:ss}");
            body.Add("");
            body.Add($"تعداد نتایج کل            : {summary.total}");
            body.Add($"تعداد پورت‌های باز         : {summary.open}");
            body.Add($"سیستم‌های دارای پورت باز   : {summary.openHosts}");
            body.Add($"پورت‌های بسته              : {summary.closed}");
            body.Add("");
            if (openResults.Count == 0)
            {
                body.Add("هیچ سیستمی با پورت باز یافت نشد.");
            }
            else
            {
                body.Add("سیستم‌های دارای پورت باز:");
                body.Add("-".PadRight(64, '-'));
                foreach (var r in openResults)
                {
                    var detail = string.IsNullOrEmpty(r.Version) ? r.Message : r.Version;
                    body.Add($"[{r.Host}]  {r.TypeDisplayName}  (پورت {r.Port})" +
                        (string.IsNullOrEmpty(r.ServerName) ? "" : $"  — {r.ServerName}") +
                        (string.IsNullOrEmpty(detail) ? "" : $"  — {detail}"));
                }
            }
            var bodyHash = ManifestGenerator.Sha256Text(string.Join("\n", body));
            body.Add("");
            body.Add($"Report content SHA-256 (excluding this line): {bodyHash}");
            var txt = string.Join("\n", body) + "\n";
            File.WriteAllText(outputBase + ".txt", txt, new UTF8Encoding(false));

            // ---------- JSON ----------
            var json = new
            {
                schema = "remote-scan-1",
                taxNotice = "TAX 181 ARTICLE . MSAM Group",
                tool = "Database Finder",
                version = ManifestGenerator.ToolVersion,
                appVersion,
                machine = Environment.MachineName,
                user = Environment.UserName,
                forensicHost = forensic,
                os = osText,
                target,
                ports = string.IsNullOrEmpty(ports) ? "default known ports" : ports,
                createdJalali = jalaliShort,
                createdUtc = nowUtc,
                summary,
                hosts = openResults.Select(r => new
                {
                    r.Host,
                    r.Port,
                    type = r.Type,
                    typeDisplayName = r.TypeDisplayName,
                    open = true,
                    r.Version,
                    r.ServerName,
                    r.Message
                }),
                contentSha256 = bodyHash
            };
            var jsonPath = outputBase + ".json";
            File.WriteAllText(jsonPath,
                JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true }),
                new UTF8Encoding(false));

            // ---------- HTML ----------
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html lang=\"fa\" dir=\"rtl\"><head><meta charset=\"utf-8\">");
            sb.Append("<title>گزارش اسکن شبکه ـ ماده ۱۸۱ ق.م.م</title><style>");
            sb.Append(@"
                @page { size: A4 portrait; margin: 12mm 12mm 14mm 12mm; }
                body { font-family: 'Vazirmatn', Tahoma, Arial, sans-serif; margin: 24px; color: #1e293b; font-size: 11.5px; line-height: 1.6; }
                .gov-card { border: 2px solid #0f172a; padding: 16px 20px; }
                .gov-header { display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid #0f172a; padding-bottom: 12px; margin-bottom: 14px; }
                .header-side { width: 30%; font-size: 10.5px; }
                .header-center { width: 40%; text-align: center; }
                .title-main { font-size: 14px; font-weight: bold; margin: 4px 0; }
                .title-sub { font-size: 11px; color: #475569; }
                .sec-title { font-size: 12px; font-weight: bold; background: #f1f5f9; border-right: 4px solid #0284c7; padding: 4px 10px; margin: 16px 0 8px 0; }
                table { width: 100%; border-collapse: collapse; margin: 10px 0; font-size: 11px; }
                th, td { border: 1px solid #cbd5e1; padding: 6px 8px; text-align: right; }
                th { background: #f8fafc; font-weight: bold; }
                .lbl-col { width: 180px; background: #f8fafc; font-weight: bold; }
                .ltr { direction: ltr; text-align: left; font-family: Consolas, monospace; }
                .sig-grid { display: flex; gap: 16px; margin-top: 36px; }
                .sig-box { flex: 1; border: 1px solid #94a3b8; padding: 8px; text-align: center; min-height: 100px; }
                @media print { body { margin: 0; padding: 0; } .gov-card { border: 1.5px solid #000; } }
            ");
            sb.Append("</style></head><body>");

            sb.Append("<div class=\"gov-card\">");
            sb.Append("<div class=\"gov-header\">");
            sb.Append("<div class=\"header-side\">");
            sb.Append("<div><strong>تاریخ اسکن:</strong> ").Append(E(jalaliShort)).Append("</div>");
            sb.Append("<div><strong>زمان تنظیم:</strong> ").Append(E(nowLocal.ToString("HH:mm:ss"))).Append("</div>");
            sb.Append("<div><strong>طبقه‌بندی:</strong> محرمانه</div>");
            sb.Append("</div>");

            sb.Append("<div class=\"header-center\">");
            sb.Append("<div style=\"font-size:12px; font-weight:bold;\">جمهوری اسلامی ایران</div>");
            sb.Append("<div style=\"font-size:11px; color:#334155;\">سازمان امور مالیاتی کشور</div>");
            sb.Append("<div class=\"title-main\">گزارش اسکن و بازرسی شبکه سامانه‌های اطلاعاتی</div>");
            sb.Append("<div class=\"title-sub\">موضوع ماده ۱۸۱ قانون مالیات‌های مستقیم</div>");
            sb.Append("</div>");

            sb.Append("<div class=\"header-side ltr\">");
            sb.Append("<div><strong>Tool:</strong> Database Finder ").Append(E(ManifestGenerator.ToolVersion)).Append("</div>");
            sb.Append("<div><strong>Host:</strong> ").Append(E(Environment.MachineName)).Append("</div>");
            sb.Append("<div><strong>User:</strong> ").Append(E(Environment.UserName)).Append("</div>");
            sb.Append("</div>");
            sb.Append("</div>"); // gov-header

            sb.Append("<div class=\"sec-title\">۱. مشخصات مأموریت و شرایط اسکن شبکه</div>");
            sb.Append("<table>");
            Row(sb, "هدف اسکن (Range / Target)", target);
            Row(sb, "پورت‌های هدف", string.IsNullOrEmpty(ports) ? "پورت‌های استاندارد دیتابیس‌ها" : ports);
            Row(sb, "سیستم و دامنه بازرس", $"{Environment.MachineName} ({forensic.DomainName})");
            Row(sb, "کاربر و سطح دسترسی", $"{Environment.UserName} ({(forensic.IsAdministrator ? "Admin" : "Standard")})");
            Row(sb, "زمان دقیق مأموریت", $"{jalaliFormal} ({nowUtc:yyyy-MM-dd HH:mm:ss} UTC)");
            Row(sb, "خلاصه یافته‌ها", $"تعداد کل اهداف: {summary.total} | پورت‌های باز: {summary.open} | سیستم‌های با پورت باز: {summary.openHosts}");
            Row(sb, "هش اعتبار گزارش (SHA-256)", bodyHash);
            sb.Append("</table>");

            sb.Append("<div class=\"sec-title\">۲. سیستم‌های شناسایی‌شده با پورت باز</div>");
            if (openResults.Count == 0)
            {
                sb.Append("<p>هیچ سامانه‌ای با پورت دیتابیس باز در محدوده مشخص‌شده یافت نشد.</p>");
            }
            else
            {
                sb.Append("<table><tr><th style=\"width:35px;\">#</th><th>آدرس IP / Host</th><th>نوع پایگاه داده</th><th>پورت</th><th>نام سرور و اطلاعات نگارش</th></tr>");
                int idx = 1;
                foreach (var r in openResults)
                {
                    var detail = string.IsNullOrEmpty(r.Version) ? r.Message : r.Version;
                    sb.Append("<tr><td style=\"text-align:center;\">").Append(idx++)
                        .Append("</td><td class=\"ltr\"><strong>").Append(E(r.Host)).Append("</strong></td>")
                        .Append("<td>").Append(E(r.TypeDisplayName)).Append("</td>")
                        .Append("<td class=\"ltr\">").Append(r.Port).Append("</td>")
                        .Append("<td>").Append(E(r.ServerName ?? "")).Append(string.IsNullOrEmpty(detail) ? "" : " — " + E(detail)).Append("</td></tr>");
                }
                sb.Append("</table>");
            }

            sb.Append("<div class=\"sig-grid\">");
            sb.Append("<div class=\"sig-box\"><div>سرپرست هیأت بازرسی ماده ۱۸۱</div><div style=\"height:45px;\"></div><div>امضاء و تاریخ: ............................</div></div>");
            sb.Append("<div class=\"sig-box\"><div>کارشناس فناوری اطلاعات و شبکه</div><div style=\"height:45px;\"></div><div>امضاء و تاریخ: ............................</div></div>");
            sb.Append("<div class=\"sig-box\"><div>مؤدی / نماینده حاضر در محل</div><div style=\"height:45px;\"></div><div>امضاء و تاریخ: ............................</div></div>");
            sb.Append("</div>");

            sb.Append("</div>"); // gov-card
            sb.Append("</body></html>");

            var htmlPath = outputBase + ".html";
            var pdfPath = outputBase + ".pdf";
            File.WriteAllText(htmlPath, sb.ToString(), new UTF8Encoding(false));
            LastGeneratedHtmlPath = htmlPath;

            bool pdfSuccess = PdfExporter.ConvertHtmlToPdf(htmlPath, pdfPath);
            if (pdfSuccess && File.Exists(pdfPath))
            {
                LastGeneratedPdfPath = pdfPath;
                return pdfPath;
            }

            return htmlPath;
        }

        private static void Row(StringBuilder sb, string k, string v)
        {
            sb.Append("<tr><td class=\"lbl-col\">").Append(k).Append("</td><td>")
                .Append(E(v)).Append("</td></tr>");
        }

        private static string G(JsonElement e, string name) =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v)
                ? v.ValueKind switch
                {
                    JsonValueKind.String => v.GetString() ?? "",
                    JsonValueKind.Number => v.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => ""
                } : "";

        private static string E(string s) =>
            s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }
}
