using ARK_Server_Manager.Lib;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for ServerSettings.xaml
    /// </summary>
    partial class ServerSettingsControl : UserControl
    {
        public static readonly DependencyProperty SettingsProperty = 
            DependencyProperty.Register("Settings", typeof(ServerSettingsViewModel), typeof(ServerSettingsControl));
        public static readonly DependencyProperty RuntimeProperty = 
            DependencyProperty.Register("Runtime", typeof(ServerRuntimeViewModel), typeof(ServerSettingsControl));
        public static readonly DependencyProperty WhitelistUserProperty = 
            DependencyProperty.Register("WhitelistUser", typeof(string), typeof(ServerSettingsControl), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty NetworkInterfacesProperty = 
            DependencyProperty.Register("NetworkInterfaces", typeof(List<NetworkAdapterEntry>), typeof(ServerSettingsControl), new PropertyMetadata(new List<NetworkAdapterEntry>()));
        public static readonly DependencyProperty AvailableVersionProperty =
            DependencyProperty.Register("AvailableVersion", typeof(NetworkUtils.AvailableVersion), typeof(ServerSettingsControl), new PropertyMetadata(new NetworkUtils.AvailableVersion()));

        CancellationTokenSource upgradeCancellationSource;

        public NetworkUtils.AvailableVersion AvailableVersion
        {
            get { return (NetworkUtils.AvailableVersion)GetValue(AvailableVersionProperty); }
            set { SetValue(AvailableVersionProperty, value); }
        }

        public string WhitelistUser
        {
            get { return (string)GetValue(WhitelistUserProperty); }
            set { SetValue(WhitelistUserProperty, value); }
        }
        
        public ServerSettingsViewModel Settings
        {
            get { return GetValue(SettingsProperty) as ServerSettingsViewModel; }
            set { SetValue(SettingsProperty, value); }
        }

        public ServerRuntimeViewModel Runtime
        {
            get { return GetValue(RuntimeProperty) as ServerRuntimeViewModel; }
            set { SetValue(RuntimeProperty, value); }
        }

        public List<NetworkAdapterEntry> NetworkInterfaces
        {
            get { return (List<NetworkAdapterEntry>)GetValue(NetworkInterfacesProperty); }
            set { SetValue(NetworkInterfacesProperty, value); }
        }

        internal ServerSettingsControl(ServerSettings settings)
        {
            InitializeComponent();
            ReinitializeFromSettings(settings);
            ReinitializeNetworkAdapters();
            CheckForUpdatesAsync();
        }

        private void ReinitializeFromSettings(ServerSettings settings)
        {
            this.Settings = new ServerSettingsViewModel(settings);
            this.Runtime = new ServerRuntimeViewModel(settings);
        }
           
#if false
        private void WhitelistAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(this.WhitelistUser))
            {
                Settings.Whitelist.Add(this.WhitelistUser);
            }
        }

        private void WhitelistRemove_Click(object sender, RoutedEventArgs e)
        {
            if (this.WhitelistControl.SelectedIndex >= 0)
            {
                Settings.Whitelist.RemoveAt(this.WhitelistControl.SelectedIndex);
            }
        }
