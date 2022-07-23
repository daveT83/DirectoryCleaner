using System.Text.RegularExpressions;

namespace DirectoryCleanup.Models
{
    public class DirectorySpecificInformation
    {
        public Regex? Regex { get; set; }
        public int MinFileCount { get; set; }
        public int MaxFileCount { get; set; }
        public DateTime OldestFile { get; set; }
        public int NumberMatched { get; set; }

    }
}
