using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ARK_Server_Manager.Lib
{
    public static class ScriptUtils
    {
        public static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        static int NextScriptId = 0;

        public static bool RunElevatedShellScript(string scriptName, string script)
        {
            return RunShellScript(scriptName, script, withElevation: true);
        }

        public static bool RunShellScript(string scriptName, string script, bool withElevation = false, bool waitForExit = true, bool deleteOnExit = true, bool includePID = false)
        {
            var scriptNameBase = includePID ? $"{scriptName}_{Process.GetCurrentProcess().Id}" : scriptName;
           
            string baseScriptPath = Path.Combine(Path.GetTempPath(), $"{scriptNameBase}.cmd");
            string scriptWrapperPath = Path.Combine(Path.GetTempPath(), $"{scriptNameBase}_wrapper.cmd");

            string scriptLogPath = null;
            scriptLogPath = Path.ChangeExtension(baseScriptPath, ".out");

            _logger.Debug($"Running Script (Elevation {withElevation}) : {script}");

            var scriptId = NextScriptId++;
            try
            {
                WriteCommandScript(baseScriptPath, script);

                //
                // Wrap to capture logging (necessary for running administrator scripts from non-admin contexts)
                //
                WriteCommandScript(scriptWrapperPath, $"CMD /C {baseScriptPath.AsQuoted()} > {scriptLogPath.AsQuoted()} 2>&1");

                //
                // Launch the process
                //
                ProcessStartInfo psInfo = new ProcessStartInfo()
                {
                    FileName = $"{scriptWrapperPath.AsQuoted()}",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    Verb = withElevation ? "runas" : String.Empty
                };

                var process = new Process
                {
                    EnableRaisingEvents = true,
                    StartInfo = psInfo,
                };

                process.Start();

                //
                // If we wait, copy the log files when the process is done.
                //
                if (waitForExit)
                {
                    process.WaitForExit();

                    try
                    {
                        _logger.Debug($"SCRIPT {scriptId} OUTPUT: {File.ReadAllText(scriptLogPath)}");
                    }
                    catch { }

                    return process.ExitCode == 0;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to run elevated script: {0}", ex.Message);
                Debugger.Break();
                return false;
            }
            finally
            {
                //
                // If we aren't waiting, we can't delete because we will kill the scripts before cmd.exe gets a chance to run them.
                //
                if (waitForExit && deleteOnExit)
                {                    
                    File.Delete(baseScriptPath);
                    File.Delete(scriptWrapperPath);
                    File.Delete(scriptLogPath);
                }
            }
        }

        public static void WriteCommandScript(string destinationPath, string script)
        {
            var builder = new StringBuilder();

            // Change to the UTF8 code page
            builder.AppendLine("chcp 65001");
            builder.Append(script);

            File.WriteAllText(destinationPath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        public static string AsQuoted(this string parameter)
        {
            return "\"" + parameter + "\"";
        }
    }
}
