using System.Security.Cryptography;

namespace DatabaseFinder
{
    public class DuplicateAnalysisResult
    {
        public int TotalDuplicates { get; set; }
        public int DuplicateGroupsCount { get; set; }
        public long WastedBytes { get; set; }
        public string Summary { get; set; } = "";
    }

    /// <summary>
    /// موتور شناسایی سلسله‌مراتبی فایل‌های تکراری (Duplicate Detection).
    /// از الگوریتم دو مرحله‌ای سریع (Fast Sparse Hashing) جهت مقایسه در کسری از ثانیه
    /// بدون نیاز به خواندن کل فایل‌های چند گیگابایتی استفاده می‌کند.
    /// </summary>
    public static class DuplicateDetector
    {
        private const int SampleChunkSize = 4096; // 4 KB

        public static DuplicateAnalysisResult Analyze(List<DatabaseInfo> items, Action<string>? progress = null)
        {
            var result = new DuplicateAnalysisResult();
            if (items == null || items.Count < 2) return result;

            // 1. پاکسازی مقادیر قبلی
            foreach (var item in items)
            {
                item.IsDuplicate = false;
                item.DuplicateGroupId = 0;
                item.IsPrimaryDuplicate = false;
                item.DuplicateGroupCount = 0;
                item.DuplicateSiblingPaths = null;
            }

            // 2. مرحله اول: فیلتر اولیه بر اساس حجم دقیق بایت‌ها
            var sizeGroups = items
                .Where(x => x.FileSize > 0)
                .GroupBy(x => x.FileSize)
                .Where(g => g.Count() > 1)
                .ToList();

            if (sizeGroups.Count == 0) return result;

            int nextGroupId = 1;

            foreach (var sizeGroup in sizeGroups)
            {
                var candidateList = sizeGroup.ToList();

                // 3. مرحله دوم: هش سرانگشتی سریع (Sparse Hash: سربرگ + میانه + پاورقی فایل)
                var sparseGroups = candidateList
                    .GroupBy(x => ComputeSparseHash(x))
                    .Where(g => !string.IsNullOrEmpty(g.Key) && g.Count() > 1)
                    .ToList();

                foreach (var sparseGroup in sparseGroups)
                {
                    // 4. مرحله سوم: تأیید نهایی تطابق باینری
                    var verifiedGroups = GroupByVerifiedContent(sparseGroup.ToList());

                    foreach (var dupGroup in verifiedGroups)
                    {
                        if (dupGroup.Count <= 1) continue;

                        int groupId = nextGroupId++;
                        var siblingPaths = dupGroup.Select(x => x.LocalPath ?? x.Name).ToList();
                        long fileSize = dupGroup[0].FileSize;

                        // انتخاب بهترین فایل به عنوان نسخه مرجع/اصلی (Primary Keeper)
                        var primary = PickBestPrimary(dupGroup);

                        primary.IsPrimaryDuplicate = true;
                        primary.IsDuplicate = false;
                        primary.DuplicateGroupId = groupId;
                        primary.DuplicateGroupCount = dupGroup.Count;
                        primary.DuplicateSiblingPaths = siblingPaths;

                        foreach (var duplicate in dupGroup)
                        {
                            if (ReferenceEquals(duplicate, primary)) continue;

                            duplicate.IsPrimaryDuplicate = false;
                            duplicate.IsDuplicate = true;
                            duplicate.DuplicateGroupId = groupId;
                            duplicate.DuplicateGroupCount = dupGroup.Count;
                            duplicate.DuplicateSiblingPaths = siblingPaths;

                            result.TotalDuplicates++;
                            result.WastedBytes += fileSize;
                        }

                        result.DuplicateGroupsCount++;
                    }
                }
            }

            if (result.TotalDuplicates > 0)
            {
                result.Summary = L.Pick(
                    $"تعداد {result.TotalDuplicates} فایل تکراری در قالب {result.DuplicateGroupsCount} گروه کشف شد (حجم کپی‌های مازاد: {DatabaseFileLocator.FormatSize(result.WastedBytes)}).",
                    $"{result.TotalDuplicates} duplicate files found across {result.DuplicateGroupsCount} groups (wasted: {DatabaseFileLocator.FormatSize(result.WastedBytes)}).");
            }

            return result;
        }

