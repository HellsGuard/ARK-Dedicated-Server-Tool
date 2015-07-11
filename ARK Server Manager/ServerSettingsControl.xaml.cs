using ARK_Server_Manager.Lib;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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
            DependencyProperty.Register("Settings", typeof(ServerProfile), typeof(ServerSettingsControl));
        public static readonly DependencyProperty RuntimeProperty = 
            DependencyProperty.Register("Runtime", typeof(ServerRuntime), typeof(ServerSettingsControl));
        public static readonly DependencyProperty WhitelistUserProperty = 
            DependencyProperty.Register("WhitelistUser", typeof(string), typeof(ServerSettingsControl), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty NetworkInterfacesProperty = 
            DependencyProperty.Register("NetworkInterfaces", typeof(List<NetworkAdapterEntry>), typeof(ServerSettingsControl), new PropertyMetadata(new List<NetworkAdapterEntry>()));
        public static readonly DependencyProperty AvailableVersionProperty =
            DependencyProperty.Register("AvailableVersion", typeof(NetworkUtils.AvailableVersion), typeof(ServerSettingsControl), new PropertyMetadata(new NetworkUtils.AvailableVersion()));

        CancellationTokenSource upgradeCancellationSource;
        RCONWindow rconWindow;

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
        
        public ServerProfile Settings
        {
            get { return GetValue(SettingsProperty) as ServerProfile; }
            set { SetValue(SettingsProperty, value); }
        }

        public ServerRuntime Runtime
        {
            get { return GetValue(RuntimeProperty) as ServerRuntime; }
            set { SetValue(RuntimeProperty, value); }
        }

        public List<NetworkAdapterEntry> NetworkInterfaces
        {
            get { return (List<NetworkAdapterEntry>)GetValue(NetworkInterfacesProperty); }
            set { SetValue(NetworkInterfacesProperty, value); }
        }

        internal ServerSettingsControl(ServerProfile settings)
        {
            InitializeComponent();
            ReinitializeFromSettings(settings);
            ReinitializeNetworkAdapters();
            CheckForUpdatesAsync();
        }

        private void ReinitializeFromSettings(ServerProfile settings)
        {
            this.Settings = settings;
            this.Runtime = new ServerRuntime(settings);
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
            if(this.Runtime.Status == ServerRuntime.ServerStatus.Updating)
            {
                // Cancel the current upgrade
                upgradeCancellationSource.Cancel();
            }
            else
            {
                if(this.Runtime.IsRunning)
                {
                    var result = MessageBox.Show("The server must be stopped to upgrade.  Do you wish to proceed?", "Server running", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if(result == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                // Start the upgrade
                upgradeCancellationSource = new CancellationTokenSource();
                if(await this.Runtime.UpgradeAsync(upgradeCancellationSource.Token, validate: true))
                {
                    if (AvailableVersion != null && AvailableVersion.Current != null)
                    {
                        this.Settings.LastInstalledVersion = AvailableVersion.Current.ToString();
                        this.Runtime.Version = AvailableVersion.Current;
                    }
                }

            }                       
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            if (this.Runtime.IsRunning)
            {
                var result = MessageBox.Show("This will shut down the server.  Do you wish to proceed?", "Stop the server?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    return;
                }

                await this.Runtime.StopAsync();
            }
            else
            {
                this.Settings.Save();
                await this.Runtime.StartAsync();
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
                    var settings = ServerProfile.LoadFrom(dialog.FileName);
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
            Settings.Save();
        }

        private void CopyProfile_Click(object sender, RoutedEventArgs e)
        {
        }

        private void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ShowCmd_Click(object sender, RoutedEventArgs e)
        {
            var cmdLine = new CommandLine(String.Format("{0} {1}", this.Runtime.GetServerExe(), this.Settings.GetServerArgs()));
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
            this.Settings.ClearLevelProgression(ServerProfile.LevelProgression.Dino);
        }

        private void DinoLevels_Reset(object sender, RoutedEventArgs e)
        {
            this.Settings.ResetLevelProgressionToDefault(ServerProfile.LevelProgression.Dino);
        }

        private void PlayerLevels_Clear(object sender, RoutedEventArgs e)
        {
            this.Settings.ClearLevelProgression(ServerProfile.LevelProgression.Player);
        }

        private void PlayerLevels_Reset(object sender, RoutedEventArgs e)
        {
            this.Settings.ResetLevelProgressionToDefault(ServerProfile.LevelProgression.Player);
        }

        private void DinoSpawn_Reset(object sender, RoutedEventArgs e)
        {
            this.Settings.ResetDinoSpawnsToDefault();
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

        private void OpenRCON_Click(object sender, RoutedEventArgs e)
        {
            if(this.rconWindow == null || !this.rconWindow.IsLoaded)
            {
                this.rconWindow = new RCONWindow(this.Settings.ProfileName, new IPEndPoint(IPAddress.Parse(this.Settings.ServerIP), this.Settings.RCONPort), this.Settings.AdminPassword);                
            }

            this.rconWindow.Show();
            this.rconWindow.Focus();
        }
    }
}
