﻿using ArkServerManager.Plugin.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ArkServerManager.Plugin.Discord.Windows
{
    /// <summary>
    /// Interaction logic for ConfigWindow.xaml
    /// </summary>
    public partial class ConfigWindow : Window
    {
        private static readonly DependencyProperty PluginConfigProperty = DependencyProperty.Register(nameof(PluginConfig), typeof(DiscordPluginConfig), typeof(ConfigWindow));
        public static readonly DependencyProperty LatestVersionProperty = DependencyProperty.Register(nameof(LatestVersion), typeof(Version), typeof(ConfigWindow), new PropertyMetadata(new Version()));
        public static readonly DependencyProperty NewVersionAvailableProperty = DependencyProperty.Register(nameof(NewVersionAvailable), typeof(bool), typeof(ConfigWindow), new PropertyMetadata(false));

        internal ConfigWindow(DiscordPlugin plugin, DiscordPluginConfig pluginConfig)
        {
            this.Plugin = plugin ?? new DiscordPlugin();
            this.PluginConfig = pluginConfig ?? new DiscordPluginConfig();

            InitializeComponent();

            if (plugin.BetaEnabled)
                Title = $"{Title} {ResourceUtils.GetResourceString(this.Resources, "Global_BetaModeLabel")}";

            this.DataContext = this;
        }

        private DiscordPlugin Plugin
        {
            get;
            set;
        }

        private DiscordPluginConfig PluginConfig
        {
            get { return GetValue(PluginConfigProperty) as DiscordPluginConfig; }
            set { SetValue(PluginConfigProperty, value); }
        }

        public Version LatestVersion
        {
            get { return (Version)GetValue(LatestVersionProperty); }
            set { SetValue(LatestVersionProperty, value); }
        }

        public bool NewVersionAvailable
        {
            get { return (bool)GetValue(NewVersionAvailableProperty); }
            set { SetValue(NewVersionAvailableProperty, value); }
        }

        private void ConfigWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DialogResult.HasValue && DialogResult.Value)
                return;

            if (PluginConfig.HasAnyChanges)
            {
                if (MessageBox.Show(ResourceUtils.GetResourceString(this.Resources, "ConfigWindow_CloseLabel"), ResourceUtils.GetResourceString(this.Resources, "ConfigWindow_CloseTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    e.Cancel = true;
            }
        }

        private void ConfigWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CheckLatestVersionAsync().DoNotWait();
        }

        private void DownloadPlugin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DownloadLatestVersion();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {nameof(DownloadPlugin_Click)}\r\n{ex.Message}");
                MessageBox.Show(ResourceUtils.GetResourceString(this.Resources, "ConfigWindow_DownloadErrorLabel"), ResourceUtils.GetResourceString(this.Resources, "ConfigWindow_DownloadErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddConfigProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var profile = new ConfigProfile();

                if (EditProfile(profile))
                    PluginConfig.ConfigProfiles.Add(profile);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {nameof(AddConfigProfile_Click)}\r\n{ex.Message}");
                MessageBox.Show(ResourceUtils.GetResourceString(this.Resources, "ConfigWindow_AddErrorLabel"), ResourceUtils.GetResourceString(this.Resources, "ConfigWindow_AddErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearConfigProfiles_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(ResourceUtils.GetResourceString(this.Resources, "ConfigWindow_ClearLabel"), ResourceUtils.GetResourceString(this.Resources, "ConfigWindow_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                if (PluginConfig.ConfigProfiles.Count == 0)
                    return;

                PluginConfig.ConfigProfiles.Clear();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {nameof(ClearConfigProfiles_Click)}\r\n{ex.Message}");
                MessageBox.Show(ResourceUtils.GetResourceString(this.Resources, "ConfigWindow_ClearErrorLabel"), ResourceUtils.GetResourceString(this.Resources, "ConfigWindow_ClearErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteConfigProfile_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(ResourceUtils.GetResourceString(this.Resources, "ConfigWindow_DeleteLabel"), ResourceUtils.GetResourceString(this.Resources, "ConfigWindow_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                var profile = ((ConfigProfile)((Button)e.Source).DataContext);
                PluginConfig.ConfigProfiles.Remove(profile);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {nameof(DeleteConfigProfile_Click)}\r\n{ex.Message}");
                MessageBox.Show(ResourceUtils.GetResourceString(this.Resources, "ConfigWindow_DeleteErrorLabel"), ResourceUtils.GetResourceString(this.Resources, "ConfigWindow_DeleteErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditConfigProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var profile = ((ConfigProfile)((Button)e.Source).DataContext);
                EditProfile(profile);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {nameof(EditConfigProfile_Click)}\r\n{ex.Message}");
                MessageBox.Show(ResourceUtils.GetResourceString(this.Resources, "ConfigWindow_EditErrorLabel"), ResourceUtils.GetResourceString(this.Resources, "ConfigWindow_EditErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveConfig();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {nameof(Save_Click)}\r\n{ex.Message}");
                MessageBox.Show(ResourceUtils.GetResourceString(this.Resources, "ConfigWindow_SaveErrorLabel"), ResourceUtils.GetResourceString(this.Resources, "ConfigWindow_SaveErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CheckLatestVersionAsync()
        {
            try
            {
                var newVersion = await NetworkUtils.CheckLatestVersionAsync(Plugin.BetaEnabled);

                this.LatestVersion = newVersion;
                this.NewVersionAvailable = Plugin.PluginVersion < newVersion;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {nameof(CheckLatestVersionAsync)}\r\n{ex.Message}");
            }
        }

        private void DownloadLatestVersion()
        {
            var cursor = this.Cursor;

            try
            {
                this.Cursor = Cursors.Wait;
                Task.Delay(500).Wait();

                var latestZip = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), Config.Default.PluginZipFilename);

                var sourceUrl = string.Empty;
                if (Plugin.BetaEnabled)
                    sourceUrl = Config.Default.LatestBetaDownloadUrl;
                else
                    sourceUrl = Config.Default.LatestDownloadUrl;

                NetworkUtils.DownloadLatestVersion(sourceUrl, latestZip);

                MessageBox.Show(ResourceUtils.GetResourceString(this.Resources, "ConfigWindow_DownloadSuccessLabel"), ResourceUtils.GetResourceString(this.Resources, "ConfigWindow_DownloadSuccessTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {nameof(DownloadLatestVersion)}\r\n{ex.Message}");
            }
            finally
            {
                this.Cursor = cursor;
            }
        }

        private bool EditProfile(ConfigProfile profile)
        {
            if (profile == null)
                return false;

            var window = new ConfigProfileWindow(Plugin, profile);
            window.Owner = this;

            var dialogResult = window.ShowDialog();
            this.BringIntoView();

            return dialogResult.HasValue && dialogResult.Value;
        }

        private void SaveConfig()
        {
            var configFile = Path.Combine(PluginHelper.PluginFolder, DiscordPluginConfig.CONFIG_FILENAME);
            JsonUtils.SerializeToFile(PluginConfig, configFile);
            PluginConfig?.CommitChanges();
        }
    }
}
