using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ARK_Server_Manager.Lib
{
    /// <summary>
    /// This class is responsible for managing all of the servers the tool knows about.
    /// </summary>
    public class ServerManager : DependencyObject
    {
        static ServerManager()
        {
            ServerManager.Instance = new ServerManager();
        }

        public static ServerManager Instance
        {
            get;
            private set;
        }

        public static readonly DependencyProperty ServersProperty = DependencyProperty.Register(nameof(Servers), typeof(ObservableCollection<Server>), typeof(ServerManager), new PropertyMetadata(new ObservableCollection<Server>()));

        public ObservableCollection<Server> Servers
        {
            get { return (ObservableCollection<Server>)GetValue(ServersProperty); }
            set { SetValue(ServersProperty, value); }
        }
      
        public ServerManager()
        {
            this.Servers.CollectionChanged += Servers_CollectionChanged;
        }

        void Servers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach(Server server in e.OldItems)
                {
                    server.Dispose();
                }
            }
        }

        public int AddFromPath(string path)
        {
            var server = Server.FromPath(path);
            this.Servers.Add(server);
            return this.Servers.Count - 1;
        }  
      
        public int AddNew()
        {
            var server = Server.FromDefaults();
            this.Servers.Add(server);
            return this.Servers.Count - 1;
        }

        public void Remove(Server server, bool deleteProfile)
        {
            if (deleteProfile)
            {
                try
                {
                    var profileFileOld = server.Profile.GetProfileFileOld();
                    if (File.Exists(profileFileOld))
                        File.Delete(profileFileOld);

                    var profileFile = server.Profile.GetProfileFile();
                    if (File.Exists(profileFile))
                        File.Delete(profileFile);

                    var profileFolder = server.Profile.GetProfileIniDir();
                    if (Directory.Exists(profileFolder))
                        Directory.Delete(profileFolder, recursive: true);
                }
                catch (Exception)
                {
                    // Best effort to delete.
                }
            }

            this.Servers.Remove(server);
        }

        public void CheckProfiles()
        {
            var serverIds = new Dictionary<string, bool>();
            foreach (var server in Servers)
            {
                if (server == null || server.Profile == null)
                    continue;

                while (serverIds.ContainsKey(server.Profile.ProfileID))
                {
                    server.Profile.ResetProfileId();
                }

                serverIds.Add(server.Profile.ProfileID, true);
            }
        }
    }
}
