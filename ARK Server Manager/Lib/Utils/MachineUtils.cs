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
    }
}
