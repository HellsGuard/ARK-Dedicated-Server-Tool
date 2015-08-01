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
        public static readonly DependencyProperty AvailableVersionProperty = DependencyProperty.Register(nameof(AvailableVersion), typeof(Version), typeof(ServerManager), new PropertyMetadata(new Version()));

        public ObservableCollection<Server> Servers
        {
            get { return (ObservableCollection<Server>)GetValue(ServersProperty); }
            set { SetValue(ServersProperty, value); }
        }

        public Version AvailableVersion
        {
            get { return (Version)GetValue(AvailableVersionProperty); }
            set { SetValue(AvailableVersionProperty, value); }
        }
      
        public ServerManager()
        {
            this.Servers.CollectionChanged += Servers_CollectionChanged;
            CheckForUpdatesAsync().DoNotWait();
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
            if(deleteProfile)
            {
                try
                {
                    File.Delete(server.Profile.GetProfilePath());
                }
                catch(Exception)
                {
                    // Best effort to delete.
                }
            }

            this.Servers.Remove(server);
        }

        public async Task CheckForUpdatesAsync()
        {
            var result = await NetworkUtils.GetLatestAvailableVersion();
            await TaskUtils.RunOnUIThreadAsync(() => this.AvailableVersion = result.Current);
        }
    }
}
