using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public static readonly DependencyProperty ServersProperty = DependencyProperty.Register("Servers", typeof(ObservableCollection<Server>), typeof(ServerManager), new PropertyMetadata(new ObservableCollection<Server>()));

        public ObservableCollection<Server> Servers
        {
            get { return (ObservableCollection<Server>)GetValue(ServersProperty); }
            set { SetValue(ServersProperty, value); }
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
                catch(Exception ex)
                {
                    // Best effort to delete.
                }
            }

            this.Servers.Remove(server);
        }
    }
}
