using System;
using System.Diagnostics;
using System.IO;

namespace ARK_Server_Manager.Lib
{
    public static class ScriptUtils
    {
        public static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        static int NextScriptId = 0;

        public static bool RunElevatedShellScript(string script)
        {
            return RunShellScript(script, withElevation: true);
        }

        public static bool RunShellScript(string script, bool withElevation = false)
        {
            string tempPath = Path.ChangeExtension(Path.GetTempFileName(), ".cmd");
            string wrapperPath = Path.ChangeExtension(Path.GetTempFileName(), ".cmd");
            string outPath = Path.ChangeExtension(Path.GetTempFileName(), ".out");
            string errorPath = Path.ChangeExtension(Path.GetTempFileName(), ".error");

            _logger.Debug($"Running Script (Elevation {withElevation}) : {script}");

            var scriptId = NextScriptId++;
            try
            {
                File.WriteAllText(tempPath, script);
                File.WriteAllText(wrapperPath, $"CMD /C \"{tempPath}\" > \"{outPath}\" 2> \"{errorPath}\"");
                ProcessStartInfo psInfo = new ProcessStartInfo()
                {
                    FileName = $"\"{wrapperPath}\"",
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                if(withElevation)
                {
                    psInfo.Verb = "runas";
                }

                var process = new Process
                {
                    EnableRaisingEvents = true,                   
                    StartInfo = psInfo,                    
                };

                process.Start();
                process.WaitForExit();

                try
                {
                    _logger.Debug($"SCRIPT {scriptId} OUTPUT: {File.ReadAllText(outPath)}");
                    _logger.Debug($"SCRIPT {scriptId} ERROR: {File.ReadAllText(errorPath)}");
                }
                catch { }

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
                File.Delete(wrapperPath);
                File.Delete(outPath);
                File.Delete(errorPath);
            }
        }

    }
}
