using ArkServerManager.Plugin.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace ArkServerManager.Plugin.Discord.Windows
{
    /// <summary>
    /// Interaction logic for ConfigWindow.xaml
    /// </summary>
    public partial class ConfigWindow : Window
    {
        private static readonly DependencyProperty ConfigProperty = DependencyProperty.Register(nameof(Config), typeof(DiscordPluginConfig), typeof(ConfigWindow));

        internal ConfigWindow(DiscordPluginConfig config)
        {
            this.Config = config;

            InitializeComponent();

            this.DataContext = this;
        }

        private DiscordPluginConfig Config
        {
            get { return GetValue(ConfigProperty) as DiscordPluginConfig; }
            set { SetValue(ConfigProperty, value); }
        }

        private void ConfigWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DialogResult.HasValue && DialogResult.Value)
                return;

            if (Config.HasAnyChanges)
            {
                if (MessageBox.Show(ResourceUtils.GetResourceString(this.Resources, "ConfigWindow_CloseLabel"), ResourceUtils.GetResourceString(this.Resources, "ConfigWindow_CloseTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    e.Cancel = true;
            }
        }

        private void AddConfigProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var profile = new ConfigProfile();

                if (EditProfile(profile))
                    Config.ConfigProfiles.Add(profile);
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
                if (Config.ConfigProfiles.Count == 0)
                    return;

                Config.ConfigProfiles.Clear();
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
                Config.ConfigProfiles.Remove(profile);
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

        private bool EditProfile(ConfigProfile profile)
        {
            if (profile == null)
                return false;

            var window = new ConfigProfileWindow(profile);
            window.Owner = this;

            var dialogResult = window.ShowDialog();
            this.BringIntoView();

            return dialogResult.HasValue && dialogResult.Value;
        }

        private void SaveConfig()
        {
            var configFile = Path.Combine(PluginHelper.PluginFolder, DiscordPluginConfig.CONFIG_FILENAME);
            JsonUtils.SerializeToFile(Config, configFile);
            Config?.CommitChanges();
        }
    }
}
