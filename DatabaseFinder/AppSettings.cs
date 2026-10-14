using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DatabaseFinder
{
    public class AppSettings
    {
        public string Language { get; set; } = "fa";
        public Dictionary<string, int> CustomPorts { get; set; } = new();
        public bool MinimizeToTray { get; set; } = true;
        public bool AutoRefresh { get; set; } = false;
        public int AutoRefreshIntervalSec { get; set; } = 30;
        public bool ShowNotifications { get; set; } = true;
        public bool DarkMode { get; set; } = false;

        private static string SettingsPath => AppPaths.SettingsPath;

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                    loaded.Normalize();
                    return loaded;
                }
            }
            catch (Exception ex) { AppLog.Write("Settings.Load", ex); }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                Normalize();
                var dir = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex) { AppLog.Write("Settings.Save", ex); }
        }

        public int GetPort(DatabaseType type, int defaultPort)
        {
            var key = type.ToString();
            return CustomPorts.TryGetValue(key, out int p) && p is >= 1 and <= 65535 ? p : defaultPort;
        }

        private void Normalize()
        {
            CustomPorts ??= new Dictionary<string, int>();
            AutoRefreshIntervalSec = Math.Clamp(AutoRefreshIntervalSec, 10, 600);
        }
    }

    public class DatabaseProfile
    {
        public string Name { get; set; } = "";
        public DatabaseType Type { get; set; }
        public string Host { get; set; } = "localhost";
        public int Port { get; set; }
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string DatabaseName { get; set; } = "";

        public string TypeDisplayName
        {
            get
            {
                return new DatabaseInfo { Type = Type }.TypeDisplayName;
            }
        }
    }

    public static class ProfileManager
    {
        private const string DpapiPrefix = "dpapi:";
        private static string ProfilesPath => AppPaths.ProfilesPath;

        private static string EncryptPassword(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return "";
            if (plain.StartsWith(DpapiPrefix, StringComparison.Ordinal)) return plain;
            try
            {
                var bytes = Encoding.UTF8.GetBytes(plain);
                var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
                return DpapiPrefix + Convert.ToBase64String(encrypted);
            }
            catch
            {
                return plain;
            }
        }

        private static string DecryptPassword(string cipher)
        {
            if (string.IsNullOrEmpty(cipher)) return "";
            if (!cipher.StartsWith(DpapiPrefix, StringComparison.Ordinal)) return cipher;
            try
            {
                var encrypted = Convert.FromBase64String(cipher.Substring(DpapiPrefix.Length));
                var bytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return "";
            }
        }

        public static List<DatabaseProfile> Load()
        {
            try
            {
                if (File.Exists(ProfilesPath))
                {
                    var json = File.ReadAllText(ProfilesPath);
                    var list = JsonSerializer.Deserialize<List<DatabaseProfile>>(json) ?? new List<DatabaseProfile>();
                    foreach (var p in list)
                    {
                        p.Password = DecryptPassword(p.Password);
                    }
                    return list;
                }
            }
            catch (Exception ex) { AppLog.Write("Profiles.Load", ex); }
            return new List<DatabaseProfile>();
        }

        public static void Save(List<DatabaseProfile> profiles)
        {
            try
            {
                var dir = Path.GetDirectoryName(ProfilesPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                var toSave = profiles.Select(p => new DatabaseProfile
                {
                    Name = p.Name,
                    Type = p.Type,
                    Host = p.Host,
                    Port = p.Port,
                    Username = p.Username,
                    Password = EncryptPassword(p.Password),
                    DatabaseName = p.DatabaseName
                }).ToList();
                var json = JsonSerializer.Serialize(toSave, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ProfilesPath, json);
            }
            catch (Exception ex) { AppLog.Write("Profiles.Save", ex); }
        }
    }
}
