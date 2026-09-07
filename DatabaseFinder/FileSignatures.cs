using System.Text;

namespace DatabaseFinder
{
    /// <summary>
    /// بررسی امضای محتوای فایل‌ها تا فایل‌های هم‌نام (مثل Acrobat.dll.bak) به‌عنوان دیتابیس شناخته نشوند.
    /// هر روش در صورت ناموفق بودن خواندن، false برمی‌گرداند تا فایل رد شود.
    /// </summary>
    public static class FileSignatures
    {
        /// <summary>آیا فایل‌های SQL Server هستند (mdf/ldf/ndf یا bak).</summary>
        public static bool IsSqlServerFile(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".bak")
                return StartsWithBytes(path, Encoding.ASCII.GetBytes("MSSQLBAK"));
            return StartsWithSqlHeader(path);
        }

        /// <summary>
        /// فایل‌های داده/لاگ SQL Server با هدر صفحه ۰ شروع می‌شوند:
        /// 01 0F 00 00 (00|08) 02
        /// </summary>
        private static bool StartsWithSqlHeader(string path)
        {
            const int len = 6;
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                if (fs.Length < len) return false;
                var b = new byte[len];
                if (fs.Read(b, 0, len) != len) return false;
                return b[0] == 0x01 && b[1] == 0x0F && b[2] == 0x00 && b[3] == 0x00
                    && (b[4] == 0x00 || b[4] == 0x08) && b[5] == 0x02;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsSqliteFile(string path)
        {
            return StartsWithBytes(path, Encoding.ASCII.GetBytes("SQLite format 3\0"));
        }

        public static bool IsAccessFile(string path)
        {
            return ContainsAscii(path, new[] { "Standard Jet DB", "Standard ACE DB" }, 32);
        }

        public static bool IsRedisRdb(string path)
        {
            return StartsWithBytes(path, Encoding.ASCII.GetBytes("REDIS"));
        }

        public static bool IsRedisFile(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".rdb") return IsRedisRdb(path);
            return ContainsAscii(path, new[] { "REDIS", "SELECT" }, 1024);
        }

        public static bool IsInnoDbFile(string path)
        {
            return ContainsBytes(path, new byte[] { 0xCE, 0xF0, 0xED, 0xFE }, 64 * 1024)
                || ContainsBytes(path, new byte[] { 0xC0, 0x64, 0xF8, 0x9A }, 64 * 1024);
        }

        public static bool IsFirebirdFile(string path)
        {
            return ContainsAscii(path, new[] { "Firebird", "InterBase" }, 8192);
        }

        public static bool IsDBaseFile(string path)
        {
            return HeaderByteIn(path, 0x03, 0x83, 0x8B, 0xCB, 0xF5, 0xFB, 0x30);
        }

        public static bool IsZip(string path)
        {
            return StartsWithBytes(path, Encoding.ASCII.GetBytes("PK\x03\x04"))
                || StartsWithBytes(path, Encoding.ASCII.GetBytes("PK\x05\x06"))
                || StartsWithBytes(path, Encoding.ASCII.GetBytes("PK\x07\x08"));
        }

        public static bool Is7z(string path)
        {
            return StartsWithBytes(path, new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C });
        }

        public static bool IsRar(string path)
        {
            return StartsWithBytes(path, new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07 });
        }

        public static bool IsGzip(string path)
        {
            return StartsWithBytes(path, new byte[] { 0x1F, 0x8B });
        }

        public static bool IsTar(string path)
        {
            return ContainsAscii(path, "ustar", 512);
        }

        public static bool IsWtfFile(string path)
        {
            return ContainsAscii(path, "WiredTiger", 4096)
                || ContainsAscii(path, new[] { "WT", "PRECISION=" }, 256);
        }

        public static bool ContainsAscii(string path, string needle, int maxHeadBytes)
        {
            return ContainsAscii(path, new[] { needle }, maxHeadBytes);
        }

        public static bool ContainsAscii(string path, string[] needles, int maxHeadBytes)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                var len = (int)Math.Min(fs.Length, maxHeadBytes);
                if (len <= 0) return false;
                var buf = new byte[len];
                int read = 0;
                while (read < buf.Length)
                {
                    int n = fs.Read(buf, read, buf.Length - read);
                    if (n <= 0) break;
                    read += n;
                }
                if (read == 0) return false;
                var hay = buf.AsSpan(0, read);
                foreach (var nd in needles)
                {
                    if (ContainsAscii(hay, nd)) return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool ContainsBytes(string path, byte[] magic, int maxHeadBytes)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                var len = (int)Math.Min(fs.Length, maxHeadBytes);
                if (len < magic.Length) return false;
                var buf = new byte[len];
                int read = 0;
                while (read < buf.Length)
                {
                    int n = fs.Read(buf, read, buf.Length - read);
                    if (n <= 0) break;
                    read += n;
                }
                if (read < magic.Length) return false;
                for (int i = 0; i <= read - magic.Length; i++)
                {
                    bool ok = true;
                    for (int j = 0; j < magic.Length; j++)
                    {
                        if (buf[i + j] != magic[j]) { ok = false; break; }
                    }
                    if (ok) return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool StartsWithBytes(string path, byte[] magic)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                if (fs.Length < magic.Length) return false;
                var buf = new byte[magic.Length];
                if (fs.Read(buf, 0, magic.Length) != magic.Length) return false;
                for (int i = 0; i < magic.Length; i++)
                {
                    if (buf[i] != magic[i]) return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool HeaderByteIn(string path, params byte[] allowed)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                int b = fs.ReadByte();
                if (b < 0) return false;
                foreach (var a in allowed)
                {
                    if (b == a) return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool ContainsAscii(ReadOnlySpan<byte> hay, string needle)
        {
            if (needle.Length == 0 || hay.Length < needle.Length) return false;
            Span<byte> nb = stackalloc byte[needle.Length];
            for (int k = 0; k < needle.Length; k++)
            {
                var c = needle[k];
                if (c >= 'A' && c <= 'Z') c = (char)(c + 32);
                nb[k] = (byte)c;
            }
            for (int i = 0; i <= hay.Length - nb.Length; i++)
            {
                bool ok = true;
                for (int j = 0; j < nb.Length; j++)
                {
                    var h = hay[i + j];
                    if (h >= (byte)'A' && h <= (byte)'Z') h = (byte)(h + 32);
                    if (h != nb[j]) { ok = false; break; }
                }
                if (ok) return true;
            }
            return false;
        }
    }
}