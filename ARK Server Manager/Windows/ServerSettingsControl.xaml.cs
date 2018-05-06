using ARK_Server_Manager.Lib;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ARK_Server_Manager.Lib.ViewModel;
using WPFSharp.Globalizer;
using System.Threading.Tasks;
using ARK_Server_Manager.Lib.Utils;
using System.Text;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using ARK_Server_Manager.Lib.Model;
using static ARK_Server_Manager.Lib.ServerApp;
using ArkServerManager.Plugin.Common;

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
        PGMSection,
        MapSpawnerOverridesSection,
        CraftingOverridesSection,
        SupplyCrateOverridesSection,

        // Properties
        MapNameTotalConversionProperty,
        BanListProperty,

        PlayerMaxXpProperty,
        DinoMaxXpProperty,
        PlayerBaseStatMultipliers,
        PlayerPerLevelStatMultipliers,
        DinoWildPerLevelStatMultipliers,
        DinoTamedPerLevelStatMultipliers,
        DinoTamedAddPerLevelStatMultipliers,
        DinoTamedAffinityPerLevelStatMultipliers,
        RCONWindowExtents,
        ServerOptions,
        ServerLogOptions,
    }

    /// <summary>
    /// Interaction logic for ServerSettings.xaml
    /// </summary>
    partial class ServerSettingsControl : UserControl
    {
        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;
        private CancellationTokenSource _upgradeCancellationSource = null;

        // Using a DependencyProperty as the backing store for ServerManager.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BaseArkApplicationListProperty = DependencyProperty.Register(nameof(BaseArkApplicationList), typeof(ArkApplicationComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty BaseDinoSettingsDinoListProperty = DependencyProperty.Register(nameof(BaseDinoSettingsDinoList), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty BaseMapSpawnerListProperty = DependencyProperty.Register(nameof(BaseMapSpawnerList), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty BaseMapSpawnerDinoListProperty = DependencyProperty.Register(nameof(BaseMapSpawnerDinoList), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty BasePrimalItemListProperty = DependencyProperty.Register(nameof(BasePrimalItemList), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty BaseSupplyCrateListProperty = DependencyProperty.Register(nameof(BaseSupplyCrateList), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty BaseGameMapsProperty = DependencyProperty.Register(nameof(BaseGameMaps), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty BaseTotalConversionsProperty = DependencyProperty.Register(nameof(BaseTotalConversions), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty BaseBranchesProperty = DependencyProperty.Register(nameof(BaseBranches), typeof(ComboBoxItemList), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty CurrentConfigProperty = DependencyProperty.Register(nameof(CurrentConfig), typeof(Config), typeof(ServerSettingsControl));
        public static readonly DependencyProperty IsAdministratorProperty = DependencyProperty.Register(nameof(IsAdministrator), typeof(bool), typeof(ServerSettingsControl), new PropertyMetadata(false));
        public static readonly DependencyProperty NetworkInterfacesProperty = DependencyProperty.Register(nameof(NetworkInterfaces), typeof(List<NetworkAdapterEntry>), typeof(ServerSettingsControl), new PropertyMetadata(new List<NetworkAdapterEntry>()));
        public static readonly DependencyProperty RuntimeProperty = DependencyProperty.Register(nameof(Runtime), typeof(ServerRuntime), typeof(ServerSettingsControl));
        public static readonly DependencyProperty ServerManagerProperty = DependencyProperty.Register(nameof(ServerManager), typeof(ServerManager), typeof(ServerSettingsControl), new PropertyMetadata(null));
        public static readonly DependencyProperty ServerProperty = DependencyProperty.Register(nameof(Server), typeof(Server), typeof(ServerSettingsControl), new PropertyMetadata(null, ServerPropertyChanged));
        public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(nameof(Settings), typeof(ServerProfile), typeof(ServerSettingsControl));
        public static readonly DependencyProperty SelectedArkApplicationDinoProperty = DependencyProperty.Register(nameof(SelectedArkApplicationDino), typeof(ArkApplication), typeof(ServerSettingsControl), new PropertyMetadata(ArkApplication.All));
        public static readonly DependencyProperty SelectedArkApplicationEngramProperty = DependencyProperty.Register(nameof(SelectedArkApplicationEngram), typeof(ArkApplication), typeof(ServerSettingsControl), new PropertyMetadata(ArkApplication.All));
        public static readonly DependencyProperty SelectedArkApplicationResourceProperty = DependencyProperty.Register(nameof(SelectedArkApplicationResource), typeof(ArkApplication), typeof(ServerSettingsControl), new PropertyMetadata(ArkApplication.All));
        public static readonly DependencyProperty SelectedCraftingOverrideProperty = DependencyProperty.Register(nameof(SelectedCraftingOverride), typeof(CraftingOverride), typeof(ServerSettingsControl));
        public static readonly DependencyProperty SelectedCustomSectionProperty = DependencyProperty.Register(nameof(SelectedCustomSection), typeof(CustomSection), typeof(ServerSettingsControl));
        public static readonly DependencyProperty SelectedNPCSpawnSettingProperty = DependencyProperty.Register(nameof(SelectedNPCSpawnSetting), typeof(NPCSpawnSettings), typeof(ServerSettingsControl));
        public static readonly DependencyProperty SelectedSupplyCrateOverrideProperty = DependencyProperty.Register(nameof(SelectedSupplyCrateOverride), typeof(SupplyCrateOverride), typeof(ServerSettingsControl));
        public static readonly DependencyProperty SelectedSupplyCrateItemSetProperty = DependencyProperty.Register(nameof(SelectedSupplyCrateItemSet), typeof(SupplyCrateItemSet), typeof(ServerSettingsControl));
        public static readonly DependencyProperty SelectedSupplyCrateItemSetEntryProperty = DependencyProperty.Register(nameof(SelectedSupplyCrateItemSetEntry), typeof(SupplyCrateItemSetEntry), typeof(ServerSettingsControl));

        #region Properties
        public ArkApplicationComboBoxItemList BaseArkApplicationList
        {
            get { return (ArkApplicationComboBoxItemList)GetValue(BaseArkApplicationListProperty); }
            set { SetValue(BaseArkApplicationListProperty, value); }
        }

        public ComboBoxItemList BaseDinoSettingsDinoList
        {
            get { return (ComboBoxItemList)GetValue(BaseDinoSettingsDinoListProperty); }
            set { SetValue(BaseDinoSettingsDinoListProperty, value); }
        }

        public ComboBoxItemList BaseMapSpawnerList
        {
            get { return (ComboBoxItemList)GetValue(BaseMapSpawnerListProperty); }
            set { SetValue(BaseMapSpawnerListProperty, value); }
        }

        public ComboBoxItemList BaseMapSpawnerDinoList
        {
            get { return (ComboBoxItemList)GetValue(BaseMapSpawnerDinoListProperty); }
            set { SetValue(BaseMapSpawnerDinoListProperty, value); }
        }

        public ComboBoxItemList BasePrimalItemList
        {
            get { return (ComboBoxItemList)GetValue(BasePrimalItemListProperty); }
            set { SetValue(BasePrimalItemListProperty, value); }
        }

        public ComboBoxItemList BaseSupplyCrateList
        {
            get { return (ComboBoxItemList)GetValue(BaseSupplyCrateListProperty); }
            set { SetValue(BaseSupplyCrateListProperty, value); }
        }

        public ComboBoxItemList BaseGameMaps
        {
            get { return (ComboBoxItemList)GetValue(BaseGameMapsProperty); }
            set { SetValue(BaseGameMapsProperty, value); }
        }

        public ComboBoxItemList BaseTotalConversions
        {
            get { return (ComboBoxItemList)GetValue(BaseTotalConversionsProperty); }
            set { SetValue(BaseTotalConversionsProperty, value); }
        }

        public ComboBoxItemList BaseBranches
        {
            get { return (ComboBoxItemList)GetValue(BaseBranchesProperty); }
            set { SetValue(BaseBranchesProperty, value); }
        }

        public Config CurrentConfig
        {
            get { return GetValue(CurrentConfigProperty) as Config; }
            set { SetValue(CurrentConfigProperty, value); }
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

        public CraftingOverride SelectedCraftingOverride
        {
            get { return GetValue(SelectedCraftingOverrideProperty) as CraftingOverride; }
            set { SetValue(SelectedCraftingOverrideProperty, value); }
        }

        public CustomSection SelectedCustomSection
        {
            get { return GetValue(SelectedCustomSectionProperty) as CustomSection; }
            set { SetValue(SelectedCustomSectionProperty, value); }
        }

        public NPCSpawnSettings SelectedNPCSpawnSetting
        {
            get { return GetValue(SelectedNPCSpawnSettingProperty) as NPCSpawnSettings; }
            set { SetValue(SelectedNPCSpawnSettingProperty, value); }
        }

        public SupplyCrateOverride SelectedSupplyCrateOverride
        {
            get { return GetValue(SelectedSupplyCrateOverrideProperty) as SupplyCrateOverride; }
            set { SetValue(SelectedSupplyCrateOverrideProperty, value); }
        }

        public SupplyCrateItemSet SelectedSupplyCrateItemSet
        {
            get { return GetValue(SelectedSupplyCrateItemSetProperty) as SupplyCrateItemSet; }
            set { SetValue(SelectedSupplyCrateItemSetProperty, value); }
        }

        public SupplyCrateItemSetEntry SelectedSupplyCrateItemSetEntry
        {
            get { return GetValue(SelectedSupplyCrateItemSetEntryProperty) as SupplyCrateItemSetEntry; }
            set { SetValue(SelectedSupplyCrateItemSetEntryProperty, value); }
        }
        #endregion

        public ServerSettingsControl()
        {
            this.CurrentConfig = Config.Default;
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this);

            this.ServerManager = ServerManager.Instance;
            this.IsAdministrator = SecurityUtils.IsAdministrator();

            RefreshBaseArkApplicationList();
            this.BaseDinoSettingsDinoList = new ComboBoxItemList();
            this.BaseMapSpawnerList = new ComboBoxItemList();
            this.BaseMapSpawnerDinoList = new ComboBoxItemList();
            this.BasePrimalItemList = new ComboBoxItemList();
            this.BaseSupplyCrateList = new ComboBoxItemList();
            this.BaseGameMaps = new ComboBoxItemList();
            this.BaseTotalConversions = new ComboBoxItemList();
            this.BaseBranches = new ComboBoxItemList();

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
                        oldserver?.Profile.Save(false, false, null);

                        ssc.Settings = server.Profile;
                        ssc.Runtime = server.Runtime;
                        ssc.ReinitializeNetworkAdapters();
                        ssc.RefreshBaseDinoList();
                        ssc.RefreshBaseMapSpawnerList();
                        ssc.RefreshBasePrimalItemList();
                        ssc.RefreshBaseSupplyCrateList();
                        ssc.RefreshBaseGameMapsList();
                        ssc.RefreshBaseTotalConversionsList();
                        ssc.RefreshBaseBranchesList();
                        ssc.LoadServerFiles();
                    }).DoNotWait();
            }
        }

        private void ResourceDictionaryChangedEvent(object source, ResourceDictionaryChangedEventArgs e)
        {
            this.Settings.DinoSettings.UpdateForLocalization();
            this.Settings.NPCSpawnSettings.UpdateForLocalization();
            this.Settings.ConfigOverrideSupplyCrateItems.UpdateForLocalization();

            this.RefreshBaseArkApplicationList();
            this.RefreshBaseDinoList();
            this.RefreshBaseMapSpawnerList();
            this.RefreshBasePrimalItemList();
            this.RefreshBaseSupplyCrateList();
            this.RefreshBaseGameMapsList();
            this.RefreshBaseTotalConversionsList();
            this.RefreshBaseBranchesList();

            this.HarvestResourceItemAmountClassMultipliersListBox.Items.Refresh();
            this.EngramsOverrideListView.Items.Refresh();

            Runtime.UpdateServerStatusString();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Window.GetWindow(this)?.Activate();

            if (sender is Window)
                ((Window)sender).Closed -= Window_Closed;

            if (sender is ShutdownWindow)
                this.Runtime?.ResetModCheckTimer();

            if (sender is ModDetailsWindow)
            {
                ((ModDetailsWindow)sender).SavePerformed -= ModDetailsWindow_SavePerformed;
                RefreshBaseGameMapsList();
                RefreshBaseTotalConversionsList();
            }
        }

        private void ModDetailsWindow_SavePerformed(object sender, ProfileEventArgs e)
        {
            if (sender is ModDetailsWindow && Equals(e.Profile, Settings))
            {
                RefreshBaseGameMapsList();
                RefreshBaseTotalConversionsList();
            }
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBoxResult.None;

            switch (this.Runtime.Status)
            {
                case ServerRuntime.ServerStatus.Initializing:
                case ServerRuntime.ServerStatus.Running:
                    // check if the server is initialising.
                    if (this.Runtime.Status == ServerRuntime.ServerStatus.Initializing)
                    {
                        result = MessageBox.Show(_globalizer.GetResourceString("ServerSettings_StartServer_StartingLabel"), _globalizer.GetResourceString("ServerSettings_StartServer_StartingTitle"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (result == MessageBoxResult.No)
                            return;

                        try
                        {
                            PluginHelper.Instance.ProcessAlert(AlertType.Shutdown, this.Settings.ProfileName, Config.Default.Alert_ServerStopMessage);
                            await Task.Delay(2000);

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
                            this.Settings.Save(false, false, null);

                            if (Config.Default.ServerUpdate_OnServerStart)
                            {
                                if (!await UpdateServer(false, true, Config.Default.ServerUpdate_UpdateModsWhenUpdatingServer, true))
                                {
                                    if (MessageBox.Show("There was a problem while performing the server update. This may leave your server in a incomplete state.\r\n\r\nDo you want to continue with the server start, this could cause problems?", "Server Update", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                                        return;
                                }
                            }

                            string validateMessage;
                            if (!this.Server.Profile.Validate(false, out validateMessage))
                            {
                                if (MessageBox.Show($"The following validation problems were encountered.\r\n\r\n{validateMessage}\r\n\r\nDo you want to continue with the server start, this could cause problems?", "Profile Validation", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                                    return;
                            }

                            await this.Server.StartAsync();

                            PluginHelper.Instance.ProcessAlert(AlertType.Startup, this.Settings.ProfileName, Config.Default.Alert_ServerStartedMessage);
                            if (this.Settings.ForceRespawnDinos)
                                PluginHelper.Instance.ProcessAlert(AlertType.Startup, this.Settings.ProfileName, Config.Default.Alert_ForceRespawnDinos);
                            await Task.Delay(2000);
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

            this.Settings.Save(false, false, null);
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

            this.Settings.Save(false, false, null);
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

        private void OpenPlayerList_Click(object sender, RoutedEventArgs e)
        {
            var window = PlayerListWindow.GetWindowForServer(this.Server);
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
            window.SavePerformed += ModDetailsWindow_SavePerformed;
            window.Show();
            window.Focus();
        }

        private void HelpSOTF_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Config.Default.ArkSotfUrl))
                return;

            Process.Start(Config.Default.ArkSotfUrl);
        }

        private void PatchNotes_Click(object sender, RoutedEventArgs e)
        {
            var url = string.Empty;
            if (Settings.SOTF_Enabled)
                url =Config.Default.ArkSotF_PatchNotesUrl;
            else
                url = Config.Default.ArkSE_PatchNotesUrl;

            if (string.IsNullOrWhiteSpace(url))
                return;

            Process.Start(url);
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
            var logFolder = Path.Combine(SteamCmdUpdater.GetLogFolder(), this.Server.Profile.ProfileName);
            if (!Directory.Exists(logFolder))
                logFolder = SteamCmdUpdater.GetLogFolder();
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

                // <server>
                var file = Path.Combine(this.Settings.InstallDirectory, Config.Default.LastUpdatedTimeFile);
                if (File.Exists(file)) files.Add(file);

                file = Path.Combine(this.Settings.InstallDirectory, "version.txt");
                if (File.Exists(file)) files.Add(file);

                // <server>\ShooterGame\Content\Mods
                var folder = Path.Combine(this.Settings.InstallDirectory, Config.Default.ServerModsRelativePath);
                var dirInfo = new DirectoryInfo(folder);
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

                //// <server>\ShooterGame\Saved\Logs
                //folder = Path.Combine(this.Settings.InstallDirectory, Config.Default.SavedRelativePath, "Logs");
                //dirInfo = new DirectoryInfo(folder);
                //if (dirInfo.Exists)
                //{
                //    files.AddRange(dirInfo.GetFiles("*.log").Where(f => f.LastWriteTime > DateTime.Today.AddDays(-MAX_DAYS)).Select(logFile => logFile.FullName));
                //}

                // Logs
                folder = Path.Combine(Config.Default.DataDir, Config.Default.LogsDir, ServerApp.LOGPREFIX_AUTOBACKUP);
                dirInfo = new DirectoryInfo(folder);
                if (dirInfo.Exists)
                {
                    files.AddRange(dirInfo.GetFiles("*.log").Where(f => f.LastWriteTime > DateTime.Today.AddDays(-MAX_DAYS)).Select(logFile => logFile.FullName));
                }

                folder = Path.Combine(Config.Default.DataDir, Config.Default.LogsDir, ServerApp.LOGPREFIX_AUTOSHUTDOWN);
                dirInfo = new DirectoryInfo(folder);
                if (dirInfo.Exists)
                {
                    files.AddRange(dirInfo.GetFiles("*.log").Where(f => f.LastWriteTime > DateTime.Today.AddDays(-MAX_DAYS)).Select(logFile => logFile.FullName));
                }

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

                file = this.Settings.GetProfileFileNew();
                if (File.Exists(file))
                {
                    var profileFile = ServerProfile.LoadFromProfileFileNew(file);
                    if (profileFile != null)
                    {
                        profileFile.AdminPassword = "obfuscated";
                        profileFile.ServerPassword = "obfuscated";
                        profileFile.SpectatorPassword = "obfuscated";
                        profileFile.WebAlarmKey = "obfuscated";
                        profileFile.WebAlarmUrl = "obfuscated";
                        obfuscateFiles.Add(file, profileFile.ToOutputStringNew());
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
                        var branchName = string.IsNullOrWhiteSpace(this.Settings.BranchName) ? Config.Default.DefaultServerBranchName : this.Settings.BranchName;
                        file = IOUtils.NormalizePath(Path.Combine(Config.Default.AutoUpdate_CacheDir, $"{Config.Default.ServerBranchFolderPrefix}{branchName}", Config.Default.LastUpdatedTimeFile));
                        if (File.Exists(file)) files.Add(file);
                    }
                }

                var comment = new StringBuilder();
                comment.AppendLine($"Windows Platform: {Environment.OSVersion.Platform}");
                comment.AppendLine($"Windows Version: {Environment.OSVersion.VersionString}");

                comment.AppendLine($"ARK Version: {this.Settings.LastInstalledVersion}");
                comment.AppendLine($"ASM Version: {App.Version}");
                comment.AppendLine($"ASM Key: {Config.Default.ASMUniqueKey}");

                comment.AppendLine($"MachinePublicIP: {Config.Default.MachinePublicIP}");
                comment.AppendLine($"ASM Directory: {Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}");
                comment.AppendLine($"Data Directory: {Config.Default.DataDir}");
                comment.AppendLine($"Config Directory: {Config.Default.ConfigDirectory}");
                comment.AppendLine($"Server Directory: {this.Settings.InstallDirectory}");

                comment.AppendLine($"SotF Server: {this.Settings.SOTF_Enabled}");
                comment.AppendLine($"PGM Server: {this.Settings.PGM_Enabled}");

                comment.AppendLine($"IsAdministrator: {SecurityUtils.IsAdministrator()}");
                comment.AppendLine($"RunAsAdministratorPrompt: {Config.Default.RunAsAdministratorPrompt}");
                comment.AppendLine($"ManageFirewallAutomatically: {Config.Default.ManageFirewallAutomatically}");
                comment.AppendLine($"SteamCmdRedirectOutput: {Config.Default.SteamCmdRedirectOutput}");
                comment.AppendLine($"SteamCmd_UseAnonymousCredentials: {Config.Default.SteamCmd_UseAnonymousCredentials}");
                comment.AppendLine($"SteamAPIKey: {!string.IsNullOrWhiteSpace(Config.Default.SteamAPIKey)}");
                comment.AppendLine($"ValidateProfileOnServerStart: {Config.Default.ValidateProfileOnServerStart}");

                comment.AppendLine($"AutoBackup_EnableBackup: {Config.Default.AutoBackup_EnableBackup}");
                comment.AppendLine($"AutoBackup_BackupPeriod: {Config.Default.AutoBackup_BackupPeriod}");
                comment.AppendLine($"AutoBackup_DeleteOldFiles: {Config.Default.AutoBackup_DeleteOldFiles}");
                comment.AppendLine($"AutoBackup_DeleteInterval: {Config.Default.AutoBackup_DeleteInterval}");

                comment.AppendLine($"AutoUpdate_EnableUpdate: {Config.Default.AutoUpdate_EnableUpdate}");
                comment.AppendLine($"AutoUpdate_CacheDir: {Config.Default.AutoUpdate_CacheDir}");
                comment.AppendLine($"AutoUpdate_UpdatePeriod: {Config.Default.AutoUpdate_UpdatePeriod}");
                comment.AppendLine($"AutoUpdate_UseSmartCopy: {Config.Default.AutoUpdate_UseSmartCopy}");
                comment.AppendLine($"AutoUpdate_RetryOnFail: {Config.Default.AutoUpdate_RetryOnFail}");
                comment.AppendLine($"AutoUpdate_ShowUpdateReason: {Config.Default.AutoUpdate_ShowUpdateReason}");
                comment.AppendLine($"AutoUpdate_ValidateServerFiles: {Config.Default.AutoUpdate_ValidateServerFiles}");
                comment.AppendLine($"AutoUpdate_OverrideServerStartup: {Config.Default.AutoUpdate_OverrideServerStartup}");

                comment.AppendLine($"ServerShutdown_EnableWorldSave: {Config.Default.ServerShutdown_EnableWorldSave}");
                comment.AppendLine($"ServerShutdown_GracePeriod: {Config.Default.ServerShutdown_GracePeriod}");
                comment.AppendLine($"ServerUpdate_UpdateModsWhenUpdatingServer: {Config.Default.ServerUpdate_UpdateModsWhenUpdatingServer}");
                comment.AppendLine($"ServerUpdate_ForceCopyMods: {Config.Default.ServerUpdate_ForceCopyMods}");
                comment.AppendLine($"ServerUpdate_ForceUpdateMods: {Config.Default.ServerUpdate_ForceUpdateMods}");
                comment.AppendLine($"ServerUpdate_ForceUpdateModsIfNoSteamInfo: {Config.Default.ServerUpdate_ForceUpdateModsIfNoSteamInfo}");
                comment.AppendLine($"ServerUpdate_OnServerStart: {Config.Default.ServerUpdate_OnServerStart}");
                comment.AppendLine($"EmailNotify_AutoRestart: {Config.Default.EmailNotify_AutoRestart}");
                comment.AppendLine($"EmailNotify_AutoBackup: {Config.Default.EmailNotify_AutoBackup}");
                comment.AppendLine($"EmailNotify_AutoUpdate: {Config.Default.EmailNotify_AutoUpdate}");
                comment.AppendLine($"EmailNotify_ShutdownRestart: {Config.Default.EmailNotify_ShutdownRestart}");

                comment.AppendLine($"ServerShutdown_UseDoExit: {Config.Default.ServerShutdown_UseDoExit}");
                comment.AppendLine($"ServerShutdown_WorldSaveDelay: {Config.Default.ServerShutdown_WorldSaveDelay}");

                comment.AppendLine($"SectionCraftingOverridesEnabled: {Config.Default.SectionCraftingOverridesEnabled}");
                comment.AppendLine($"SectionMapSpawnerOverridesEnabled: {Config.Default.SectionMapSpawnerOverridesEnabled}");
                comment.AppendLine($"SectionSupplyCrateOverridesEnabled: {Config.Default.SectionSupplyCrateOverridesEnabled}");

                var zipFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), this.Settings.ProfileID + ".zip");
                if (File.Exists(zipFile)) File.Delete(zipFile);

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
                var result = this.Settings.Validate(true, out validationMessage);

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
                Settings.ChangeInstallationFolder(dialog.FileName);
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
                    this.Server.Profile.ResetProfileId();

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

        private void ArkAutoSettings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ArkAutoSettings_ErrorLabel"), _globalizer.GetResourceString("ServerSettings_ArkAutoSettings_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void SaveBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = System.Windows.Input.Cursors.Wait);

                var app = new ServerApp(true)
                {
                    DeleteOldServerBackupFiles = !Config.Default.AutoBackup_EnableBackup,
                    SendEmails = false,
                    OutputLogs = false,
                    ServerProcess = ServerProcessType.Backup,
                };

                var profile = ServerProfileSnapshot.Create(Server.Profile);

                var exitCode = await Task.Run(() => app.PerformProfileBackup(profile));
                if (exitCode != ServerApp.EXITCODE_NORMALEXIT && exitCode != ServerApp.EXITCODE_CANCELLED)
                    throw new ApplicationException($"An error occured during the backup process - ExitCode: {exitCode}");

                MessageBox.Show("The backup was successful.", _globalizer.GetResourceString("ServerSettings_BackupServer_Title"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ServerSettings_BackupServer_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = null);
            }
        }

        private void SaveRestore_Click(object sender, RoutedEventArgs e)
        {
            var window = new WorldSaveRestoreWindow(Server.Profile);
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            window.ShowDialog();
        }

        private void HiddenField_GotFocus(object sender, RoutedEventArgs e)
        {
            var hideTextBox = sender as TextBox;
            if (hideTextBox != null)
            {
                TextBox textBox = null;
                if (Equals(hideTextBox, HideServerPasswordTextBox)) 
                    textBox = ServerPasswordTextBox;
                if (Equals(hideTextBox, HideAdminPasswordTextBox))
                    textBox = AdminPasswordTextBox;
                if (Equals(hideTextBox, HideSpectatorPasswordTextBox))
                    textBox = SpectatorPasswordTextBox;
                if (Equals(hideTextBox, HideWebKeyTextBox))
                    textBox = WebKeyTextBox;
                if (Equals(hideTextBox, HideWebURLTextBox))
                    textBox = WebURLTextBox;
                if (Equals(hideTextBox, HideBranchPasswordTextBox)) 
                    textBox = BranchPasswordTextBox;

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
                if (textBox == BranchPasswordTextBox)
                    hideTextBox = HideBranchPasswordTextBox;

                if (hideTextBox != null)
                {
                    hideTextBox.Visibility = System.Windows.Visibility.Visible;
                    textBox.Visibility = System.Windows.Visibility.Collapsed;
                }
                UpdateLayout();
            }
        }

        private void ComboBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null)
                return;

            if (comboBox.IsDropDownOpen)
                return;

            e.Handled = true;
        }

        private void ComboBoxItemList_LostFocus(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null)
                return;

            if (comboBox.SelectedItem == null)
            {
                var text = comboBox.Text;

                var source = comboBox.ItemsSource as ComboBoxItemList;
                source?.Add(new Lib.ViewModel.ComboBoxItem
                {
                    ValueMember = text,
                    DisplayMember = text,
                });

                comboBox.SelectedValue = text;
            }

            var expression = comboBox.GetBindingExpression(Selector.SelectedValueProperty);
            expression?.UpdateSource();

            expression = comboBox.GetBindingExpression(ComboBox.TextProperty);
            expression?.UpdateSource();
        }

        private void OutOfDateModUpdate_Click(object sender, RoutedEventArgs e)
        {
            this.Runtime?.ResetModCheckTimer();
        }

        private void ServerName_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            Settings.ValidateServerName();
        }

        private void MOTD_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            Settings.ValidateMOTD();
        }

        private void SyncProfile_Click(object sender, RoutedEventArgs e)
        {
            var window = new ProfileSyncWindow(ServerManager, Server.Profile);
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            window.ShowDialog();
        }

        private void EnableSOTFCheckbox_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox == null || checkBox != EnableSOTFCheckbox)
                return;

            this.Settings.ServerMap = Config.Default.DefaultServerMap;
            this.Settings.TotalConversionModId = string.Empty;
            this.Settings.ServerModIds = string.Empty;
            this.Settings.BranchName = string.Empty;
            this.Settings.BranchPassword = string.Empty;
            RefreshBaseGameMapsList();
            RefreshBaseTotalConversionsList();
            RefreshBaseBranchesList();
        }

        #region Dinos
        private void DinoCustomization_Reset(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DinoCustomization_ResetLabel"), _globalizer.GetResourceString("ServerSettings_DinoCustomization_ResetTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.DinoSettings.Reset();
            RefreshBaseDinoList();
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

        private void PasteCustomDinos_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomConfigDataWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            // read the pasted data into an ini file.
            var iniFile = IniFileUtils.ReadString(window.ConfigData);

            Server.Profile.DinoSettings.RenderToModel();

            // cycle through the sections, adding them to the engrams list. Will bypass any sections that are named as per the ARK default sections.
            foreach (var section in iniFile.Sections.Where(s => s.SectionName != null && !SystemIniFile.SectionNames.ContainsValue(s.SectionName)))
            {
                var dinoSpawnWeightMultipliers = new AggregateIniValueList<DinoSpawn>(nameof(Server.Profile.DinoSpawnWeightMultipliers), null);
                dinoSpawnWeightMultipliers.FromIniValues(section.KeysToStringArray().Where(s => s.StartsWith($"{dinoSpawnWeightMultipliers.IniCollectionKey}=")));
                Server.Profile.DinoSpawnWeightMultipliers.AddRange(dinoSpawnWeightMultipliers);
                Server.Profile.DinoSpawnWeightMultipliers.IsEnabled |= dinoSpawnWeightMultipliers.IsEnabled;

                var preventDinoTameClassNames = new StringIniValueList(nameof(Server.Profile.PreventDinoTameClassNames), null);
                preventDinoTameClassNames.FromIniValues(section.KeysToStringArray().Where(s => s.StartsWith($"{preventDinoTameClassNames.IniCollectionKey}=")));
                Server.Profile.PreventDinoTameClassNames.AddRange(preventDinoTameClassNames);
                Server.Profile.PreventDinoTameClassNames.IsEnabled |= preventDinoTameClassNames.IsEnabled;

                var npcReplacements = new AggregateIniValueList<NPCReplacement>(nameof(Server.Profile.NPCReplacements), null);
                npcReplacements.FromIniValues(section.KeysToStringArray().Where(s => s.StartsWith($"{npcReplacements.IniCollectionKey}=")));
                Server.Profile.NPCReplacements.AddRange(npcReplacements);
                Server.Profile.NPCReplacements.IsEnabled |= npcReplacements.IsEnabled;

                var tamedDinoClassDamageMultipliers = new AggregateIniValueList<ClassMultiplier>(nameof(Server.Profile.TamedDinoClassDamageMultipliers), null);
                tamedDinoClassDamageMultipliers.FromIniValues(section.KeysToStringArray().Where(s => s.StartsWith($"{tamedDinoClassDamageMultipliers.IniCollectionKey}=")));
                Server.Profile.TamedDinoClassDamageMultipliers.AddRange(tamedDinoClassDamageMultipliers);
                Server.Profile.TamedDinoClassDamageMultipliers.IsEnabled |= tamedDinoClassDamageMultipliers.IsEnabled;

                var tamedDinoClassResistanceMultipliers = new AggregateIniValueList<ClassMultiplier>(nameof(Server.Profile.TamedDinoClassResistanceMultipliers), null);
                tamedDinoClassResistanceMultipliers.FromIniValues(section.KeysToStringArray().Where(s => s.StartsWith($"{tamedDinoClassResistanceMultipliers.IniCollectionKey}=")));
                Server.Profile.TamedDinoClassResistanceMultipliers.AddRange(tamedDinoClassResistanceMultipliers);
                Server.Profile.TamedDinoClassResistanceMultipliers.IsEnabled |= tamedDinoClassResistanceMultipliers.IsEnabled;

                var dinoClassDamageMultipliers = new AggregateIniValueList<ClassMultiplier>(nameof(Server.Profile.DinoClassDamageMultipliers), null);
                dinoClassDamageMultipliers.FromIniValues(section.KeysToStringArray().Where(s => s.StartsWith($"{dinoClassDamageMultipliers.IniCollectionKey}=")));
                Server.Profile.DinoClassDamageMultipliers.AddRange(dinoClassDamageMultipliers);
                Server.Profile.DinoClassDamageMultipliers.IsEnabled |= dinoClassDamageMultipliers.IsEnabled;

                var dinoClassResistanceMultipliers = new AggregateIniValueList<ClassMultiplier>(nameof(Server.Profile.DinoClassResistanceMultipliers), null);
                dinoClassResistanceMultipliers.FromIniValues(section.KeysToStringArray().Where(s => s.StartsWith($"{dinoClassResistanceMultipliers.IniCollectionKey}=")));
                Server.Profile.DinoClassResistanceMultipliers.AddRange(dinoClassResistanceMultipliers);
                Server.Profile.DinoClassResistanceMultipliers.IsEnabled |= dinoClassResistanceMultipliers.IsEnabled;
            }

            Server.Profile.DinoSettings = new DinoSettingsList(Server.Profile.DinoSpawnWeightMultipliers, Server.Profile.PreventDinoTameClassNames, Server.Profile.NPCReplacements, Server.Profile.TamedDinoClassDamageMultipliers, Server.Profile.TamedDinoClassResistanceMultipliers, Server.Profile.DinoClassDamageMultipliers, Server.Profile.DinoClassResistanceMultipliers);
            Server.Profile.DinoSettings.RenderToView();

            RefreshBaseDinoList();
        }

        private void RemoveDinoSetting_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DinoCustomization_DinoRemoveRecordLabel"), _globalizer.GetResourceString("ServerSettings_DinoCustomization_DinoRemoveRecordTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var dino = ((DinoSettings)((Button)e.Source).DataContext);
            if (!dino.KnownDino)
            {
                this.Settings.DinoSettings.Remove(dino);
                RefreshBaseDinoList();
            }
        }

        private void SaveCustomDinos_Click(object sender, RoutedEventArgs e)
        {
            Settings.DinoSettings.RenderToModel();

            var iniValues = Settings.DinoSpawnWeightMultipliers.ToIniValues().ToList();
            iniValues.AddRange(Settings.PreventDinoTameClassNames.ToIniValues());
            iniValues.AddRange(Settings.NPCReplacements.ToIniValues());
            iniValues.AddRange(Settings.DinoClassDamageMultipliers.ToIniValues());
            iniValues.AddRange(Settings.DinoClassResistanceMultipliers.ToIniValues());
            iniValues.AddRange(Settings.TamedDinoClassDamageMultipliers.ToIniValues());
            iniValues.AddRange(Settings.TamedDinoClassResistanceMultipliers.ToIniValues());
            var iniValue = string.Join("\r\n", iniValues);

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.NoWrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_DinoCustomizations_SaveTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }
        #endregion

        #region Resources
        private void HarvestResourceItemAmountClassMultipliers_Reset(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_CustomHarvest_ResetLabel"), _globalizer.GetResourceString("ServerSettings_CustomHarvest_ResetTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.HarvestResourceItemAmountClassMultipliers.Reset();
        }

        private void PasteCustomResources_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomConfigDataWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            // read the pasted data into an ini file.
            var iniFile = IniFileUtils.ReadString(window.ConfigData);

            // cycle through the sections, adding them to the engrams list. Will bypass any sections that are named as per the ARK default sections.
            foreach (var section in iniFile.Sections.Where(s => s.SectionName != null && !SystemIniFile.SectionNames.ContainsValue(s.SectionName)))
            {
                var harvestResourceItemAmountClassMultipliers = new AggregateIniValueList<ResourceClassMultiplier>(nameof(Server.Profile.HarvestResourceItemAmountClassMultipliers), null);
                harvestResourceItemAmountClassMultipliers.FromIniValues(section.KeysToStringArray().Where(s => s.StartsWith($"{harvestResourceItemAmountClassMultipliers.IniCollectionKey}=")));
                Server.Profile.HarvestResourceItemAmountClassMultipliers.AddRange(harvestResourceItemAmountClassMultipliers);
                Server.Profile.HarvestResourceItemAmountClassMultipliers.IsEnabled |= harvestResourceItemAmountClassMultipliers.IsEnabled;
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

        private void SaveCustomResources_Click(object sender, RoutedEventArgs e)
        {
            var iniValues = Settings.HarvestResourceItemAmountClassMultipliers.ToIniValues().ToList();
            var iniValue = string.Join("\r\n", iniValues);

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.NoWrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_CustomResources_SaveTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }
        #endregion

        #region Engrams
        private void Engrams_Reset(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_EngramsOverride_ResetLabel"), _globalizer.GetResourceString("ServerSettings_EngramsOverride_ResetTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.ResetEngramsSection();
        }

        private void Engrams_SelectAll(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_EngramsOverride_SelectAllConfirmLabel"), _globalizer.GetResourceString("ServerSettings_EngramsOverride_SelectAllConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            foreach (var engram in Settings.OverrideNamedEngramEntries)
            {
                engram.SaveEngramOverride = true;
            }
        }

        private void Engrams_UnselectAll(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_EngramsOverride_UnselectAllConfirmLabel"), _globalizer.GetResourceString("ServerSettings_EngramsOverride_UnselectAllConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            foreach (var engram in Settings.OverrideNamedEngramEntries)
            {
                engram.SaveEngramOverride = false;
            }
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

        private void PasteCustomEngrams_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomConfigDataWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            // read the pasted data into an ini file.
            var iniFile = IniFileUtils.ReadString(window.ConfigData);

            // cycle through the sections, adding them to the engrams list. Will bypass any sections that are named as per the ARK default sections.
            foreach (var section in iniFile.Sections.Where(s => s.SectionName != null && !SystemIniFile.SectionNames.ContainsValue(s.SectionName)))
            {
                var overrideNamedEngramEntries = new AggregateIniValueList<EngramEntry>(nameof(Server.Profile.OverrideNamedEngramEntries), null);
                overrideNamedEngramEntries.FromIniValues(section.KeysToStringArray().Where(s => s.StartsWith($"{overrideNamedEngramEntries.IniCollectionKey}=")));
                Server.Profile.OverrideNamedEngramEntries.AddRange(overrideNamedEngramEntries);
                Server.Profile.OverrideNamedEngramEntries.IsEnabled |= overrideNamedEngramEntries.IsEnabled;
            }
        }

        private void RemoveEngramOverride_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_EngramsOverride_EngramsRemoveRecordLabel"), _globalizer.GetResourceString("ServerSettings_EngramsOverride_EngramsRemoveRecordTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var engram = ((EngramEntry)((Button)e.Source).DataContext);
            if (!engram.KnownEngram)
                this.Settings.OverrideNamedEngramEntries.Remove(engram);
        }

        private void SaveCustomEngrams_Click(object sender, RoutedEventArgs e)
        {
            Settings.OverrideNamedEngramEntries.OnlyAllowSelectedEngrams = Settings.OnlyAllowSpecifiedEngrams;

            var iniValues = Settings.OverrideNamedEngramEntries.ToIniValues().ToList();
            var iniValue = string.Join("\r\n", iniValues);

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.NoWrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_CustomEngrams_SaveTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }
        #endregion

        #region Crafting Overrides
        private void AddCraftingOverride_Click(object sender, RoutedEventArgs e)
        {
            Settings.ConfigOverrideItemCraftingCosts.Add(new CraftingOverride());
            Settings.ConfigOverrideItemCraftingCosts.IsEnabled = true;
        }

        private void AddCraftingOverrideResource_Click(object sender, RoutedEventArgs e)
        {
            SelectedCraftingOverride?.BaseCraftingResourceRequirements.Add(new CraftingResourceRequirement());
        }

        private void ClearCraftingOverrides_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SelectedCraftingOverride = null;
            Settings.ConfigOverrideItemCraftingCosts.Clear();
            Settings.ConfigOverrideItemCraftingCosts.IsEnabled = false;
        }

        private void ClearCraftingOverrideResources_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SelectedCraftingOverride?.BaseCraftingResourceRequirements.Clear();
        }

        private void PasteCraftingOverride_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomConfigDataWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            // read the pasted data into an ini file.
            var iniFile = IniFileUtils.ReadString(window.ConfigData.Replace(" ", ""));

            // cycle through the sections, adding them to the engrams list. Will bypass any sections that are named as per the ARK default sections.
            foreach (var section in iniFile.Sections.Where(s => s.SectionName != null && !SystemIniFile.SectionNames.ContainsValue(s.SectionName)))
            {
                var configOverrideItemCraftingCosts = new AggregateIniValueList<CraftingOverride>(nameof(Server.Profile.ConfigOverrideItemCraftingCosts), null);
                configOverrideItemCraftingCosts.FromIniValues(section.KeysToStringArray().Where(s => s.StartsWith($"{configOverrideItemCraftingCosts.IniCollectionKey}=")));
                Server.Profile.ConfigOverrideItemCraftingCosts.AddRange(configOverrideItemCraftingCosts);
                Server.Profile.ConfigOverrideItemCraftingCosts.IsEnabled |= configOverrideItemCraftingCosts.IsEnabled;
            }

            RefreshBasePrimalItemList();
        }

        private void RemoveCraftingOverrideItem_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var item = ((CraftingOverride)((Button)e.Source).DataContext);
            Settings.ConfigOverrideItemCraftingCosts.Remove(item);
            Settings.ConfigOverrideItemCraftingCosts.IsEnabled = Settings.ConfigOverrideItemCraftingCosts.Count > 0;
        }

        private void RemoveCraftingOverrideResource_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedCraftingOverride == null)
                return;

            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var item = ((CraftingResourceRequirement)((Button)e.Source).DataContext);
            SelectedCraftingOverride.BaseCraftingResourceRequirements.Remove(item);
        }

        private void SaveCraftingOverride_Click(object sender, RoutedEventArgs e)
        {
            var iniValues = Settings.ConfigOverrideItemCraftingCosts.ToIniValues().ToList();
            var iniValue = string.Join("\r\n", iniValues);

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.NoWrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_CraftingOverrides_SaveTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }

        private void SaveCraftingOverrideItem_Click(object sender, RoutedEventArgs e)
        {
            var item = ((CraftingOverride)((Button)e.Source).DataContext);
            if (item == null)
                return;

            var iniName = Settings.ConfigOverrideItemCraftingCosts.IniCollectionKey;
            var iniValue = $"{iniName}={item.ToINIValue()}";

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.Wrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_CraftingOverrides_SaveTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }
        #endregion

        #region Custom GameUserSettings
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
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SelectedCustomSection?.Clear();
        }

        private void ClearCustomSections_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

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

            if (!result.HasValue || !result.Value)
                return;

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

        private void PasteCustomSections_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomConfigDataWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            // read the pasted data into an ini file.
            var iniFile = IniFileUtils.ReadString(window.ConfigData);

            // cycle through the sections, adding them to the custom section list. Will bypass any sections that are named as per the ARK default sections.
            foreach (var section in iniFile.Sections.Where(s => !string.IsNullOrWhiteSpace(s.SectionName) && !SystemIniFile.SectionNames.ContainsValue(s.SectionName)))
            {
                Settings.CustomGameUserSettingsSections.Add(section.SectionName, section.KeysToStringArray(), false);
            }
        }

        private void RemoveCustomItem_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            if (SelectedCustomSection == null)
                return;

            var item = ((CustomItem)((Button)e.Source).DataContext);
            SelectedCustomSection.Remove(item);
        }

        private void RemoveCustomSection_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var section = ((CustomSection)((Button)e.Source).DataContext);
            Settings.CustomGameUserSettingsSections.Remove(section);
        }
        #endregion

        #region Custom Levels 
        private void AddDinoLevel_Click(object sender, RoutedEventArgs e)
        {
            var level = ((Level)((Button)e.Source).DataContext);
            this.Settings.DinoLevels.AddNewLevel(level, Config.Default.CustomLevelXPIncrease_Dino);
        }

        private void AddPlayerLevel_Click(object sender, RoutedEventArgs e)
        {
            var level = ((Level)((Button)e.Source).DataContext);
            this.Settings.PlayerLevels.AddNewLevel(level, Config.Default.CustomLevelXPIncrease_Player);
        }

        private void DinoLevels_Clear(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DinoLevels_ClearLabel"), _globalizer.GetResourceString("ServerSettings_DinoLevels_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.ClearLevelProgression(ServerProfile.LevelProgression.Dino);
        }

        private void DinoLevels_Recalculate(object sender, RoutedEventArgs e)
        {
            this.Settings.DinoLevels.UpdateTotals();
            this.CustomDinoLevelsView.Items.Refresh();
        }

        private void DinoLevels_ResetOfficial(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DinoLevels_ResetLabel"), _globalizer.GetResourceString("ServerSettings_DinoLevels_ResetTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.ResetLevelProgressionToOfficial(ServerProfile.LevelProgression.Dino);
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

        private void PlayerLevels_Clear(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_PlayerLevels_ClearLabel"), _globalizer.GetResourceString("ServerSettings_PlayerLevels_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.ClearLevelProgression(ServerProfile.LevelProgression.Player);
        }

        private void PlayerLevels_Recalculate(object sender, RoutedEventArgs e)
        {
            this.Settings.PlayerLevels.UpdateTotals();
            this.CustomPlayerLevelsView.Items.Refresh();
        }

        private void PlayerLevels_ResetOfficial(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_PlayerLevels_ResetLabel"), _globalizer.GetResourceString("ServerSettings_PlayerLevels_ResetTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            this.Settings.ResetLevelProgressionToOfficial(ServerProfile.LevelProgression.Player);
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
        #endregion

        #region Server Files 
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
                    var steamUserList = SteamUserList.GetList(steamUsers, steamIds);
                    Settings.ServerFilesAdmins.AddRange(steamUserList);

                    Settings.SaveServerFileAdministrators();
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
                    var steamUserList = SteamUserList.GetList(steamUsers, steamIds);
                    Settings.ServerFilesExclusive.AddRange(steamUserList);

                    Settings.SaveServerFileExclusive();
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
                    var steamUserList = SteamUserList.GetList(steamUsers, steamIds);
                    Settings.ServerFilesWhitelisted.AddRange(steamUserList);

                    Settings.SaveServerFileWhitelisted();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Add Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClearAdminPlayers_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            Settings.ServerFilesAdmins.Clear();

            Settings.SaveServerFileAdministrators();
        }

        private void ClearExclusivePlayers_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            Settings.ServerFilesExclusive.Clear();

            Settings.SaveServerFileExclusive();
        }

        private void ClearWhitelistPlayers_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            Settings.ServerFilesWhitelisted.Clear();

            Settings.SaveServerFileWhitelisted();
        }

        private async void ReloadAdminPlayers_Click(object sender, RoutedEventArgs e)
        {
            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = Cursors.Wait);
                await Task.Delay(500);

                Settings.LoadServerFileAdministrators();
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

                Settings.LoadServerFileExclusive();
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

                Settings.LoadServerFileWhitelisted();
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
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var mod = ((SteamUserItem)((Button)e.Source).DataContext);
            Settings.ServerFilesAdmins.Remove(mod.SteamId);

            Settings.SaveServerFileAdministrators();
        }

        private void RemoveExclusivePlayer_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var mod = ((SteamUserItem)((Button)e.Source).DataContext);
            Settings.ServerFilesExclusive.Remove(mod.SteamId);

            Settings.SaveServerFileExclusive();
        }

        private void RemoveWhitelistPlayer_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var mod = ((SteamUserItem)((Button)e.Source).DataContext);
            Settings.ServerFilesWhitelisted.Remove(mod.SteamId);

            Settings.SaveServerFileWhitelisted();
        }

        private void SteamProfileNavigate_Click(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            var item = ((SteamUserItem)((Hyperlink)e.Source).DataContext);

            Process.Start(new ProcessStartInfo(item.ProfileUrl));
            e.Handled = true;
        }
        #endregion

        #region PGM Settings
        private void PastePGMSettings_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomConfigDataWindow();
            window.ConfigDataTextWrapping = TextWrapping.Wrap;
            window.Title = _globalizer.GetResourceString("ServerSettings_PGM_PasteSettingsTitle");
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_PGM_PasteSettingsConfirmLabel"), _globalizer.GetResourceString("ServerSettings_PGM_PasteSettingsConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            // read the pasted data into an ini file.
            var iniFile = IniFileUtils.ReadString(window.ConfigData);

            var prop = Settings.GetType().GetProperty(nameof(Settings.PGM_Terrain));
            if (prop == null)
                return;
            var attr = prop.GetCustomAttributes(typeof(IniFileEntryAttribute), false).OfType<IniFileEntryAttribute>().FirstOrDefault();
            var keyName = string.IsNullOrWhiteSpace(attr?.Key) ? prop.Name : attr.Key;

            // cycle through the sections, adding them to the engrams list. Will bypass any sections that are named as per the ARK default sections.
            foreach (var section in iniFile.Sections.Where(s => s.SectionName != null && !SystemIniFile.SectionNames.ContainsValue(s.SectionName)))
            {
                foreach (var key in section.Keys.Where(s => s.KeyName.Equals(keyName)))
                {
                    Settings.PGM_Terrain.InitializeFromINIValue(key.KeyValue);
                }
            }
        }

        private void SavePGMSettings_Click(object sender, RoutedEventArgs e)
        {
            var prop = Settings.GetType().GetProperty(nameof(Settings.PGM_Terrain));
            if (prop == null)
                return;
            var attr = prop.GetCustomAttributes(typeof(IniFileEntryAttribute), false).OfType<IniFileEntryAttribute>().FirstOrDefault();
            var iniName = string.IsNullOrWhiteSpace(attr?.Key) ? prop.Name : attr.Key;
            var iniValue = $"{iniName}={Settings.PGM_Terrain.ToINIValue()}";

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.Wrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_PGM_SaveSettingsTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }

        private void RandomPGMSettings_Click(object sender, RoutedEventArgs e)
        {
            Settings.RandomizePGMSettings();
        }
        #endregion

        #region Map Spawner Overrides
        private void AddNPCSpawn_Click(object sender, RoutedEventArgs e)
        {
            Settings.NPCSpawnSettings.Add(new NPCSpawnSettings());
        }

        private void AddNPCSpawnEntry_Click(object sender, RoutedEventArgs e)
        {
            SelectedNPCSpawnSetting?.NPCSpawnEntrySettings.Add(new NPCSpawnEntrySettings());
        }

        private void ClearNPCSpawn_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SelectedNPCSpawnSetting = null;
            Settings.NPCSpawnSettings.Clear();
        }

        private void ClearNPCSpawnEntry_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SelectedNPCSpawnSetting?.NPCSpawnEntrySettings.Clear();
        }

        private void PasteNPCSpawn_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomConfigDataWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            // read the pasted data into an ini file.
            var iniFile = IniFileUtils.ReadString(window.ConfigData.Replace(" ", ""));

            Server.Profile.NPCSpawnSettings.RenderToModel();

            // cycle through the sections, adding them to the custom section list. Will bypass any sections that are named as per the ARK default sections.
            foreach (var section in iniFile.Sections.Where(s => s.SectionName != null && !SystemIniFile.SectionNames.ContainsValue(s.SectionName)))
            {
                var configAddNPCSpawnEntriesContainer = new NPCSpawnContainerList<NPCSpawnContainer>(nameof(Server.Profile.ConfigAddNPCSpawnEntriesContainer), NPCSpawnContainerType.Add);
                configAddNPCSpawnEntriesContainer.FromIniValues(section.KeysToStringArray().Where(s => s.StartsWith($"{configAddNPCSpawnEntriesContainer.IniCollectionKey}=")));
                Server.Profile.ConfigAddNPCSpawnEntriesContainer.AddRange(configAddNPCSpawnEntriesContainer);
                Server.Profile.ConfigAddNPCSpawnEntriesContainer.IsEnabled |= configAddNPCSpawnEntriesContainer.IsEnabled;

                var configSubtractNPCSpawnEntriesContainer = new NPCSpawnContainerList<NPCSpawnContainer>(nameof(Server.Profile.ConfigSubtractNPCSpawnEntriesContainer), NPCSpawnContainerType.Subtract);
                configSubtractNPCSpawnEntriesContainer.FromIniValues(section.KeysToStringArray().Where(s => s.StartsWith($"{configSubtractNPCSpawnEntriesContainer.IniCollectionKey}=")));
                Server.Profile.ConfigSubtractNPCSpawnEntriesContainer.AddRange(configSubtractNPCSpawnEntriesContainer);
                Server.Profile.ConfigSubtractNPCSpawnEntriesContainer.IsEnabled |= configSubtractNPCSpawnEntriesContainer.IsEnabled;

                var configOverrideNPCSpawnEntriesContainer = new NPCSpawnContainerList<NPCSpawnContainer>(nameof(Server.Profile.ConfigOverrideNPCSpawnEntriesContainer), NPCSpawnContainerType.Override);
                configOverrideNPCSpawnEntriesContainer.FromIniValues(section.KeysToStringArray().Where(s => s.StartsWith($"{configOverrideNPCSpawnEntriesContainer.IniCollectionKey}=")));
                Server.Profile.ConfigOverrideNPCSpawnEntriesContainer.AddRange(configOverrideNPCSpawnEntriesContainer);
                Server.Profile.ConfigOverrideNPCSpawnEntriesContainer.IsEnabled |= configOverrideNPCSpawnEntriesContainer.IsEnabled;
            }

            Server.Profile.NPCSpawnSettings = new NPCSpawnSettingsList(Server.Profile.ConfigAddNPCSpawnEntriesContainer, Server.Profile.ConfigSubtractNPCSpawnEntriesContainer, Server.Profile.ConfigOverrideNPCSpawnEntriesContainer);
            Server.Profile.NPCSpawnSettings.RenderToView();

            RefreshBaseMapSpawnerList();
            RefreshBaseDinoList();
        }

        private void RemoveNPCSpawn_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var item = ((NPCSpawnSettings)((Button)e.Source).DataContext);
            Settings.NPCSpawnSettings.Remove(item);
        }

        private void RemoveNPCSpawnEntry_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedNPCSpawnSetting == null)
                return;

            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var item = ((NPCSpawnEntrySettings)((Button)e.Source).DataContext);
            SelectedNPCSpawnSetting.NPCSpawnEntrySettings.Remove(item);
        }

        private void SaveNPCSpawns_Click(object sender, RoutedEventArgs e)
        {
            Settings.NPCSpawnSettings.RenderToModel();

            var iniValues = Settings.ConfigAddNPCSpawnEntriesContainer.ToIniValues().ToList();
            iniValues.AddRange(Settings.ConfigSubtractNPCSpawnEntriesContainer.ToIniValues());
            iniValues.AddRange(Settings.ConfigOverrideNPCSpawnEntriesContainer.ToIniValues());
            var iniValue = string.Join("\r\n", iniValues);

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.NoWrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_MapSpawnerOverrides_SaveTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }

        private void SaveNPCSpawn_Click(object sender, RoutedEventArgs e)
        {
            Settings.NPCSpawnSettings.RenderToModel();

            var item = ((NPCSpawnSettings)((Button)e.Source).DataContext);
            if (item == null)
                return;

            string iniName = null;
            string iniValue = null;
            switch (item.ContainerType)
            {
                case NPCSpawnContainerType.Add:
                    iniName = Settings.ConfigAddNPCSpawnEntriesContainer.IniCollectionKey;
                    var addItem = Settings.ConfigAddNPCSpawnEntriesContainer.FirstOrDefault(i => i.UniqueId == item.UniqueId);
                    iniValue = $"{iniName}={addItem?.ToIniValue(Settings.ConfigAddNPCSpawnEntriesContainer.ContainerType)}";
                    break;
                case NPCSpawnContainerType.Subtract:
                    iniName = Settings.ConfigSubtractNPCSpawnEntriesContainer.IniCollectionKey;
                    var subtractItem = Settings.ConfigSubtractNPCSpawnEntriesContainer.FirstOrDefault(i => i.UniqueId == item.UniqueId);
                    iniValue = $"{iniName}={subtractItem?.ToIniValue(Settings.ConfigSubtractNPCSpawnEntriesContainer.ContainerType)}";
                    break;
                case NPCSpawnContainerType.Override:
                    iniName = Settings.ConfigOverrideNPCSpawnEntriesContainer.IniCollectionKey;
                    var overrideItem = Settings.ConfigOverrideNPCSpawnEntriesContainer.FirstOrDefault(i => i.UniqueId == item.UniqueId);
                    iniValue = $"{iniName}={overrideItem?.ToIniValue(Settings.ConfigOverrideNPCSpawnEntriesContainer.ContainerType)}";
                    break;
                default:
                    return;
            }

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.Wrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_MapSpawnerOverrides_SaveTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }
        #endregion

        #region Supply Crate Overrides
        private void AddSupplyCrate_Click(object sender, RoutedEventArgs e)
        {
            Settings.ConfigOverrideSupplyCrateItems.Add(new SupplyCrateOverride());
            Settings.ConfigOverrideSupplyCrateItems.IsEnabled = true;
        }

        private void AddSupplyCrateItemSet_Click(object sender, RoutedEventArgs e)
        {
            SelectedSupplyCrateOverride?.ItemSets.Add(new SupplyCrateItemSet());
        }

        private void AddSupplyCrateItemSetEntry_Click(object sender, RoutedEventArgs e)
        {
            SelectedSupplyCrateItemSet?.ItemEntries.Add(new SupplyCrateItemSetEntry());
        }

        private void AddSupplyCrateItem_Click(object sender, RoutedEventArgs e)
        {
            SelectedSupplyCrateItemSetEntry?.Items.Add(new SupplyCrateItemEntrySettings());
        }

        private void ClearSupplyCrates_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SelectedSupplyCrateItemSetEntry = null;
            SelectedSupplyCrateItemSet = null;
            SelectedSupplyCrateOverride = null;
            Settings.ConfigOverrideSupplyCrateItems.Clear();
            Settings.ConfigOverrideSupplyCrateItems.IsEnabled = false;
        }

        private void ClearSupplyCrateItemSets_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SelectedSupplyCrateItemSetEntry = null;
            SelectedSupplyCrateItemSet = null;
            SelectedSupplyCrateOverride?.ItemSets.Clear();
        }

        private void ClearSupplyCrateItemSetEntries_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SelectedSupplyCrateItemSetEntry = null;
            SelectedSupplyCrateItemSet?.ItemEntries.Clear();
        }

        private void ClearSupplyCrateItems_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_ClearLabel"), _globalizer.GetResourceString("ServerSettings_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SelectedSupplyCrateItemSetEntry?.Items.Clear();
        }

        private void PasteSupplyCrate_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomConfigDataWindow();
            window.Owner = Window.GetWindow(this);
            window.Closed += Window_Closed;
            var result = window.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            // read the pasted data into an ini file.
            var iniFile = IniFileUtils.ReadString(window.ConfigData.Replace(" ", ""));

            Server.Profile.ConfigOverrideSupplyCrateItems.RenderToModel();

            // cycle through the sections, adding them to the engrams list. Will bypass any sections that are named as per the ARK default sections.
            foreach (var section in iniFile.Sections.Where(s => s.SectionName != null && !SystemIniFile.SectionNames.ContainsValue(s.SectionName)))
            {
                var configOverrideSupplyCrateItems = new SupplyCrateOverrideList(nameof(Server.Profile.ConfigOverrideSupplyCrateItems));
                configOverrideSupplyCrateItems.FromIniValues(section.KeysToStringArray().Where(s => s.StartsWith($"{configOverrideSupplyCrateItems.IniCollectionKey}=")));
                Server.Profile.ConfigOverrideSupplyCrateItems.AddRange(configOverrideSupplyCrateItems);
                Server.Profile.ConfigOverrideSupplyCrateItems.IsEnabled |= configOverrideSupplyCrateItems.IsEnabled;
            }

            var errors = Server.Profile.ConfigOverrideSupplyCrateItems.RenderToView();

            RefreshBaseSupplyCrateList();
            RefreshBasePrimalItemList();

            if (errors.Length > 0)
            {
                var error = $"The following errors have been found:\r\n\r\n{string.Join("\r\n", errors)}";

                var window2 = new CommandLineWindow(error);
                window2.OutputTextWrapping = TextWrapping.NoWrap;
                window2.Height = 500;
                window2.Title = "Import Errors";
                window2.Owner = Window.GetWindow(this);
                window2.ShowDialog();
            }
        }

        private void RemoveSupplyCrate_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var item = ((SupplyCrateOverride)((Button)e.Source).DataContext);
            Settings.ConfigOverrideSupplyCrateItems.Remove(item);
            Settings.ConfigOverrideSupplyCrateItems.IsEnabled = Settings.ConfigOverrideSupplyCrateItems.Count > 0;
        }

        private void RemoveSupplyCrateItemSet_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSupplyCrateOverride == null)
                return;

            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var item = ((SupplyCrateItemSet)((Button)e.Source).DataContext);
            SelectedSupplyCrateOverride.ItemSets.Remove(item);
        }

        private void RemoveSupplyCrateItemSetEntry_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSupplyCrateItemSet == null)
                return;

            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var item = ((SupplyCrateItemSetEntry)((Button)e.Source).DataContext);
            SelectedSupplyCrateItemSet.ItemEntries.Remove(item);
        }

        private void RemoveSupplyCrateItem_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSupplyCrateItemSetEntry == null)
                return;

            if (MessageBox.Show(_globalizer.GetResourceString("ServerSettings_DeleteLabel"), _globalizer.GetResourceString("ServerSettings_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var item = ((SupplyCrateItemEntrySettings)((Button)e.Source).DataContext);
            SelectedSupplyCrateItemSetEntry.Items.Remove(item);
        }

        private void SaveSupplyCrates_Click(object sender, RoutedEventArgs e)
        {
            Settings.ConfigOverrideSupplyCrateItems.RenderToModel();

            var iniValues = Settings.ConfigOverrideSupplyCrateItems.ToIniValues().ToList();
            var iniValue = string.Join("\r\n", iniValues);

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.NoWrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_SupplyCrate_SaveTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }

        private void SaveSupplyCrate_Click(object sender, RoutedEventArgs e)
        {
            var item = ((SupplyCrateOverride)((Button)e.Source).DataContext);
            if (item == null)
                return;

            Settings.ConfigOverrideSupplyCrateItems.RenderToModel();

            var iniName = Settings.ConfigOverrideSupplyCrateItems.IniCollectionKey;
            var iniValue = $"{iniName}={item.ToINIValue()}";

            var window = new CommandLineWindow(iniValue);
            window.OutputTextWrapping = TextWrapping.Wrap;
            window.Height = 500;
            window.Title = _globalizer.GetResourceString("ServerSettings_SupplyCrate_SaveTitle");
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }
        #endregion

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

        public void RefreshBaseArkApplicationList()
        {
            var newList = new ArkApplicationComboBoxItemList();

            var values = Enum.GetValues(typeof(ArkApplication)).Cast<ArkApplication>();
            foreach (var value in values)
            {
                var name = _globalizer.GetResourceString($"{nameof(ArkApplication)}_{value}");
                if (string.IsNullOrWhiteSpace(name) && (value == ArkApplication.All || value == ArkApplication.Unknown))
                    name = value.ToString();

                if (!string.IsNullOrWhiteSpace(name))
                    newList.Add(new ArkApplicationComboBoxItem(value, name));
            }

            this.BaseArkApplicationList = newList;
            this.ArkApplicationsDinoComboBox.SelectedValue = SelectedArkApplicationDino;
            this.ArkApplicationsResourceComboBox.SelectedValue = SelectedArkApplicationResource;
            this.ArkApplicationsEngramComboBox.SelectedValue = SelectedArkApplicationEngram;
        }

        public void RefreshBaseDinoList()
        {
            var newList = new ComboBoxItemList();

            foreach (var dino in GameData.GetDinoSpawns())
            {
                if (string.IsNullOrWhiteSpace(dino.ClassName))
                    continue;

                newList.Add(new Lib.ViewModel.ComboBoxItem
                {
                    DisplayMember = string.IsNullOrWhiteSpace(dino.Mod) ? $"{dino.DisplayName}" : $"{dino.DisplayName} ({dino.Mod})",
                    ValueMember = dino.ClassName,
                });
            }

            newList.Sort(i => $"{i.GroupMember}||{i.DisplayMember}");

            foreach (var dinoSetting in this.Settings.DinoSettings)
            {
                if (string.IsNullOrWhiteSpace(dinoSetting.ReplacementClass))
                    continue;

                if (!newList.Any(s => s.ValueMember.Equals(dinoSetting.ReplacementClass, StringComparison.OrdinalIgnoreCase)))
                {
                    newList.Add(new Lib.ViewModel.ComboBoxItem
                    {
                        DisplayMember = dinoSetting.ReplacementClass,
                        ValueMember = dinoSetting.ReplacementClass,
                    });
                }
            }

            foreach (var spawnSetting in this.Settings.NPCSpawnSettings)
            {
                foreach (var spawnEntry in spawnSetting.NPCSpawnEntrySettings)
                {
                    if (string.IsNullOrWhiteSpace(spawnEntry.NPCClassString))
                        continue;

                    if (!newList.Any(s => s.ValueMember.Equals(spawnEntry.NPCClassString, StringComparison.OrdinalIgnoreCase)))
                    {
                        newList.Add(new Lib.ViewModel.ComboBoxItem
                        {
                            DisplayMember = spawnEntry.NPCClassString,
                            ValueMember = spawnEntry.NPCClassString,
                        });
                    }
                }
            }

            try
            {
                this.DinoSettingsGrid.BeginInit();
                this.NPCSpawnEntrySettingsGrid.BeginInit();

                this.BaseDinoSettingsDinoList = newList;
                this.BaseMapSpawnerDinoList = newList;
            }
            finally
            {
                this.DinoSettingsGrid.EndInit();
                this.NPCSpawnEntrySettingsGrid.EndInit();
            }
        }

        public void RefreshBaseMapSpawnerList()
        {
            var newList = new ComboBoxItemList();

            foreach (var mapSpawner in GameData.GetMapSpawners())
            {
                newList.Add(new Lib.ViewModel.ComboBoxItem
                {
                    DisplayMember = string.IsNullOrWhiteSpace(mapSpawner.Mod) ? $"{mapSpawner.DisplayName}" : $"{mapSpawner.DisplayName} ({mapSpawner.Mod})",
                    ValueMember = mapSpawner.ClassName,
                });
            }

            newList.Sort(i => $"{i.GroupMember}||{i.DisplayMember}");

            foreach (var spawnSetting in this.Settings.NPCSpawnSettings)
            {
                if (string.IsNullOrWhiteSpace(spawnSetting.NPCSpawnEntriesContainerClassString))
                    continue;

                if (!newList.Any(s => s.ValueMember.Equals(spawnSetting.NPCSpawnEntriesContainerClassString, StringComparison.OrdinalIgnoreCase)))
                {
                    newList.Add(new Lib.ViewModel.ComboBoxItem
                    {
                        DisplayMember = spawnSetting.NPCSpawnEntriesContainerClassString,
                        ValueMember = spawnSetting.NPCSpawnEntriesContainerClassString,
                    });
                }
            }

            try
            {
                this.NPCSpawnSettingsGrid.BeginInit();

                this.BaseMapSpawnerList = newList;
            }
            finally
            {
                this.NPCSpawnSettingsGrid.EndInit();
            }
        }

        public void RefreshBasePrimalItemList()
        {
            var newList = new ComboBoxItemList();

            foreach (var primalItem in GameData.GetItems())
            {
                var categoryName = primalItem.ArkApplication == ArkApplication.SurvivalEvolved ? string.Empty : $" ({primalItem.ArkApplication.ToString()})";

                newList.Add(new Lib.ViewModel.ComboBoxItem
                {
                    DisplayMember = string.IsNullOrWhiteSpace(primalItem.Mod) ? $"{primalItem.DisplayName}" : $"{primalItem.DisplayName} ({primalItem.Mod})",
                    ValueMember = primalItem.ClassName,
                });
            }

            newList.Sort(i => $"{i.GroupMember}||{i.DisplayMember}");

            foreach (var craftingItem in this.Settings.ConfigOverrideItemCraftingCosts)
            {
                if (string.IsNullOrWhiteSpace(craftingItem.ItemClassString))
                    continue;

                if (!newList.Any(s => s.ValueMember.Equals(craftingItem.ItemClassString, StringComparison.OrdinalIgnoreCase)))
                {
                    newList.Add(new Lib.ViewModel.ComboBoxItem
                    {
                        DisplayMember = craftingItem.ItemClassString,
                        ValueMember = craftingItem.ItemClassString,
                    });
                }

                foreach (var craftingResource in craftingItem.BaseCraftingResourceRequirements)
                {
                    if (string.IsNullOrWhiteSpace(craftingResource.ResourceItemTypeString))
                        continue;

                    if (!newList.Any(s => s.ValueMember.Equals(craftingResource.ResourceItemTypeString, StringComparison.OrdinalIgnoreCase)))
                    {
                        newList.Add(new Lib.ViewModel.ComboBoxItem
                        {
                            DisplayMember = craftingResource.ResourceItemTypeString,
                            ValueMember = craftingResource.ResourceItemTypeString,
                        });
                    }
                }
            }

            foreach (var supplyCrate in this.Settings.ConfigOverrideSupplyCrateItems)
            {
                foreach (var itemSet in supplyCrate.ItemSets)
                {
                    foreach (var itemEntry in itemSet.ItemEntries)
                    {
                        foreach (var itemClass in itemEntry.Items)
                        {
                            if (string.IsNullOrWhiteSpace(itemClass.ItemClassString))
                                continue;

                            if (!newList.Any(s => s.ValueMember.Equals(itemClass.ItemClassString, StringComparison.OrdinalIgnoreCase)))
                            {
                                newList.Add(new Lib.ViewModel.ComboBoxItem
                                {
                                    DisplayMember = itemClass.ItemClassString,
                                    ValueMember = itemClass.ItemClassString,
                                });
                            }
                        }
                    }
                }
            }

            try
            {
                this.CraftingOverrideItemGrid.BeginInit();
                this.CraftingOverrideResourceGrid.BeginInit();
                this.SupplyCrateItemsGrid.BeginInit();

                this.BasePrimalItemList = newList;
            }
            finally
            {
                this.CraftingOverrideItemGrid.EndInit();
                this.CraftingOverrideResourceGrid.EndInit();
                this.SupplyCrateItemsGrid.EndInit();
            }
        }

        public void RefreshBaseSupplyCrateList()
        {
            var newList = new ComboBoxItemList();

            foreach (var primalItem in GameData.GetSupplyCrates())
            {
                newList.Add(new Lib.ViewModel.ComboBoxItem
                {
                    DisplayMember = string.IsNullOrWhiteSpace(primalItem.Mod) ? $"{primalItem.DisplayName}" : $"{primalItem.DisplayName} ({primalItem.Mod})",
                    ValueMember = primalItem.ClassName,
                });
            }

            newList.Sort(i => $"{i.GroupMember}||{i.DisplayMember}");

            foreach (var supplyCrate in this.Settings.ConfigOverrideSupplyCrateItems)
            {
                if (!newList.Any(s => s.ValueMember.Equals(supplyCrate.SupplyCrateClassString, StringComparison.OrdinalIgnoreCase)))
                {
                    if (string.IsNullOrWhiteSpace(supplyCrate.SupplyCrateClassString))
                        continue;

                    newList.Add(new Lib.ViewModel.ComboBoxItem
                    {
                        DisplayMember = supplyCrate.SupplyCrateClassString,
                        ValueMember = supplyCrate.SupplyCrateClassString,
                    });
                }
            }

            try
            {
                this.SupplyCratesGrid.BeginInit();

                this.BaseSupplyCrateList = newList;
            }
            finally
            {
                this.SupplyCratesGrid.EndInit();
            }
        }

        public void RefreshBaseGameMapsList()
        {
            var newList = new ComboBoxItemList();

            if (this.Settings.SOTF_Enabled)
            {
                foreach (var gameMap in GameData.GetGameMapsSotF())
                {
                    newList.Add(gameMap);
                }
            }
            else
            {
                foreach (var gameMap in GameData.GetGameMaps())
                {
                    newList.Add(gameMap);
                }
            }

            if (!string.IsNullOrWhiteSpace(this.Settings.ServerMap))
            {
                if (!newList.Any(m => m.ValueMember.Equals(this.Settings.ServerMap, StringComparison.OrdinalIgnoreCase)))
                {
                    newList.Add(new Lib.ViewModel.ComboBoxItem
                    {
                        DisplayMember = this.Settings.SOTF_Enabled ? GameData.FriendlyMapSotFNameForClass(this.Settings.ServerMap) : GameData.FriendlyMapNameForClass(this.Settings.ServerMap),
                        ValueMember = this.Settings.ServerMap,
                    });
                }
            }

            this.BaseGameMaps = newList;
            this.GameMapComboBox.SelectedValue = this.Settings.ServerMap;
        }

        public void RefreshBaseTotalConversionsList()
        {
            var newList = new ComboBoxItemList();

            if (this.Settings.SOTF_Enabled)
            {
                foreach (var gameMap in GameData.GetTotalConversionsSotF())
                {
                    newList.Add(gameMap);
                }
            }
            else
            {
                foreach (var item in GameData.GetTotalConversions())
                {
                    newList.Add(item);
                }
            }

            if (!string.IsNullOrWhiteSpace(this.Settings.TotalConversionModId))
            {
                if (!newList.Any(m => m.ValueMember.Equals(this.Settings.TotalConversionModId, StringComparison.OrdinalIgnoreCase)))
                {
                    newList.Add(new Lib.ViewModel.ComboBoxItem
                    {
                        DisplayMember = this.Settings.SOTF_Enabled ? GameData.FriendlyTotalConversionSotFNameForClass(this.Settings.TotalConversionModId) : GameData.FriendlyTotalConversionNameForClass(this.Settings.TotalConversionModId),
                        ValueMember = this.Settings.TotalConversionModId,
                    });
                }
            }

            this.BaseTotalConversions = newList;
            this.TotalConversionComboBox.SelectedValue = this.Settings.TotalConversionModId;
        }

        public void RefreshBaseBranchesList()
        {
            var newList = new ComboBoxItemList();

            if (this.Settings.SOTF_Enabled)
            {
                foreach (var item in GameData.GetBranchesSotF())
                {
                    newList.Add(item);
                }
            }
            else
            {
                foreach (var item in GameData.GetBranches())
                {
                    newList.Add(item);
                }
            }

            if (!string.IsNullOrWhiteSpace(this.Settings.BranchName))
            {
                if (!newList.Any(m => m.ValueMember.Equals(this.Settings.BranchName, StringComparison.OrdinalIgnoreCase)))
                {
                    newList.Add(new Lib.ViewModel.ComboBoxItem
                    {
                        DisplayMember = this.Settings.SOTF_Enabled ? GameData.FriendlyBranchSotFName(this.Settings.BranchName) : GameData.FriendlyBranchName(this.Settings.BranchName),
                        ValueMember = this.Settings.BranchName,
                    });
                }
            }

            this.BaseBranches = newList;
            this.BranchComboBox.SelectedValue = this.Settings.BranchName;
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
                                RefreshBaseGameMapsList();
                                RefreshBaseTotalConversionsList();
                                RefreshBaseBranchesList();
                                break;

                            case ServerSettingsResetAction.ChatAndNotificationsSection:
                                this.Settings.ResetChatAndNotificationSection();
                                break;

                            case ServerSettingsResetAction.CraftingOverridesSection:
                                this.Settings.ResetCraftingOverridesSection();
                                RefreshBasePrimalItemList();
                                break;

                            case ServerSettingsResetAction.CustomLevelsSection:
                                this.Settings.ResetCustomLevelsSection();
                                break;

                            case ServerSettingsResetAction.DinoSettingsSection:
                                this.Settings.ResetDinoSettingsSection();
                                RefreshBaseDinoList();
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

                            case ServerSettingsResetAction.MapSpawnerOverridesSection:
                                this.Settings.ResetNPCSpawnOverridesSection();
                                RefreshBaseMapSpawnerList();
                                RefreshBaseDinoList();
                                break;

                            case ServerSettingsResetAction.PGMSection:
                                this.Settings.ResetPGMSection();
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

                            case ServerSettingsResetAction.SupplyCrateOverridesSection:
                                this.Settings.ResetSupplyCreateOverridesSection();
                                RefreshBaseSupplyCrateList();
                                RefreshBasePrimalItemList();
                                break;

                            // Properties
                            case ServerSettingsResetAction.MapNameTotalConversionProperty:
                                // set the map name to the ARK default.
                                var mapName = Config.Default.DefaultServerMap;

                                // check if we are running an official total conversion mod.
                                if (!this.Settings.TotalConversionModId.Equals(ModUtils.MODID_PRIMITIVEPLUS))
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

                            case ServerSettingsResetAction.BanListProperty:
                                this.Settings.ResetBanlist();
                                break;

                            case ServerSettingsResetAction.PlayerMaxXpProperty:
                                this.Settings.ResetOverrideMaxExperiencePointsPlayer();
                                break;

                            case ServerSettingsResetAction.DinoMaxXpProperty:
                                this.Settings.ResetOverrideMaxExperiencePointsDino();
                                break;

                            case ServerSettingsResetAction.PlayerBaseStatMultipliers:
                                this.Settings.PlayerBaseStatMultipliers.Reset();
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

                            case ServerSettingsResetAction.ServerOptions:
                                this.Settings.ResetServerOptions();
                                break;

                            case ServerSettingsResetAction.ServerLogOptions:
                                this.Settings.ResetServerLogOptions();
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
                    execute: async (parameter) =>
                    {
                        try
                        {
                            dockPanel.IsEnabled = false;
                            OverlayMessage.Content = _globalizer.GetResourceString("ServerSettings_OverlayMessage_SavingLabel");
                            OverlayGrid.Visibility = Visibility.Visible;

                            await Task.Delay(500);

                            // NOTE: This parameter is of type object and must be cast in most cases before use.
                            var server = parameter as Server;
                            if (server != null)
                            {
                                if (server.Profile.EnableAutoShutdown1 || server.Profile.EnableAutoShutdown2)
                                {
                                    if (server.Profile.SOTF_Enabled)
                                    {
                                        MessageBox.Show(_globalizer.GetResourceString("ServerSettings_Save_AutoRestart_SotF_ErrorLabel"), _globalizer.GetResourceString("ServerSettings_Save_AutoRestart_SotF_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                                        server.Profile.EnableAutoShutdown1 = false;
                                        server.Profile.RestartAfterShutdown1 = true;
                                        server.Profile.EnableAutoShutdown2 = false;
                                        server.Profile.RestartAfterShutdown2 = true;
                                        server.Profile.AutoRestartIfShutdown = false;
                                    }
                                }

                                if (server.Profile.EnableAutoUpdate)
                                {
                                    if (server.Profile.SOTF_Enabled)
                                    {
                                        MessageBox.Show(_globalizer.GetResourceString("ServerSettings_Save_AutoUpdate_SotF_ErrorLabel"), _globalizer.GetResourceString("ServerSettings_Save_AutoUpdate_SotF_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                                        server.Profile.EnableAutoUpdate = false;
                                        server.Profile.AutoRestartIfShutdown = false;
                                    }
                                }

                                server.Profile.Save(false, false, (p, m, n) => { OverlayMessage.Content = m; });

                                RefreshBaseDinoList();
                                RefreshBaseMapSpawnerList();
                                RefreshBasePrimalItemList();
                                RefreshBaseSupplyCrateList();
                                RefreshBaseGameMapsList();
                                RefreshBaseTotalConversionsList();
                                RefreshBaseBranchesList();

                                OverlayMessage.Content = _globalizer.GetResourceString("ServerSettings_OverlayMessage_PermissionsLabel");
                                await Task.Delay(500);

                                if (!server.Profile.UpdateDirectoryPermissions())
                                {
                                    MessageBox.Show(_globalizer.GetResourceString("ServerSettings_Save_UpdatePermissions_ErrorLabel"), _globalizer.GetResourceString("ServerSettings_Save_UpdatePermissions_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                                }

                                OverlayMessage.Content = _globalizer.GetResourceString("ServerSettings_OverlayMessage_SchedulesLabel");
                                await Task.Delay(500);

                                if (!server.Profile.UpdateSchedules())
                                {
                                    MessageBox.Show(_globalizer.GetResourceString("ServerSettings_Save_UpdateSchedule_ErrorLabel"), _globalizer.GetResourceString("ServerSettings_Save_UpdateSchedule_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        finally
                        {
                            OverlayGrid.Visibility = Visibility.Collapsed;
                            dockPanel.IsEnabled = true;
                        }
                    },
                    canExecute: (parameter) =>
                    {
                        return (parameter as Server) != null;
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

                    var branch = new ServerBranchSnapshot() { BranchName = this.Server.Profile.BranchName, BranchPassword = this.Server.Profile.BranchPassword };
                    return await this.Server.UpgradeAsync(_upgradeCancellationSource.Token, updateServer, branch, true, updateMods, (p, m, n) => { TaskUtils.RunOnUIThreadAsync(() => { window?.AddMessage(m, n); }).DoNotWait(); });
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
            Settings.LoadServerFileAdministrators();
            Settings.LoadServerFileExclusive();
            Settings.LoadServerFileWhitelisted();
        }
        #endregion
    }
}
