using System;
using System.IO;
using System.Linq;
using Ionic.Zip;

namespace ARK_Server_Manager.Lib.Utils
{
    public static class ZipUtils
    {
        //public static void ZipFile(string zipFile, string fileToZip)
        //{
        //    if (string.IsNullOrWhiteSpace(zipFile))
        //        throw new ArgumentNullException(nameof(zipFile));
        //    if (string.IsNullOrWhiteSpace(fileToZip))
        //        throw new ArgumentNullException(nameof(fileToZip));
        //    if (!File.Exists(fileToZip))
        //        throw new FileNotFoundException("The file to zip does not exist or could not be found.", fileToZip);
        //}

        //public static void ZipFolder(string zipFile, string folderToZip, bool recursive)
        //{
        //    if (string.IsNullOrWhiteSpace(zipFile))
        //        throw new ArgumentNullException(nameof(zipFile));
        //    if (string.IsNullOrWhiteSpace(folderToZip))
        //        throw new ArgumentNullException(nameof(folderToZip));
        //    if (!Directory.Exists(folderToZip))
        //        throw new DirectoryNotFoundException("The folder to zip does not exist or could not be found.");
        //}

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
    }
}
