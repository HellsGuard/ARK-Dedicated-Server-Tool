using ARK_Server_Manager.Lib;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ARK_Server_Manager.Lib.ViewModel;
using WPFSharp.Globalizer;
using System.Threading.Tasks;
using ARK_Server_Manager.Lib.Utils;
using System.Text;
using System.Windows.Data;
using System.Windows.Documents;
using ARK_Server_Manager.Lib.Model;

namespace ARK_Server_Manager
{
    public enum ServerSettingsCustomLevelsAction
    {
        ExportPlayerLevels,
        ImportPlayerLevels,
        UpdatePlayerXPCap,
        ExportDinoLevels,
        ImportDinoLevels,
        UpdateDinoXPCap,
    }

    public enum ServerSettingsResetAction
    {
        // Sections
        AdministrationSection,
        RulesSection,
        ChatAndNotificationsSection,
        HudAndVisualsSection,
        PlayerSettingsSection,
        DinoSettingsSection,
        EnvironmentSection,
        StructuresSection,
        EngramsSection,
        CustomLevelsSection,
        SOTFSection,

        // Properties
        MapNameIslandProperty,
        MapNameCenterProperty,
        MapNameScorchedEarthProperty,
        MapNameTotalConversionProperty,
        TotalConversionPrimitivePlusProperty,

        PlayerMaxXpProperty,
        DinoMaxXpProperty,
        PlayerPerLevelStatMultipliers,
        DinoWildPerLevelStatMultipliers,
        DinoTamedPerLevelStatMultipliers,
        DinoTamedAddPerLevelStatMultipliers,
        DinoTamedAffinityPerLevelStatMultipliers,
        RCONWindowExtents,
    }

    /// <summary>
    /// Interaction logic for ServerSettings.xaml
    /// </summary>
    partial class ServerSettingsControl : UserControl
    {
        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;
        private CancellationTokenSource _upgradeCancellationSource = null;

