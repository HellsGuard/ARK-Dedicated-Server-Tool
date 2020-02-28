﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ARK_Server_Manager.Lib;
using WPFSharp.Globalizer;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for WorldSaveRestoreWindow.xaml
    /// </summary>
    public partial class WorldSaveRestoreWindow : Window
    {
        public class WorldSaveFileList : SortableObservableCollection<WorldSaveFile>  
        {
            public new void Add(WorldSaveFile item)
            {
                if (item == null || this.Any(m => m.FileName.Equals(item.FileName)))
                    return;

                base.Add(item);
            }

            public override string ToString()
            {
                return $"{nameof(WorldSaveFile)} - {Count}";
            }
        }

        public class WorldSaveFile : DependencyObject 
        {
            public static readonly DependencyProperty CreatedDateProperty = DependencyProperty.Register(nameof(CreatedDate), typeof(DateTime), typeof(WorldSaveFile), new PropertyMetadata(DateTime.MinValue));
            public static readonly DependencyProperty FileProperty = DependencyProperty.Register(nameof(File), typeof(string), typeof(WorldSaveFile), new PropertyMetadata(string.Empty));
            public static readonly DependencyProperty FileNameProperty = DependencyProperty.Register(nameof(FileName), typeof(string), typeof(WorldSaveFile), new PropertyMetadata(string.Empty));
            public static readonly DependencyProperty UpdatedDateProperty = DependencyProperty.Register(nameof(UpdatedDate), typeof(DateTime), typeof(WorldSaveFile), new PropertyMetadata(DateTime.MinValue));
            public static readonly DependencyProperty IsActiveFileProperty = DependencyProperty.Register(nameof(IsActiveFile), typeof(bool), typeof(WorldSaveFile), new PropertyMetadata(false));
            public static readonly DependencyProperty IsArchiveFileProperty = DependencyProperty.Register(nameof(IsArchiveFile), typeof(bool), typeof(WorldSaveFile), new PropertyMetadata(false));

            public DateTime CreatedDate
            {
                get { return (DateTime)GetValue(CreatedDateProperty); }
                set { SetValue(CreatedDateProperty, value); }
            }

            public string File
            {
                get { return (string)GetValue(FileProperty); }
                set { SetValue(FileProperty, value); }
            }

            public string FileName
            {
                get { return (string)GetValue(FileNameProperty); }
                set { SetValue(FileNameProperty, value); }
            }

            public DateTime UpdatedDate
            {
                get { return (DateTime)GetValue(UpdatedDateProperty); }
                set { SetValue(UpdatedDateProperty, value); }
            }

            public bool IsActiveFile
            {
                get { return (bool)GetValue(IsActiveFileProperty); }
                set { SetValue(IsActiveFileProperty, value); }
            }

            public bool IsArchiveFile
            {
                get { return (bool)GetValue(IsArchiveFileProperty); }
                set { SetValue(IsArchiveFileProperty, value); }
            }

            public override string ToString()
            {
                return FileName;
            }
        }

        public class WorldSaveFileComparer : IComparer<WorldSaveFile>
        {
            public int Compare(WorldSaveFile x, WorldSaveFile y)
            {
                if (x == null && y == null)
                    return 0;
                if (x == null)
                    return 1;
                if (y == null)
                    return -1;

                if (x.IsActiveFile && y.IsActiveFile)
                {
                    if (x.UpdatedDate == y.UpdatedDate)
                        return 0;
                    return x.UpdatedDate < y.UpdatedDate ? 1 : -1;
                }
                if (x.IsActiveFile)
                    return -1;
                if (y.IsActiveFile)
                    return 1;

                if (x.UpdatedDate == y.UpdatedDate)
                    return 0;
                return x.UpdatedDate < y.UpdatedDate ? 1 : -1;
            }
        }

        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;
        private readonly ServerProfile _profile = null;

        public static readonly DependencyProperty WorldSaveFilesProperty = DependencyProperty.Register(nameof(WorldSaveFiles), typeof(WorldSaveFileList), typeof(WorldSaveRestoreWindow), new PropertyMetadata(null));

        public WorldSaveRestoreWindow(ServerProfile profile)
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this);

            _profile = profile;
            this.Title = string.Format(_globalizer.GetResourceString("WorldSaveRestore_ProfileTitle"), _profile?.ProfileName);

            WorldSaveFiles = new WorldSaveFileList();

            this.DataContext = this;
        }

        public WorldSaveFileList WorldSaveFiles
        {
            get { return GetValue(WorldSaveFilesProperty) as WorldSaveFileList; }
            set { SetValue(WorldSaveFilesProperty, value); }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadWorldSaveFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("WorldSaveRestore_Load_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            var item = ((WorldSaveFile)((Button)e.Source).DataContext);
            if (item == null)
                return;

            var message = $"You are about to delete backup file\r\n{item.FileName}.\r\n\r\nDo you want to continue?";
            if (MessageBox.Show(this, message, "Delete Backup Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = System.Windows.Input.Cursors.Wait);
                await Task.Delay(500);

                File.Delete(item.File);

                MessageBox.Show(this, "The backup file has been deleted.", "Delete Backup Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Delete Backup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                await LoadWorldSaveFiles();
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        private async void Reload_Click(object sender, RoutedEventArgs e)
        {
            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = System.Windows.Input.Cursors.Wait);
                await Task.Delay(500);

                await LoadWorldSaveFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("WorldSaveRestore_Refresh_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        private async void Restore_Click(object sender, RoutedEventArgs e)
        {
            var item = ((WorldSaveFile)((Button)e.Source).DataContext);
            if (item == null)
                return;

            var restoreAll = true;
            if (item.IsArchiveFile)
            {
                var message = $"You are about to restore backup file\r\n{item.FileName}.\r\n\r\nSelect Yes to restore the world save, player and tribe files.\r\nSelect No to only restore the world save.\r\nSelect Cancel to quit the restore.";
                var result = MessageBox.Show(this, message, "Restore Backup Confirmation", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Cancel)
                    return;
                restoreAll = result == MessageBoxResult.Yes;
            }
            else
            {
                var message = $"You are about to restore backup file\r\n{item.FileName}.\r\n\r\nDo you want to continue?";
                if (MessageBox.Show(this, message, "Restore Backup Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;
            }

            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = System.Windows.Input.Cursors.Wait);
                await Task.Delay(500);

                var restoredFileCount = _profile.RestoreSaveFiles(item.File, item.IsArchiveFile, restoreAll);

                MessageBox.Show(this, $"The backup file has been restored, {restoredFileCount} file(s) restored.", "Restore Backup Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Restore Backup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                await LoadWorldSaveFiles();
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }

        private async Task LoadWorldSaveFiles()
        {
            var cursor = this.Cursor;

            try
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = System.Windows.Input.Cursors.Wait);
                await Task.Delay(500);

                WorldSaveFiles.Clear();

                var saveFolder = ServerProfile.GetProfileSavePath(_profile);
                if (!Directory.Exists(saveFolder))
                    return;

                var saveFolderInfo = new DirectoryInfo(saveFolder);
                var mapName = ServerProfile.GetProfileMapFileName(_profile);
                var mapFileName = $"{mapName}{Config.Default.MapExtension}";
                var searchPattern = $"{mapName}*{Config.Default.MapExtension}";

                var saveFiles = saveFolderInfo.GetFiles(searchPattern);
                foreach (var file in saveFiles)
                {
                    WorldSaveFiles.Add(new WorldSaveFile { File = file.FullName , FileName = file.Name, CreatedDate = file.CreationTime, UpdatedDate = file.LastWriteTime, IsArchiveFile = false, IsActiveFile = file.Name.Equals(mapFileName, StringComparison.OrdinalIgnoreCase) });
                }

                var backupFolder = ServerApp.GetServerBackupFolder(_profile.ProfileName);
                if (Directory.Exists(backupFolder))
                {
                    var backupFolderInfo = new DirectoryInfo(backupFolder);
                    searchPattern = $"{mapName}*{Config.Default.BackupExtension}";

                    var backupFiles = backupFolderInfo.GetFiles(searchPattern);
                    foreach (var file in backupFiles)
                    {
                        WorldSaveFiles.Add(new WorldSaveFile { File = file.FullName, FileName = file.Name, CreatedDate = file.CreationTime, UpdatedDate = file.LastWriteTime, IsArchiveFile = true, IsActiveFile = false });
                    }
                }

                WorldSaveFiles.Sort(f => f, new WorldSaveFileComparer());
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => this.Cursor = cursor);
            }
        }
    }
}
