using ARK_Server_Manager.Lib;
using System;
using System.IO;
using System.Windows;
using WPFSharp.Globalizer;
using System.Reflection;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public SettingsWindow()
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (SecurityUtils.IsAdministrator())
            {
                // remove the old task schedule
                TaskSchedulerUtils.ScheduleCacheUpdater(Config.Default.AutoUpdate_CacheDir, null, 0);

                // check if the Auto Update has been enabled.
                if (Config.Default.AutoUpdate_EnableUpdate)
                {
                    // check if an update period has been set.
                    if (Config.Default.AutoUpdate_UpdatePeriod <= 0)
                    {
                        MessageBox.Show(_globalizer.GetResourceString(""), _globalizer.GetResourceString(""), MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    // check if the cache directory has been set and it exists.
                    if (string.IsNullOrWhiteSpace(Config.Default.AutoUpdate_CacheDir) || !Directory.Exists(Config.Default.AutoUpdate_CacheDir))
                    {
                        MessageBox.Show(_globalizer.GetResourceString("GlobalSettings_CacheDirectory_ErrorLabel"), _globalizer.GetResourceString("GlobalSettings_CacheDirectory_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                var command = Assembly.GetEntryAssembly().Location;
                if (!TaskSchedulerUtils.ScheduleAutoUpdate(command, Config.Default.AutoUpdate_EnableUpdate ? Config.Default.AutoUpdate_UpdatePeriod : 0))
                {
                    MessageBox.Show(_globalizer.GetResourceString("GlobalSettings_CacheTaskUpdate_ErrorLabel"), _globalizer.GetResourceString("GlobalSettings_CacheTaskUpdate_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    if (Config.Default.AutoUpdate_EnableUpdate && Config.Default.AutoUpdate_UpdatePeriod > 0)
                    {
                        MessageBox.Show(String.Format(_globalizer.GetResourceString("GlobalSettings_CacheUpdate_EnabledLabel"), Config.Default.AutoUpdate_UpdatePeriod), _globalizer.GetResourceString("GlobalSettings_CacheUpdate_EnabledTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(_globalizer.GetResourceString("GlobalSettings_CacheUpdate_DisabledLabel"), _globalizer.GetResourceString("GlobalSettings_CacheUpdate_DisabledTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }

            Config.Default.Save();
            base.OnClosed(e);
        }
    }
}