#endif

        private void ReinitializeNetworkAdapters()
        {
            var adapters = NetworkUtils.GetAvailableIPV4NetworkAdapters();

            //
            // Filter out self-assigned addresses
            //
            adapters.RemoveAll(a => a.IPAddress.StartsWith("169.254."));           
            this.NetworkInterfaces = adapters;            


            //
            // If there isn't already an adapter assigned, pick one
            //
            var preferredIP = NetworkUtils.GetPreferredIP(adapters);
            preferredIP.Description = "(Recommended) " + preferredIP.Description;
            if(String.IsNullOrWhiteSpace(this.Settings.ServerIP))
            {
                if(preferredIP != null)
                {
                    this.Settings.ServerIP = preferredIP.IPAddress;
                }
            } 
            else if(adapters.FirstOrDefault(a => String.Equals(a.IPAddress, this.Settings.ServerIP, StringComparison.OrdinalIgnoreCase)) == null) 
            {
                MessageBox.Show(
                    String.Format("Your Local IP address {0} is no longer available.  Please review the available IP addresses and select a valid one.  If you have a server running on the original IP, you will need to stop it first.", this.Settings.ServerIP), 
                    "Local IP invalid", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);            
            }
        }        

        private async void Upgrade_Click(object sender, RoutedEventArgs e)
        {
            if(this.Runtime.Model.ExecutionStatus == ServerRuntime.ServerStatus.Updating)
            {
                // Cancel the current upgrade
                upgradeCancellationSource.Cancel();
            }
            else
            {
                if(this.Runtime.Model.IsRunning)
                {
                    var result = MessageBox.Show("The server must be stopped to upgrade.  Do you wish to proceed?", "Server running", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if(result == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                // Start the upgrade
                upgradeCancellationSource = new CancellationTokenSource();
                if(await this.Runtime.Model.UpgradeAsync(upgradeCancellationSource.Token, validate: true))
                {
                    if (AvailableVersion != null && AvailableVersion.Current != null)
                    {
                        this.Settings.Model.LastInstalledVersion = AvailableVersion.Current.ToString();
                        this.Runtime.Model.InstalledVersion = AvailableVersion.Current;
                    }
                }

            }                       
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            if (this.Runtime.Model.IsRunning)
            {
                var result = MessageBox.Show("This will shut down the server.  Do you wish to proceed?", "Stop the server?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    return;
                }

                await this.Runtime.Model.StopAsync();
            }
            else
            {
                this.Settings.Model.Save();
                await this.Runtime.Model.StartAsync();
            }
        }

        private void SelectSaveDirectory_Click(object sender, RoutedEventArgs e)
        {            
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.Title = "Select Save Directory";
            if (!String.IsNullOrWhiteSpace(Settings.SaveDirectory))
            {
                dialog.InitialDirectory = Settings.SaveDirectory;
            }

            var result = dialog.ShowDialog();            
            if(result == CommonFileDialogResult.Ok)
            {
                Settings.SaveDirectory = dialog.FileName;
            }
        }

        private void SelectInstallDirectory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.Title = "Select Install Directory";
            if(!String.IsNullOrWhiteSpace(Settings.InstallDirectory))
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
            dialog.Title = "Load Server Profile or GameUserSettings.ini";
            dialog.Filters.Add(new CommonFileDialogFilter("Profile", Config.Default.LoadProfileExtensionList));
            if(!Directory.Exists(Config.Default.ConfigDirectory))
            {
                System.IO.Directory.CreateDirectory(Config.Default.ConfigDirectory);
            }

            dialog.InitialDirectory = Config.Default.ConfigDirectory;
            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                try
                {
                    var settings = ServerSettings.LoadFrom(dialog.FileName);
                    if (settings != null)
                    {
                        ReinitializeFromSettings(settings);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(String.Format("The profile at {0} failed to load.  The error was: {1}\r\n{2}", dialog.FileName, ex.Message, ex.StackTrace), "Profile failed to load", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }            
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Settings.Model.Save();
        }

        private void CopyProfile_Click(object sender, RoutedEventArgs e)
        {
        }

        private void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ShowCmd_Click(object sender, RoutedEventArgs e)
        {
            var cmdLine = new CommandLine(String.Format("{0} {1}", this.Runtime.Model.GetServerExe(), this.Settings.Model.GetServerArgs()));
            cmdLine.ShowDialog();
        }

        private void RemovePlayerLevel_Click(object sender, RoutedEventArgs e)
        {            
            if(this.Settings.PlayerLevels.Count == 1)
            {
                MessageBox.Show("You can't delete the last level.  If you want to disable the feature, uncheck Enable Custom Level Progressions.", "Can't delete last item", MessageBoxButton.OK, MessageBoxImage.Hand);
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
            this.Settings.PlayerLevels.AddNewLevel(level);
        }

        private void RemoveDinoLevel_Click(object sender, RoutedEventArgs e)
        {
            if (this.Settings.DinoLevels.Count == 1)
            {
                MessageBox.Show("You can't delete the last level.  If you want to disable the feature, uncheck Enable Custom Level Progressions.", "Can't delete last item", MessageBoxButton.OK, MessageBoxImage.Hand);
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
            this.Settings.DinoLevels.AddNewLevel(level);
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

        private void RefreshLocalIPs_Click(object sender, RoutedEventArgs e)
        {
            ReinitializeNetworkAdapters();
        }

        private void DinoLevels_Clear(object sender, RoutedEventArgs e)
        {
            this.Settings.Model.ClearLevelProgression(ServerSettings.LevelProgression.Dino);
        }

        private void DinoLevels_Reset(object sender, RoutedEventArgs e)
        {
            this.Settings.Model.ResetLevelProgressionToDefault(ServerSettings.LevelProgression.Dino);
        }

        private void PlayerLevels_Clear(object sender, RoutedEventArgs e)
        {
            this.Settings.Model.ClearLevelProgression(ServerSettings.LevelProgression.Player);
        }

        private void PlayerLevels_Reset(object sender, RoutedEventArgs e)
        {
            this.Settings.Model.ResetLevelProgressionToDefault(ServerSettings.LevelProgression.Player);
        }

        private void DinoSpawn_Reset(object sender, RoutedEventArgs e)
        {
            this.Settings.Model.ResetDinoSpawnsToDefault();
        }

        private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            await CheckForUpdatesAsync();
        }

        private async Task CheckForUpdatesAsync()
        {
            var result = await NetworkUtils.CheckForUpdatesAsync();
            await App.Current.Dispatcher.BeginInvoke(new Action(() => this.AvailableVersion = result));
        }

        private void BackupListView_Loaded(object sender, RoutedEventArgs e)
        {
            BackupRefresh();
        }

        private void CreateBackupButton_Click(object sender, RoutedEventArgs e)
        {
            string formatFile = @"\" + DateTime.Now.ToString("dddd, dd MMMM yyyy - hh_mm tt") + ".zip";
            string savedFilesDir = @System.IO.Path.Combine(this.Settings.InstallDirectory, Config.Default.BackupThisLoc);
            string backupToDir = @System.IO.Path.Combine(this.Settings.InstallDirectory, Config.Default.BackupDir + formatFile);
            
            if (!System.IO.File.Exists(backupToDir)) 
            {
                ZipFile.CreateFromDirectory(savedFilesDir, backupToDir, CompressionLevel.Fastest, true);
                BackupRefresh();
            }
            else
            {
                MessageBox.Show(String.Format("There is already a backup matching {0}.\r\nPlease wait a few minutes to create a new one.", backupToDir), "Backup Exists", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        private void LoadBackupButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedBackup = ((ListViewItem)BackupListView.SelectedItem).Content.ToString();
            string fileDir = System.IO.Path.Combine(Config.Default.BackupDir + @"\" + selectedBackup);
            string unpackBackupTo = System.IO.Path.Combine(this.Settings.InstallDirectory + @"\ShooterGame");
            var settings = ServerSettings.LoadFrom(System.IO.Path.Combine(this.Settings.InstallDirectory, Config.Default.ServerConfigRelativePath + @"\" + Config.Default.ServerGameUserSettingsFile));

            var mbResult = MessageBox.Show(String.Format("Are you sure you want to load the following backup? \r\n\n {0} \r\n\nDoing so will overwrite save files with the ones from this backup.", selectedBackup), "Confirm loading backup", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (mbResult == MessageBoxResult.Yes)
            {
                System.IO.Directory.Delete(unpackBackupTo, true);
                ZipFile.ExtractToDirectory(fileDir, unpackBackupTo);
                ReinitializeFromSettings(settings);
            }
        }

        private void DelBackupButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedBackup = ((ListViewItem)BackupListView.SelectedItem).Content.ToString();
            string fileDir = System.IO.Path.Combine(Config.Default.BackupDir + @"\" + selectedBackup);

            var mbResult = MessageBox.Show(String.Format("Are you sure you want to delete the following backup? \r\n\n {0} \r\n\n", selectedBackup), "Confirm deleting backup", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (mbResult == MessageBoxResult.Yes)
            {
                System.IO.Directory.Delete(fileDir, true);
            }
        }

        public void BackupRefresh()
        {
            string[] fileArray = Directory.GetFiles(Config.Default.BackupDir, "*.ZIP");

            BackupListView.Items.Clear();

            foreach (string file in fileArray)
            {
                string fileName = System.IO.Path.GetFileName(file);

                ListViewItem fileNameItem = new ListViewItem();
                fileNameItem.Content = fileName;
                fileNameItem.Tag = file;

                BackupListView.Items.Add(fileNameItem);
            }
        }
    }
}
