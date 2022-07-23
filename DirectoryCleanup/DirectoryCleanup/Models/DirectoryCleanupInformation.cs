namespace DirectoryCleanup.Models
{
    public class DirectoryCleanupInformation
    {
        public string? DirectoryToCleanup { get; set; }
        public string? BackupDirectory { get; set; }
        public bool IsCleanSubdirs { get; set; }
        public bool IsCleanRootFolder { get; set; }
        public Dictionary<string, List<DirectorySpecificInformation>>? DirectoryInformation { get; set; }
    }
}
