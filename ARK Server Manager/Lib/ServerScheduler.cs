using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib
{
    public static class ServerScheduler
    {
        private const string TaskFolder = "\\ArkServerManager";

        private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
   
        public static bool ScheduleCacheUpdater(string cacheDir, string steamCmdDir, int autoUpdatePeriod)
        {
            var schedulerKey = $"ArkServerManager\\AutoUpdateSeverCache";
            var rootSrcPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var scriptPath = Path.Combine(rootSrcPath, "Lib", "ServerUpdater", "AutoUpdateServerCache.ps1");
            var logPath = Path.Combine(cacheDir, "UpdateCache.log");

            //
            // Write the command to execute and copy mcrcon for updaters to use.
            //
            var cacheUpdateCmdPath = Path.Combine(cacheDir, "UpdateCache.cmd");
            File.WriteAllText(cacheUpdateCmdPath, $"powershell -ExecutionPolicy Bypass -File \"{scriptPath}\" \"{cacheDir}\" \"{steamCmdDir}\" > \"{logPath}\"");
            PlaceMCRcon(cacheDir);

            //
            // Schedule the task
            //
            var builder = new StringBuilder();
            builder.AppendLine($"schtasks /Delete /TN {schedulerKey} /F");
            if (autoUpdatePeriod != 0)
            {
                builder.AppendLine($"schTasks /Create /TN {schedulerKey} /TR \"'{cacheUpdateCmdPath}'\" /SC MINUTE /MO {autoUpdatePeriod} /NP /RL LIMITED ");
                builder.AppendLine("IF ERRORLEVEL 1 EXIT 1");
            }

            builder.AppendLine("EXIT 0");
            var script = builder.ToString();
            
            return ScriptUtils.RunElevatedShellScript(script);
        }

        public static bool ScheduleUpdates(string updateKey, int autoUpdatePeriod, string cacheDir, string installDir, string rconIP, int rconPort, string rconPass, int graceMinutes)
        {
            var schedulerKey = $"ArkServerManager\\AutoUpgrade_{updateKey}";

            var rootSrcPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var scriptPath = Path.Combine(rootSrcPath, "Lib", "ServerUpdater", "AutoUpdateFromCache.ps1");
            
            var serverUpdateCmdPath = Path.Combine(installDir, "ShooterGame", "Saved", "Config", "WindowsServer", "UpdateServerFromCache.cmd");
            var logPath = Path.Combine(installDir, "ShooterGame", "Saved", "Config", "WindowsServer", "UpdateServerFromCache.log");
            File.WriteAllText(serverUpdateCmdPath, $"powershell -ExecutionPolicy Bypass -File \"{scriptPath}\"  \"{cacheDir}\" \"{installDir}\" \"{rconIP}\" \"{rconPort}\" \"{rconPass}\" \"{graceMinutes}\" > \"{logPath}\"");
            PlaceMCRcon(cacheDir);

            var builder = new StringBuilder();
            builder.AppendLine($"schtasks /Delete /TN {schedulerKey} /F");
            if (autoUpdatePeriod != 0)
            {
                builder.AppendLine($"schTasks /Create /TN {schedulerKey} /TR \"'{serverUpdateCmdPath}'\" /SC MINUTE /MO {autoUpdatePeriod} /NP /RL LIMITED ");
                builder.AppendLine("IF ERRORLEVEL 1 EXIT 1");
                builder.AppendLine($"schtasks /Run /TN {schedulerKey}");
                builder.AppendLine("IF ERRORLEVEL 1 EXIT 1");                
            }

            builder.AppendLine("EXIT 0");
            var script = builder.ToString();
            return ScriptUtils.RunElevatedShellScript(script);
        }

        public static bool ScheduleAutoStart(string updateKey, bool enableAutoStart, string command, string args)
        {
            var schedulerKey = $"ArkServerManager\\AutoStart_{updateKey}";
            var builder = new StringBuilder();
            builder.AppendLine($"schtasks /Delete /TN {schedulerKey} /F");
            if (enableAutoStart)
            {
                builder.AppendLine($"schtasks /Create /TN {schedulerKey} /TR \"'{command}' '{args}'\" /SC ONSTART /DELAY 0001:00 /NP /RL LIMITED");
                builder.AppendLine("IF ERRORLEVEL 1 EXIT 1");                
            }

            builder.AppendLine("EXIT 0");
            var script = builder.ToString();
            bool result = ScriptUtils.RunElevatedShellScript(script);
            return result;
        }
        private static bool PlaceMCRcon(string dir)
        {
            var rootSrcPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            const string McrconExe = "mcrcon.exe";
            var mcrconSrcPath = Path.Combine(rootSrcPath, "Lib", "ServerUpdater", McrconExe);
            var mcrconDestPath = Path.Combine(dir, McrconExe);
            try
            {
                if (!File.Exists(mcrconDestPath))
                {
                    File.Copy(mcrconSrcPath, mcrconDestPath);
                }
            }
            catch (IOException)
            {
                return false;
            }

            return true;
        }
    }
}
