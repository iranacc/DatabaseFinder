namespace DatabaseFinder
{
    public class DatabaseDisplayModel
    {
        public string TypeDisplayName { get; set; } = "";
        public string Port { get; set; } = "";
        public string ServiceName { get; set; } = "";
        public string ProcessDisplay { get; set; } = "";
        public string DetectionMethod { get; set; } = "";
        public string Version { get; set; } = "";
        public string HostAddress { get; set; } = "localhost";
        public bool Selected { get; set; }

        // حالت آفلاین / اسکن هارد
        public bool IsOnline { get; set; } = true;
        public bool IsBackup { get; set; }
        public string DisplayName { get; set; } = "";
        public string Location { get; set; } = "";
        public string SizeInfo { get; set; } = "";
    }
}