using ARK_Server_Manager.Lib;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
        public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register("Settings", typeof(ServerSettingsViewModel), typeof(ServerSettingsControl));
        public static readonly DependencyProperty RuntimeProperty = DependencyProperty.Register("Runtime", typeof(ServerRuntimeViewModel), typeof(ServerSettingsControl));
        public static readonly DependencyProperty WhitelistUserProperty = DependencyProperty.Register("WhitelistUser", typeof(string), typeof(ServerSettingsControl), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty NetworkInterfacesProperty = DependencyProperty.Register("NetworkInterfaces", typeof(List<NetworkAdapterEntry>), typeof(ServerSettingsControl), new PropertyMetadata(new List<NetworkAdapterEntry>()));

        CancellationTokenSource upgradeCancellationSource;

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
            var adapters = NetworkAdapters.GetAvailableIPV4NetworkAdapters();
            this.NetworkInterfaces = adapters;

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
                await this.Runtime.Model.UpgradeAsync(upgradeCancellationSource.Token, validate: true);
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
    }
}
