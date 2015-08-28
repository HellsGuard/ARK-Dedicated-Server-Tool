using ARK_Server_Manager.Lib;
using Microsoft.WindowsAPICodePack.Dialogs;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using WPFSharp.Globalizer;
using SteamKit2;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : GlobalizedApplication
    {
        static public string Version
        {
            get;
            set;
        }

        new static public App Instance
        {
            get;
            private set;
        }

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += ErrorHandling.CurrentDomain_UnhandledException;
            App.Instance = this;
            MigrateSettings();

            ReconfigureLogging();
            App.Version = App.GetDeployedVersion();
        }

        private static void MigrateSettings()
        {
            //
            // Migrate settings when we update.
            //
            if (Config.Default.UpgradeConfig)
            {
                Config.Default.Upgrade();
                Config.Default.Reload();
                Config.Default.UpgradeConfig = false;

#if false
                object previousEnableSettingsCache = null;
                try { previousEnableSettingsCache = Config.Default.GetPreviousVersion(nameof(Config.Default.GLOBAL_EnableServerCache)); }
                catch (SettingsPropertyNotFoundException) { /* this would get thrown if we were renaming a property, see http://www.codeproject.com/Articles/247333/Renaming-User-Settings-properties-between-software */ }

                if (previousEnableSettingsCache == null)
                {
                    int serverupdatePeriod = 0;
                    Int32.TryParse(Config.Default.GetPreviousVersion(nameof(Config.Default.ServerCacheUpdatePeriod)).ToString(), out serverupdatePeriod);
                    if (!String.IsNullOrWhiteSpace(Config.Default.GetPreviousVersion(nameof(Config.Default.ServerCacheDir)).ToString()) &&
                       serverupdatePeriod > 0)
                    {
                        Config.Default.GLOBAL_EnableServerCache = true;
                    }
                }
#endif
                Config.Default.Save();
            }
        }

        public static void ReconfigureLogging()
        {               
            string logDir = Path.Combine(Config.Default.DataDir, Config.Default.LogsDir);
            LogManager.Configuration.Variables["logDir"] = logDir;

            System.IO.Directory.CreateDirectory(logDir);
            var target = (FileTarget)LogManager.Configuration.FindTargetByName("statuswatcher");
            target.FileName = Path.Combine(logDir, "ASM_ServerStatusWatcher.log");

            target = (FileTarget)LogManager.Configuration.FindTargetByName("debugFile");
            target.FileName = Path.Combine(logDir, "ASM_Debug.log");

            target = (FileTarget)LogManager.Configuration.FindTargetByName("scripts");
            target.FileName = Path.Combine(logDir, "ASM_Scripts.log");

            LogManager.ReconfigExistingLoggers();
        }   

        public static string GetProfileLogDir(string profileName)
        {
            var logFilePath = Path.Combine(Config.Default.DataDir, Config.Default.LogsDir, profileName);
            return logFilePath;
        }

        public static Logger GetProfileLogger(string profileName, string name)
        {
            var loggerName = $"{profileName}_{name}";

            Logger logger = null;
            var config = LogManager.Configuration;
            if (config.FindTargetByName(loggerName) == null)
            {            
                var logFile = new FileTarget();
                config.AddTarget(loggerName, logFile);

                var logFilePath = GetProfileLogDir(profileName);
                logFile.FileName = Path.Combine(logFilePath, $"{name}.log");
                logFile.Layout = "${time} ${message}";
                var datePlaceholder = "{#}";
                logFile.ArchiveFileName = Path.Combine(logFilePath, $"{name}.{datePlaceholder}.log");
                logFile.ArchiveNumbering = ArchiveNumberingMode.DateAndSequence;
                logFile.ArchiveEvery = FileArchivePeriod.Day;
                logFile.ArchiveDateFormat = "yyyyMMdd";

                var rule = new LoggingRule(loggerName, LogLevel.Info, logFile);
                config.LoggingRules.Add(rule);

                LogManager.Configuration = config;
            }

            logger = LogManager.GetLogger(loggerName);
            return logger;
        }

        protected override void OnStartup(StartupEventArgs e)
        {                                  
            // Initial configuration setting
            if(String.IsNullOrWhiteSpace(Config.Default.DataDir))
            {
                MessageBox.Show("It appears you do not have a data directory set.  The data directory is where your profiles and SteamCMD will be stored.  It is not the same as the server installation directory, which you can choose for each profile.  You will now be asked to select the location where the Ark Server Manager data directory is located.  You may later change this in the Settings window.", "Select Data Directory", MessageBoxButton.OK, MessageBoxImage.Information);

                while (String.IsNullOrWhiteSpace(Config.Default.DataDir))
                {
                    var dialog = new CommonOpenFileDialog();
                    dialog.EnsureFileExists = true;
                    dialog.IsFolderPicker = true;
                    dialog.Multiselect = false;
                    dialog.Title = "Select a Data Directory";
                    dialog.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
                    var result = dialog.ShowDialog();
                    if (result == CommonFileDialogResult.Ok)
                    {
                        var confirm = MessageBox.Show(String.Format("Ark Server Manager will store profiles and SteamCMD in the following directories:\r\n\r\nProfiles: {0}\r\nSteamCMD: {1}\r\n\r\nIs this ok?", Path.Combine(dialog.FileName, Config.Default.ProfilesDir), Path.Combine(dialog.FileName, Config.Default.SteamCmdDir)), "Confirm location", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                        if(confirm == MessageBoxResult.No)
                        {
                            continue;
                        }
                        else if(confirm == MessageBoxResult.Yes)
                        {
                            Config.Default.DataDir = dialog.FileName;
                            ReconfigureLogging();
                            break;
                        }
                        else
                        {
                            Environment.Exit(0);
                        }
                    }
                }
            }

            Config.Default.ConfigDirectory = Path.Combine(Config.Default.DataDir, Config.Default.ProfilesDir);            
            System.IO.Directory.CreateDirectory(Config.Default.ConfigDirectory);
            Config.Default.Save();

            if(String.IsNullOrWhiteSpace(Config.Default.MachinePublicIP))
            {
                Task.Factory.StartNew(async () => await App.DiscoverMachinePublicIP(forceOverride: false));
            }

            
            base.OnStartup(e);
        }

        public static async Task DiscoverMachinePublicIP(bool forceOverride)
        {
            var publicIP = await NetworkUtils.DiscoverPublicIPAsync();
            if(publicIP != null)
            {
                await App.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if(forceOverride || String.IsNullOrWhiteSpace(Config.Default.MachinePublicIP))
                    {
                        Config.Default.MachinePublicIP = publicIP;
                    }
                }));
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            foreach(var server in ServerManager.Instance.Servers)
            {
                try
                {
                    server.Profile.Save();
                }
                catch(Exception ex)
                {
                    MessageBox.Show(String.Format("Failed to save profile {0}.  {1}\n{2}", server.Profile.ProfileName, ex.Message, ex.StackTrace), "Failed to save profile", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            Config.Default.Save();
            base.OnExit(e);
        }

        private static string GetDeployedVersion()
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                Assembly asmCurrent = System.Reflection.Assembly.GetExecutingAssembly();
                string executePath = new Uri(asmCurrent.GetName().CodeBase).LocalPath;

                xmlDoc.Load(executePath + ".manifest");
                XmlNamespaceManager ns = new XmlNamespaceManager(xmlDoc.NameTable);
                ns.AddNamespace("asmv1", "urn:schemas-microsoft-com:asm.v1");
                string xPath = "/asmv1:assembly/asmv1:assemblyIdentity/@version";
                XmlNode node = xmlDoc.SelectSingleNode(xPath, ns);
                string version = node.Value;
                return version;
            }
            catch
            {
                return "Unknown";
            }            
        }
    }
}
