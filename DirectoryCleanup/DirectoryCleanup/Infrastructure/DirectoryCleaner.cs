using DirectoryCleanup.Models;
using System.Text.RegularExpressions;

namespace DirectoryCleanup.Infrastructure
{
    public class DirectoryCleaner
    {
        public DirectoryCleanupInformation DirectoryCleanupInformation { get; private set; }
        public int DefaultMinFiles { get; set; }
        public int DefaultMaxFiles { get; set; }
        public DateTime DefaultOldestFile { get; set; }
        public bool IsLowMemoryMode { get; set; }

        public DirectoryCleaner(string directoryToClean, string? backupDirectopy = null)
        {
            DirectoryCleanupInformation = new DirectoryCleanupInformation();

            DirectoryCleanupInformation.IsCleanRootFolder = true;
            DirectoryCleanupInformation.IsCleanSubdirs = true;
            DirectoryCleanupInformation.DirectoryToCleanup = directoryToClean;
            DirectoryCleanupInformation.BackupDirectory = backupDirectopy;
            DefaultMaxFiles = int.MaxValue;
            DefaultMinFiles = 0;
            DefaultOldestFile = DateTime.Now;
            IsLowMemoryMode = false;
            DirectoryCleanupInformation.DirectoryInformation = new Dictionary<string, List<DirectorySpecificInformation>>();
        }

        /// <summary>
        /// Set IsCleanRootFolder property
        /// </summary>
        /// <param name="isCleanRootFolder"></param>
        public void SetIsCleanRootFolder(bool isCleanRootFolder)
        {
            DirectoryCleanupInformation.IsCleanRootFolder = isCleanRootFolder;
        }

        /// <summary>
        /// Set IsCleanSubdirs property
        /// </summary>
        /// <param name="isCleanSubDirs"></param>
        public void SetIsCleanSubdirs(bool isCleanSubDirs)
        {
            DirectoryCleanupInformation.IsCleanSubdirs = isCleanSubDirs;
        }

        /// <summary>
        /// Adds a new directory specific entry to the list
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="regex"></param>
        /// <param name="minFileCount"></param>
        /// <param name="maxFileCount"></param>
        /// <param name="oldestFile"></param>
        public DirectorySpecificInformation AddDirectorySpecificInformation(string directory, Regex regex, int minFileCount, int maxFileCount, DateTime oldestFile, int numberMatched = 0)
        {
            DirectorySpecificInformation dsi = new DirectorySpecificInformation()
            {
                Regex = regex
                ,
                MaxFileCount = maxFileCount
                ,
                MinFileCount = minFileCount
                ,
                OldestFile = oldestFile
                ,
                NumberMatched = numberMatched
            };

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            if (DirectoryCleanupInformation.DirectoryInformation.ContainsKey(directory))
            {
                DirectoryCleanupInformation.DirectoryInformation[directory].Add(dsi);
            }
            else
            {
                DirectoryCleanupInformation.DirectoryInformation.Add(directory, new List<DirectorySpecificInformation>() { dsi });
            }

            return dsi;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }

        /// <summary>
        /// Cleans the directory
        /// </summary>
        public void Cleanup()
        {
            bool isBackup = false;
#pragma warning disable CS8604 // Possible null reference argument.
            DirectoryInfo directoryInfo = new DirectoryInfo(DirectoryCleanupInformation.DirectoryToCleanup);
#pragma warning restore CS8604 // Possible null reference argument.

            if (!String.IsNullOrEmpty(DirectoryCleanupInformation.BackupDirectory) && !String.IsNullOrWhiteSpace(DirectoryCleanupInformation.BackupDirectory))
            {
                isBackup = true;
            }

            if (DirectoryCleanupInformation.IsCleanSubdirs)
            {
                foreach (DirectoryInfo directory in directoryInfo.EnumerateDirectories("*", SearchOption.AllDirectories))
                {
                    Console.WriteLine(directory.FullName);
                    CleanupFiles(directory, isBackup);

                    if (IsLowMemoryMode && DirectoryCleanupInformation.DirectoryInformation.ContainsKey(directory.FullName))
                    {
                        DirectoryCleanupInformation.DirectoryInformation.Remove(directory.FullName);
                    }
                }
            }

            if (DirectoryCleanupInformation.IsCleanRootFolder)
            {
                CleanupFiles(directoryInfo, isBackup);
            }

            if (IsLowMemoryMode)
            {
                DirectoryCleanupInformation = new DirectoryCleanupInformation();
            }
        }

