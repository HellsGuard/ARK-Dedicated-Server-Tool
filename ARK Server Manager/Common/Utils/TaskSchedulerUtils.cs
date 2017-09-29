using Microsoft.Win32.TaskScheduler;
using NLog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace ARK_Server_Manager.Lib
{
    public static class TaskSchedulerUtils
    {
        private const string PREFIX_BACKUP = "AutoBackup";
        private const string PREFIX_SHUTDOWN = "AutoShutdown";
        private const string PREFIX_START = "AutoStart";
        private const string PREFIX_UPDATE = "AutoUpdate";

        private const int EXECUTION_TIME_LIMIT = 3;

        public enum ShutdownType
        {
            Shutdown1,
            Shutdown2,
        }

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        static TaskSchedulerUtils()
        {
            TaskFolder = "ArkServerManager";
        }

        public static string TaskFolder
        {
            get;
            set;
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
                _logger.Debug($"Unable to calculate the ComputeKey (MD5).\r\n{ex.Message}.");
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

        public static void RunAutoBackup(string taskKey, string taskSuffix)
        {
            var taskName = $"{PREFIX_BACKUP}_{taskKey}";
            if (!string.IsNullOrWhiteSpace(taskSuffix))
                taskName += $"_{taskSuffix}";

            var taskFolder = TaskService.Instance.RootFolder.SubFolders.Exists(TaskFolder) ? TaskService.Instance.RootFolder.SubFolders[TaskFolder] : null;
            if (taskFolder == null)
                return;

            var task = taskFolder.Tasks.Exists(taskName) ? taskFolder.Tasks[taskName] : null;
            if (task == null)
                return;

            task.Run();
        }

        public static void RunAutoUpdate(string taskKey, string taskSuffix)
        {
            var taskName = $"{PREFIX_UPDATE}_{taskKey}";
            if (!string.IsNullOrWhiteSpace(taskSuffix))
                taskName += $"_{taskSuffix}";

            var taskFolder = TaskService.Instance.RootFolder.SubFolders.Exists(TaskFolder) ? TaskService.Instance.RootFolder.SubFolders[TaskFolder] : null;
            if (taskFolder == null)
                return;

            var task = taskFolder.Tasks.Exists(taskName) ? taskFolder.Tasks[taskName] : null;
            if (task == null)
                return;

            task.Run();
        }

        public static bool ScheduleAutoBackup(string taskKey, string taskSuffix, string command, int autoBackupPeriod)
        {
            var taskName = $"{PREFIX_BACKUP}_{taskKey}";
            if (!string.IsNullOrWhiteSpace(taskSuffix))
                taskName += $"_{taskSuffix}";

            var taskFolder = TaskService.Instance.RootFolder.SubFolders.Exists(TaskFolder) ? TaskService.Instance.RootFolder.SubFolders[TaskFolder] : null;

            if (autoBackupPeriod > 0)
            {
                // create the task folder
                if (taskFolder == null)
                {
                    try
                    {
                        taskFolder = TaskService.Instance.RootFolder.CreateFolder(TaskFolder, null, false);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"{nameof(ScheduleAutoBackup)} - Unable to create the Server Manager task folder. {ex.Message}\r\n{ex.StackTrace}");
                        return false;
                    }
                }

                if (taskFolder == null)
                    return false;

                var task = taskFolder.Tasks.Exists(taskName) ? taskFolder.Tasks[taskName] : null;
                var taskDefinition = task?.Definition ?? TaskService.Instance.NewTask();

                if (taskDefinition == null)
                    return false;

                Version.TryParse(App.Version, out Version appVersion);

                taskDefinition.Principal.LogonType = TaskLogonType.InteractiveToken;
                taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;

                taskDefinition.RegistrationInfo.Description = "Server Auto-Backup";
                taskDefinition.RegistrationInfo.Source = "Server Manager";
                taskDefinition.RegistrationInfo.Version = appVersion;

                taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.FromHours(EXECUTION_TIME_LIMIT);
                taskDefinition.Settings.Priority = ProcessPriorityClass.Normal;

                // Add/Edit the trigger that will fire every x minutes
                var triggers = taskDefinition.Triggers.OfType<TimeTrigger>();
                if (triggers.Count() == 0)
                {
                    var trigger = new TimeTrigger
                    {
                        StartBoundary = DateTime.Today.AddHours(DateTime.Now.Hour + 1),
                        ExecutionTimeLimit = TimeSpan.FromHours(EXECUTION_TIME_LIMIT),
                        Repetition = { Interval = TimeSpan.FromMinutes(autoBackupPeriod) },
                    };
                    taskDefinition.Triggers.Add(trigger);
                }
                else
                {
                    foreach (var trigger in triggers)
                    {
                        trigger.Repetition.Interval = TimeSpan.FromMinutes(autoBackupPeriod);
                    }
                }

                // Create an action that will launch whenever the trigger fires
                taskDefinition.Actions.Clear();
                var action = new ExecAction
                {
                    Path = command,
                    Arguments = App.ARG_AUTOBACKUP
                };
                taskDefinition.Actions.Add(action);

                try
                {
                    task = taskFolder.RegisterTaskDefinition(taskName, taskDefinition, TaskCreation.CreateOrUpdate, null, null, TaskLogonType.InteractiveToken);
                    return task != null;
                }
                catch (Exception ex)
                {
                    _logger.Error($"{nameof(ScheduleAutoBackup)} - Unable to create the ScheduleAutoBackup task. {ex.Message}\r\n{ex.StackTrace}");
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
                    _logger.Error($"{nameof(ScheduleAutoBackup)} - Unable to delete the ScheduleAutoBackup task. {ex.Message}\r\n{ex.StackTrace}");
                }
            }

            return false;
        }

        public static bool ScheduleAutoShutdown(string taskKey, string taskSuffix, string command, TimeSpan? restartTime, string profileName, ShutdownType type)
        {
            var taskName = $"{PREFIX_SHUTDOWN}_{taskKey}";
            if (!string.IsNullOrWhiteSpace(taskSuffix))
                taskName += $"_{taskSuffix}";

            var taskFolder = TaskService.Instance.RootFolder.SubFolders.Exists(TaskFolder) ? TaskService.Instance.RootFolder.SubFolders[TaskFolder] : null;

            if (restartTime.HasValue)
            {
                // create the task folder
                if (taskFolder == null)
                {
                    try
                    {
                        taskFolder = TaskService.Instance.RootFolder.CreateFolder(TaskFolder, null, false);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"{nameof(ScheduleAutoShutdown)} - Unable to create the Server Manager task folder. {ex.Message}\r\n{ex.StackTrace}");
                        return false;
                    }
                }

                if (taskFolder == null)
                    return false;

                var task = taskFolder.Tasks.Exists(taskName) ? taskFolder.Tasks[taskName] : null;
                var taskDefinition = task?.Definition ?? TaskService.Instance.NewTask();

                if (taskDefinition == null)
                    return false;

                Version.TryParse(App.Version, out Version appVersion);

                taskDefinition.Principal.LogonType = TaskLogonType.InteractiveToken;
                taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;

                taskDefinition.RegistrationInfo.Description = $"Server Auto-Shutdown - {profileName}";
                taskDefinition.RegistrationInfo.Source = "Server Manager";
                taskDefinition.RegistrationInfo.Version = appVersion;

                taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.FromHours(EXECUTION_TIME_LIMIT);
                taskDefinition.Settings.Priority = ProcessPriorityClass.Normal;

                // Add/Edit the trigger that will fire every day at the specified restart time
                var triggers = taskDefinition.Triggers.OfType<DailyTrigger>();
                if (triggers.Count() == 0)
                {
                    var trigger = new DailyTrigger
                    {
                        StartBoundary = DateTime.Today.Add(restartTime.Value),
                        ExecutionTimeLimit = TimeSpan.FromHours(EXECUTION_TIME_LIMIT)
                    };
                    taskDefinition.Triggers.Add(trigger);
                }
                else
                {
                    foreach (var trigger in triggers)
                    {
                        trigger.StartBoundary = DateTime.Today.Add(restartTime.Value);
                    }
                }

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
                var action = new ExecAction
                {
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
                    _logger.Error($"{nameof(ScheduleAutoShutdown)} - Unable to create the ScheduleAutoShutdown task. {ex.Message}\r\n{ex.StackTrace}");
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
                    _logger.Error($"{nameof(ScheduleAutoShutdown)} - Unable to delete the ScheduleAutoShutdown task. {ex.Message}\r\n{ex.StackTrace}");
                }
            }

            return false;
        }

        public static bool ScheduleAutoStart(string taskKey, string taskSuffix, bool enableAutoStart, string command, string profileName, bool onBoot)
        {
            var taskName = $"{PREFIX_START}_{taskKey}";
            if (!string.IsNullOrWhiteSpace(taskSuffix))
                taskName += $"_{taskSuffix}";

            var taskFolder = TaskService.Instance.RootFolder.SubFolders.Exists(TaskFolder) ? TaskService.Instance.RootFolder.SubFolders[TaskFolder] : null;

            if (enableAutoStart)
            {
                // create the task folder
                if (taskFolder == null)
                {
                    try
                    {
                        taskFolder = TaskService.Instance.RootFolder.CreateFolder(TaskFolder, null, false);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"{nameof(ScheduleAutoStart)} - Unable to create the Server Manager task folder. {ex.Message}\r\n{ex.StackTrace}");
                        return false;
                    }
                }

                if (taskFolder == null)
                    return false;

                var task = taskFolder.Tasks.Exists(taskName) ? taskFolder.Tasks[taskName] : null;
                var taskDefinition = task?.Definition ?? TaskService.Instance.NewTask();

                if (taskDefinition == null)
                    return false;

                Version.TryParse(App.Version, out Version appVersion);

                taskDefinition.Principal.LogonType = TaskLogonType.ServiceAccount;
                taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;

                taskDefinition.RegistrationInfo.Description = $"Server Auto-Start - {profileName}";
                taskDefinition.RegistrationInfo.Source = "Server Manager";
                taskDefinition.RegistrationInfo.Version = appVersion;

                taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.FromHours(EXECUTION_TIME_LIMIT);
                taskDefinition.Settings.Priority = ProcessPriorityClass.Normal;

                // Add a trigger that will fire after the machine has started
                if (onBoot)
                {
                    var triggers = taskDefinition.Triggers.OfType<BootTrigger>();
                    if (triggers.Count() == 0)
                    {
                        var trigger = new BootTrigger
                        {
                            Delay = TimeSpan.FromMinutes(1),
                            ExecutionTimeLimit = TimeSpan.FromHours(EXECUTION_TIME_LIMIT)
                        };
                        taskDefinition.Triggers.Add(trigger);
                    }
                    else
                    {
                        foreach (var trigger in triggers)
                        {
                            trigger.Delay = TimeSpan.FromMinutes(1);
                        }
                    }
                }
                else
                {
                    var triggers = taskDefinition.Triggers.OfType<LogonTrigger>();
                    if (triggers.Count() == 0)
                    {
                        var trigger = new LogonTrigger
                        {
                            Delay = TimeSpan.FromMinutes(1),
                            ExecutionTimeLimit = TimeSpan.FromHours(EXECUTION_TIME_LIMIT)
                        };
                        taskDefinition.Triggers.Add(trigger);
                    }
                    else
                    {
                        foreach (var trigger in triggers)
                        {
                            trigger.Delay = TimeSpan.FromMinutes(1);
                        }
                    }
                }

                // Create an action that will launch whenever the trigger fires
                taskDefinition.Actions.Clear();
                var action = new ExecAction
                {
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
                    _logger.Error($"{nameof(ScheduleAutoStart)} - Unable to create the ScheduleAutoStart task. {ex.Message}\r\n{ex.StackTrace}");
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
                    _logger.Error($"{nameof(ScheduleAutoStart)} - Unable to delete the ScheduleAutoStart task. {ex.Message}\r\n{ex.StackTrace}");
                }
            }

            return false;
        }

        public static bool ScheduleAutoUpdate(string taskKey, string taskSuffix, string command, int autoUpdatePeriod)
        {
            var taskName = $"{PREFIX_UPDATE}_{taskKey}";
            if (!string.IsNullOrWhiteSpace(taskSuffix))
                taskName += $"_{taskSuffix}";

            var taskFolder = TaskService.Instance.RootFolder.SubFolders.Exists(TaskFolder) ? TaskService.Instance.RootFolder.SubFolders[TaskFolder] : null;

            if (autoUpdatePeriod > 0)
            {

                // create the task folder
                if (taskFolder == null)
                {
                    try
                    {
                        taskFolder = TaskService.Instance.RootFolder.CreateFolder(TaskFolder, null, false);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"{nameof(ScheduleAutoUpdate)} - Unable to create the Server Manager task folder. {ex.Message}\r\n{ex.StackTrace}");
                        return false;
                    }
                }

                if (taskFolder == null)
                    return false;

                var task = taskFolder.Tasks.Exists(taskName) ? taskFolder.Tasks[taskName] : null;
                var taskDefinition = task?.Definition ?? TaskService.Instance.NewTask();

                if (taskDefinition == null)
                    return false;

                Version.TryParse(App.Version, out Version appVersion);

                taskDefinition.Principal.LogonType = TaskLogonType.InteractiveToken;
                taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;

                taskDefinition.RegistrationInfo.Description = "Server Auto-Update";
                taskDefinition.RegistrationInfo.Source = "Server Manager";
                taskDefinition.RegistrationInfo.Version = appVersion;

                taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.FromHours(EXECUTION_TIME_LIMIT);
                taskDefinition.Settings.Priority = ProcessPriorityClass.Normal;

                // Add/Edit the trigger that will fire every x minutes
                var triggers = taskDefinition.Triggers.OfType<TimeTrigger>();
                if (triggers.Count() == 0)
                {
                    var trigger = new TimeTrigger
                    {
                        StartBoundary = DateTime.Today.AddHours(DateTime.Now.Hour + 1),
                        ExecutionTimeLimit = TimeSpan.FromHours(EXECUTION_TIME_LIMIT),
                        Repetition = { Interval = TimeSpan.FromMinutes(autoUpdatePeriod) },
                    };
                    taskDefinition.Triggers.Add(trigger);
                }
                else
                {
                    foreach (var trigger in triggers)
                    {
                        trigger.Repetition.Interval = TimeSpan.FromMinutes(autoUpdatePeriod);
                    }
                }

                // Create an action that will launch whenever the trigger fires
                taskDefinition.Actions.Clear();
                var action = new ExecAction
                {
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
                    _logger.Error($"{nameof(ScheduleAutoUpdate)} - Unable to create the ScheduleAutoUpdate task. {ex.Message}\r\n{ex.StackTrace}");
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
                    _logger.Error($"{nameof(ScheduleAutoUpdate)} - Unable to delete the ScheduleAutoUpdate task. {ex.Message}\r\n{ex.StackTrace}");
                }
            }

            return false;
        }

        public static TaskState TaskStateAutoBackup(string taskKey, string taskSuffix, out DateTime nextRunTime)
        {
            nextRunTime = DateTime.MinValue;

            var taskName = $"{PREFIX_BACKUP}_{taskKey}";
            if (!string.IsNullOrWhiteSpace(taskSuffix))
                taskName += $"_{taskSuffix}";

            var taskFolder = TaskService.Instance.RootFolder.SubFolders.Exists(TaskFolder) ? TaskService.Instance.RootFolder.SubFolders[TaskFolder] : null;
            if (taskFolder == null)
                return TaskState.Unknown;

            var task = taskFolder.Tasks.Exists(taskName) ? taskFolder.Tasks[taskName] : null;
            if (task == null)
                return TaskState.Unknown;

            nextRunTime = task.NextRunTime;
            return task.State;
        }

        public static TaskState TaskStateAutoUpdate(string taskKey, string taskSuffix, out DateTime nextRunTime)
        {
            nextRunTime = DateTime.MinValue;

            var taskName = $"{PREFIX_UPDATE}_{taskKey}";
            if (!string.IsNullOrWhiteSpace(taskSuffix))
                taskName += $"_{taskSuffix}";

            var taskFolder = TaskService.Instance.RootFolder.SubFolders.Exists(TaskFolder) ? TaskService.Instance.RootFolder.SubFolders[TaskFolder] : null;
            if (taskFolder == null)
                return TaskState.Unknown;

            var task = taskFolder.Tasks.Exists(taskName) ? taskFolder.Tasks[taskName] : null;
            if (task == null)
                return TaskState.Unknown;

            nextRunTime = task.NextRunTime;
            return task.State;
        }
    }
}
