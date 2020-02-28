﻿using System;
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
    /// Interaction logic for WorkshopFilesWindow.xaml
    /// </summary>
    public partial class WorkshopFilesWindow : Window
    {
        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;
        private readonly ServerProfile _profile = null;
        private ModDetailList _modDetails = null;
        private readonly bool _isSotF = false;

        private readonly ModDetailsWindow _window = null;

        public static readonly DependencyProperty WorkshopFilesProperty = DependencyProperty.Register(nameof(WorkshopFiles), typeof(WorkshopFileList), typeof(WorkshopFilesWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty WorkshopFilesViewProperty = DependencyProperty.Register(nameof(WorkshopFilesView), typeof(ICollectionView), typeof(WorkshopFilesWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty WorkshopFilterStringProperty = DependencyProperty.Register(nameof(WorkshopFilterString), typeof(string), typeof(WorkshopFilesWindow), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty WorkshopFilterExistingProperty = DependencyProperty.Register(nameof(WorkshopFilterExisting), typeof(bool), typeof(WorkshopFilesWindow), new PropertyMetadata(false));

        public WorkshopFilesWindow(ModDetailList modDetails, ServerProfile profile)
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this);

            _profile = profile;
            _isSotF = _profile?.SOTF_Enabled ?? false;
            this.Title = string.Format(_globalizer.GetResourceString("WorkshopFiles_ProfileTitle"), _profile?.ProfileName);

            UpdateModDetailsList(modDetails);

            this.DataContext = this;
        }

        public WorkshopFilesWindow(ModDetailsWindow window, ServerProfile profile)
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this);

            _window = window;
            _profile = profile;
            _isSotF = _profile?.SOTF_Enabled ?? false;
            this.Title = string.Format(_globalizer.GetResourceString("WorkshopFiles_ProfileTitle"), _profile?.ProfileName);

            UpdateModDetailsList(window?.ModDetails);

            this.DataContext = this;
        }

        public WorkshopFileList WorkshopFiles
        {
            get { return GetValue(WorkshopFilesProperty) as WorkshopFileList; }
            set
            {
                SetValue(WorkshopFilesProperty, value);

                WorkshopFilesView = CollectionViewSource.GetDefaultView(WorkshopFiles);
                WorkshopFilesView.Filter = new Predicate<object>(Filter);
            }
        }

        public ICollectionView WorkshopFilesView
        {
            get { return GetValue(WorkshopFilesViewProperty) as ICollectionView; }
            set { SetValue(WorkshopFilesViewProperty, value); }
        }

        public string WorkshopFilterString
        {
            get { return (string)GetValue(WorkshopFilterStringProperty); }
            set { SetValue(WorkshopFilterStringProperty, value); }
        }

        public bool WorkshopFilterExisting
        {
            get { return (bool)GetValue(WorkshopFilterExistingProperty); }
            set { SetValue(WorkshopFilterExistingProperty, value); }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadWorkshopItems(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("WorkshopFiles_Load_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Filter_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            WorkshopFilesView?.Refresh();
        }

        private void ModDetails_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            WorkshopFilesView?.Refresh();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var item = ((WorkshopFileItem)((Button)e.Source).DataContext);

            var mod = ModDetail.GetModDetail(item);

            var selectedIndex = _window?.SelectedRowIndex() ?? -1;
            if (selectedIndex >= 0)
                _modDetails.Insert(selectedIndex, mod);
            else
                _modDetails.Add(mod);
        }

        private async void Reload_Click(object sender, RoutedEventArgs e)
        {
            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = System.Windows.Input.Cursors.Wait);
                await Task.Delay(500);

                await LoadWorkshopItems(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("WorkshopFiles_Refresh_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        private void RequestNavigate_Click(object sender, RequestNavigateEventArgs e)
        {
            var item = ((WorkshopFileItem)((Hyperlink)e.Source).DataContext);

            Process.Start(new ProcessStartInfo(item.WorkshopUrl));
            e.Handled = true;
        }

        public bool Filter(object obj)
        {
            var data = obj as WorkshopFileItem;
            if (data == null)
                return false;

            if (WorkshopFilterExisting && _modDetails.Any(m => m.ModId.Equals(data.WorkshopId)))
                return false;

            var filterString = WorkshopFilterString.ToLower();

            if (string.IsNullOrWhiteSpace(filterString))
                return true;

            return data.WorkshopId.Contains(filterString) || data.TitleFilterString.Contains(filterString);
        }

        private async Task LoadWorkshopItems(bool loadFromCacheFile)
        {
            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = System.Windows.Input.Cursors.Wait);
                await Task.Delay(500);

                WorkshopFileDetailResponse localCache = null;
                WorkshopFileDetailResponse steamCache = null;

                await Task.Run( () => {
                    var file = Path.Combine(Config.Default.DataDir, _isSotF ? Config.Default.WorkshopCacheFile_SotF : Config.Default.WorkshopCacheFile);

                    // try to load the cache file.
                    localCache = WorkshopFileDetailResponse.Load(file);

                    if (loadFromCacheFile)
                    {
                        steamCache = localCache;

                        // check if the cache is old
                        if (localCache != null && localCache.cached.AddHours(Config.Default.WorkshopCache_ExpiredHours) < DateTime.UtcNow)
                            // cache is considered old, clear cache variable so it will reload from internet
                            steamCache = null;
                    }

                    // check if the cache exists
                    if (steamCache == null)
                    {
                        steamCache = SteamUtils.GetSteamModDetails(_isSotF ? Config.Default.AppId_SotF : Config.Default.AppId);
                        if (steamCache != null)
                            steamCache.Save(file);
                        else
                        {
                            MessageBox.Show(_globalizer.GetResourceString("WorkshopFiles_Refresh_FailedLabel"), _globalizer.GetResourceString("WorkshopFiles_Refresh_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                            steamCache = localCache;
                        }
                    }
                });

                WorkshopFiles = WorkshopFileList.GetList(steamCache);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        public void UpdateModDetailsList(ModDetailList modDetails)
        {
            if (_modDetails != null)
                _modDetails.CollectionChanged -= ModDetails_CollectionChanged;

            _modDetails = modDetails ?? new ModDetailList();
            if (_modDetails != null)
                _modDetails.CollectionChanged += ModDetails_CollectionChanged;

            WorkshopFilesView?.Refresh();
        }
    }
}
