using System;
using System.Diagnostics;
using System.IO;

namespace ARK_Server_Manager.Lib
{
    public static class ScriptUtils
    {
        public static bool RunElevatedShellScript(string script)
        {
            return RunShellScript(script, withElevation: true);
        }

        public static bool RunShellScript(string script, bool withElevation = false)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetRandomFileName(), ".cmd"));
            try
            {
                File.WriteAllText(tempPath, script);
                ProcessStartInfo psInfo = new ProcessStartInfo()
                {
                    FileName = tempPath                    
                };

                if(withElevation)
                {
                    psInfo.Verb = "runas";
                }

                var process = new Process
                {
                    EnableRaisingEvents = true,
                    StartInfo = psInfo
                };

                process.Start();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to run elevated script: {0}", ex.Message);
                Debugger.Break();
                return false;
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

    }
}
