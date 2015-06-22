using ARK_Server_Manager.Lib;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        ServerSettingsViewModel settingsViewModel;
        ServerRuntimeViewModel runtimeViewModel;
        CancellationTokenSource upgradeCancellationSource;

        public ServerSettingsViewModel Settings
        {
            get { return this.settingsViewModel; }
        }

        public ServerRuntimeViewModel Runtime
        {
            get { return this.runtimeViewModel; }
        }

        internal ServerSettingsControl(ServerSettingsViewModel viewModel)
        {
            InitializeComponent();
            this.settingsViewModel = viewModel;
            this.runtimeViewModel = new ServerRuntimeViewModel(settingsViewModel.Model);
            this.DataContext = this;
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
                    var result = MessageBox.Show("The server must be stopped to upgrade.  Do you wish to proceed?", "Server running", MessageBoxButton.YesNo);
                    if(result != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                // Start the upgrade
                upgradeCancellationSource = new CancellationTokenSource();
                await this.Runtime.Model.UpgradeAsync(upgradeCancellationSource.Token);
            }                       
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            if (this.Runtime.Model.IsRunning)
            {
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
            if (!String.IsNullOrWhiteSpace(settingsViewModel.SaveDirectory))
            {
                dialog.InitialDirectory = settingsViewModel.SaveDirectory;
            }

            var result = dialog.ShowDialog();            
            if(result == CommonFileDialogResult.Ok)
            {
                settingsViewModel.SaveDirectory = dialog.FileName;
            }
        }

        private void SelectInstallDirectory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if(!String.IsNullOrWhiteSpace(settingsViewModel.InstallDirectory))
            {
                dialog.InitialDirectory = settingsViewModel.InstallDirectory;
            }

            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                settingsViewModel.InstallDirectory = dialog.FileName;
            }
        }
    }
}
