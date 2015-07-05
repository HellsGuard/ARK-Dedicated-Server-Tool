using ARK_Server_Manager.Lib;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using WPFSharp.Globalizer;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : GlobalizedApplication
    {
        static public string Version
        {
            get;
            set;
        }

        static public App Instance
        {
            get;
            private set;
        }

        public static readonly ServerStatusWatcher ServerWatcher = new ServerStatusWatcher();

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += ErrorHandling.CurrentDomain_UnhandledException;
            App.Instance = this;
        }

        static App()
        {
            App.Version = App.GetDeployedVersion();
        }   

        protected override void OnStartup(StartupEventArgs e)
        {           
            //System.Configuration.ConfigurationSettings.AppSettings.
                        
            // Initial configuration setting
            if(String.IsNullOrWhiteSpace(Config.Default.DataDir))
            {
                MessageBox.Show("It appears you do not have a data directory set.  The data directory is where your profiles and SteamCMD will be stored.  It is not the same as the server installation directory, which you can choose for each profile.  You will now be asked to select the location where the Ark Server Manager data directory is located.  You may later change this in the Settings window.", "Select Data Directory", MessageBoxButton.OK, MessageBoxImage.Information);

                while (String.IsNullOrWhiteSpace(Config.Default.DataDir))
                {
                    var dialog = new CommonOpenFileDialog();
                    dialog.EnsureFileExists = true;
                    dialog.IsFolderPicker = true;
                    dialog.Multiselect = false;
                    dialog.Title = "Select a Data Directory";
                    dialog.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
                    var result = dialog.ShowDialog();
                    if (result == CommonFileDialogResult.Ok)
                    {
                        var confirm = MessageBox.Show(String.Format("Ark Server Manager will store Profiles, SteamCMD and Backups in the following directories:\r\n\r\nProfiles: {0}\r\n\nSteamCMD: {1}\r\n\nBackups: {2}\r\n\r\nIs this ok?", Path.Combine(dialog.FileName, Config.Default.ProfilesDir), Path.Combine(dialog.FileName, Config.Default.SteamCmdDir), Path.Combine(dialog.FileName, Config.Default.DefaultBackupDir)), "Confirm location", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                        if(confirm == MessageBoxResult.No)
                        {
                            continue;
                        }
                        else if(confirm == MessageBoxResult.Yes)
                        {
                            Config.Default.DataDir = dialog.FileName;
                            break;
                        }
                        else
                        {
                            Environment.Exit(0);
                        }
                    }
                }
            }

            Config.Default.ConfigDirectory = Path.Combine(Config.Default.DataDir, Config.Default.ProfilesDir);            
            System.IO.Directory.CreateDirectory(Config.Default.ConfigDirectory);

            if (String.IsNullOrWhiteSpace(Config.Default.BackupDir))
            {
                Config.Default.BackupDir = Path.Combine(Config.Default.DataDir, Config.Default.DefaultBackupDir);
                System.IO.Directory.CreateDirectory(Config.Default.BackupDir);
            }
            
            Config.Default.Save();

            if(String.IsNullOrWhiteSpace(Config.Default.MachinePublicIP))
            {
                Task.Factory.StartNew(async () => await App.DiscoverMachinePublicIP(forceOverride: false));
            }

            base.OnStartup(e);
        }

        public static async Task DiscoverMachinePublicIP(bool forceOverride)
        {
            var publicIP = await NetworkUtils.DiscoverPublicIPAsync();
            if(publicIP != null)
            {
                await App.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if(forceOverride || String.IsNullOrWhiteSpace(Config.Default.MachinePublicIP))
                    {
                        Config.Default.MachinePublicIP = publicIP;
                    }
                }));
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Config.Default.Save();
            base.OnExit(e);
        }

        private static string GetDeployedVersion()
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                Assembly asmCurrent = System.Reflection.Assembly.GetExecutingAssembly();
                string executePath = new Uri(asmCurrent.GetName().CodeBase).LocalPath;

                xmlDoc.Load(executePath + ".manifest");
                XmlNamespaceManager ns = new XmlNamespaceManager(xmlDoc.NameTable);
                ns.AddNamespace("asmv1", "urn:schemas-microsoft-com:asm.v1");
                string xPath = "/asmv1:assembly/asmv1:assemblyIdentity/@version";
                XmlNode node = xmlDoc.SelectSingleNode(xPath, ns);
                string version = node.Value;
                return version;
            }
            catch
            {
                return "Unknown";
            }            
        }
    }
}
