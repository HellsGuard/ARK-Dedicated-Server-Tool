using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib
{
    public static class DebugUtils
    {
        public static async Task WriteFormatThreadSafeAsync(string format, params object[] args)
        {
            await TaskUtils.RunOnUIThreadAsync(() => Debug.WriteLine(format, args));
        }
    }
}
