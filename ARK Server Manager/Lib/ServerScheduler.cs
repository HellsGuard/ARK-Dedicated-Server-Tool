using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib
{
    public static class ServerScheduler
    {
        private const string TaskFolder = "\\ArkServerManager";

        public static void ScheduleUpdates(string updateKey, int autoUpdatePeriod, string outputLocation, string serverIP, int rconPort, string adminPass, string installDirectory, string steamCmdPath)
        {
            var schedulerKey = $"{updateKey}_AutoUpgrade";

            using (var taskService = new TaskService())
            {
                if (autoUpdatePeriod == 0)
                {
                    DeleteTask(schedulerKey, taskService);
                }
                else
                {
                    var exeFullPath = ServerUpdaterGenerator.GenerateUpdaterExe(outputLocation, serverIP, rconPort, adminPass, installDirectory, steamCmdPath);
                    RegisterTask(schedulerKey, taskService, exeFullPath, "Auto", d =>
                    {
                        d.RegistrationInfo.Description = "Ark Server Auto-Upgrade";
                        var trigger = new TimeTrigger(DateTime.Now + TimeSpan.FromMinutes(autoUpdatePeriod));
                        trigger.Repetition.Interval = TimeSpan.FromMinutes(autoUpdatePeriod);
                        d.Triggers.Add(trigger);
                    });
                }
            }
        }

        public static void ScheduleAutoStart(string updateKey, bool enableAutoStart, string command, string args)
        {
            var schedulerKey = $"{updateKey}_AutoStart";
            using (var taskService = new TaskService())
            {
                DeleteTask(schedulerKey, taskService);
                if (enableAutoStart)
                {
                    RegisterTask(schedulerKey, taskService, command, args, d =>
                    {
                        var trigger = new BootTrigger();
                        trigger.Delay = TimeSpan.FromMinutes(1);
                        d.Triggers.Add(trigger);
                    });
                }
            }
        }

        private static void DeleteTask(string schedulerKey, TaskService taskService)
        {
            GetASMFolder(taskService).DeleteTask(schedulerKey, exceptionOnNotExists: false);
        }

        private static void RegisterTask(string schedulerKey, TaskService taskService, string command, string args, Action<TaskDefinition> augmentDefinition)
        {
            string user = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            var definition = taskService.NewTask();
            definition.Principal.UserId = user;
            definition.Principal.LogonType = TaskLogonType.Password;
            definition.Actions.Add(new ExecAction(command, args));
            augmentDefinition.Invoke(definition);
            GetASMFolder(taskService).RegisterTaskDefinition(schedulerKey, definition, TaskCreation.CreateOrUpdate, user);
        }

        public static TaskFolder GetASMFolder(TaskService service)
        {
            return service.GetFolder(TaskFolder) ?? service.RootFolder.CreateFolder(TaskFolder);
        }
    }
}
