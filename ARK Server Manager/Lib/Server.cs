using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ARK_Server_Manager.Lib
{
    public class Server : DependencyObject
    {
        public static readonly DependencyProperty ProfileProperty = DependencyProperty.Register("Profile", typeof(ServerProfile), typeof(Server), new PropertyMetadata((ServerProfile)null));
        public static readonly DependencyProperty RuntimeProperty = DependencyProperty.Register("Runtime", typeof(ServerRuntime), typeof(Server), new PropertyMetadata((ServerRuntime)null));

        public ServerProfile Profile
        {
            get { return (ServerProfile)GetValue(ProfileProperty); }
            set { SetValue(ProfileProperty, value); }
        }
        
        public ServerRuntime Runtime
        {
            get { return (ServerRuntime)GetValue(RuntimeProperty); }
            set { SetValue(RuntimeProperty, value); }
        }

        public static Server FromPath(string path)
        {
            var profile = ServerProfile.LoadFrom(path);
            return FromProfile(profile);
        }   
     
        public static Server FromDefaults()
        {
            var profile = ServerProfile.FromDefaults();
            return FromProfile(profile);
        }

        private static Server FromProfile(ServerProfile profile)
        {
            return new Server
            {
                Profile = profile,
                Runtime = new ServerRuntime(profile)
            };
        }
    }
}
