using ARK_Server_Manager.Lib;
using EO.Wpf;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty IsIpValidProperty = DependencyProperty.Register(nameof(IsIpValid), typeof(bool), typeof(MainWindow));
        public static readonly DependencyProperty CurrentConfigProperty = DependencyProperty.Register(nameof(CurrentConfig), typeof(Config), typeof(MainWindow));
        public static readonly DependencyProperty ServerManagerProperty = DependencyProperty.Register(nameof(ServerManager), typeof(ServerManager), typeof(MainWindow), new PropertyMetadata(null));
        
        public static MainWindow Instance
        {
            get;
            private set;
        }

        public bool IsIpValid
        {
            get { return (bool)GetValue(IsIpValidProperty); }
            set { SetValue(IsIpValidProperty, value); }
        }

        public Config CurrentConfig
        {
            get { return GetValue(CurrentConfigProperty) as Config; }
            set { SetValue(CurrentConfigProperty, value); }
        }

        public ServerManager ServerManager
        {
            get { return (ServerManager)GetValue(ServerManagerProperty); }
            set { SetValue(ServerManagerProperty, value); }
        }

        public Version LatestASMVersion
        {
            get { return (Version)GetValue(LatestASMVersionProperty); }
            set { SetValue(LatestASMVersionProperty, value); }
        }

        public static readonly DependencyProperty LatestASMVersionProperty = DependencyProperty.Register(nameof(LatestASMVersion), typeof(Version), typeof(MainWindow), new PropertyMetadata(new Version()));

        public bool NewASMAvailable
        {
            get { return (bool)GetValue(NewASMAvailableProperty); }
            set { SetValue(NewASMAvailableProperty, value); }
        }

        public static readonly DependencyProperty NewASMAvailableProperty = DependencyProperty.Register(nameof(NewASMAvailable), typeof(bool), typeof(MainWindow), new PropertyMetadata(false));


        ActionQueue versionChecker;

        public MainWindow()
        {
            this.CurrentConfig = Config.Default;

            InitializeComponent();
            MainWindow.Instance = this;
            this.ServerManager = ServerManager.Instance;

            this.DataContext = this;
            this.versionChecker = new ActionQueue();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //
            // Kick off the initialization.
            //
            TaskUtils.RunOnUIThreadAsync(() =>
                {
                    // We need to load the set of existing servers, or create a blank one if we don't have any...
                    foreach (var profile in Directory.EnumerateFiles(Config.Default.ConfigDirectory, "*" + Config.Default.ProfileExtension))
                    {
                        try
                        {
                            ServerManager.Instance.AddFromPath(profile);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(String.Format("The profile at {0} failed to load.  The error was: {1}\r\n{2}", profile, ex.Message, ex.StackTrace), "Profile failed to load", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                    }

                    Tabs.SelectedIndex = 0;
                }).DoNotWait();

            this.versionChecker.PostAction(CheckForUpdates).DoNotWait();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            this.versionChecker.DisposeAsync().DoNotWait();
        }

        private async Task CheckForUpdates()
        {
            var newVersion = await NetworkUtils.GetLatestASMVersion();
            TaskUtils.RunOnUIThreadAsync(() =>
            {
                try
                {
                    var appVersion = new Version();
                    Version.TryParse(App.Version, out appVersion);

                    this.LatestASMVersion = newVersion;
                    this.NewASMAvailable = appVersion < newVersion;
                }
                catch (Exception)
                {
                    // Ignore.
                }
            }).DoNotWait();

            await Task.Delay(Config.Default.UpdateCheckTime * 60 * 1000);
            this.versionChecker.PostAction(CheckForUpdates).DoNotWait();
        }
        public void Settings_Click(object sender, RoutedEventArgs args)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }

        public void Help_Click(object sender, RoutedEventArgs args)
        {
        }

        public void Servers_Remove(object sender, TabItemCloseEventArgs args)
        {
            args.Canceled = true;
            var server = ServerManager.Instance.Servers[args.ItemIndex];
            var result = MessageBox.Show("Are you sure you want to delete this profile?\r\n\r\nNOE: This will only delete the profile, not the installation directory, save games or settings files contained therein.", String.Format("Delete {0}?", server.Profile.ProfileName), MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if(result == MessageBoxResult.Yes)
            {
                ServerManager.Instance.Remove(server, deleteProfile: true);
                args.Canceled = false;
            }
        }

        private  async void RefreshPublicIP_Click(object sender, RoutedEventArgs e)
        {
            await App.DiscoverMachinePublicIP(forceOverride: true);
        }

        public void Servers_AddNew(object sender, NewItemRequestedEventArgs e)
        {
            var index = this.ServerManager.AddNew();
            ((EO.Wpf.TabControl)e.Source).SelectedIndex = index;
        }

        private void Donate_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Ark Server Manager is an open-source project, provided completely free of charge.  You can still donate if you would like; however you are under no obligation.  If you wish to donate, your browser will open to PayPal's website where you may donate as little or as much as you like.  Would you like to donate now?", "Make a donation?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var process = Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=cliff%2es%2ehudson%40gmail%2ecom&lc=US&item_name=Ark%20Server%20Manager&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donate_SM%2egif%3aNonHosted");
            }
        }

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteFile(string name);

        private bool Unblock(string fileName)
        {
            return DeleteFile(fileName + ":Zone.Identifier");
        }

        private void Upgrade_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show($"Version {this.LatestASMVersion} is now available.  To upgrade the application must close.  Close and upgrade now?", "Upgrade available", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if(result == MessageBoxResult.Yes)
            {
                try
                {
                    var applicationZip = Path.Combine(Path.GetTempPath(), "ASMLatest.zip");
                    var extractPath = Path.Combine(Path.GetTempPath(), "ASMLatest");
                    var updateFilePath = Path.Combine(Path.GetTempPath(), "ASMUpdate.cmd");
                    var currentInstallPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var backupPath = currentInstallPath + "_bak";
                    
                    // Grab the latest bits
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(Config.Default.ASMDownloadUrl, applicationZip);
                        Unblock(applicationZip);
                    }

                    // Extract them
                    try { Directory.Delete(extractPath, true); } catch { }
                    ZipFile.ExtractToDirectory(applicationZip, extractPath);

                    // Replace the current installation
                    File.WriteAllText(updateFilePath, $"timeout 2 \r\nrmdir /s /q \"{backupPath}\" \r\n rename \"{currentInstallPath}\" \"{Path.GetFileName(backupPath)}\"  \r\nxcopy /e /y \"{extractPath}\" \"{currentInstallPath}\\\" \r\nrmdir /s /q \"{backupPath}\"\r\nstart \"\" \"{Assembly.GetExecutingAssembly().Location}\" \r\nexit");
                    var startInfo = new ProcessStartInfo()
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/K \"{updateFilePath}\"",
                        WorkingDirectory = Path.GetTempPath()
                    };

                    Process.Start(startInfo);
                    Application.Current.Shutdown(0);
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"Unable to run the update process.  Please report this!\nException {ex.Message}: {ex.StackTrace}", "Update failed!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
