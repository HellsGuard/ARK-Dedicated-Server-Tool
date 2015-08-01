using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ARK_Server_Manager.Lib
{
    public class Server : DependencyObject, IDisposable
    {
        public static readonly DependencyProperty ProfileProperty = DependencyProperty.Register(nameof(Profile), typeof(ServerProfile), typeof(Server), new PropertyMetadata((ServerProfile)null));
        public static readonly DependencyProperty RuntimeProperty = DependencyProperty.Register(nameof(Runtime), typeof(ServerRuntime), typeof(Server), new PropertyMetadata((ServerRuntime)null));

        public ServerProfile Profile
        {
            get { return (ServerProfile)GetValue(ProfileProperty); }
            protected set { SetValue(ProfileProperty, value); }
        }
        
        public ServerRuntime Runtime
        {
            get { return (ServerRuntime)GetValue(RuntimeProperty); }
            protected set { SetValue(RuntimeProperty, value); }
        }

        public void ImportFromPath(string path)
        {
            var profile = ServerProfile.LoadFrom(path);
            InitializeFromProfile(profile);
        }

        private Server(ServerProfile profile)
        {
            InitializeFromProfile(profile);
        }

        private void InitializeFromProfile(ServerProfile profile)
        {
            this.Profile = profile;
            this.Runtime = new ServerRuntime();
            this.Runtime.AttachToProfile(this.Profile).Wait();
        }

        public static Server FromPath(string path)
        {
            var profile = ServerProfile.LoadFrom(path);
            return new Server(profile);
        }   
     
        public static Server FromDefaults()
        {
            var profile = ServerProfile.FromDefaults();
            return new Server(profile);
        }

        public async Task StartAsync()
        {
            await this.Runtime.AttachToProfile(this.Profile);
            await this.Runtime.StartAsync();
        }

        public async Task StopAsync()
        {
            await this.Runtime.StopAsync();
        }

        public async Task<bool> UpgradeAsync(CancellationToken cancellationToken, bool validate)
        {
            await this.Runtime.AttachToProfile(this.Profile);
            var success = await this.Runtime.UpgradeAsync(cancellationToken, validate);
            this.Profile.LastInstalledVersion = this.Runtime.Version.ToString();
            return success;
        }

        public void Dispose()
        {
            this.Runtime.Dispose();
        }
    }
}
