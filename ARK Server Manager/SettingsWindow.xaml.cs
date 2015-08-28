using ARK_Server_Manager.Lib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (SecurityUtils.IsAdministrator())
            {
                if (Config.Default.ServerCacheUpdatePeriod != 0 && !Directory.Exists(Config.Default.ServerCacheDir))
                {
                    MessageBox.Show("The cache directory must specify a valid location or the cache update period must be 0.", "Invalid cache directory", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!ServerScheduler.ScheduleCacheUpdater(Config.Default.ServerCacheDir, Updater.GetSteamCMDPath(), Config.Default.GLOBAL_EnableServerCache ? Config.Default.ServerCacheUpdatePeriod : 0))
                {
                    MessageBox.Show("Failed to update the cache task.  Ensure you have administrative rights and try again.", "Update task failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    if (!Config.Default.GLOBAL_EnableServerCache || (Config.Default.ServerCacheUpdatePeriod == 0))
                    {
                        MessageBox.Show("Server cache updating disabled.", "Updates disabled", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Server cache will update every {Config.Default.ServerCacheUpdatePeriod} minutes.", "Updates enabled", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }

            Config.Default.Save();
            base.OnClosed(e);
        }
    }
}
