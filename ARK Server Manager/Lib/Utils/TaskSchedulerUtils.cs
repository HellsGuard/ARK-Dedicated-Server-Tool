using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace ARK_Server_Manager.Lib
{
    public static class TaskSchedulerUtils
    {
        private const string TASK_FOLDER = "ArkServerManager";

        public static bool ScheduleAutoRestart(string taskKey, string taskSuffix, string command, TimeSpan? restartTime)
        {
            var schedulerKey = $"{TASK_FOLDER}\\AutoRestart_{taskKey}";
            if (!string.IsNullOrWhiteSpace(taskSuffix))
                schedulerKey += $"_{taskSuffix}";
            var args = $"{ServerApp.ARGUMENT_AUTORESTART}{taskKey}";

            var builder = new StringBuilder();
            builder.AppendLine($"schtasks /Delete /TN {schedulerKey} /F");

            if (restartTime.HasValue)
            {
                builder.AppendLine($"schTasks /Create /TN {schedulerKey} /TR \"'{command}' '{args}'\" /SC DAILY /ST {restartTime.Value.Hours:D2}:{restartTime.Value.Minutes:D2} {(MachineUtils.IsWindowsServer() ? "/NP" : "")} /RL HIGHEST");
                builder.AppendLine("IF ERRORLEVEL 1 EXIT 1");
            }

            builder.AppendLine("EXIT 0");

            return ScriptUtils.RunElevatedShellScript(nameof(ScheduleAutoRestart), builder.ToString());
        }

        public static bool ScheduleAutoStart(string taskKey, bool enableAutoStart, string command, string args)
        {
            var schedulerKey = $"{TASK_FOLDER}\\AutoStart_{taskKey}";

            var builder = new StringBuilder();
            builder.AppendLine($"schtasks /Delete /TN {schedulerKey} /F");

            if (enableAutoStart)
            {
                if (string.IsNullOrWhiteSpace(args))
                {
                    builder.AppendLine($"schtasks /Create /RU SYSTEM /TN {schedulerKey} /TR \"'{command}'\" /SC ONSTART /DELAY 0001:00 /NP /RL HIGHEST");
                }
                else
                {
                    builder.AppendLine($"schtasks /Create /RU SYSTEM /TN {schedulerKey} /TR \"'{command}' '{args}'\" /SC ONSTART /DELAY 0001:00 /NP /RL HIGHEST");
                }
                    
                builder.AppendLine("IF ERRORLEVEL 1 EXIT 1");                
            }

            builder.AppendLine("EXIT 0");

            return ScriptUtils.RunElevatedShellScript(nameof(ScheduleAutoStart), builder.ToString());
        }

        public static bool ScheduleAutoUpdate(string taskKey, string command, int autoUpdatePeriod)
        {
            var schedulerKey = $"{TASK_FOLDER}\\AutoUpdate_{taskKey}";
            var args = ServerApp.ARGUMENT_AUTOUPDATE;

            var builder = new StringBuilder();
            builder.AppendLine($"schtasks /Delete /TN {schedulerKey} /F");

            if (autoUpdatePeriod != 0)
            {
                var startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0).AddHours(1);

                builder.AppendLine($"schTasks /Create /TN {schedulerKey} /TR \"'{command}' '{args}'\" /SC MINUTE /MO {autoUpdatePeriod} /ST {startTime.Hour:D2}:{startTime.Minute:D2} {(MachineUtils.IsWindowsServer() ? "/NP" : "")} /RL HIGHEST");
                builder.AppendLine("IF ERRORLEVEL 1 EXIT 1");
            }

            builder.AppendLine("EXIT 0");

            return ScriptUtils.RunElevatedShellScript(nameof(ScheduleAutoUpdate), builder.ToString());
        }

        public static string ComputeKey(string folder)
        {
            try
            {
                using (var hashAlgo = MD5.Create())
                {
                    var hashStr = Encoding.UTF8.GetBytes(folder);
                    var hash = hashAlgo.ComputeHash(hashStr);

                    StringBuilder sb = new StringBuilder();
                    foreach (var b in hash)
                    {
                        // can be "x2" if you want lowercase
                        sb.Append(b.ToString("x2"));
                    }
                    return sb.ToString();
                }
            }
            catch (TargetInvocationException ex)
            {
                // Exception has been thrown by the target of an invocation. 
                // This error message seems to occur when using MD5 hash algorithm on an environment where FIPS is enabled. 
                // Swallow the exception and allow the SHA1 algorithm to be used.
                Debug.WriteLine(ex.Message);
            }

            // An error occurred using the MD5 hash, try using SHA1 instead.
            using (var hashAlgo = SHA1.Create())
            {
                var hashStr = Encoding.UTF8.GetBytes(folder);
                var hash = hashAlgo.ComputeHash(hashStr);

                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                {
                    // can be "x2" if you want lowercase
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        #region Archive Methods
        public static bool ScheduleCacheUpdater(string cacheDir, string steamCmdDir, int autoUpdatePeriod)
        {
            var schedulerKey = $"{TASK_FOLDER}\\AutoUpdateSeverCache";
            var schedulerKey2 = $"{TASK_FOLDER}\\AutoUpdateServer";

            var rootSrcPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var scriptPath = Path.Combine(rootSrcPath, "Lib", "ServerUpdater", "AutoUpdateServerCache.ps1");
            if (File.Exists(scriptPath)) File.Delete(scriptPath);

            //
            // Write the command to execute and copy mcrcon for updaters to use.
            //
            var cacheUpdateCmdPath = Path.Combine(cacheDir, "UpdateCache.cmd");
            if (File.Exists(cacheUpdateCmdPath)) File.Delete(cacheUpdateCmdPath);
            var logPath = Path.Combine(cacheDir, "UpdateCache.log");
            if (File.Exists(logPath)) File.Delete(logPath);
            //ScriptUtils.WriteCommandScript(cacheUpdateCmdPath, $"powershell -ExecutionPolicy Bypass -File \"{scriptPath}\" \"{cacheDir}\" \"{steamCmdDir}\" > \"{logPath}\"");

            PlaceMCRcon(cacheDir);

            //
            // Schedule the task
            //
            var builder = new StringBuilder();
            builder.AppendLine($"schtasks /Delete /TN {schedulerKey} /F");
            builder.AppendLine($"schtasks /Delete /TN {schedulerKey2} /F");
            //if (autoUpdatePeriod != 0)
            //{
            //    builder.AppendLine($"schTasks /Create /TN {schedulerKey} /TR \"'{cacheUpdateCmdPath}'\" /SC MINUTE /MO {autoUpdatePeriod} /NP /RL HIGHEST ");
            //    builder.AppendLine("IF ERRORLEVEL 1 EXIT 1");
            //}

            builder.AppendLine("EXIT 0");

            return ScriptUtils.RunElevatedShellScript(nameof(ScheduleCacheUpdater), builder.ToString());
        }

        public static bool ScheduleUpdates(string taskKey, int autoUpdatePeriod, string cacheDir, string installDir, string rconIP, int rconPort, string rconPass, int graceMinutes, TimeSpan? forceRestartTime)
        {
            var schedulerKey = $"{TASK_FOLDER}\\AutoUpgrade_{taskKey}";
            var forceSchedulerKey = $"{TASK_FOLDER}\\AutoUpgrade_Force_{taskKey}";

            var rootSrcPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var scriptPath = Path.Combine(rootSrcPath, "Lib", "ServerUpdater", "AutoUpdateFromCache.ps1");
            if (File.Exists(scriptPath)) File.Delete(scriptPath);

            var serverUpdateCmdPath = Path.Combine(installDir, "ShooterGame", "Saved", "Config", "WindowsServer", "UpdateServerFromCache.cmd");
            if (File.Exists(serverUpdateCmdPath)) File.Delete(serverUpdateCmdPath);
            var logPath = Path.Combine(installDir, "ShooterGame", "Saved", "Config", "WindowsServer", "UpdateServerFromCache.log");
            if (File.Exists(logPath)) File.Delete(logPath);
            //ScriptUtils.WriteCommandScript(serverUpdateCmdPath, $"powershell -ExecutionPolicy Bypass -File \"{scriptPath}\"  \"{cacheDir}\" \"{installDir}\" \"{rconIP}\" \"{rconPort}\" \"{rconPass}\" \"{graceMinutes}\" > \"{logPath}\"");

            //string forceServerUpdateCmdPath = null;
            //if (forceRestartTime.HasValue)
            //{
            //    forceServerUpdateCmdPath = Path.Combine(installDir, "ShooterGame", "Saved", "Config", "WindowsServer", "ForceUpdateServerFromCache.cmd");
            //    var cmdBuilder = new StringBuilder();
            //    cmdBuilder.AppendLine($"echo force_update > {(installDir + @"\ForceUpdate.txt").AsQuoted()} ");
            //    cmdBuilder.AppendLine($"schTasks /Run /TN {schedulerKey}");
            //    ScriptUtils.WriteCommandScript(forceServerUpdateCmdPath, cmdBuilder.ToString());
            //}

            PlaceMCRcon(cacheDir);

            var builder = new StringBuilder();
            builder.AppendLine($"schtasks /Delete /TN {schedulerKey} /F");
            builder.AppendLine($"schtasks /Delete /TN {forceSchedulerKey} /F");

            //if (autoUpdatePeriod != 0)
            //{
            //    builder.AppendLine($"schTasks /Create /TN {schedulerKey} /TR \"'{serverUpdateCmdPath}'\" /SC MINUTE /MO {autoUpdatePeriod} /NP /RL HIGHEST ");
            //    builder.AppendLine("IF ERRORLEVEL 1 EXIT 1");

            //    if(forceRestartTime.HasValue)
            //    {
            //        builder.AppendLine($"schTasks /Create /TN {forceSchedulerKey} /TR \"'{forceServerUpdateCmdPath}'\" /SC DAILY /ST {forceRestartTime.Value.Hours:D2}:{forceRestartTime.Value.Minutes:D2} /NP /RL HIGHEST");
            //        builder.AppendLine("IF ERRORLEVEL 1 EXIT 1");
            //    }
            //    builder.AppendLine($"schtasks /Run /TN {schedulerKey}");
            //    builder.AppendLine("IF ERRORLEVEL 1 EXIT 1");                
            //}

            builder.AppendLine("EXIT 0");

            return ScriptUtils.RunElevatedShellScript(nameof(ScheduleUpdates), builder.ToString());
        }

        private static bool PlaceMCRcon(string dir)
        {
            const string McrconExe = "mcrcon.exe";

            var rootSrcPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var mcrconSrcPath = Path.Combine(rootSrcPath, "Lib", "ServerUpdater", McrconExe);
            var mcrconDestPath = Path.Combine(dir, McrconExe);

            try
            {
                if (File.Exists(mcrconSrcPath)) File.Delete(mcrconSrcPath);
                if (File.Exists(mcrconDestPath)) File.Delete(mcrconDestPath);
                //if (!File.Exists(mcrconDestPath))
                //{
                //    File.Copy(mcrconSrcPath, mcrconDestPath);
                //}
            }
            catch (IOException)
            {
                return false;
            }

            return true;
        }
        #endregion
    }
}
