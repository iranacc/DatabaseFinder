using System.Globalization;
using System.Text;

namespace DatabaseFinder
{
    /// <summary>
    /// محاسبات و قالب‌بندی تقویم هجری شمسی (جلالی) ویژه صورت‌جلسات رسمی ممیزی و بازرسی ماده ۱۸۱.
    /// </summary>
    public static class JalaliDateHelper
    {
        private static readonly PersianCalendar _pc = new();

        private static readonly string[] _persianMonths =
        {
            "",
            "فروردین", "اردیبهشت", "خرداد",
            "تیر", "مرداد", "شهریور",
            "مهر", "آبان", "آذر",
            "دی", "بهمن", "اسفند"
        };

        private static readonly Dictionary<DayOfWeek, string> _persianDaysOfWeek = new()
        {
            { DayOfWeek.Saturday, "شنبه" },
            { DayOfWeek.Sunday, "یکشنبه" },
            { DayOfWeek.Monday, "دوشنبه" },
            { DayOfWeek.Tuesday, "سه‌شنبه" },
            { DayOfWeek.Wednesday, "چهارشنبه" },
            { DayOfWeek.Thursday, "پنج‌شنبه" },
            { DayOfWeek.Friday, "جمعه" }
        };

        /// <summary>
        /// استخراج اجزای تاریخ شمسی: سال، ماه، روز.
        /// </summary>
        public static (int Year, int Month, int Day) ToJalali(DateTime dt)
        {
            if (dt < new DateTime(622, 3, 22)) return (0, 0, 0);
            return (_pc.GetYear(dt), _pc.GetMonth(dt), _pc.GetDayOfMonth(dt));
        }

        /// <summary>
        /// قالب عددی کوتاه تاریخ شمسی، به عنوان مثال: 1405/08/21
        /// </summary>
        public static string FormatShort(DateTime dt)
        {
            if (dt < new DateTime(622, 3, 22)) return "-";
            var (y, m, d) = ToJalali(dt);
            return $"{y:D4}/{m:D2}/{d:D2}";
        }

        /// <summary>
        /// تاریخ و ساعت شمسی، به عنوان مثال: 1405/08/21 11:45:20
        /// </summary>
        public static string FormatFull(DateTime dt)
        {
            var (y, m, d) = ToJalali(dt);
            return $"{y:D4}/{m:D2}/{d:D2} {dt.Hour:D2}:{dt.Minute:D2}:{dt.Second:D2}";
        }

        public static string Format(DateTime dt) => FormatFull(dt);

        /// <summary>
        /// فرمت اداری و حقوقی کامل با نام روز و ماه، به عنوان مثال:
        /// چهارشنبه ۲۱ آبان ۱۴۰۵ - ساعت ۱۱:۴۵
        /// </summary>
        public static string FormatFormal(DateTime dt, bool usePersianDigits = true)
        {
            var (y, m, d) = ToJalali(dt);
            var monthName = m >= 1 && m <= 12 ? _persianMonths[m] : m.ToString();
            var dayName = _persianDaysOfWeek.TryGetValue(dt.DayOfWeek, out var name) ? name : "";

            var text = $"{dayName} {d} {monthName} {y} — ساعت {dt.Hour:D2}:{dt.Minute:D2}";
            return usePersianDigits ? ToPersianDigits(text) : text;
        }

        /// <summary>
        /// نام ماه هجری شمسی (۱ تا ۱۲)
        /// </summary>
        public static string GetMonthName(int month)
        {
            return month >= 1 && month <= 12 ? _persianMonths[month] : "";
        }

        /// <summary>
        /// تبدیل ارقام انگلیسی به ارقام فارسی جهت اسناد اداری رسمی
        /// </summary>
        public static string ToPersianDigits(string? text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var sb = new StringBuilder(text.Length);
            foreach (var ch in text)
            {
                if (ch >= '0' && ch <= '9')
                    sb.Append((char)('۰' + (ch - '0')));
                else
                    sb.Append(ch);
            }
            return sb.ToString();
        }
    }
}
