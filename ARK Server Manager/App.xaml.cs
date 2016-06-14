using ARK_Server_Manager.Lib;
using Microsoft.WindowsAPICodePack.Dialogs;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using WPFSharp.Globalizer;
using System.Threading;
using System.Globalization;

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

        private GlobalizedApplication _globalizedApplication;

        public App()
        {
            var culture = new CultureInfo(GlobalizationManager.FallBackLanguage);
            if (!string.IsNullOrWhiteSpace(Config.Default.CultureName))
                culture = new CultureInfo(Config.Default.CultureName);

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            AppDomain.CurrentDomain.UnhandledException += ErrorHandling.CurrentDomain_UnhandledException;
            App.Instance = this;
            MigrateSettings();

            ReconfigureLogging();
            App.Version = App.GetDeployedVersion();

            _globalizedApplication = GlobalizedApplication.Instance;
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
            base.OnStartup(e);
            
            // Initial configuration setting
            if (String.IsNullOrWhiteSpace(Config.Default.DataDir))
            {
                MessageBox.Show(_globalizedApplication.GetResourceString("Application_DataDirectoryLabel"), _globalizedApplication.GetResourceString("Application_DataDirectoryTitle"), MessageBoxButton.OK, MessageBoxImage.Information);

                while (String.IsNullOrWhiteSpace(Config.Default.DataDir))
                {
                    var dialog = new CommonOpenFileDialog();
                    dialog.EnsureFileExists = true;
                    dialog.IsFolderPicker = true;
                    dialog.Multiselect = false;
                    dialog.Title = _globalizedApplication.GetResourceString("Application_DataDirectory_DialogTitle");
                    dialog.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
                    if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
                    {
                        Environment.Exit(0);
                    }

                    var confirm = MessageBox.Show(String.Format(_globalizedApplication.GetResourceString("Application_DataDirectory_ConfirmLabel"), Path.Combine(dialog.FileName, Config.Default.ProfilesDir), Path.Combine(dialog.FileName, Config.Default.SteamCmdDir)), _globalizedApplication.GetResourceString("Application_DataDirectory_ConfirmTitle"), MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    if (confirm == MessageBoxResult.Cancel)
                    {
                        Environment.Exit(0);
                    }
                    else if (confirm == MessageBoxResult.Yes)
                    {
                        Config.Default.DataDir = dialog.FileName;
                        ReconfigureLogging();
                        break;
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
                    MessageBox.Show(String.Format(_globalizedApplication.GetResourceString("Application_Profile_SaveFailedLabel"), server.Profile.ProfileName, ex.Message, ex.StackTrace), _globalizedApplication.GetResourceString("Application_Profile_SaveFailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
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
