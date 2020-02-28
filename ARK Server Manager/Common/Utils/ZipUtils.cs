﻿using Ionic.Zip;
using System;
using System.IO;
using System.Linq;

namespace ARK_Server_Manager.Lib.Utils
{
    public static class ZipUtils
    {
        public static bool DoesFileExist(string zipFile, string entryName)
        {
            if (string.IsNullOrWhiteSpace(zipFile))
                throw new ArgumentNullException(nameof(zipFile));
            if (string.IsNullOrWhiteSpace(entryName))
                throw new ArgumentNullException(nameof(entryName));

            if (!File.Exists(zipFile))
                throw new FileNotFoundException();

            using (var zip = ZipFile.Read(zipFile))
            {
                return zip.Entries.Any(e => Path.GetFileName(e.FileName).Equals(entryName, StringComparison.OrdinalIgnoreCase));
            }
        }

        public static int ExtractAFile(string zipFile, string entryName, string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(zipFile))
                throw new ArgumentNullException(nameof(zipFile));
            if (string.IsNullOrWhiteSpace(entryName))
                throw new ArgumentNullException(nameof(entryName));
            if (string.IsNullOrWhiteSpace(destinationPath))
                throw new ArgumentNullException(nameof(destinationPath));

            if (!File.Exists(zipFile))
                throw new FileNotFoundException();
            if (!Directory.Exists(destinationPath))
                Directory.CreateDirectory(destinationPath);

            using (var zip = ZipFile.Read(zipFile))
            {
                var selection = zip.Entries.Where(e => Path.GetFileName(e.FileName).Equals(entryName, StringComparison.OrdinalIgnoreCase));

                foreach (var entry in selection)
                {
                    entry.Extract(destinationPath, ExtractExistingFileAction.OverwriteSilently);
                }

                return selection.Count();
            }
        }

        public static int ExtractAllFiles(string zipFile, string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(zipFile))
                throw new ArgumentNullException(nameof(zipFile));
            if (string.IsNullOrWhiteSpace(destinationPath))
                throw new ArgumentNullException(nameof(destinationPath));

            if (!Directory.Exists(destinationPath))
                Directory.CreateDirectory(destinationPath);

            using (var zip = ZipFile.Read(zipFile))
            {
                zip.ExtractAll(destinationPath, ExtractExistingFileAction.OverwriteSilently);

                return zip.Entries.Count;
            }
        }

        public static void ZipAFile(string zipFile, string entryName, string content)
        {
            if (string.IsNullOrWhiteSpace(zipFile))
                throw new ArgumentNullException(nameof(zipFile));
            if (string.IsNullOrWhiteSpace(entryName))
                throw new ArgumentNullException(nameof(entryName));
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentNullException(nameof(content));

            if (!File.Exists(zipFile))
            {
                using (var zip = new ZipFile())
                {
                    zip.AddEntry(entryName, content);

                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.Default;

                    zip.Save(zipFile);
                }
            }
            else
            {
                using (var zip = ZipFile.Read(zipFile))
                {
                    zip.AddEntry(entryName, content);

                    zip.Save();
                }
            }
        }

        public static void ZipFiles(string zipFile, string[] filesToZip, string comment, bool preserveDirHierarchy = true, string directoryPathInArchive = "")
        {
            if (string.IsNullOrWhiteSpace(zipFile))
                throw new ArgumentNullException(nameof(zipFile));
            if (filesToZip == null || filesToZip.Length == 0)
                throw new ArgumentNullException(nameof(filesToZip));

            using (var zip = new ZipFile())
            {
                zip.AddFiles(filesToZip.Where(f => !string.IsNullOrWhiteSpace(f) && File.Exists(f)), preserveDirHierarchy, directoryPathInArchive);

                zip.CompressionLevel = Ionic.Zlib.CompressionLevel.Default;
                if (!string.IsNullOrWhiteSpace(comment))
                    zip.Comment = comment;

                zip.Save(zipFile);
            }
        }
    }
}
