using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using WPFSharp.Globalizer;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : GlobalizedApplication
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initial configuration setting
            if(String.IsNullOrWhiteSpace(Config.Default.DataDir))
            {
                Config.Default.DataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Config.Default.DefaultDataDir);
                Config.Default.ConfigDirectory = Path.Combine(Config.Default.DataDir, Config.Default.ProfilesDir);
                System.IO.Directory.CreateDirectory(Config.Default.ConfigDirectory);
            }
        }
    }
}
