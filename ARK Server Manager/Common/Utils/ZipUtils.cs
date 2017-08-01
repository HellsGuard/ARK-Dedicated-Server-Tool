using System;
using System.IO;
using System.Linq;
using Ionic.Zip;

namespace ARK_Server_Manager.Lib.Utils
{
    public static class ZipUtils
    {
        public static void ZipFiles(string zipFile, string[] filesToZip, string comment)
        {
            if (string.IsNullOrWhiteSpace(zipFile))
                throw new ArgumentNullException(nameof(zipFile));
            if (filesToZip == null || filesToZip.Length == 0)
                throw new ArgumentNullException(nameof(filesToZip));

            using (var zip = new ZipFile())
            {
                zip.AddFiles(filesToZip.Where(f => !string.IsNullOrWhiteSpace(f) && File.Exists(f)), true, string.Empty);

                zip.CompressionLevel = Ionic.Zlib.CompressionLevel.Default;
                if (!string.IsNullOrWhiteSpace(comment))
                    zip.Comment = comment;

                zip.Save(zipFile);
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
    }
}
