using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ARK_Server_Manager.Lib
{
    /// <summary>
    /// This class is responsible for managing all of the servers the tool knows about.
    /// </summary>
    class ServerManager : DependencyObject
    {
        public ObservableCollection<Server> Servers
        {
            get { return (ObservableCollection<Server>)GetValue(ServersProperty); }
            set { SetValue(ServersProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Servers.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ServersProperty =
            DependencyProperty.Register("Servers", typeof(ObservableCollection<Server>), typeof(ServerManager), new PropertyMetadata(new ObservableCollection<Server>()));

        
        public void LoadFromProfile(string path)
        {
            var server = Server.FromPath(path);
            this.Servers.Add(server);
        }        
    }
}
