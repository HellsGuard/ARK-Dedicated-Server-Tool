using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using ARK_Server_Manager.Lib;
using ARK_Server_Manager.Lib.Model;
using WPFSharp.Globalizer;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for ModDetailsWindow.xaml
    /// </summary>
    public partial class ModDetailsWindow : Window
    {
        private GlobalizedApplication _globalizer = GlobalizedApplication.Instance;
        private ServerProfile _profile = null;

        public static readonly DependencyProperty ResponseProperty = DependencyProperty.Register(nameof(Response), typeof(PublishedFileDetailsResponse), typeof(ModDetailsWindow), new PropertyMetadata(null));

        public ModDetailsWindow(ServerProfile profile)
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this);

            _profile = profile;
            this.Title = string.Format(_globalizer.GetResourceString("ModDetails_ProfileTitle"), _profile?.ProfileName);

            this.DataContext = this;
        }

        public PublishedFileDetailsResponse Response
        {
            get { return GetValue(ResponseProperty) as PublishedFileDetailsResponse; }
            set { SetValue(ResponseProperty, value); }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await PopulateModDetails();
        }

        private void Mod_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private async Task PopulateModDetails()
        {
            if (_profile == null)
            {
                this.ClearValue(ResponseProperty);
                return;
            }

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = System.Windows.Input.Cursors.Wait);
                await Task.Delay(1000);

                // build a list of mods to be processed
                var modIdList = new List<string>();

                var serverMapModId = ModUtils.GetMapModId(_profile.ServerMap);
                if (!string.IsNullOrWhiteSpace(serverMapModId))
                    modIdList.Add(serverMapModId);

                if (!string.IsNullOrWhiteSpace(_profile.TotalConversionModId))
                    modIdList.Add(_profile.TotalConversionModId);

                modIdList.AddRange(ModUtils.GetModIdList(_profile.ServerModIds));

                // remove all duplicate mod ids.
                modIdList = modIdList.Distinct().ToList();

                // get the details of the mods to be processed.
                Response = ModUtils.GetSteamModDetails(modIdList);

                Response.PopulateExtended(Path.Combine(_profile.InstallDirectory, Config.Default.ServerModsRelativePath));
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(_globalizer.GetResourceString("ModDetails_ModDetailsLoad_FailedLabel"), ex.Message), _globalizer.GetResourceString("ModDetails_ModDetailsLoad_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                this.ClearValue(ResponseProperty);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = null);
            }
        }
    }
}
