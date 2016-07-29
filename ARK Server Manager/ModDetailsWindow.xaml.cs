using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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

        private WorkshopFilesWindow _workshopFilesWindow = null;

        public static readonly DependencyProperty ModDetailsProperty = DependencyProperty.Register(nameof(ModDetails), typeof(ModDetailList), typeof(ModDetailsWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty ModDetailsChangedProperty = DependencyProperty.Register(nameof(ModDetailsChanged), typeof(bool), typeof(ModDetailsWindow), new PropertyMetadata(false));
        public static readonly DependencyProperty ModDetailsViewProperty = DependencyProperty.Register(nameof(ModDetailsView), typeof(ICollectionView), typeof(ModDetailsWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty ModDetailsFilterStringProperty = DependencyProperty.Register(nameof(ModDetailsFilterString), typeof(string), typeof(ModDetailsWindow), new PropertyMetadata(string.Empty));

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
            set
            {
                SetValue(ModDetailsProperty, value);

                ModDetailsView = CollectionViewSource.GetDefaultView(ModDetails);
                ModDetailsView.Filter = new Predicate<object>(Filter);

                if (_workshopFilesWindow != null)
                    _workshopFilesWindow.UpdateModDetailsList(value);
            }
        }

        public bool ModDetailsChanged
        {
            get { return (bool)GetValue(ModDetailsChangedProperty); }
            set { SetValue(ModDetailsChangedProperty, value); }
        }

        public ICollectionView ModDetailsView
        {
            get { return GetValue(ModDetailsViewProperty) as ICollectionView; }
            set { SetValue(ModDetailsViewProperty, value); }
        }

        public string ModDetailsFilterString
        {
            get { return (string)GetValue(ModDetailsFilterStringProperty); }
            set { SetValue(ModDetailsFilterStringProperty, value); }
        }


        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadModsFromProfile();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ModDetails_Load_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (ModDetailsChanged)
            {
                var result = MessageBox.Show(_globalizer.GetResourceString("ModDetails_Unsaved_Label"), _globalizer.GetResourceString("ModDetails_Unsaved_Title"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                    e.Cancel = true;
            }
        }

        private void Filter_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            ModDetailsView?.Refresh();
        }

        private void ModDetails_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ModDetailsView?.Refresh();
            ModDetailsChanged = true;
        }

        private void WorkshopFilesWindow_Closed(object sender, EventArgs e)
        {
            _workshopFilesWindow = null;
            this.Activate();
        }

        private void Mod_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var mod = ((ModDetail)((Hyperlink)e.Source).DataContext);

            Process.Start(new ProcessStartInfo(mod.ModUrl));
            e.Handled = true;
        }

        private void AddMods_Click(object sender, RoutedEventArgs e)
        {
            if (_workshopFilesWindow != null)
                return;

            _workshopFilesWindow = new WorkshopFilesWindow(ModDetails);
            _workshopFilesWindow.Owner = this;
            _workshopFilesWindow.Closed += WorkshopFilesWindow_Closed;
            _workshopFilesWindow.Show();
        }

        private async void LoadMods_Click(object sender, RoutedEventArgs e)
        {
            if (_profile == null)
                return;

            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = System.Windows.Input.Cursors.Wait);
                await Task.Delay(500);

                await LoadModsFromServerFolder();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ModDetails_Load_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
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
            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = System.Windows.Input.Cursors.Wait);
                await Task.Delay(500);

                await LoadModsFromList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ModDetails_Refresh_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        private async void ReloadMods_Click(object sender, RoutedEventArgs e)
        {
            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = System.Windows.Input.Cursors.Wait);
                await Task.Delay(500);

                await LoadModsFromProfile();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ModDetails_Reload_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        private void RemoveMod_Click(object sender, RoutedEventArgs e)
        {
            var mod = ((ModDetail)((Button)e.Source).DataContext);
            ModDetails.Remove(mod);
        }

        private void RemoveAllMods_Click(object sender, RoutedEventArgs e)
        {
            ModDetails.Clear();
        }

        private async void SaveMods_Click(object sender, RoutedEventArgs e)
        {
            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = System.Windows.Input.Cursors.Wait);
                await Task.Delay(500);

                string mapString;
                string totalConversionString;
                string modIdString;

                // check if there are any unknown mod types.
                if (ModDetails.AnyUnknownModTypes)
                {
                    if (MessageBox.Show(_globalizer.GetResourceString("ModDetails_Save_UnknownLabel"), _globalizer.GetResourceString("ModDetails_Save_UnknownTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
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

                await LoadModsFromProfile();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("ModDetails_Save_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }


        public bool Filter(object obj)
        {
            var data = obj as ModDetail;
            if (data == null)
                return false;

            var filterString = ModDetailsFilterString.ToLower();

            if (string.IsNullOrWhiteSpace(filterString))
                return true;

            return data.ModId.Contains(filterString) || data.TitleFilterString.Contains(filterString);
        }

        private async Task<PublishedFileDetailsResponse> GetModDetails(List<string> modIdList)
        {
            if (modIdList == null || modIdList.Count == 0)
                return new PublishedFileDetailsResponse();

            try
            {
                var newModIdList = ModUtils.ValidateModList(modIdList);

                // get the details of the mods to be processed.
                return await Task.Run( () =>  ModUtils.GetSteamModDetails(newModIdList) );
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(_globalizer.GetResourceString("ModDetails_Load_FailedLabel"), ex.Message), _globalizer.GetResourceString("ModDetails_Load_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private async Task LoadModsFromList()
        {
            var modIdList = ModDetails.Select(m => m.ModId).ToList();

            var response = await GetModDetails(modIdList);
            var modDetails = ModDetailList.GetModDetails(response, Path.Combine(_profile.InstallDirectory, Config.Default.ServerModsRelativePath));
            UpdateModDetailsList(modDetails);

            ModDetailsChanged = true;
        }

        private async Task LoadModsFromProfile()
        {
            // build a list of mods to be processed
            var modIdList = new List<string>();

            var serverMapModId = ModUtils.GetMapModId(_profile.ServerMap);
            if (!string.IsNullOrWhiteSpace(serverMapModId))
                modIdList.Add(serverMapModId);

            if (!string.IsNullOrWhiteSpace(_profile.TotalConversionModId))
                modIdList.Add(_profile.TotalConversionModId);

            modIdList.AddRange(ModUtils.GetModIdList(_profile.ServerModIds));

            var response = await GetModDetails(modIdList);
            var modDetails = ModDetailList.GetModDetails(response, Path.Combine(_profile.InstallDirectory, Config.Default.ServerModsRelativePath));
            UpdateModDetailsList(modDetails);

            ModDetailsChanged = false;
        }

        private async Task LoadModsFromServerFolder()
        {
            // build a list of mods to be processed
            var modIdList = new List<string>();

            var directoryNames = Directory.GetDirectories(Path.Combine(_profile.InstallDirectory, Config.Default.ServerModsRelativePath));
            foreach (var directoryName in directoryNames)
            {
                var modFile = $"{directoryName}.mod";
                if (File.Exists(modFile))
                {
                    modIdList.Add(Path.GetFileNameWithoutExtension(modFile));
                }
            }

            var response = await GetModDetails(modIdList);
            var modDetails = ModDetailList.GetModDetails(response, Path.Combine(_profile.InstallDirectory, Config.Default.ServerModsRelativePath));
            UpdateModDetailsList(modDetails);

            ModDetailsChanged = true;
        }

        private void UpdateModDetailsList(ModDetailList modDetails)
        {
            if (ModDetails != null)
                ModDetails.CollectionChanged -= ModDetails_CollectionChanged;

            ModDetails = modDetails ?? new ModDetailList();
            if (ModDetails != null)
                ModDetails.CollectionChanged += ModDetails_CollectionChanged;

            ModDetailsView?.Refresh();
        }
    }
}
