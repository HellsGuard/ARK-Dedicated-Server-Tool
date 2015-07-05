using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for GlobalSettings.xaml
    /// </summary>
    public partial class GlobalSettings : UserControl
    {
        public string Version
        {
            get;
            set;
        }

        public Config CurrentConfig
        {
            get;
            set;
        }

        public GlobalSettings()
        {
            this.Version = GetDeployedVersion();

            this.CurrentConfig = Config.Default;
            this.DataContext = this;

            InitializeComponent();
        }

        private string GetDeployedVersion()
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

        public void SetDataDir_Click(object sender, RoutedEventArgs args)
        {
            var optionResult = MessageBox.Show("Changing the data directory will move any existing profiles to the new location, but it will not move any server installations.  Do you still want to change this directory?", "Confim changing data directory", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (optionResult == MessageBoxResult.Yes)
            {
                var dialog = new CommonOpenFileDialog();
                dialog.IsFolderPicker = true;
                dialog.Title = "Select Data Directory";
                dialog.InitialDirectory = Config.Default.DataDir;
                var result = dialog.ShowDialog();

                if (result == CommonFileDialogResult.Ok)
                {
                    if (!String.Equals(dialog.FileName, Config.Default.DataDir))
                    {
                        try
                        {
                            // Set up the destination directories
                            string newConfigDirectory = Path.Combine(dialog.FileName, Config.Default.ProfilesDir);
                            string newBackupDirectory = Path.Combine(dialog.FileName, Config.Default.DefaultBackupDir);
                            string oldSteamDirectory = Path.Combine(Config.Default.DataDir, Config.Default.SteamCmdDir);
                            string newSteamDirectory = Path.Combine(dialog.FileName, Config.Default.SteamCmdDir);

                            Directory.CreateDirectory(newConfigDirectory);
                            Directory.CreateDirectory(newSteamDirectory);
                            Directory.CreateDirectory(newBackupDirectory);

                            // Copy the Profiles
                            foreach (var file in Directory.EnumerateFiles(Config.Default.ConfigDirectory, "*.*", SearchOption.AllDirectories))
                            {
                                string sourceWithoutRoot = file.Substring(Config.Default.ConfigDirectory.Length + 1);
                                string destination = Path.Combine(newConfigDirectory, sourceWithoutRoot);
                                if (!File.Exists(destination))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                                    File.Copy(file, destination);
                                }
                            }

                            // Copy the SteamCMD files
                            foreach (var file in Directory.EnumerateFiles(oldSteamDirectory, "*.*", SearchOption.AllDirectories))
                            {
                                string sourceWithoutRoot = file.Substring(oldSteamDirectory.Length + 1);
                                string destination = Path.Combine(newSteamDirectory, sourceWithoutRoot);
                                if (!File.Exists(destination))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                                    File.Copy(file, destination);
                                }
                            }

                            // Remove the old directories
                            Directory.Delete(Config.Default.ConfigDirectory, true);
                            Directory.Delete(oldSteamDirectory, true);

                            // Update the config
                            Config.Default.DataDir = dialog.FileName;
                            Config.Default.ConfigDirectory = newConfigDirectory;
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show(String.Format("There was an error changing the data directory: {0}\r\nPlease correct the error and try again, or contact technical support for assistance.", ex.Message), "Failed to change data directory", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                        
                    }
                }
            }
        }

        public void SetBackupDir_Click(object sender, RoutedEventArgs args) 
        {
            var mbResult = MessageBox.Show("Are you sure you wish to change the default backup directory?", "Backup directory change confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (mbResult == MessageBoxResult.Yes) 
            {
                var fileDialog = new CommonOpenFileDialog();
                fileDialog.IsFolderPicker = true;
                fileDialog.Title = "Select Backup Directory";
                fileDialog.InitialDirectory = Config.Default.DataDir;
                var dialogResult = fileDialog.ShowDialog();

                if (dialogResult == CommonFileDialogResult.Ok)
                {
                    if (!String.Equals(fileDialog.FileName, Config.Default.DataDir)) 
                    {
                        try
                        {
                            // Update Config
                            Config.Default.BackupDir = fileDialog.FileName;
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show(String.Format("There was an error changing the backup directory: {0}\r\nPlease correct the error and try again, or contact technical support for assistance.", ex.Message), "Failed to change backup directory", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                    }
                }
            }
        }

    }
}
