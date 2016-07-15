using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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

        public static readonly DependencyProperty ModDetailsProperty = DependencyProperty.Register(nameof(ModDetails), typeof(ModDetailList), typeof(ModDetailsWindow), new PropertyMetadata(null));

        public ModDetailsWindow(ServerProfile profile)
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this);

            _profile = profile;
            this.Title = string.Format(_globalizer.GetResourceString("ModDetails_ProfileTitle"), _profile?.ProfileName);

            this.DataContext = this;
        }

        public ModDetailList ModDetails
        {
            get { return GetValue(ModDetailsProperty) as ModDetailList; }
            set { SetValue(ModDetailsProperty, value); }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var response = await GetModDetails();

                ModDetails = ModDetailList.GetModDetails(response, Path.Combine(_profile.InstallDirectory, Config.Default.ServerModsRelativePath));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Load Mod Details Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Mod_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var mod = ((ModDetail)((Hyperlink)e.Source).DataContext);

            Process.Start(new ProcessStartInfo(mod.ModUrl));
            e.Handled = true;
        }

        private void AddNewMod_Click(object sender, RoutedEventArgs e)
        {
            ModDetails.Add();
        }

        private void MoveModDown_Click(object sender, RoutedEventArgs e)
        {
            var mod = ((ModDetail)((Button)e.Source).DataContext);
            ModDetails.MoveDown(mod);
        }

        private void MoveModUp_Click(object sender, RoutedEventArgs e)
        {
            var mod = ((ModDetail)((Button)e.Source).DataContext);
            ModDetails.MoveUp(mod);
        }

        private async void RefreshMods_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var modIdList = ModDetails.Select(m => m.ModId).ToList();
                var response = await GetModDetails(modIdList);

                ModDetails = ModDetailList.GetModDetails(response, Path.Combine(_profile.InstallDirectory, Config.Default.ServerModsRelativePath));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Refresh Mod Details Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveMod_Click(object sender, RoutedEventArgs e)
        {
            var mod = ((ModDetail)((Button)e.Source).DataContext);
            ModDetails.Remove(mod);
        }

        private void SaveMods_Click(object sender, RoutedEventArgs e)
        {
            string mapString;
            string totalConversionString;
            string modIdString;

            // check if there are any unknown mod types.
            if (ModDetails.AnyUnknownModTypes)
            {
                if (MessageBox.Show("There are one or more unknown mod types, this could cause problems when the save is performed. Do you want to continue?", "Unknown Mod Types", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;
            }

            if (ModDetails.GetModStrings(out mapString, out totalConversionString, out modIdString))
            {
                if (mapString != null)
                    _profile.ServerMap = mapString;
                else if (_profile.ServerMap.Contains('/'))
                    _profile.ServerMap = string.Empty;

                _profile.TotalConversionModId = totalConversionString ?? string.Empty;
                _profile.ServerModIds = modIdString ?? string.Empty;
            }
        }

        private async Task<PublishedFileDetailsResponse> GetModDetails()
        {
            if (_profile == null)
                return null;

            var cursor = this.Cursor;

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

                return await GetModDetails(modIdList);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(_globalizer.GetResourceString("ModDetails_ModDetailsLoad_FailedLabel"), ex.Message), _globalizer.GetResourceString("ModDetails_ModDetailsLoad_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        private async Task<PublishedFileDetailsResponse> GetModDetails(List<string> modIdList)
        {
            if (modIdList == null || modIdList.Count == 0)
                return null;

            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = System.Windows.Input.Cursors.Wait);
                await Task.Delay(1000);

                // remove all duplicate mod ids.
                modIdList = modIdList.Distinct().ToList();

                // get the details of the mods to be processed.
                return ModUtils.GetSteamModDetails(modIdList);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(_globalizer.GetResourceString("ModDetails_ModDetailsLoad_FailedLabel"), ex.Message), _globalizer.GetResourceString("ModDetails_ModDetailsLoad_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }
    }
}
