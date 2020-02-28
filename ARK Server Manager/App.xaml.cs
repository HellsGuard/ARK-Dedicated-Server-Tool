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
using System.Globalization;
using System.Diagnostics;
using System.Linq;
using ArkServerManager.Plugin.Common;
using System.Net;
using ArkData;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : GlobalizedApplication
    {
        public const string ARG_AUTOBACKUP = "-ab";
        public const string ARG_AUTOSHUTDOWN1 = "-as1";
        public const string ARG_AUTOSHUTDOWN2 = "-as2";
        public const string ARG_AUTORESTART = "-ar";
        public const string ARG_AUTOUPDATE = "-au";
        public const string ARG_BETA = "-beta";
        public const string ARG_RCON = "-rcon";

        public new static App Instance
        {
            get;
            private set;
        }

        public static bool ApplicationStarted
        {
            get;
            set;
        }

        public static string Version
        {
            get;
            set;
        }

        private GlobalizedApplication _globalizer;

        public App()
        {
            if (string.IsNullOrWhiteSpace(Config.Default.ASMUniqueKey))
                Config.Default.ASMUniqueKey = Guid.NewGuid().ToString();

            ApplicationStarted = false;
            Args = string.Empty;
            BetaVersion = false;

            AppDomain.CurrentDomain.UnhandledException += ErrorHandling.CurrentDomain_UnhandledException;
            App.Instance = this;
            MigrateSettings();

            ReconfigureLogging();
            App.Version = App.GetDeployedVersion();
        }

        public string Args
        {
            get;
            set;
        }

        public bool BetaVersion
        {
            get;
            set;
        }

        public static async Task DiscoverMachinePublicIP(bool forceOverride)
        {
            var publicIP = await NetworkUtils.DiscoverPublicIPAsync();
            if (string.IsNullOrWhiteSpace(publicIP))
                return;

            await App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (forceOverride || String.IsNullOrWhiteSpace(Config.Default.MachinePublicIP))
                {
                    Config.Default.MachinePublicIP = publicIP;
                }
            }));
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

        public static string GetProfileLogDir(string profileName)
        {
            var logFilePath = Path.Combine(Config.Default.DataDir, Config.Default.LogsDir, profileName);
            return logFilePath;
        }

        public static Logger GetProfileLogger(string profileName, string name, LogLevel minLevel, LogLevel maxLevel)
        {
            var loggerName = $"{profileName}_{name}".Replace(" ", "_");

            if (LogManager.Configuration.FindTargetByName(loggerName) == null)
            {            
                var logFilePath = GetProfileLogDir(profileName);
                if (!System.IO.Directory.Exists(logFilePath))
                    System.IO.Directory.CreateDirectory(logFilePath);

                var logFile = new FileTarget(loggerName)
                {
                    FileName = Path.Combine(logFilePath, $"{name}.log"),
                    Layout = "${time} ${message}",
                    ArchiveFileName = Path.Combine(logFilePath, $"{name}.{{#}}.log"),
                    ArchiveNumbering = ArchiveNumberingMode.DateAndSequence,
                    ArchiveEvery = FileArchivePeriod.Day,
                    ArchiveDateFormat = "yyyyMMdd"
                };
                LogManager.Configuration.AddTarget(loggerName, logFile);

                var rule = new LoggingRule(loggerName, minLevel, maxLevel, logFile);
                LogManager.Configuration.LoggingRules.Add(rule);
                LogManager.ReconfigExistingLoggers();
            }

            return LogManager.GetLogger(loggerName);
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

                Config.Default.Save();
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            _globalizer = GlobalizedApplication.Instance;
            try
            {
                if (!string.IsNullOrWhiteSpace(Config.Default.CultureName))
                    _globalizer.GlobalizationManager.SwitchLanguage(Config.Default.CultureName, true);
            }
            catch (CultureNotFoundException ex)
            {
                // just output the exception message, it should default back to the fallback language.
                Debug.WriteLine(ex.Message);
            }

            if (!string.IsNullOrWhiteSpace(Config.Default.StyleName))
                _globalizer.StyleManager.SwitchStyle($"{Config.Default.StyleName}.xaml");

            Args = string.Join(" ", e.Args);

            // check if we are starting ASM for server restart
            if (e.Args.Any(a => a.Equals(ARG_BETA)))
            {
                BetaVersion = true;
            }

            var installPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            PluginHelper.Instance.BetaEnabled = BetaVersion;
            PluginHelper.Instance.LoadPlugins(installPath, true);

            // check if we are starting ASM for the old server restart - no longer supported
            if (e.Args.Any(a => a.StartsWith(ARG_AUTORESTART)))
            {
                // just exit
                Environment.Exit(0);
            }

            // check if we are starting ASM for server shutdown
            if (e.Args.Any(a => a.StartsWith(ARG_AUTOSHUTDOWN1)))
            {
                var arg = e.Args.FirstOrDefault(a => a.StartsWith(ARG_AUTOSHUTDOWN1));
                var exitCode = ServerApp.PerformAutoShutdown(arg, ServerApp.ServerProcessType.AutoShutdown1);

                // once we are finished, just exit
                Environment.Exit(exitCode);
            }

            // check if we are starting ASM for server shutdown
            if (e.Args.Any(a => a.StartsWith(ARG_AUTOSHUTDOWN2)))
            {
                var arg = e.Args.FirstOrDefault(a => a.StartsWith(ARG_AUTOSHUTDOWN2));
                var exitCode = ServerApp.PerformAutoShutdown(arg, ServerApp.ServerProcessType.AutoShutdown2);

                // once we are finished, just exit
                Environment.Exit(exitCode);
            }

            // check if we are starting ASM for server updating
            if (e.Args.Any(a => a.Equals(ARG_AUTOUPDATE)))
            {
                var exitCode = ServerApp.PerformAutoUpdate();

                // once we are finished, just exit
                Environment.Exit(exitCode);
            }

            // check if we are starting ASM for server backups
            if (e.Args.Any(a => a.Equals(ARG_AUTOBACKUP)))
            {
                var exitCode = ServerApp.PerformAutoBackup();

                // once we are finished, just exit
                Environment.Exit(exitCode);
            }

            // check if we are starting ASM for server updating
            if (e.Args.Any(a => a.Equals(ARG_RCON)))
            {
                var rcon = new OpenRCONWindow();
                rcon.ShowInTaskbar = true;
                rcon.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                rcon.ShowDialog();

                Config.Default.Save();

                // once we are finished, just exit
                Environment.Exit(0);
            }

            if (Config.Default.RunAsAdministratorPrompt && !SecurityUtils.IsAdministrator())
            {
                var result = MessageBox.Show(_globalizer.GetResourceString("Application_RunAsAdministratorLabel"), _globalizer.GetResourceString("Application_RunAsAdministratorTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    var processInfo = new ProcessStartInfo(Assembly.GetExecutingAssembly().CodeBase);

                    // The following properties run the new process as administrator
                    processInfo.UseShellExecute = true;
                    processInfo.Verb = "runas";
                    processInfo.Arguments = string.Join(" ", e.Args);

                    // Start the new process
                    try
                    {
                        Process.Start(processInfo);

                        // Shut down the current process
                        Application.Current.Shutdown(0);

                        return;
                    }
                    catch (Exception)
                    {
                        // The user did not allow the application to run as administrator
                        MessageBox.Show(_globalizer.GetResourceString("Application_RunAsAdministrator_FailedLabel"), _globalizer.GetResourceString("Application_RunAsAdministrator_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            // check if application is already running
            if (ProcessUtils.IsAlreadyRunning())
            {
                var result = MessageBox.Show(_globalizer.GetResourceString("Application_SingleInstanceLabel"), _globalizer.GetResourceString("Application_SingleInstanceTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    if (ProcessUtils.SwitchToCurrentInstance())
                    {
                        // Shut down the current process
                        Application.Current.Shutdown(0);

                        return;
                    }

                    MessageBox.Show(_globalizer.GetResourceString("Application_SingleInstance_FailedLabel"), _globalizer.GetResourceString("Application_SingleInstance_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            ApplicationStarted = true;

            // Initial configuration setting
            if (String.IsNullOrWhiteSpace(Config.Default.DataDir))
            {
                MessageBox.Show(_globalizer.GetResourceString("Application_DataDirectoryLabel"), _globalizer.GetResourceString("Application_DataDirectoryTitle"), MessageBoxButton.OK, MessageBoxImage.Information);

                while (String.IsNullOrWhiteSpace(Config.Default.DataDir))
                {
                    var dialog = new CommonOpenFileDialog();
                    dialog.EnsureFileExists = true;
                    dialog.IsFolderPicker = true;
                    dialog.Multiselect = false;
                    dialog.Title = _globalizer.GetResourceString("Application_DataDirectory_DialogTitle");
                    dialog.InitialDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
                    {
                        Environment.Exit(0);
                    }

                    var confirm = MessageBox.Show(String.Format(_globalizer.GetResourceString("Application_DataDirectory_ConfirmLabel"), Path.Combine(dialog.FileName, Config.Default.ProfilesDir), Path.Combine(dialog.FileName, Config.Default.SteamCmdDir)), _globalizer.GetResourceString("Application_DataDirectory_ConfirmTitle"), MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
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

            // initialize all the game data
            GameData.Initialize();

            DataFileDetails.PlayerFileExtension = Config.Default.PlayerFileExtension;
            DataFileDetails.TribeFileExtension = Config.Default.TribeFileExtension;

            Task.Factory.StartNew(async () => await App.DiscoverMachinePublicIP(forceOverride: true));
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (ApplicationStarted)
            {
                foreach(var server in ServerManager.Instance.Servers)
                {
                    try
                    {
                        server.Profile.Save(false, false, null);
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show(String.Format(_globalizer.GetResourceString("Application_Profile_SaveFailedLabel"), server.Profile.ProfileName, ex.Message, ex.StackTrace), _globalizer.GetResourceString("Application_Profile_SaveFailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                Config.Default.Save();
            }

            ApplicationStarted = false;

            base.OnExit(e);
        }

        public static void ReconfigureLogging()
        {               
            string logDir = Path.Combine(Config.Default.DataDir, Config.Default.LogsDir);

            System.IO.Directory.CreateDirectory(logDir);

            LogManager.Configuration.Variables["logDir"] = logDir;

            var fileTargets = LogManager.Configuration.AllTargets.OfType<FileTarget>();
            foreach (var fileTarget in fileTargets)
            {
                var fileName = Path.GetFileNameWithoutExtension(fileTarget.FileName.ToString());
                fileTarget.FileName = Path.Combine(logDir, $"{fileName}.log");
                fileTarget.ArchiveFileName = Path.Combine(logDir, $"{fileName}.{{#}}.log");
            }

            LogManager.ReconfigExistingLoggers();
        }   
    }
}