        /// <summary>
        /// Cleans up a given directory
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="isBackup"></param>
        private void CleanupFiles(DirectoryInfo directory, bool isBackup)
        {
            List<DirectorySpecificInformation>? directorySpecificInformations = null;
            bool isMatched;

            if (DirectoryCleanupInformation.DirectoryInformation.ContainsKey(directory.FullName))
            {
                directorySpecificInformations = DirectoryCleanupInformation.DirectoryInformation[directory.FullName];
            }

            foreach (FileInfo file in directory.EnumerateFiles("*", SearchOption.TopDirectoryOnly).OrderByDescending(x => x.LastWriteTime))
            {
                isMatched = false;

                if (directorySpecificInformations != null)
                {
                    foreach (DirectorySpecificInformation dsi in directorySpecificInformations)
                    {
                        if (dsi.Regex.IsMatch(file.Name))
                        {
                            if (IsDeleteFile(dsi, file.LastWriteTime, directory.FullName))
                            {
                                DeleteFile(file, isBackup);
                                isMatched = true;
                            }
                            dsi.NumberMatched++;
                            break;
                        }
                    }

                }

                if (!isMatched)
                {
                    if (IsDeleteFile(AddDirectorySpecificInformation(directory.FullName, BuildRegex(file.Name, file.Extension), DefaultMinFiles, DefaultMaxFiles, DefaultOldestFile, 1), file.LastWriteTime, directory.FullName))
                    {
                        DeleteFile(file, isBackup);
                    }
                    directorySpecificInformations = DirectoryCleanupInformation.DirectoryInformation[directory.FullName];
                }
            }
        }

        /// <summary>
        /// Determines if the file is old enough to delete
        /// </summary>
        /// <param name="lastWriteTime"></param>
        /// <param name="oldestFile"></param>
        /// <returns></returns>
        private bool IsFileOldEnough(DateTime lastWriteTime, DateTime oldestFile)
        {
            if (lastWriteTime.Subtract(oldestFile).TotalMilliseconds < 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Should the file be deleted
        /// </summary>
        /// <param name="dsi"></param>
        /// <param name="lastWriteTime"></param>
        /// <param name="directory"></param>
        /// <returns></returns>
        private bool IsDeleteFile(DirectorySpecificInformation dsi, DateTime lastWriteTime, string directory)
        {
            if (IsFileOldEnough(lastWriteTime, dsi.OldestFile) && dsi.NumberMatched >= dsi.MinFileCount)
            {
                return true;
            }
            else if (dsi.NumberMatched >= dsi.MaxFileCount)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Builds a regular expression based on a filename and extension. All letters and digits are generalized. If you wantthis implemented differnetly you can override this method.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileExtension"></param>
        /// <returns></returns>
        public virtual Regex BuildRegex(string fileName, string fileExtension)
        {
            int digitCount = 0;
            int letterCount = 0;
            string regexString = "";

            foreach (char c in fileName.Replace(fileExtension, "").ToCharArray())
            {
                if (Char.IsDigit(c))
                {
                    if (letterCount > 0)
                    {
                        regexString += "\\w{" + letterCount + "}";
                        letterCount = 0;
                    }
                    digitCount++;
                }
                else if (Char.IsLetter(c))
                {
                    if (digitCount > 0)
                    {
                        regexString += "\\d{" + digitCount + "}";
                        digitCount = 0;
                    }
                    letterCount++;
                }
                else
                {

                    if (letterCount > 0)
                    {
                        regexString += "\\w{" + letterCount + "}";
                        letterCount = 0;
                    }
                    else if (digitCount > 0)
                    {
                        regexString += "\\d{" + digitCount + "}";
                        digitCount = 0;
                    }
                    regexString += c;
                }
            }

            if (letterCount > 0)
            {
                regexString += "\\w{" + letterCount + "}";
            }
            else if (digitCount > 0)
            {
                regexString += "\\d{" + digitCount + "}";
            }
            regexString += fileExtension;

            return new Regex(regexString);
        }

        /// <summary>
        /// Deletes or moves a file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="isBackup"></param>
        private void DeleteFile(FileInfo file, bool isBackup)
        {
            if (isBackup)
            {
#pragma warning disable CS8604 // Possible null reference argument.
                file.MoveTo(Path.Combine(DirectoryCleanupInformation.BackupDirectory, file.Directory.FullName.Replace(DirectoryCleanupInformation.DirectoryToCleanup, "")));
#pragma warning restore CS8604 // Possible null reference argument.
            }
            else
            {
                file.Delete();
            }
        }
    }
}