        // Using a DependencyProperty as the backing store for ServerManager.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentConfigProperty = DependencyProperty.Register(nameof(CurrentConfig), typeof(Config), typeof(ServerSettingsControl));
        public static readonly DependencyProperty DinoSettingsProperty = DependencyProperty.Register(nameof(BaseDinoSettings), typeof(DinoSettingsList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty IsAdministratorProperty = DependencyProperty.Register(nameof(IsAdministrator), typeof(bool), typeof(ServerSettingsControl), new PropertyMetadata(false));
        public static readonly DependencyProperty NetworkInterfacesProperty = DependencyProperty.Register(nameof(NetworkInterfaces), typeof(List<NetworkAdapterEntry>), typeof(ServerSettingsControl), new PropertyMetadata(new List<NetworkAdapterEntry>()));
        public static readonly DependencyProperty RuntimeProperty = DependencyProperty.Register(nameof(Runtime), typeof(ServerRuntime), typeof(ServerSettingsControl));
        public static readonly DependencyProperty ServerManagerProperty = DependencyProperty.Register(nameof(ServerManager), typeof(ServerManager), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty ServerProperty = DependencyProperty.Register(nameof(Server), typeof(Server), typeof(ServerSettingsControl), new PropertyMetadata(null, ServerPropertyChanged));
        public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(nameof(Settings), typeof(ServerProfile), typeof(ServerSettingsControl));
        public static readonly DependencyProperty SelectedCustomSectionProperty = DependencyProperty.Register(nameof(SelectedCustomSection), typeof(CustomSection), typeof(ServerSettingsControl));
        public static readonly DependencyProperty SelectedArkApplicationDinoProperty = DependencyProperty.Register(nameof(SelectedArkApplicationDino), typeof(ArkApplication), typeof(ServerSettingsControl), new PropertyMetadata(ArkApplication.All));
        public static readonly DependencyProperty SelectedArkApplicationEngramProperty = DependencyProperty.Register(nameof(SelectedArkApplicationEngram), typeof(ArkApplication), typeof(ServerSettingsControl), new PropertyMetadata(ArkApplication.All));
        public static readonly DependencyProperty SelectedArkApplicationResourceProperty = DependencyProperty.Register(nameof(SelectedArkApplicationResource), typeof(ArkApplication), typeof(ServerSettingsControl), new PropertyMetadata(ArkApplication.All));
        public static readonly DependencyProperty ServerFilesAdminsProperty = DependencyProperty.Register(nameof(ServerFilesAdmins), typeof(SteamUserList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty ServerFilesExclusiveProperty = DependencyProperty.Register(nameof(ServerFilesExclusive), typeof(SteamUserList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty ServerFilesWhitelistedProperty = DependencyProperty.Register(nameof(ServerFilesWhitelisted), typeof(SteamUserList), typeof(ServerSettingsControl), new PropertyMetadata(null));

        #region Properties
        public Config CurrentConfig
        {
            get { return GetValue(CurrentConfigProperty) as Config; }
            set { SetValue(CurrentConfigProperty, value); }
        }

        public DinoSettingsList BaseDinoSettings
        {
            get { return (DinoSettingsList)GetValue(DinoSettingsProperty); }
            set { SetValue(DinoSettingsProperty, value); }
        }

        public bool IsAdministrator
        {
            get { return (bool)GetValue(IsAdministratorProperty); }
            set { SetValue(IsAdministratorProperty, value); }
        }

        public List<NetworkAdapterEntry> NetworkInterfaces
        {
            get { return (List<NetworkAdapterEntry>)GetValue(NetworkInterfacesProperty); }
            set { SetValue(NetworkInterfacesProperty, value); }
        }

        public ServerRuntime Runtime
        {
            get { return GetValue(RuntimeProperty) as ServerRuntime; }
            set { SetValue(RuntimeProperty, value); }
        }

        public ServerManager ServerManager
        {
            get { return (ServerManager)GetValue(ServerManagerProperty); }
            set { SetValue(ServerManagerProperty, value); }
        }

        public Server Server
        {
            get { return (Server)GetValue(ServerProperty); }
            set { SetValue(ServerProperty, value); }
        }

        public ServerProfile Settings
        {
            get { return GetValue(SettingsProperty) as ServerProfile; }
            set { SetValue(SettingsProperty, value); }
        }

        public CustomSection SelectedCustomSection
        {
            get { return GetValue(SelectedCustomSectionProperty) as CustomSection; }
            set { SetValue(SelectedCustomSectionProperty, value); }
        }

        public ArkApplication SelectedArkApplicationDino
        {
            get { return (ArkApplication)GetValue(SelectedArkApplicationDinoProperty); }
            set { SetValue(SelectedArkApplicationDinoProperty, value); }
        }

        public ArkApplication SelectedArkApplicationEngram
        {
            get { return (ArkApplication)GetValue(SelectedArkApplicationEngramProperty); }
            set { SetValue(SelectedArkApplicationEngramProperty, value); }
        }

        public ArkApplication SelectedArkApplicationResource
        {
            get { return (ArkApplication)GetValue(SelectedArkApplicationResourceProperty); }
            set { SetValue(SelectedArkApplicationResourceProperty, value); }
        }

        public SteamUserList ServerFilesAdmins
        {
            get { return (SteamUserList)GetValue(ServerFilesAdminsProperty); }
            set { SetValue(ServerFilesAdminsProperty, value); }
        }

        public SteamUserList ServerFilesExclusive
        {
            get { return (SteamUserList)GetValue(ServerFilesExclusiveProperty); }
            set { SetValue(ServerFilesExclusiveProperty, value); }
        }

        public SteamUserList ServerFilesWhitelisted
        {
            get { return (SteamUserList)GetValue(ServerFilesWhitelistedProperty); }
            set { SetValue(ServerFilesWhitelistedProperty, value); }
        }
        #endregion

        public ServerSettingsControl()
        {
            this.CurrentConfig = Config.Default;
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this);

            this.ServerManager = ServerManager.Instance;
            this.IsAdministrator = SecurityUtils.IsAdministrator();

            this.BaseDinoSettings = new DinoSettingsList();
            this.ServerFilesAdmins = new SteamUserList();
            this.ServerFilesWhitelisted = new SteamUserList();

            // hook into the language change event
            GlobalizedApplication.Instance.GlobalizationManager.ResourceDictionaryChangedEvent += ResourceDictionaryChangedEvent;
        }

        #region Event Methods
        private static void ServerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ssc = (ServerSettingsControl)d;
            var oldserver = (Server)e.OldValue;
            var server = (Server)e.NewValue;
            if (server != null)
            {
                TaskUtils.RunOnUIThreadAsync(() =>
                    {
                        if (oldserver != null)
                        {
                            oldserver.Profile.Save(false);
                        }

                        ssc.Settings = server.Profile;
                        ssc.Runtime = server.Runtime;
                        ssc.ReinitializeNetworkAdapters();
                        ssc.RefreshDinoSettingsCombobox();
                        ssc.LoadServerFiles();
                    }).DoNotWait();
            }
        }

        private void ResourceDictionaryChangedEvent(object source, ResourceDictionaryChangedEventArgs e)
        {
            this.Settings.DinoSettings.UpdateForLocalization();

            this.RefreshDinoSettingsCombobox();
            this.HarvestResourceItemAmountClassMultipliersListBox.Items.Refresh();
            this.EngramsOverrideListView.Items.Refresh();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Window.GetWindow(this)?.Activate();
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBoxResult.None;

            switch (this.Runtime.Status)
            {
                case ServerRuntime.ServerStatus.Initializing:
                case ServerRuntime.ServerStatus.Running:
                    // check if the server is initialising, or if RCON is not enabled.
                    if (this.Runtime.Status == ServerRuntime.ServerStatus.Initializing || !this.Server.Profile.RCONEnabled)
                    {
                        result = MessageBox.Show(_globalizer.GetResourceString("ServerSettings_StartServer_StartingLabel"), _globalizer.GetResourceString("ServerSettings_StartServer_StartingTitle"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (result == MessageBoxResult.No)
                            return;

                        try
                        {
                            await this.Server.StopAsync();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_StopServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        try
                        {
                            var shutdownWindow = ShutdownWindow.OpenShutdownWindow(this.Server);
                            if (shutdownWindow == null)
                            {
                                MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ShutdownServer_AlreadyOpenLabel"), _globalizer.GetResourceString("ServerSettings_ShutdownServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            shutdownWindow.Owner = Window.GetWindow(this);
                            shutdownWindow.Closed += Window_Closed;
                            shutdownWindow.Show();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_ShutdownServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    break;

                case ServerRuntime.ServerStatus.Stopped:
                    Mutex mutex = null;
                    bool createdNew = false;

                    try
                    {
                        // try to establish a mutex for the profile.
                        mutex = new Mutex(true, ServerApp.GetMutexName(this.Server.Profile.InstallDirectory), out createdNew);

                        // check if the mutex was established
                        if (createdNew)
                        {
                            this.Settings.Save(false);

                            if (Config.Default.ServerUpdate_OnServerStart && !this.Server.Profile.AutoManagedMods)
                            {
                                if (!await UpdateServer(false, true, Config.Default.ServerUpdate_UpdateModsWhenUpdatingServer, true))
                                {
                                    if (MessageBox.Show("There was a problem while performing the server update. This may leave your server in a incomplete state.\r\n\r\nDo you want to continue with the server start, this could cause problems?", "Server Update", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                                        return;
                                }
                            }

                            string validateMessage;
                            if (!this.Server.Profile.Validate(out validateMessage))
                            {
                                if (MessageBox.Show($"The following validation problems were encountered.\r\n\r\n{validateMessage}\r\n\r\nDo you want to continue with the server start, this could cause problems?", "Profile Validation", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                                    return;
                            }

                            await this.Server.StartAsync();
                        }
                        else
                        {
                            // display an error message and exit
                            MessageBox.Show(_globalizer.GetResourceString("ServerSettings_StartServer_MutexFailedLabel"), _globalizer.GetResourceString("ServerSettings_StartServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_StartServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        if (mutex != null)
                        {
                            if (createdNew)
                            {
                                mutex.ReleaseMutex();
                                mutex.Dispose();
                            }
                            mutex = null;
                        }
                    }
                    break;
            }
        }

        private async void Upgrade_Click(object sender, RoutedEventArgs e)
        {
            switch (this.Runtime.Status)
            {
                case ServerRuntime.ServerStatus.Stopped:
                case ServerRuntime.ServerStatus.Uninstalled:
                    break;

                case ServerRuntime.ServerStatus.Running:
                case ServerRuntime.ServerStatus.Initializing:
                    var result = MessageBox.Show(_globalizer.GetResourceString("ServerSettings_UpgradeServer_RunningLabel"), _globalizer.GetResourceString("ServerSettings_UpgradeServer_RunningTitle"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.No)
                        return;

                    break;

                case ServerRuntime.ServerStatus.Updating:
                    return;
            }

            await UpdateServer(true, true, Config.Default.ServerUpdate_UpdateModsWhenUpdatingServer, false);
        }

        private async void ModUpgrade_Click(object sender, RoutedEventArgs e)
        {
            switch (this.Runtime.Status)
            {
                case ServerRuntime.ServerStatus.Stopped:
                case ServerRuntime.ServerStatus.Uninstalled:
                    break;

                default:
                    return;
            }

            await UpdateServer(true, false, true, false);
        }

        private void OpenRCON_Click(object sender, RoutedEventArgs e)
        {
            var window = RCONWindow.GetRCONForServer(this.Server);
            window.Closed += Window_Closed;
            window.Show();
            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }

            window.Focus();
        }

        private void OpenModDetails_Click(object sender, RoutedEventArgs e)
        {
            var window = new ModDetailsWindow(this.Server.Profile);
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            window.Show();
            window.Focus();
        }

        private void HelpSOTF_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Config.Default.ArkSotfUrl);
        }

        private void PatchNotes_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.SOTF_Enabled)
                Process.Start(Config.Default.ArkSotF_PatchNotesUrl);
            else
                Process.Start(Config.Default.ArkSE_PatchNotesUrl);
        }

        private void NeedAdmin_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(_globalizer.GetResourceString("ServerSettings_AdminRequired_ErrorLabel"), _globalizer.GetResourceString("ServerSettings_AdminRequired_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RefreshLocalIPs_Click(object sender, RoutedEventArgs e)
        {
            ReinitializeNetworkAdapters();
        }

        private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
        {
            var logFolder = Path.Combine(Updater.GetLogFolder(), this.Server.Profile.ProfileName);
            if (!Directory.Exists(logFolder))
                logFolder = Updater.GetLogFolder();
            if (!Directory.Exists(logFolder))
                logFolder = Config.Default.DataDir;
            Process.Start("explorer.exe", logFolder);
        }

        private void OpenServerFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", this.Server.Profile.InstallDirectory);
        }

        private async void CreateSupportZip_Click(object sender, RoutedEventArgs e)
        {
            const int MAX_DAYS = 2;

            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = Cursors.Wait);
                await Task.Delay(500);

                var obfuscateFiles = new Dictionary<string, string>();
                var files = new List<string>();
                var folder = string.Empty;
                var file = string.Empty;
                DirectoryInfo dirInfo;

                // <server>
                file = Path.Combine(this.Settings.InstallDirectory, Config.Default.LastUpdatedTimeFile);
                if (File.Exists(file)) files.Add(file);

                // <server>\ShooterGame\Content\Mods
                folder = Path.Combine(this.Settings.InstallDirectory, Config.Default.ServerModsRelativePath);
                dirInfo = new DirectoryInfo(folder);
                if (dirInfo.Exists)
                {
                    files.AddRange(dirInfo.GetFiles("*.mod").Select(modFile => modFile.FullName));
                    foreach (var modFolder in dirInfo.GetDirectories())
                    {
                        file = Path.Combine(modFolder.FullName, Config.Default.LastUpdatedTimeFile);
                        if (File.Exists(file)) files.Add(file);
                    }
                }

                // <server>\ShooterGame\Saved\Config\WindowsServer
                file = Path.Combine(this.Settings.InstallDirectory, Config.Default.ServerConfigRelativePath, "Game.ini");
                if (File.Exists(file))
                {
                    var iniFile = IniFileUtils.ReadFromFile(file);
                    if (iniFile != null)
                    {
                        obfuscateFiles.Add(file, iniFile.ToOutputString());
                    }
                }
                file = Path.Combine(this.Settings.InstallDirectory, Config.Default.ServerConfigRelativePath, "GameUserSettings.ini");
                if (File.Exists(file))
                {
                    var iniFile = IniFileUtils.ReadFromFile(file);
                    if (iniFile != null)
                    {
                        iniFile.WriteKey("ServerSettings", "ServerPassword", "obfuscated");
                        iniFile.WriteKey("ServerSettings", "ServerAdminPassword", "obfuscated");
                        iniFile.WriteKey("ServerSettings", "SpectatorPassword", "obfuscated");
                        obfuscateFiles.Add(file, iniFile.ToOutputString());
                    }
                }
                file = Path.Combine(this.Settings.InstallDirectory, Config.Default.ServerConfigRelativePath, "RunServer.cmd");
                if (File.Exists(file)) files.Add(file);

                // <server>\ShooterGame\Saved\Logs
                folder = Path.Combine(this.Settings.InstallDirectory, Config.Default.SavedRelativePath, "Logs");
                dirInfo = new DirectoryInfo(folder);
                if (dirInfo.Exists)
                {
                    files.AddRange(dirInfo.GetFiles("*.log").Where(f => f.LastWriteTime > DateTime.Today.AddDays(-MAX_DAYS)).Select(logFile => logFile.FullName));
                }

                // Logs
                folder = Path.Combine(Config.Default.DataDir, Config.Default.LogsDir, ServerApp.LOGPREFIX_AUTOUPDATE);
                dirInfo = new DirectoryInfo(folder);
                if (dirInfo.Exists)
                {
                    files.AddRange(dirInfo.GetFiles("*.log").Where(f => f.LastWriteTime > DateTime.Today.AddDays(-MAX_DAYS)).Select(logFile => logFile.FullName));
                }

                // Logs/<server>
                folder = Path.Combine(Config.Default.DataDir, Config.Default.LogsDir, this.Settings.ProfileName);
                dirInfo = new DirectoryInfo(folder);
                if (dirInfo.Exists)
                {
                    files.AddRange(dirInfo.GetFiles("*.*", SearchOption.AllDirectories).Where(f => f.LastWriteTime > DateTime.Today.AddDays(-MAX_DAYS)).Select(logFile => logFile.FullName));
                }

                // Profile
                file = this.Settings.GetProfileFile();
                if (File.Exists(file))
                {
                    var profileFile = ServerProfile.LoadFromProfileFile(file);
                    if (profileFile != null)
                    {
                        profileFile.AdminPassword = "obfuscated";
                        profileFile.ServerPassword = "obfuscated";
                        profileFile.SpectatorPassword = "obfuscated";
                        profileFile.WebAlarmKey = "obfuscated";
                        profileFile.WebAlarmUrl = "obfuscated";
                        obfuscateFiles.Add(file, profileFile.ToOutputString());
                    }
                }

                // <data folder>\SteamCMD\steamapps\workshop\content\<app id>
                if (this.Settings.SOTF_Enabled)
                    folder = Path.Combine(Config.Default.DataDir, Config.Default.SteamCmdDir, Config.Default.ArkSteamWorkshopFolderRelativePath_SotF);
                else
                    folder = Path.Combine(Config.Default.DataDir, Config.Default.SteamCmdDir, Config.Default.ArkSteamWorkshopFolderRelativePath);
                if (Directory.Exists(folder))
                {
                    foreach (var modFolder in Directory.GetDirectories(folder))
                    {
                        file = Path.Combine(modFolder, Config.Default.LastUpdatedTimeFile);
                        if (File.Exists(file)) files.Add(file);
                    }
                }

                if (!this.Settings.SOTF_Enabled)
                {
                    // <server cache>
                    if (!string.IsNullOrWhiteSpace(Config.Default.AutoUpdate_CacheDir))
                    {
                        file = Path.Combine(Config.Default.AutoUpdate_CacheDir, Config.Default.LastUpdatedTimeFile);
                        if (File.Exists(file)) files.Add(file);
                    }
                }

                var comment = new StringBuilder();
                comment.AppendLine($"ARK Version: {this.Settings.LastInstalledVersion}");
                comment.AppendLine($"ASM Version: {App.Version}");

                comment.AppendLine($"MachinePublicIP: {Config.Default.MachinePublicIP}");
                comment.AppendLine($"ASM Directory: {Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}");
                comment.AppendLine($"Config Directory: {Config.Default.ConfigDirectory}");
                comment.AppendLine($"Data Directory: {Config.Default.DataDir}");

                comment.AppendLine($"SotF Server: {this.Settings.SOTF_Enabled}");

                comment.AppendLine($"IsAdministrator: {SecurityUtils.IsAdministrator()}");
                comment.AppendLine($"RunAsAdministratorPrompt: {Config.Default.RunAsAdministratorPrompt}");
                comment.AppendLine($"ManageFirewallAutomatically: {Config.Default.ManageFirewallAutomatically}");
                comment.AppendLine($"SteamCmdRedirectOutput: {Config.Default.SteamCmdRedirectOutput}");
                comment.AppendLine($"SteamCmd_UseAnonymousCredentials: {Config.Default.SteamCmd_UseAnonymousCredentials}");

                comment.AppendLine($"AutoUpdate_EnableUpdate: {Config.Default.AutoUpdate_EnableUpdate}");
                comment.AppendLine($"AutoUpdate_CacheDir: {Config.Default.AutoUpdate_CacheDir}");
                comment.AppendLine($"AutoUpdate_UpdatePeriod: {Config.Default.AutoUpdate_UpdatePeriod}");
                comment.AppendLine($"AutoUpdate_UseSmartCopy: {Config.Default.AutoUpdate_UseSmartCopy}");

                comment.AppendLine($"ServerShutdown_EnableWorldSave: {Config.Default.ServerShutdown_EnableWorldSave}");
                comment.AppendLine($"ServerShutdown_GracePeriod: {Config.Default.ServerShutdown_GracePeriod}");
                comment.AppendLine($"ServerUpdate_UpdateModsWhenUpdatingServer: {Config.Default.ServerUpdate_UpdateModsWhenUpdatingServer}");
                comment.AppendLine($"ServerUpdate_ForceCopyMods: {Config.Default.ServerUpdate_ForceCopyMods}");
                comment.AppendLine($"ServerUpdate_ForceUpdateMods: {Config.Default.ServerUpdate_ForceUpdateMods}");
                comment.AppendLine($"EmailNotify_AutoRestart: {Config.Default.EmailNotify_AutoRestart}");
                comment.AppendLine($"EmailNotify_AutoUpdate: {Config.Default.EmailNotify_AutoUpdate}");

                var zipFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), Guid.NewGuid().ToString() + ".zip");
                ZipUtils.ZipFiles(zipFile, files.ToArray(), comment.ToString());

                foreach (var kvp in obfuscateFiles)
                {
                    ZipUtils.ZipAFile(zipFile, kvp.Key, kvp.Value);
                }

                MessageBox.Show($"The support zip file has been created and saved to your desktop.\r\nThe filename is {Path.GetFileName(zipFile)}", "Support ZipFile Creation", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Support ZipFile Creation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        private async void ValidateProfile_Click(object sender, RoutedEventArgs e)
        {
            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = Cursors.Wait);
                await Task.Delay(500);

                string validationMessage;
                var result = this.Settings.Validate(out validationMessage);

                if (result)
                    MessageBox.Show("The profile passed the basic validation.", "Profile Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                else
                    MessageBox.Show(validationMessage, "Profile Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Profile Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        private void SelectInstallDirectory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.Title = _globalizer.GetResourceString("ServerSettings_InstallServer_Title");
            if (!String.IsNullOrWhiteSpace(Settings.InstallDirectory))
            {
                dialog.InitialDirectory = Settings.InstallDirectory;
            }

            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                Settings.InstallDirectory = dialog.FileName;
            }
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.EnsureFileExists = true;
            dialog.Multiselect = false;
            dialog.Title = _globalizer.GetResourceString("ServerSettings_LoadConfig_Title");
            dialog.Filters.Add(new CommonFileDialogFilter("Profile", Config.Default.LoadProfileExtensionList));
            if (!Directory.Exists(Config.Default.ConfigDirectory))
            {
                System.IO.Directory.CreateDirectory(Config.Default.ConfigDirectory);
            }

            dialog.InitialDirectory = Config.Default.ConfigDirectory;
            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                try
                {
                    this.Server.ImportFromPath(dialog.FileName);
                    this.Settings = this.Server.Profile;
                    this.Runtime = this.Server.Runtime;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(String.Format(_globalizer.GetResourceString("ServerSettings_LoadConfig_ErrorLabel"), dialog.FileName, ex.Message, ex.StackTrace), _globalizer.GetResourceString("ServerSettings_LoadConfig_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        private void ShowCmd_Click(object sender, RoutedEventArgs e)
        {
            var cmdLine = new CommandLineWindow(String.Format("{0} {1}", this.Runtime.GetServerExe(), this.Settings.GetServerArgs()));
            cmdLine.Owner = Window.GetWindow(this);
            cmdLine.ShowDialog();
        }

        private void CreateSymLink_Click(object sender, RoutedEventArgs e)
        {
            // check if the ASM clusters folder exists
            var asmClustersFolder = Path.Combine(Config.Default.DataDir, Config.Default.ClustersDir);
            if (!Directory.Exists(asmClustersFolder))
                Directory.CreateDirectory(asmClustersFolder);

            var profileClustersFolder = Path.Combine(Settings.InstallDirectory, Config.Default.SavedRelativePath, Config.Default.ClustersDir);
            if (Directory.Exists(profileClustersFolder))
            {
                if (MachineUtils.IsDirectorySymbolic(profileClustersFolder))
                {
                    if (MessageBox.Show("Cluster folder already exists for this server, do you want to recreate?", "Cluster Folder Exists", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                        return;
                    Directory.Delete(profileClustersFolder);
                }
                else
                {
                    MessageBox.Show("The SymLink could not be created, a folder called Clusters already exists for this profile. Delete the Clusters folder and try again.", "Create Symlink Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            try
            {
                if (!MachineUtils.CreateSymLink(profileClustersFolder, asmClustersFolder, true))
                    MessageBox.Show("The SymLink could not be created.", "Create Symlink Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Create Symlink Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Settings.SetHasClusterSymLink();
        }

        private void ArkAutoSettings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ArkAutoSettings_ErrorLabel"), _globalizer.GetResourceString("ServerSettings_ArkAutoSettings_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RemovePlayerLevel_Click(object sender, RoutedEventArgs e)
        {
            if (this.Settings.PlayerLevels.Count == 1)
            {
                MessageBox.Show(_globalizer.GetResourceString("ServerSettings_CustomLevels_LastRemove_ErrorLabel"), _globalizer.GetResourceString("ServerSettings_CustomLevels_LastRemove_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Hand);
            }
            else
            {
                var level = ((Level)((Button)e.Source).DataContext);
                this.Settings.PlayerLevels.RemoveLevel(level);
            }
        }

        private void AddPlayerLevel_Click(object sender, RoutedEventArgs e)
        {
            var level = ((Level)((Button)e.Source).DataContext);
            this.Settings.PlayerLevels.AddNewLevel(level, Config.Default.CustomLevelXPIncrease_Player);
        }

        private void RemoveDinoLevel_Click(object sender, RoutedEventArgs e)
        {
            if (this.Settings.DinoLevels.Count == 1)
            {
                MessageBox.Show(_globalizer.GetResourceString("ServerSettings_CustomLevels_LastRemove_ErrorLabel"), _globalizer.GetResourceString("ServerSettings_CustomLevels_LastRemove_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Hand);
            }
            else
            {
                var level = ((Level)((Button)e.Source).DataContext);
                this.Settings.DinoLevels.RemoveLevel(level);
            }
        }

        private void AddDinoLevel_Click(object sender, RoutedEventArgs e)
        {
            var level = ((Level)((Button)e.Source).DataContext);
            this.Settings.DinoLevels.AddNewLevel(level, Config.Default.CustomLevelXPIncrease_Dino);
        }

        private void RemoveDinoSetting_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DinoCustomization_DinoRemoveRecordLabel"), _globalizer.GetResourceString("ServerSettings_DinoCustomization_DinoRemoveRecordTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var dino = ((DinoSettings)((Button)e.Source).DataContext);
            if (!dino.KnownDino)
            {
                this.Settings.DinoSettings.Remove(dino);
                RefreshDinoSettingsCombobox();
            }
        }

        private void RemoveHarvestResource_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_Harvest_HarvestRemoveRecordLabel"), _globalizer.GetResourceString("ServerSettings_Harvest_HarvestRemoveRecordTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var resource = ((ResourceClassMultiplier)((Button)e.Source).DataContext);
            if (!resource.KnownResource)
                this.Settings.HarvestResourceItemAmountClassMultipliers.Remove(resource);
        }

        private void RemoveEngramOverride_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_EngramsOverride_EngramsRemoveRecordLabel"), _globalizer.GetResourceString("ServerSettings_EngramsOverride_EngramsRemoveRecordTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var engram = ((EngramEntry)((Button)e.Source).DataContext);
            if (!engram.KnownEngram)
                this.Settings.OverrideNamedEngramEntries.Remove(engram);
        }

        private void AddCustomItem_Click(object sender, RoutedEventArgs e)
        {
            SelectedCustomSection?.Add(string.Empty, string.Empty);
        }

        private void AddCustomSection_Click(object sender, RoutedEventArgs e)
        {
            Settings.CustomGameUserSettingsSections.Add(string.Empty, new string[0]);
        }

        private void ClearCustomItems_Click(object sender, RoutedEventArgs e)
        {
            SelectedCustomSection?.Clear();
        }

        private void ClearCustomSections_Click(object sender, RoutedEventArgs e)
        {
            SelectedCustomSection = null;
            Settings.CustomGameUserSettingsSections.Clear();
        }

        private void ImportCustomSections_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.EnsureFileExists = true;
            dialog.Multiselect = false;
            dialog.Title = _globalizer.GetResourceString("ServerSettings_LoadCustomConfig_Title");
            dialog.Filters.Add(new CommonFileDialogFilter("Ini Files", "*.ini"));
            dialog.InitialDirectory = Settings.InstallDirectory;
            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                try
                {
                    // read the selected ini file.
                    var iniFile = IniFileUtils.ReadFromFile(dialog.FileName);

                    // cycle through the sections, adding them to the custom section list. Will bypass any sections that are named as per the ARK default sections.
                    foreach (var section in iniFile.Sections.Where(s => !string.IsNullOrWhiteSpace(s.SectionName) && !SystemIniFile.SectionNames.ContainsValue(s.SectionName)))
                    {
                        Settings.CustomGameUserSettingsSections.Add(section.SectionName, section.KeysToStringArray(), false);
                    }

                    MessageBox.Show(_globalizer.GetResourceString("ServerSettings_LoadCustomConfig_Label"), _globalizer.GetResourceString("ServerSettings_LoadCustomConfig_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_LoadCustomConfig_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        private void PasteCustomItems_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedCustomSection == null)
                return;

            var window = new CustomConfigDataWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (result.HasValue && result.Value)
            {
                // read the pasted data into an ini file.
                var iniFile = IniFileUtils.ReadString(window.ConfigData);
                // get the section with the same name as the currently selected custom section.
                var section = iniFile?.GetSection(SelectedCustomSection.SectionName);
                // check if the section exists.
                if (section == null)
                    // section is not exists, get the section with the empty name.
                    section = iniFile?.GetSection(string.Empty) ?? new IniSection();

                // cycle through the section keys, adding them to the selected custom section.
                foreach (var key in section.Keys)
                {
                    // check if the key name has been defined.
                    if (!string.IsNullOrWhiteSpace(key.KeyName))
                        SelectedCustomSection.Add(key.KeyName, key.KeyValue);
                }
            }
        }

        private void PasteCustomSections_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomConfigDataWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (result.HasValue && result.Value)
            {
                // read the pasted data into an ini file.
                var iniFile = IniFileUtils.ReadString(window.ConfigData);

                // cycle through the sections, adding them to the custom section list. Will bypass any sections that are named as per the ARK default sections.
                foreach (var section in iniFile.Sections.Where(s => !string.IsNullOrWhiteSpace(s.SectionName) && !SystemIniFile.SectionNames.ContainsValue(s.SectionName)))
                {
                    Settings.CustomGameUserSettingsSections.Add(section.SectionName, section.KeysToStringArray(), false);
                }
            }
        }

        private void RemoveCustomItem_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedCustomSection == null)
                return;

            var item = ((CustomItem)((Button)e.Source).DataContext);
            SelectedCustomSection.Remove(item);
        }

        private void RemoveCustomSection_Click(object sender, RoutedEventArgs e)
        {
            var section = ((CustomSection)((Button)e.Source).DataContext);
            Settings.CustomGameUserSettingsSections.Remove(section);
        }

        private void PlayerLevels_Recalculate(object sender, RoutedEventArgs e)
        {
            this.Settings.PlayerLevels.UpdateTotals();
            this.CustomPlayerLevelsView.Items.Refresh();
        }

        private void DinoLevels_Recalculate(object sender, RoutedEventArgs e)
        {
            this.Settings.DinoLevels.UpdateTotals();
            this.CustomDinoLevelsView.Items.Refresh();
        }

        private void DinoLevels_Clear(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DinoLevels_ClearLabel"), _globalizer.GetResourceString("ServerSettings_DinoLevels_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.ClearLevelProgression(ServerProfile.LevelProgression.Dino);
        }

        private void DinoLevels_ResetOfficial(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DinoLevels_ResetLabel"), _globalizer.GetResourceString("ServerSettings_DinoLevels_ResetTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.ResetLevelProgressionToOfficial(ServerProfile.LevelProgression.Dino);
        }

        private void PlayerLevels_Clear(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_PlayerLevels_ClearLabel"), _globalizer.GetResourceString("ServerSettings_PlayerLevels_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.ClearLevelProgression(ServerProfile.LevelProgression.Player);
        }

        private void PlayerLevels_ResetOfficial(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_PlayerLevels_ResetLabel"), _globalizer.GetResourceString("ServerSettings_PlayerLevels_ResetTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.ResetLevelProgressionToOfficial(ServerProfile.LevelProgression.Player);
        }

        private void HarvestResourceItemAmountClassMultipliers_Reset(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_CustomHarvest_ResetLabel"), _globalizer.GetResourceString("ServerSettings_CustomHarvest_ResetTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.HarvestResourceItemAmountClassMultipliers.Reset();
        }

        private void Engrams_Reset(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_EngramsOverride_ResetLabel"), _globalizer.GetResourceString("ServerSettings_EngramsOverride_ResetTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.OverrideNamedEngramEntries.Reset();
        }

        private void DinoCustomization_Reset(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DinoCustomization_ResetLabel"), _globalizer.GetResourceString("ServerSettings_DinoCustomization_ResetTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.DinoSettings.Reset();
            RefreshDinoSettingsCombobox();
        }

        private void MaxXPPlayer_Reset(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_PlayerMaxXP_ResetLabel"), _globalizer.GetResourceString("ServerSettings_PlayerMaxXP_ResetTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.ResetOverrideMaxExperiencePointsPlayer();
        }

        private void MaxXPDino_Reset(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DinoMaxXP_ResetLabel"), _globalizer.GetResourceString("ServerSettings_DinoMaxXP_ResetTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.ResetOverrideMaxExperiencePointsDino();
        }

        private void HiddenField_GotFocus(object sender, RoutedEventArgs e)
        {
            var hideTextBox = sender as TextBox;
            if (hideTextBox != null)
            {
                TextBox textBox = null;
                if (hideTextBox == HideServerPasswordTextBox) 
                    textBox = ServerPasswordTextBox;
                if (hideTextBox == HideAdminPasswordTextBox)
                    textBox = AdminPasswordTextBox;
                if (hideTextBox == HideSpectatorPasswordTextBox)
                    textBox = SpectatorPasswordTextBox;
                if (hideTextBox == HideWebKeyTextBox)
                    textBox = WebKeyTextBox;
                if (hideTextBox == HideWebURLTextBox)
                    textBox = WebURLTextBox;

                if (textBox != null)
                {
                    textBox.Visibility = System.Windows.Visibility.Visible;
                    hideTextBox.Visibility = System.Windows.Visibility.Collapsed;
                    textBox.Focus();
                }
                UpdateLayout();
            }
        }

        private void HiddenField_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                TextBox hideTextBox = null;
                if (textBox == ServerPasswordTextBox)
                    hideTextBox = HideServerPasswordTextBox;
                if (textBox == AdminPasswordTextBox)
                    hideTextBox = HideAdminPasswordTextBox;
                if (textBox == SpectatorPasswordTextBox)
                    hideTextBox = HideSpectatorPasswordTextBox;
                if (textBox == WebKeyTextBox)
                    hideTextBox = HideWebKeyTextBox;
                if (textBox == WebURLTextBox)
                    hideTextBox = HideWebURLTextBox;

                if (hideTextBox != null)
                {
                    hideTextBox.Visibility = System.Windows.Visibility.Visible;
                    textBox.Visibility = System.Windows.Visibility.Collapsed;
                }
                UpdateLayout();
            }
        }

        private void DinoArkApplications_OnFilter(object sender, FilterEventArgs e)
        {
            var item = e.Item as DinoSettings;
            if (item == null)
                e.Accepted = false;
            else
                e.Accepted = (SelectedArkApplicationDino == ArkApplication.All || item.ArkApplication == SelectedArkApplicationDino);
        }

        private void DinoArkApplications_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            var view = this.DinoSettingsGrid.ItemsSource as ListCollectionView;
            view?.Refresh();
        }

        private void EngramArkApplications_OnFilter(object sender, FilterEventArgs e)
        {
            var item = e.Item as EngramEntry;
            if (item == null)
                e.Accepted = false;
            else
                e.Accepted = (SelectedArkApplicationEngram == ArkApplication.All || item.ArkApplication == SelectedArkApplicationEngram);
        }

        private void EngramArkApplications_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            var view = this.EngramsOverrideListView.ItemsSource as ListCollectionView;
            view?.Refresh();
        }

        private void ResourceArkApplications_OnFilter(object sender, FilterEventArgs e)
        {
            var item = e.Item as ResourceClassMultiplier;
            if (item == null)
                e.Accepted = false;
            else
                e.Accepted = (SelectedArkApplicationResource == ArkApplication.All || item.ArkApplication == SelectedArkApplicationResource);
        }

        private void ResourceArkApplications_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            var view = this.HarvestResourceItemAmountClassMultipliersListBox.ItemsSource as ListCollectionView;
            view?.Refresh();
        }

        private void AddAdminPlayer_Click(object sender, RoutedEventArgs e)
        {
            var window = new AddSteamUserWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (result.HasValue && result.Value)
            {
                try
                {
                    var steamIdsString = window.SteamUsers;
                    var steamIds = steamIdsString.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    var steamUsers = SteamUtils.GetSteamUserDetails(steamIds.ToList());
                    var steamUserList = SteamUserList.GetList(steamUsers);
                    this.ServerFilesAdmins.AddRange(steamUserList);

                    SaveServerFileAdministrators();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Add Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddExclusivePlayer_Click(object sender, RoutedEventArgs e)
        {
            var window = new AddSteamUserWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (result.HasValue && result.Value)
            {
                try
                {
                    var steamIdsString = window.SteamUsers;
                    var steamIds = steamIdsString.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    var steamUsers = SteamUtils.GetSteamUserDetails(steamIds.ToList());
                    var steamUserList = SteamUserList.GetList(steamUsers);
                    this.ServerFilesExclusive.AddRange(steamUserList);

                    SaveServerFileExclusive();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Add Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddWhitelistPlayer_Click(object sender, RoutedEventArgs e)
        {
            var window = new AddSteamUserWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (result.HasValue && result.Value)
            {
                try
                {
                    var steamIdsString = window.SteamUsers;
                    var steamIds = steamIdsString.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    var steamUsers = SteamUtils.GetSteamUserDetails(steamIds.ToList());
                    var steamUserList = SteamUserList.GetList(steamUsers);
                    this.ServerFilesWhitelisted.AddRange(steamUserList);

                    SaveServerFileWhitelisted();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Add Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClearAdminPlayers_Click(object sender, RoutedEventArgs e)
        {
            ServerFilesAdmins.Clear();

            SaveServerFileAdministrators();
        }

        private void ClearExclusivePlayers_Click(object sender, RoutedEventArgs e)
        {
            ServerFilesExclusive.Clear();

            SaveServerFileExclusive();
        }

        private void ClearWhitelistPlayers_Click(object sender, RoutedEventArgs e)
        {
            ServerFilesWhitelisted.Clear();

            SaveServerFileWhitelisted();
        }

        private async void ReloadAdminPlayers_Click(object sender, RoutedEventArgs e)
        {
            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = Cursors.Wait);
                await Task.Delay(500);

                LoadServerFileAdministrators();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Refresh Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        private async void ReloadExclusivePlayers_Click(object sender, RoutedEventArgs e)
        {
            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = Cursors.Wait);
                await Task.Delay(500);

                LoadServerFileExclusive();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Refresh Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        private async void ReloadWhitelistPlayers_Click(object sender, RoutedEventArgs e)
        {
            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = Cursors.Wait);
                await Task.Delay(500);

                LoadServerFileWhitelisted();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Refresh Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        private void RemoveAdminPlayer_Click(object sender, RoutedEventArgs e)
        {
            var mod = ((SteamUserItem)((Button)e.Source).DataContext);
            ServerFilesAdmins.Remove(mod);

            SaveServerFileAdministrators();
        }

        private void RemoveExclusivePlayer_Click(object sender, RoutedEventArgs e)
        {
            var mod = ((SteamUserItem)((Button)e.Source).DataContext);
            ServerFilesExclusive.Remove(mod);

            SaveServerFileExclusive();
        }

        private void RemoveWhitelistPlayer_Click(object sender, RoutedEventArgs e)
        {
            var mod = ((SteamUserItem)((Button)e.Source).DataContext);
            ServerFilesWhitelisted.Remove(mod);

            SaveServerFileWhitelisted();
        }

        private void SteamProfileNavigate_Click(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            var item = ((SteamUserItem)((Hyperlink)e.Source).DataContext);

            Process.Start(new ProcessStartInfo(item.ProfileUrl));
            e.Handled = true;
        }
        #endregion

        #region Methods
        private CommonFileDialog GetCustomLevelCommonFileDialog(ServerSettingsCustomLevelsAction action)
        {
            CommonFileDialog dialog = null;

            switch (action)
            {
                case ServerSettingsCustomLevelsAction.ExportDinoLevels:
                case ServerSettingsCustomLevelsAction.ExportPlayerLevels:
                    dialog = new CommonSaveFileDialog();
                    dialog.Title = GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_ExportDialogTitle");
                    dialog.DefaultExtension = GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_ExportDefaultExtension");
                    dialog.Filters.Add(new CommonFileDialogFilter(GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_ExportFilterLabel"), GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_ExportFilterExtension")));
                    break;

                case ServerSettingsCustomLevelsAction.ImportDinoLevels:
                case ServerSettingsCustomLevelsAction.ImportPlayerLevels:
                    dialog = new CommonOpenFileDialog();
                    dialog.Title = GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_ImportDialogTitle");
                    dialog.DefaultExtension = GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_ImportDefaultExtension");
                    dialog.Filters.Add(new CommonFileDialogFilter(GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_ImportFilterLabel"), GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_ImportFilterExtension")));
                    break;
            }

            return dialog;
        }

        public ICommand CustomLevelActionCommand
        {
            get
            {
                return new RelayCommand<ServerSettingsCustomLevelsAction>(
                    execute: (action) =>
                    {
                        var errorTitle = GlobalizedApplication.Instance.GetResourceString("Generic_ErrorLabel");

                        try
                        {
                            var dialog = GetCustomLevelCommonFileDialog(action);
                            var dialogValue = string.Empty;
                            if (dialog != null && dialog.ShowDialog() == CommonFileDialogResult.Ok)
                                dialogValue = dialog.FileName;

                            switch (action)
                            {
                                case ServerSettingsCustomLevelsAction.ExportDinoLevels:
                                    errorTitle = GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_ExportErrorTitle");

                                    this.Settings.ExportDinoLevels(dialogValue);
                                    break;

                                case ServerSettingsCustomLevelsAction.ImportDinoLevels:
                                    errorTitle = GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_ImportErrorTitle");

                                    this.Settings.ImportDinoLevels(dialogValue);
                                    break;

                                case ServerSettingsCustomLevelsAction.UpdateDinoXPCap:
                                    errorTitle = GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_UpdateErrorTitle");

                                    this.Settings.UpdateOverrideMaxExperiencePointsDino();
                                    break;

                                case ServerSettingsCustomLevelsAction.ExportPlayerLevels:
                                    errorTitle = GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_ExportErrorTitle");

                                    this.Settings.ExportPlayerLevels(dialogValue);
                                    break;

                                case ServerSettingsCustomLevelsAction.ImportPlayerLevels:
                                    errorTitle = GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_ImportErrorTitle");

                                    this.Settings.ImportPlayerLevels(dialogValue);
                                    break;

                                case ServerSettingsCustomLevelsAction.UpdatePlayerXPCap:
                                    errorTitle = GlobalizedApplication.Instance.GetResourceString("ServerSettings_CustomLevel_UpdateErrorTitle");

                                    this.Settings.UpdateOverrideMaxExperiencePointsPlayer();
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, errorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    },
                    canExecute: (action) => true
                );
            }
        }

        public void RefreshDinoSettingsCombobox()
        {
            this.BaseDinoSettings = this.Settings.DinoSettings.Clone();
            this.DinoSettingsGrid.Items.Refresh();
        }

        private void ReinitializeNetworkAdapters()
        {
            var adapters = NetworkUtils.GetAvailableIPV4NetworkAdapters();

            //
            // Filter out self-assigned addresses
            //
            adapters.RemoveAll(a => a.IPAddress.StartsWith("169.254."));
            adapters.Insert(0, new NetworkAdapterEntry(String.Empty, _globalizer.GetResourceString("ServerSettings_LocalIPArkChooseLabel")));
            var savedServerIp = this.Settings.ServerIP;
            this.NetworkInterfaces = adapters;
            this.Settings.ServerIP = savedServerIp;


            //
            // If there isn't already an adapter assigned, pick one
            //
            var preferredIP = NetworkUtils.GetPreferredIP(adapters);
            preferredIP.Description = _globalizer.GetResourceString("ServerSettings_LocalIPRecommendedLabel") + " " + preferredIP.Description;
            if (String.IsNullOrWhiteSpace(this.Settings.ServerIP))
            {
                // removed to enforce the 'Let ARK choose' option.
                //if (preferredIP != null)
                //{
                //    this.Settings.ServerIP = preferredIP.IPAddress;
                //}
            }
            else if (adapters.FirstOrDefault(a => String.Equals(a.IPAddress, this.Settings.ServerIP, StringComparison.OrdinalIgnoreCase)) == null)
            {
                MessageBox.Show(
                    String.Format(_globalizer.GetResourceString("ServerSettings_LocalIP_ErrorLabel"), this.Settings.ServerIP),
                    _globalizer.GetResourceString("ServerSettings_LocalIP_ErrorTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public ICommand ResetActionCommand
        {
            get
            {
                return new RelayCommand<ServerSettingsResetAction>(
                    execute: (action) =>
                    {
                        if (action != ServerSettingsResetAction.MapNameTotalConversionProperty)
                        {
                            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ResetLabel"), _globalizer.GetResourceString("ServerSettings_ResetTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                                return;
                        }

                        switch (action)
                        {
                            // sections
                            case ServerSettingsResetAction.AdministrationSection:
                                this.Settings.ResetAdministrationSection();
                                break;

                            case ServerSettingsResetAction.ChatAndNotificationsSection:
                                this.Settings.ResetChatAndNotificationSection();
                                break;

                            case ServerSettingsResetAction.CustomLevelsSection:
                                this.Settings.ResetCustomLevelsSection();
                                break;

                            case ServerSettingsResetAction.DinoSettingsSection:
                                this.Settings.ResetDinoSettings();
                                RefreshDinoSettingsCombobox();
                                break;

                            case ServerSettingsResetAction.EngramsSection:
                                this.Settings.ResetEngramsSection();
                                break;

                            case ServerSettingsResetAction.EnvironmentSection:
                                this.Settings.ResetEnvironmentSection();
                                break;

                            case ServerSettingsResetAction.HudAndVisualsSection:
                                this.Settings.ResetHUDAndVisualsSection();
                                break;

                            case ServerSettingsResetAction.PlayerSettingsSection:
                                this.Settings.ResetPlayerSettings();
                                break;

                            case ServerSettingsResetAction.RulesSection:
                                this.Settings.ResetRulesSection();
                                break;

                            case ServerSettingsResetAction.SOTFSection:
                                this.Settings.ResetSOTFSection();
                                break;

                            case ServerSettingsResetAction.StructuresSection:
                                this.Settings.ResetStructuresSection();
                                break;

                            // Properties
                            case ServerSettingsResetAction.MapNameIslandProperty:
                                this.Settings.ResetMapName(Config.Default.DefaultServerMap_TheIsland);
                                break;

                            case ServerSettingsResetAction.MapNameCenterProperty:
                                this.Settings.ResetMapName(Config.Default.DefaultServerMap_TheCenter);
                                break;

                            case ServerSettingsResetAction.MapNameScorchedEarthProperty:
                                this.Settings.ResetMapName(Config.Default.DefaultServerMap_ScorchedEarth);
                                break;

                            case ServerSettingsResetAction.MapNameTotalConversionProperty:
                                // set the map name to the ARK default.
                                var mapName = Config.Default.DefaultServerMap_TheIsland;

                                // check if we are running an official total conversion mod.
                                if (!this.Settings.TotalConversionModId.Equals(Config.Default.DefaultTotalConversion_PrimitivePlus))
                                {
                                    // we need to read the mod file and retreive the map name
                                    mapName = ModUtils.GetMapName(this.Settings.InstallDirectory, this.Settings.TotalConversionModId);
                                    if (string.IsNullOrWhiteSpace(mapName))
                                    {
                                        MessageBox.Show("The map name could not be found, please check the total conversion mod id is correct and the mod has been downloaded.", "Find Total Conversion Map Name Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                        break;
                                    }
                                }

                                this.Settings.ServerMap = mapName;

                                MessageBox.Show("The map name has been updated.", "Find Total Conversion Map Name", MessageBoxButton.OK, MessageBoxImage.Information);
                                break;

                            case ServerSettingsResetAction.TotalConversionPrimitivePlusProperty:
                                this.Settings.TotalConversionModId = Config.Default.DefaultTotalConversion_PrimitivePlus;
                                this.Settings.ServerMap = Config.Default.DefaultServerMap_TheIsland;
                                break;

                            case ServerSettingsResetAction.PlayerMaxXpProperty:
                                this.Settings.ResetOverrideMaxExperiencePointsPlayer();
                                break;

                            case ServerSettingsResetAction.DinoMaxXpProperty:
                                this.Settings.ResetOverrideMaxExperiencePointsDino();
                                break;

                            case ServerSettingsResetAction.PlayerPerLevelStatMultipliers:
                                this.Settings.PerLevelStatsMultiplier_Player.Reset();
                                break;

                            case ServerSettingsResetAction.DinoWildPerLevelStatMultipliers:
                                this.Settings.PerLevelStatsMultiplier_DinoWild.Reset();
                                break;

                            case ServerSettingsResetAction.DinoTamedPerLevelStatMultipliers:
                                this.Settings.PerLevelStatsMultiplier_DinoTamed.Reset();
                                break;

                            case ServerSettingsResetAction.DinoTamedAddPerLevelStatMultipliers:
                                this.Settings.PerLevelStatsMultiplier_DinoTamed_Add.Reset();
                                break;

                            case ServerSettingsResetAction.DinoTamedAffinityPerLevelStatMultipliers:
                                this.Settings.PerLevelStatsMultiplier_DinoTamed_Affinity.Reset();
                                break;

                            case ServerSettingsResetAction.RCONWindowExtents:
                                this.Settings.ResetRCONWindowExtents();
                                break;
                        }
                    },
                    canExecute: (action) => true
                );
            }
        }

        public ICommand SaveCommand
        {
            get
            {
                return new RelayCommand<object>(
                    execute: (parameter) =>
                    {
                        // NOTE: This parameter is of type object and must be cast in most cases before use.
                        var settings = (Server)parameter;
                        if (settings.Profile.EnableAutoRestart)
                        {
                            if (settings.Profile.SOTF_Enabled)
                            {
                                MessageBox.Show(_globalizer.GetResourceString("ServerSettings_Save_AutoRestart_SotF_ErrorLabel"), _globalizer.GetResourceString("ServerSettings_Save_AutoRestart_SotF_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                                settings.Profile.EnableAutoRestart = false;
                                settings.Profile.AutoRestartIfShutdown = false;
                            }
                        }

                        if (settings.Profile.EnableAutoUpdate)
                        {
                            if (settings.Profile.SOTF_Enabled)
                            {
                                MessageBox.Show(_globalizer.GetResourceString("ServerSettings_Save_AutoUpdate_SotF_ErrorLabel"), _globalizer.GetResourceString("ServerSettings_Save_AutoUpdate_SotF_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                                settings.Profile.EnableAutoUpdate = false;
                                settings.Profile.AutoRestartIfShutdown = false;
                            }
                        }

                        settings.Profile.Save(false);

                        if (!settings.Profile.UpdateSchedules())
                        {
                            MessageBox.Show(_globalizer.GetResourceString("ServerSettings_Save_UpdateSchedule_ErrorLabel"), _globalizer.GetResourceString("ServerSettings_Save_UpdateSchedule_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    },
                    canExecute: (parameter) =>
                    {
                        bool canSave = true;

                        // NOTE: Some logic if necessary.  If this return's false, the associated object to which this command is bound (like the Save button in this case) will be automatically disabled,
                        // eliminating any extra Xaml binding for the IsEnabled property.
                        return canSave;
                    }
                );
            }
        }

        private async Task<bool> UpdateServer(bool establishLock, bool updateServer, bool updateMods, bool closeProgressWindow)
        {
            if (_upgradeCancellationSource != null)
                return false;

            ProgressWindow window = null;
            Mutex mutex = null;
            bool createdNew = !establishLock;

            try
            {
                if (establishLock)
                {
                    // try to establish a mutex for the profile.
                    mutex = new Mutex(true, ServerApp.GetMutexName(this.Server.Profile.InstallDirectory), out createdNew);
                }

                // check if the mutex was established
                if (createdNew)
                {
                    this._upgradeCancellationSource = new CancellationTokenSource();

                    window = new ProgressWindow(string.Format(_globalizer.GetResourceString("Progress_UpgradeServer_WindowTitle"), this.Server.Profile.ProfileName));
                    window.Owner = Window.GetWindow(this);
                    window.Closed += Window_Closed;
                    window.Show();

                    await Task.Delay(1000);
                    return await this.Server.UpgradeAsync(_upgradeCancellationSource.Token, updateServer, true, updateMods, (p, m, n) => { TaskUtils.RunOnUIThreadAsync(() => { window?.AddMessage(m, n); }).DoNotWait(); });
                }
                else
                {
                    // display an error message and exit
                    MessageBox.Show(_globalizer.GetResourceString("ServerSettings_UpgradeServer_MutexFailedLabel"), _globalizer.GetResourceString("ServerSettings_UpgradeServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (window != null)
                {
                    window.AddMessage(ex.Message);
                    window.AddMessage(ex.StackTrace);
                }
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_UpgradeServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            finally
            {
                this._upgradeCancellationSource = null;

                if (window != null)
                {
                    window.CloseWindow();
                    if (closeProgressWindow)
                        window.Close();
                }

                if (mutex != null)
                {
                    if (createdNew)
                    {
                        mutex.ReleaseMutex();
                        mutex.Dispose();
                    }
                    mutex = null;
                }
            }
        }

        private void LoadServerFiles()
        {
            LoadServerFileAdministrators();
            LoadServerFileExclusive();
            LoadServerFileWhitelisted();
        }

        private void LoadServerFileAdministrators()
        {
            try
            {
                this.ServerFilesAdmins = new SteamUserList();

                var file = Path.Combine(Settings.InstallDirectory, Config.Default.SavedRelativePath, Config.Default.ArkAdminFile);
                if (!File.Exists(file))
                    return;

                var steamIds = File.ReadAllLines(file);
                var steamUsers = SteamUtils.GetSteamUserDetails(steamIds.ToList());

                this.ServerFilesAdmins = SteamUserList.GetList(steamUsers);
            }
            catch (Exception ex)
            {
                this.ServerFilesAdmins = new SteamUserList();
                MessageBox.Show(ex.Message, "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadServerFileExclusive()
        {
            try
            {
                this.ServerFilesExclusive = new SteamUserList();

                var file = Path.Combine(Settings.InstallDirectory, Config.Default.ServerBinaryRelativePath, Config.Default.ArkExclusiveFile);
                if (!File.Exists(file))
                    return;

                var steamIds = File.ReadAllLines(file);
                var steamUsers = SteamUtils.GetSteamUserDetails(steamIds.ToList());

                this.ServerFilesExclusive = SteamUserList.GetList(steamUsers);
            }
            catch (Exception ex)
            {
                this.ServerFilesExclusive = new SteamUserList();
                MessageBox.Show(ex.Message, "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadServerFileWhitelisted()
        {
            try
            {
                this.ServerFilesWhitelisted = new SteamUserList();

                var file = Path.Combine(Settings.InstallDirectory, Config.Default.ServerBinaryRelativePath, Config.Default.ArkWhitelistFile);
                if (!File.Exists(file))
                    return;

                var steamIds = File.ReadAllLines(file);
                var steamUsers = SteamUtils.GetSteamUserDetails(steamIds.ToList());

                this.ServerFilesWhitelisted = SteamUserList.GetList(steamUsers);
            }
            catch (Exception ex)
            {
                this.ServerFilesWhitelisted = new SteamUserList();
                MessageBox.Show(ex.Message, "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveServerFileAdministrators()
        {
            try
            {
                var file = Path.Combine(Settings.InstallDirectory, Config.Default.SavedRelativePath, Config.Default.ArkAdminFile);

                File.WriteAllLines(file, this.ServerFilesAdmins.ToArray());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveServerFileExclusive()
        {
            try
            {
                var file = Path.Combine(Settings.InstallDirectory, Config.Default.ServerBinaryRelativePath, Config.Default.ArkExclusiveFile);

                File.WriteAllLines(file, this.ServerFilesExclusive.ToArray());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveServerFileWhitelisted()
        {
            try
            {
                var file = Path.Combine(Settings.InstallDirectory, Config.Default.ServerBinaryRelativePath, Config.Default.ArkWhitelistFile);

                File.WriteAllLines(file, this.ServerFilesWhitelisted.ToArray());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion
    }
}
