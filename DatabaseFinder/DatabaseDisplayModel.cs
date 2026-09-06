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
    }
}