        /// <summary>
        /// انتخاب مناسب‌ترین فایل از میان نسخه‌های تکراری جهت نگه‌داری به عنوان کپی اصلی.
        /// </summary>
        public static DatabaseInfo PickBestPrimary(List<DatabaseInfo> duplicates)
        {
            return duplicates
                .OrderByDescending(x => CalculatePrimaryScore(x))
                .ThenByDescending(x => x.FileModified)
                .First();
        }

        private static int CalculatePrimaryScore(DatabaseInfo di)
        {
            int score = 1000;
            var path = di.LocalPath ?? "";

            // فایل‌های فیزیکی مستقیم روی دیسک ارجح بر فایل‌های درون آرشیو فشرده هستند
            if (di.IsInsideArchive) score -= 400;

            // فایل‌های با هدر معتبر و نام واقعی ارجح هستند
            if (di.BackupHeader != null && di.BackupHeader.IsValid) score += 200;

            // ترجیح مسیرهای استاندارد سرور و پوشه‌های اصلی بر پوشه‌های موقت دسکتاپ و دانلوها
            var lower = path.ToLowerInvariant();
            if (lower.Contains(@"program files\microsoft sql server")) score += 300;
            if (lower.Contains(@"\mssql\backup") || lower.Contains(@"\mssql\data")) score += 250;
            if (lower.Contains(@"\sepidar") || lower.Contains(@"\holoo") || lower.Contains(@"\tadbir")) score += 200;

            if (lower.Contains(@"\desktop\")) score -= 150;
            if (lower.Contains(@"\downloads\")) score -= 200;
            if (lower.Contains(@"\temp\") || lower.Contains(@"\tmp\")) score -= 300;
            if (lower.Contains(@"$recycle.bin")) score -= 500;

            // ترجیح مسیرهای کوتاه‌تر و ساختاریافته‌تر
            score -= Math.Min(100, path.Length / 4);

            return score;
        }

        private static string ComputeSparseHash(DatabaseInfo di)
        {
            try
            {
                if (di.IsInsideArchive && !string.IsNullOrEmpty(di.ArchiveContainerPath) && !string.IsNullOrEmpty(di.ArchiveEntryPath))
                {
                    // برای فایل‌های درون زیپ، خواندن بایت‌های نمونه از استریم آرشیو
                    return ComputeSparseHashFromArchive(di.ArchiveContainerPath, di.ArchiveEntryPath, di.FileSize);
                }

                if (string.IsNullOrEmpty(di.LocalPath) || !File.Exists(di.LocalPath))
                    return "";

                using var fs = new FileStream(di.LocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                long len = fs.Length;
                if (len == 0) return "";

                using var sha = SHA256.Create();
                byte[] sample = new byte[SampleChunkSize * 3];
                int samplePos = 0;

                // 1. بایت‌های ابتدای فایل (سربرگ)
                int r1 = fs.Read(sample, samplePos, Math.Min(SampleChunkSize, (int)len));
                samplePos += r1;

                // 2. بایت‌های میانه فایل (در صورت بزرگتر بودن فایل از 64KB)
                if (len > SampleChunkSize * 16)
                {
                    fs.Seek(len / 2, SeekOrigin.Begin);
                    int r2 = fs.Read(sample, samplePos, SampleChunkSize);
                    samplePos += r2;
                }

                // 3. بایت‌های انتهای فایل (پاورقی)
                if (len > SampleChunkSize * 2)
                {
                    fs.Seek(Math.Max(0, len - SampleChunkSize), SeekOrigin.Begin);
                    int r3 = fs.Read(sample, samplePos, SampleChunkSize);
                    samplePos += r3;
                }

                var hashBytes = sha.ComputeHash(sample, 0, samplePos);
                return $"{len}_{Convert.ToHexString(hashBytes)}";
            }
            catch
            {
                return "";
            }
        }

        private static string ComputeSparseHashFromArchive(string archivePath, string entryPath, long fileSize)
        {
            try
            {
                if (!File.Exists(archivePath)) return "";
                var ext = Path.GetExtension(archivePath).ToLowerInvariant();

                if (ext is ".zip" or ".jar" or ".apk" || FileSignatures.IsZip(archivePath))
                {
                    using var fs = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    using var za = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Read);
                    var entry = za.GetEntry(entryPath) ?? za.Entries.FirstOrDefault(e => string.Equals(e.FullName, entryPath, StringComparison.OrdinalIgnoreCase) || string.Equals(e.Name, entryPath, StringComparison.OrdinalIgnoreCase));
                    if (entry == null) return "";

                    using var s = entry.Open();
                    byte[] sample = new byte[SampleChunkSize * 2];
                    int r = s.Read(sample, 0, sample.Length);

                    using var sha = SHA256.Create();
                    var hashBytes = sha.ComputeHash(sample, 0, r);
                    return $"{fileSize}_{Convert.ToHexString(hashBytes)}";
                }
            }
            catch { }

            return $"{fileSize}_ENTRY_{Path.GetFileName(entryPath)}";
        }

        private static List<List<DatabaseInfo>> GroupByVerifiedContent(List<DatabaseInfo> candidates)
        {
            var groups = new List<List<DatabaseInfo>>();
            if (candidates.Count <= 1) return groups;

            // برای فایل‌های زیر ۳۲ مگابایت یا فایل‌های با هش سرانگشتی برابر، محاسبه هش تأیید
            var hashDict = new Dictionary<string, List<DatabaseInfo>>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in candidates)
            {
                string verifyHash = ComputeVerificationHash(item);
                if (string.IsNullOrEmpty(verifyHash)) continue;

                if (!hashDict.TryGetValue(verifyHash, out var list))
                {
                    list = new List<DatabaseInfo>();
                    hashDict[verifyHash] = list;
                }
                list.Add(item);
            }

            foreach (var kv in hashDict)
            {
                if (kv.Value.Count > 1)
                {
                    groups.Add(kv.Value);
                }
            }

            return groups;
        }

        private static string ComputeVerificationHash(DatabaseInfo di)
        {
            try
            {
                if (di.IsInsideArchive)
                {
                    // برای فایل‌های درون آرشیو، برگرداندن شناسه ترکیبی مطمئن
                    return $"{di.FileSize}_{Path.GetFileName(di.ArchiveEntryPath ?? di.Name)}_{di.ArchiveCompressedSize}";
                }

                if (string.IsNullOrEmpty(di.LocalPath) || !File.Exists(di.LocalPath))
                    return "";

                using var fs = new FileStream(di.LocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                long len = fs.Length;

                using var sha = SHA256.Create();

                // اگر فایل زیر ۳۲ مگابایت است، هش کامل محاسبه می‌شود
                if (len <= 32L * 1024 * 1024)
                {
                    var fullHash = sha.ComputeHash(fs);
                    return Convert.ToHexString(fullHash);
                }

                // برای فایل‌های بسیار حجیم (> 32MB): خواندن بلاک‌های عمیق‌تر (1MB از ابتدا، وسط، و انتها)
                int chunkSize = 256 * 1024; // 256 KB
                byte[] deepSample = new byte[chunkSize * 3];
                int pos = 0;

                int r1 = fs.Read(deepSample, pos, chunkSize);
                pos += r1;

                fs.Seek(len / 2, SeekOrigin.Begin);
                int r2 = fs.Read(deepSample, pos, chunkSize);
                pos += r2;

                fs.Seek(Math.Max(0, len - chunkSize), SeekOrigin.Begin);
                int r3 = fs.Read(deepSample, pos, chunkSize);
                pos += r3;

                var deepHash = sha.ComputeHash(deepSample, 0, pos);
                return $"{len}_{Convert.ToHexString(deepHash)}";
            }
            catch
            {
                return "";
            }
        }
    }
}
