using ARK_Server_Manager.Lib;
using EO.Wpf;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using WPFSharp.Globalizer;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GlobalizedApplication _globalizedApplication = GlobalizedApplication.Instance;

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
            WindowUtils.RemoveDefaultResourceDictionary(this);

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
                            MessageBox.Show(String.Format(_globalizedApplication.GetResourceString("MainWindow_ProfileLoad_FailedLabel"), profile, ex.Message, ex.StackTrace), _globalizedApplication.GetResourceString("MainWindow_ProfileLoad_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                    }

                    Tabs.SelectedIndex = 0;
                }).DoNotWait();

            this.versionChecker.PostAction(CheckForUpdates).DoNotWait();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            RCONWindow.CloseAllWindows();
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
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        public void Help_Click(object sender, RoutedEventArgs args)
        {
            Process.Start(Config.Default.HelpUrl);
        }

        public void Servers_Remove(object sender, TabItemCloseEventArgs args)
        {
            args.Canceled = true;
            var server = ServerManager.Instance.Servers[args.ItemIndex];
            var result = MessageBox.Show(_globalizedApplication.GetResourceString("MainWindow_ProfileDelete_Label"), String.Format(_globalizedApplication.GetResourceString("MainWindow_ProfileDelete_Title"), server.Profile.ProfileName), MessageBoxButton.YesNo, MessageBoxImage.Warning);
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
            var result = MessageBox.Show(_globalizedApplication.GetResourceString("MainWindow_Donate_Label"), _globalizedApplication.GetResourceString("MainWindow_Donate_Title"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var process = Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=cliff%2es%2ehudson%40gmail%2ecom&lc=US&item_name=Ark%20Server%20Manager&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donate_SM%2egif%3aNonHosted");
            }
        }

        private void RCON_Click(object sender, RoutedEventArgs e)
        {
            var window = new OpenRCON();
            window.Owner = this;
            window.ShowDialog();
        }

        private void Upgrade_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(String.Format(_globalizedApplication.GetResourceString("MainWindow_Upgrade_Label"), this.LatestASMVersion), _globalizedApplication.GetResourceString("MainWindow_Upgrade_Title"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if(result == MessageBoxResult.Yes)
            {
                try
                {
                    Updater.UpdateASM();
                }
                catch(Exception ex)
                {
                    MessageBox.Show(String.Format(_globalizedApplication.GetResourceString("MainWindow_Upgrade_FailedLabel"), ex.Message, ex.StackTrace), _globalizedApplication.GetResourceString("MainWindow_Upgrade_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ASMPatchNotes_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Config.Default.ASM_PatchNotesUrl);
        }
    }
}
