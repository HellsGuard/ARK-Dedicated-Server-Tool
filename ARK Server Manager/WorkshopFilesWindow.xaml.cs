using System;
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
        private GlobalizedApplication _globalizer = GlobalizedApplication.Instance;
        private ModDetailList _modDetails = null;

        public static readonly DependencyProperty WorkshopFilesProperty = DependencyProperty.Register(nameof(WorkshopFiles), typeof(WorkshopFileList), typeof(WorkshopFilesWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty WorkshopFilesViewProperty = DependencyProperty.Register(nameof(WorkshopFilesView), typeof(ICollectionView), typeof(WorkshopFilesWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty WorkshopFilterStringProperty = DependencyProperty.Register(nameof(WorkshopFilterString), typeof(string), typeof(WorkshopFilesWindow), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty WorkshopFilterExistingProperty = DependencyProperty.Register(nameof(WorkshopFilterExisting), typeof(bool), typeof(WorkshopFilesWindow), new PropertyMetadata(false));
        public static readonly DependencyProperty SelectedCountProperty = DependencyProperty.Register(nameof(SelectedCount), typeof(int), typeof(WorkshopFilesWindow), new PropertyMetadata(0));

        public WorkshopFilesWindow(ModDetailList modDetails)
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this);

            UpdateModDetailsList(modDetails);

            this.DataContext = this;
        }

        public WorkshopFileList WorkshopFiles
        {
            get { return GetValue(WorkshopFilesProperty) as WorkshopFileList; }
            set
            {
                SetValue(WorkshopFilesProperty, value);

                SelectedCount = WorkshopFiles?.SelectedCount ?? 0;
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

        public int SelectedCount
        {
            get { return (int)GetValue(SelectedCountProperty); }
            set { SetValue(SelectedCountProperty, value); }
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

        private void DataGrid_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            SelectedCount = WorkshopFiles?.SelectedCount ?? 0;
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
            _modDetails.Add(mod);
        }

        private void AddSelected_Click(object sender, RoutedEventArgs e)
        {
            var selected = WorkshopFiles.GetSelectedItems();

            var mods = selected.Select(i => ModDetail.GetModDetail(i)).ToArray();
            _modDetails.AddRange(mods);
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

        private void Unselect_Click(object sender, RoutedEventArgs e)
        {
            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = System.Windows.Input.Cursors.Wait);
                Task.Delay(500).Wait();

                foreach (var item in WorkshopFiles)
                {
                    item.Selected = false;
                }
                SelectedCount = WorkshopFiles?.SelectedCount ?? 0;
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

                WorkshopFileDetailResponse cache = null;

                await Task.Run( () => {
                    var file = Path.Combine(Config.Default.DataDir, Config.Default.WorkshopCacheFile);

                    if (loadFromCacheFile)
                        // try to load the cache file.
                        cache = WorkshopFileDetailResponse.Load(file);

                    // check if the cache exists
                    if (cache == null)
                    {
                        cache = ModUtils.GetSteamModDetails();
                        if (cache != null)
                            cache.Save(file);
                    }
                });

                WorkshopFiles = WorkshopFileList.GetList(cache);
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
