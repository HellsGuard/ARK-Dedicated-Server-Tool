using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WPFSharp.Globalizer;

namespace ARK_Server_Manager
{
    public static class ErrorHandling
    {
        public static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Oops!  Bad news everyone - the app is going down!
            // Write out a log file with all the details so users can send us the info...
            
            string file = Path.GetTempFileName();
            string crashFile = file + ".dmp";
            try
            {
                MiniDumpToFile(crashFile);
            }
            finally
            {

                var exception = e.ExceptionObject as Exception;
                var details = new StringBuilder();
                details.AppendLine("ARK Server Manager Crash Report");
                details.AppendLine("Please report this crash to ChronosWS or HellsGuard on Steam").AppendLine();
                details.Append("Assembly: ").Append(Assembly.GetExecutingAssembly().ToString()).AppendLine();
                details.Append("Crash Dump: ").AppendLine(crashFile);
                details.AppendLine("Exception Message:");
                details.AppendLine(exception.Message).AppendLine();
                details.AppendLine("Stack Trace:");
                details.AppendLine(exception.StackTrace);
                File.WriteAllText(file, details.ToString());

                var result = MessageBox.Show(String.Format(@"
OOPS!  ARK Server Manager has suffered from an internal error and must shut down.
This is probably a bug and should be reported.  The error files are below:
Error File: {0}
Crash Dump: {1}
Please send this file to ChronosWS or HellsGuard on Steam.  The crash log
will now be opened in notepad.
", file, crashFile), "ARK Server Manager crashed", MessageBoxButton.OK, MessageBoxImage.Exclamation);


                if (result == MessageBoxResult.OK)
                {
                    Process.Start("notepad.exe", file);
                }
            }
        }

        internal enum MINIDUMP_TYPE
        {
            MiniDumpNormal = 0x00000000,
            MiniDumpWithDataSegs = 0x00000001,
            MiniDumpWithFullMemory = 0x00000002,
            MiniDumpWithHandleData = 0x00000004,
            MiniDumpFilterMemory = 0x00000008,
            MiniDumpScanMemory = 0x00000010,
            MiniDumpWithUnloadedModules = 0x00000020,
            MiniDumpWithIndirectlyReferencedMemory = 0x00000040,
            MiniDumpFilterModulePaths = 0x00000080,
            MiniDumpWithProcessThreadData = 0x00000100,
            MiniDumpWithPrivateReadWriteMemory = 0x00000200,
            MiniDumpWithoutOptionalData = 0x00000400,
            MiniDumpWithFullMemoryInfo = 0x00000800,
            MiniDumpWithThreadInfo = 0x00001000,
            MiniDumpWithCodeSegs = 0x00002000
        }
        [DllImport("dbghelp.dll")]
        static extern bool MiniDumpWriteDump(
            IntPtr hProcess,
            Int32 ProcessId,
            IntPtr hFile,
            MINIDUMP_TYPE DumpType,
            IntPtr ExceptionParam,
            IntPtr UserStreamParam,
            IntPtr CallackParam);

        public static void MiniDumpToFile(String fileToDump)
        {
            FileStream fsToDump = null;
            fsToDump = File.Create(fileToDump);

            Process thisProcess = Process.GetCurrentProcess();
            MiniDumpWriteDump(thisProcess.Handle, 
                              thisProcess.Id,
                              fsToDump.SafeFileHandle.DangerousGetHandle(),
                              MINIDUMP_TYPE.MiniDumpWithFullMemory,
                              IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            fsToDump.Close();
        }
    }
}
