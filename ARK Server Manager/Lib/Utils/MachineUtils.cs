using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace ARK_Server_Manager.Lib
{
    public static class MachineUtils
    {
        private const int OS_ANYSERVER = 29;

        [DllImport("shlwapi.dll", SetLastError = true, EntryPoint = "#437")]
        private static extern bool IsOS(int os);

        public static bool IsWindowsServer()
        {
            return IsOS(OS_ANYSERVER);
        }

        [DllImport("kernel32.dll", EntryPoint = "CreateSymbolicLinkW", CharSet = CharSet.Unicode)]
        public static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

        public enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }

        public static bool CreateSymLink(string link, string target, bool isDirectory = false)
        {
            return CreateSymbolicLink(link, target, isDirectory ? 1 : 0);
        }

        public static bool IsDirectorySymbolic(string path)
        {
            var pathInfo = new DirectoryInfo(path);
            return pathInfo.Exists && pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
        }

        public static bool IsFileSymbolic(string path)
        {
            var pathInfo = new FileInfo(path);
            return pathInfo.Exists && pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
        }
    }
}
