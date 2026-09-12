using System.Text;
using System.Text.Json;

namespace DatabaseFinder
{
    /// <summary>
    /// گزارش فارسی چاپی (صورتجلسه) از روی manifest.json.
    /// جنبه نمایشی دارد؛ ارزش اثباتی از مانیفست فنی (txt/md/json) می‌آید.
    /// خروجی HTML راست‌به‌چپ که با Word باز و به PDF تبدیل می‌شود.
    /// </summary>
    public static class ReportGenerator
    {
        public static string GenerateFromManifest(string manifestJsonPath)
        {
            using var stream = File.OpenRead(manifestJsonPath);
            using var doc = JsonDocument.Parse(stream);
            var r = doc.RootElement;

            string S(string name) => r.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString() ?? "" : "";

            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html lang=\"fa\" dir=\"rtl\"><head><meta charset=\"utf-8\">");
            sb.Append("<title>صورتجلسه برداشت داده ـ ماده ۱۸۱</title>");
            sb.Append("<style>");
            sb.Append("body{font-family:Tahoma,Arial,sans-serif;margin:40px;color:#111;}");
            sb.Append("h1{text-align:center;font-size:20px;margin-bottom:0;}");
            sb.Append(".sub{text-align:center;color:#444;margin-top:4px;}");
            sb.Append(".stamp{text-align:center;font-weight:bold;letter-spacing:2px;margin:12px 0;}");
            sb.Append("table{width:100%;border-collapse:collapse;margin:14px 0;font-size:12px;}");
            sb.Append("th,td{border:1px solid #555;padding:6px 8px;text-align:right;}");
            sb.Append("th{background:#eee;}");
            sb.Append(".ltr{direction:ltr;text-align:left;font-family:Consolas,monospace;font-size:11px;word-break:break-all;}");
            sb.Append(".ok{color:#1b5e20;font-weight:bold;}.bad{color:#b71c1c;font-weight:bold;}");
            sb.Append(".sig{display:flex;gap:40px;margin-top:60px;}");
            sb.Append(".sig div{flex:1;border-top:1px solid #000;padding-top:6px;text-align:center;}");
            sb.Append("@media print{body{margin:10mm;}}");
            sb.Append("</style></head><body>");

            sb.Append("<div class=\"stamp\">TAX 181 ARTICLE . MSAM Group</div>");
            sb.Append("<h1>صورتجلسه برداشت داده‌های مالی (ماده ۱۸۱ قانون مالیات‌های مستقیم)</h1>");
            sb.Append("<p class=\"sub\">این گزارش نمای خلاصه است؛ مستند فنی کامل در فایل‌های manifest.txt و manifest.json همین پوشه ثبت شده است.</p>");

            if (r.TryGetProperty("caseFile", out var c) && c.ValueKind == JsonValueKind.Object)
            {
                sb.Append("<h2>مشخصات پرونده</h2><table>");
                Row(sb, "شماره پرونده", G(c, "CaseNumber"));
                Row(sb, "نام مأمور", G(c, "Officer"));
                Row(sb, "شماره حکم / مجوز", G(c, "Warrant"));
                Row(sb, "توضیحات", G(c, "Notes"));
                sb.Append("</table>");
            }

            sb.Append("<h2>خلاصه اجرا</h2><table>");
            Row(sb, "ابزار", $"Database Finder {E(S("version"))}");
            Row(sb, "سیستم محل برداشت", E(S("machine")));
            Row(sb, "کاربر سیستم", E(S("user")));
            Row(sb, "زمان ایجاد (UTC)", E(S("createdUtc")));
            Row(sb, "پوشه پرونده", E(S("root")));
            if (r.TryGetProperty("summary", out var s) && s.ValueKind == JsonValueKind.Object)
            {
                Row(sb, "دیتابیس‌ها (موفق / ناموفق)", $"{G(s, "succeeded")} / {G(s, "failed")}");
                Row(sb, "تعداد فایل‌ها", G(s, "files"));
            }
            if (r.TryGetProperty("acquisition", out var a) && a.ValueKind == JsonValueKind.Array)
            {
                int match = 0, diff = 0;
                foreach (var x in a.EnumerateArray())
                {
                    var m = G(x, "Match");
                    if (m == "MATCH") match++;
                    else if (m == "DIFF") diff++;
                }
                Row(sb, "تطابق مبدأ و مقصد", $"{match} مورد عین اصل، {diff} مورد مغایر، {a.GetArrayLength() - match - diff} مورد راستی‌آزمایی‌نشده");
            }
            Row(sb, "هش خود مانیفست", E(S("selfSha256")));
            sb.Append("</table>");

            if (r.TryGetProperty("databases", out var dbs) && dbs.ValueKind == JsonValueKind.Array && dbs.GetArrayLength() > 0)
            {
                sb.Append("<h2>دیتابیس‌ها</h2><table><tr><th>نام</th><th>موتور</th><th>روش</th><th>وضعیت</th><th>حجم</th></tr>");
                foreach (var d in dbs.EnumerateArray())
                {
                    var st = G(d, "Status");
                    var cls = st == "ok" ? "ok" : st == "failed" ? "bad" : "";
                    sb.Append("<tr><td>").Append(E(G(d, "Name"))).Append("</td><td>").Append(E(G(d, "Engine")))
                        .Append("</td><td>").Append(E(G(d, "Method"))).Append("</td><td class=\"").Append(cls).Append("\">")
                        .Append(E(st)).Append(E(G(d, "Error")).Length > 0 ? " ـ " + E(G(d, "Error")) : "")
                        .Append("</td><td>").Append(E(G(d, "Bytes"))).Append("</td></tr>");
                }
                sb.Append("</table>");
            }

            if (r.TryGetProperty("acquisition", out var ac) && ac.ValueKind == JsonValueKind.Array && ac.GetArrayLength() > 0)
            {
                sb.Append("<h2>تطابق فایل‌ها (مبدأ با مقصد)</h2><table><tr><th>فایل</th><th>وضعیت</th><th>هش مبدأ (SHA-256)</th></tr>");
                foreach (var x in ac.EnumerateArray())
                {
                    var m = G(x, "Match");
                    var cls = m == "MATCH" ? "ok" : m == "DIFF" ? "bad" : "";
                    sb.Append("<tr><td>").Append(E(G(x, "RelPath"))).Append("</td><td class=\"").Append(cls).Append("\">")
                        .Append(m == "MATCH" ? "عین اصل" : m == "DIFF" ? "مغایر" : "راستی‌آزمایی‌نشده")
                        .Append("</td><td class=\"ltr\">").Append(E(G(x, "SourceHash"))).Append("</td></tr>");
                }
                sb.Append("</table>");
            }

            sb.Append("<div class=\"sig\"><div>نام و امضای مأمور<br><br>تاریخ: ............</div>");
            sb.Append("<div>نام و امضای نماینده مؤدی<br><br>تاریخ: ............</div></div>");
            sb.Append("</body></html>");

            var htmlPath = Path.Combine(
                Path.GetDirectoryName(manifestJsonPath) ?? ".",
                Path.GetFileNameWithoutExtension(manifestJsonPath).Replace("manifest", "report") + ".html");
            if (htmlPath == manifestJsonPath) htmlPath += ".html";
            File.WriteAllText(htmlPath, sb.ToString(), new UTF8Encoding(false));
            return htmlPath;
        }

        private static void Row(StringBuilder sb, string k, string v)
        {
            sb.Append("<tr><th style=\"width:220px;\">").Append(k).Append("</th><td>")
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
