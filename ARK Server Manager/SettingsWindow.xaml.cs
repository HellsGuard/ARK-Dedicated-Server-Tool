using ARK_Server_Manager.Lib;
using System;
using System.IO;
using System.Windows;
using WPFSharp.Globalizer;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private GlobalizedApplication _globalizedApplication = GlobalizedApplication.Instance;

        public SettingsWindow()
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (SecurityUtils.IsAdministrator())
            {
                if (Config.Default.ServerCacheUpdatePeriod != 0 && !Directory.Exists(Config.Default.ServerCacheDir))
                {
                    MessageBox.Show(_globalizedApplication.GetResourceString("GlobalSettings_CacheDirectory_ErrorLabel"), _globalizedApplication.GetResourceString("GlobalSettings_CacheDirectory_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!ServerScheduler.ScheduleCacheUpdater(Config.Default.ServerCacheDir, Updater.GetSteamCMDPath(), Config.Default.GLOBAL_EnableServerCache ? Config.Default.ServerCacheUpdatePeriod : 0))
                {
                    MessageBox.Show(_globalizedApplication.GetResourceString("GlobalSettings_CacheTaskUpdate_ErrorLabel"), _globalizedApplication.GetResourceString("GlobalSettings_CacheTaskUpdate_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    if (!Config.Default.GLOBAL_EnableServerCache || (Config.Default.ServerCacheUpdatePeriod == 0))
                    {
                        MessageBox.Show(_globalizedApplication.GetResourceString("GlobalSettings_CacheUpdate_DisabledLabel"), _globalizedApplication.GetResourceString("GlobalSettings_CacheUpdate_DisabledTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(String.Format(_globalizedApplication.GetResourceString("GlobalSettings_CacheUpdate_EnabledLabel"), Config.Default.ServerCacheUpdatePeriod), _globalizedApplication.GetResourceString("GlobalSettings_CacheUpdate_EnabledTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }

            Config.Default.Save();
            base.OnClosed(e);
        }
    }
}
