using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32.TaskScheduler;

namespace ARK_Server_Manager.Lib
{
    public static class TaskSchedulerUtils
    {
        private const string TASK_FOLDER = "ArkServerManager";
        private const int EXECUTION_TIME_LIMIT = 3;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public enum ShutdownType
        {
            Shutdown1,
            Shutdown2,
        }

        public static bool ScheduleAutoShutdown(string taskKey, string taskSuffix, string command, TimeSpan? restartTime, string profileName, ShutdownType type)
        {
            var taskName = $"AutoShutdown_{taskKey}";
            if (!string.IsNullOrWhiteSpace(taskSuffix))
                taskName += $"_{taskSuffix}";

            var taskFolder = TaskService.Instance.RootFolder.SubFolders.Exists(TASK_FOLDER) ? TaskService.Instance.RootFolder.SubFolders[TASK_FOLDER] : null;

            if (restartTime.HasValue)
            {
                // create the task folder
                if (taskFolder == null)
                {
                    try
                    {
                        taskFolder = TaskService.Instance.RootFolder.CreateFolder(TASK_FOLDER, null, false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug($"Unable to create the ASM task folder.\r\n{ex.Message}.");
                        return false;
                    }
                }

                if (taskFolder == null)
                    return false;

                var task = taskFolder.Tasks.Exists(taskName) ? taskFolder.Tasks[taskName] : null;
                var taskDefinition = task?.Definition ?? TaskService.Instance.NewTask();

                if (taskDefinition == null)
                    return false;

                Version appVersion;
                Version.TryParse(App.Version, out appVersion);

                taskDefinition.Principal.LogonType = TaskLogonType.InteractiveToken;
                taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;

                taskDefinition.RegistrationInfo.Description = $"Ark Server Auto-Shutdown - {profileName}";
                taskDefinition.RegistrationInfo.Source = "Ark Server Manager";
                taskDefinition.RegistrationInfo.Version = appVersion;

                taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.FromHours(EXECUTION_TIME_LIMIT);
                taskDefinition.Settings.Priority = ProcessPriorityClass.Normal;

                // Add a trigger that will fire every day at the specified restart time
                taskDefinition.Triggers.Clear();
                var trigger = new DailyTrigger {
                                  StartBoundary = DateTime.Today.Add(restartTime.Value),
                                  ExecutionTimeLimit = TimeSpan.FromHours(EXECUTION_TIME_LIMIT)
                              };
                taskDefinition.Triggers.Add(trigger);

                // Create an action that will launch whenever the trigger fires
                var arguments = string.Empty;
                switch (type)
                {
                    case ShutdownType.Shutdown1:
                        arguments = App.ARG_AUTOSHUTDOWN1;
                        break;
                    case ShutdownType.Shutdown2:
                        arguments = App.ARG_AUTOSHUTDOWN2;
                        break;
                    default:
                        return false;
                }

                taskDefinition.Actions.Clear();
                var action = new ExecAction {
                                 Path = command,                                 
                                 Arguments = $"{arguments}{taskKey}"
                             };
                taskDefinition.Actions.Add(action);

                try
                {
                    task = taskFolder.RegisterTaskDefinition(taskName, taskDefinition, TaskCreation.CreateOrUpdate, null, null, TaskLogonType.InteractiveToken);
                    return task != null;
                }
                catch (Exception ex)
                {
                    Logger.Debug($"Unable to create the ScheduleAutoShutdown task.\r\n{ex.Message}.");
                }
            }
            else
            {
                if (taskFolder == null)
                    return true;

                // Retrieve the task to be deleted
                var task = taskFolder.Tasks.Exists(taskName) ? taskFolder.Tasks[taskName] : null;
                if (task == null)
                    return true;

                try
                {
                    // Delete the task
                    taskFolder.DeleteTask(taskName, false);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Debug($"Unable to delete the ScheduleAutoShutdown task.\r\n{ex.Message}.");
                }
            }

            return false;
        }

        public static bool ScheduleAutoStart(string taskKey, string taskSuffix, bool enableAutoStart, string command, string profileName)
        {
            var taskName = $"AutoStart_{taskKey}";
            if (!string.IsNullOrWhiteSpace(taskSuffix))
                taskName += $"_{taskSuffix}";

            var taskFolder = TaskService.Instance.RootFolder.SubFolders.Exists(TASK_FOLDER) ? TaskService.Instance.RootFolder.SubFolders[TASK_FOLDER] : null;

            if (enableAutoStart)
            {
                // create the task folder
                if (taskFolder == null)
                {
                    try
                    {
                        taskFolder = TaskService.Instance.RootFolder.CreateFolder(TASK_FOLDER, null, false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug($"Unable to create the ASM task folder.\r\n{ex.Message}.");
                        return false;
                    }
                }

                if (taskFolder == null)
                    return false;

                var task = taskFolder.Tasks.Exists(taskName) ? taskFolder.Tasks[taskName] : null;
                var taskDefinition = task?.Definition ?? TaskService.Instance.NewTask();

                if (taskDefinition == null)
                    return false;

                Version appVersion;
                Version.TryParse(App.Version, out appVersion);

                taskDefinition.Principal.LogonType = TaskLogonType.ServiceAccount;
                taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;

                taskDefinition.RegistrationInfo.Description = $"Ark Server Auto-Start - {profileName}";
                taskDefinition.RegistrationInfo.Source = "Ark Server Manager";
                taskDefinition.RegistrationInfo.Version = appVersion;

                taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.FromHours(EXECUTION_TIME_LIMIT);
                taskDefinition.Settings.Priority = ProcessPriorityClass.Normal;

                // Add a trigger that will fire after the machine has started
                taskDefinition.Triggers.Clear();
                var trigger = new BootTrigger {
                                  Delay = TimeSpan.FromMinutes(1),
                                  ExecutionTimeLimit = TimeSpan.FromHours(EXECUTION_TIME_LIMIT)
                              };
                taskDefinition.Triggers.Add(trigger);

                // Create an action that will launch whenever the trigger fires
                taskDefinition.Actions.Clear();
                var action = new ExecAction {
                                 Path = command,
                                 Arguments = string.Empty
                             };
                taskDefinition.Actions.Add(action);

                try
                {
                    task = taskFolder.RegisterTaskDefinition(taskName, taskDefinition, TaskCreation.CreateOrUpdate, "SYSTEM", null, TaskLogonType.ServiceAccount);
                    return task != null;
                }
                catch (Exception ex)
                {
                    Logger.Debug($"Unable to create the ScheduleAutoStart task.\r\n{ex.Message}.");
                }
            }
            else
            {
                if (taskFolder == null)
                    return true;

                // Retrieve the task to be deleted
                var task = taskFolder.Tasks.Exists(taskName) ? taskFolder.Tasks[taskName] : null;
                if (task == null)
                    return true;

                try
                {
                    // Delete the task
                    taskFolder.DeleteTask(taskName, false);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Debug($"Unable to delete the ScheduleAutoStart task.\r\n{ex.Message}.");
                }
            }

            return false;
        }

        public static bool ScheduleAutoUpdate(string taskKey, string taskSuffix, string command, int autoUpdatePeriod)
        {
            var taskName = $"AutoUpdate_{taskKey}";
            if (!string.IsNullOrWhiteSpace(taskSuffix))
                taskName += $"_{taskSuffix}";

            var taskFolder = TaskService.Instance.RootFolder.SubFolders.Exists(TASK_FOLDER) ? TaskService.Instance.RootFolder.SubFolders[TASK_FOLDER] : null;

            if (autoUpdatePeriod > 0)
            {
                
                // create the task folder
                if (taskFolder == null)
                {
                    try
                    {
                        taskFolder = TaskService.Instance.RootFolder.CreateFolder(TASK_FOLDER, null, false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug($"Unable to create the ASM task folder.\r\n{ex.Message}.");
                        return false;
                    }
                }

                if (taskFolder == null)
                    return false;

                var task = taskFolder.Tasks.Exists(taskName) ? taskFolder.Tasks[taskName] : null;
                var taskDefinition = task?.Definition ?? TaskService.Instance.NewTask();

                if (taskDefinition == null)
                    return false;

                Version appVersion;
                Version.TryParse(App.Version, out appVersion);

                taskDefinition.Principal.LogonType = TaskLogonType.InteractiveToken;
                taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;

                taskDefinition.RegistrationInfo.Description = "Ark Server Auto-Update";
                taskDefinition.RegistrationInfo.Source = "Ark Server Manager";
                taskDefinition.RegistrationInfo.Version = appVersion;

                taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.FromHours(EXECUTION_TIME_LIMIT);
                taskDefinition.Settings.Priority = ProcessPriorityClass.Normal;

                // Add a trigger that will fire every x minutes
                taskDefinition.Triggers.Clear();
                var trigger = new TimeTrigger {
                                  StartBoundary = DateTime.Today.AddHours(DateTime.Now.Hour + 1),
                                  ExecutionTimeLimit = TimeSpan.FromHours(EXECUTION_TIME_LIMIT),
                                  Repetition = {Interval = TimeSpan.FromMinutes(autoUpdatePeriod)},
                              };
                taskDefinition.Triggers.Add(trigger);

                // Create an action that will launch whenever the trigger fires
                taskDefinition.Actions.Clear();
                var action = new ExecAction {
                                 Path = command,
                                 Arguments = App.ARG_AUTOUPDATE
                             };
                taskDefinition.Actions.Add(action);

                try
                {
                    task = taskFolder.RegisterTaskDefinition(taskName, taskDefinition, TaskCreation.CreateOrUpdate, null, null, TaskLogonType.InteractiveToken);
                    return task != null;
                }
                catch (Exception ex)
                {
                    Logger.Debug($"Unable to create the ScheduleAutoUpdate task.\r\n{ex.Message}.");
                }
            }
            else
            {
                if (taskFolder == null)
                    return true;

                // Retrieve the task to be deleted
                var task = taskFolder.Tasks.Exists(taskName) ? taskFolder.Tasks[taskName] : null;
                if (task == null)
                    return true;

                try
                {
                    // Delete the task
                    taskFolder.DeleteTask(taskName, false);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Debug($"Unable to delete the ScheduleAutoUpdate task.\r\n{ex.Message}.");
                }
            }

            return false;
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
                Logger.Error($"Unable to calculate the ComputeKey (MD5).\r\n{ex.Message}.");
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
    }
}
