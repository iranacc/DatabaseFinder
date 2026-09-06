using System.Text.Json;

namespace DatabaseFinder
{
    public class AppSettings
    {
        public Dictionary<string, int> CustomPorts { get; set; } = new();
        public bool MinimizeToTray { get; set; } = true;
        public bool AutoRefresh { get; set; } = false;
        public int AutoRefreshIntervalSec { get; set; } = 30;
        public bool ShowNotifications { get; set; } = true;

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DatabaseFinder", "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }

        public int GetPort(DatabaseType type, int defaultPort)
        {
            var key = type.ToString();
            return CustomPorts.TryGetValue(key, out int p) ? p : defaultPort;
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
        private static readonly string ProfilesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DatabaseFinder", "profiles.json");

        public static List<DatabaseProfile> Load()
        {
            try
            {
                if (File.Exists(ProfilesPath))
                {
                    var json = File.ReadAllText(ProfilesPath);
                    return JsonSerializer.Deserialize<List<DatabaseProfile>>(json) ?? new List<DatabaseProfile>();
                }
            }
            catch { }
            return new List<DatabaseProfile>();
        }

        public static void Save(List<DatabaseProfile> profiles)
        {
            try
            {
                var dir = Path.GetDirectoryName(ProfilesPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ProfilesPath, json);
            }
            catch { }
        }
    }
}
