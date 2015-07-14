using QueryMaster;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ARK_Server_Manager.Lib
{
    public class ServerRuntime : DependencyObject, IDisposable
    {
        public enum ServerStatus
        {
            Unknown,
            Stopping,
            Stopped,
            Initializing,
            Running,
            Updating,
            Uninstalled
        }

        public enum SteamStatus
        {
            Unknown,
            NeedPublicIP,
            Unavailable,
            WaitingForPublication,
            Available
        }

        #region Model Properties

        public static readonly DependencyProperty SteamProperty = DependencyProperty.Register("Steam", typeof(SteamStatus), typeof(ServerRuntime), new PropertyMetadata(SteamStatus.Unknown));
        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register("Status", typeof(ServerStatus), typeof(ServerRuntime), new PropertyMetadata(ServerStatus.Unknown));
        public static readonly DependencyProperty MaxPlayersProperty = DependencyProperty.Register("MaxPlayers", typeof(int), typeof(ServerRuntime), new PropertyMetadata(0));
        public static readonly DependencyProperty PlayersProperty = DependencyProperty.Register("Players", typeof(int), typeof(ServerRuntime), new PropertyMetadata(0));
        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register("Version", typeof(Version), typeof(ServerRuntime), new PropertyMetadata(new Version()));

        public SteamStatus Steam
        {
            get { return (SteamStatus)GetValue(SteamProperty); }
            protected set { SetValue(SteamProperty, value); }
        }

        public ServerStatus Status
        {
            get { return (ServerStatus)GetValue(StatusProperty); }
            protected set { SetValue(StatusProperty, value); }
        }

        public int MaxPlayers
        {
            get { return (int)GetValue(MaxPlayersProperty); }
            protected set { SetValue(MaxPlayersProperty, value); }
        }

        public int Players
        {
            get { return (int)GetValue(PlayersProperty); }
            protected set { SetValue(PlayersProperty, value); }
        }

        public Version Version
        {
            get { return (Version)GetValue(VersionProperty); }
            protected set { SetValue(VersionProperty, value); }
        }
                
        #endregion

        private struct ProfileSnapshot
        {
            public string ServerName;
            public string InstallDirectory;
            public int QueryPort;
            public int ServerConnectionPort;
            public string ServerIP;
            public string LastInstalledVersion;
            public string ProfileName;            
            public bool RCONEnabled;
            public int RCONPort;
            public string ServerArgs;
        };

        private ProfileSnapshot profileSnapshot;
        private IAsyncDisposable updateRegistration;
        private Process serverProcess;

        public ServerRuntime()
        {
        }

        private void RegisterForUpdates()
        {
            if (this.updateRegistration == null)
            {
                IPEndPoint localServerQueryEndPoint;
                IPEndPoint steamServerQueryEndPoint;
                GetServerEndpoints(out localServerQueryEndPoint, out steamServerQueryEndPoint);
                this.updateRegistration = ServerStatusWatcher.Instance.RegisterForUpdates(this.profileSnapshot.InstallDirectory, localServerQueryEndPoint, steamServerQueryEndPoint, ProcessLocalUpdate, ProcessSteamUpdate);
            }
        }

        private void UnregisterForUpdates()
        {
            if (this.updateRegistration != null)
            {
                this.updateRegistration.DisposeAsync().DoNotWait();
                this.updateRegistration = null;
            }
        }
        
        public async Task AttachToProfile(ServerProfile settings)
        {
            this.profileSnapshot = new ProfileSnapshot
            {
                InstallDirectory = settings.InstallDirectory,
                QueryPort = settings.ServerPort,
                ServerConnectionPort = settings.ServerConnectionPort,
                ServerIP = settings.ServerIP,
                LastInstalledVersion = settings.LastInstalledVersion,
                ProfileName = settings.ProfileName,
                RCONEnabled = settings.RCONEnabled,
                RCONPort = settings.RCONPort,
                ServerName = settings.ServerName,
                ServerArgs = settings.GetServerArgs()
            };

            Version lastInstalled;
            if (Version.TryParse(settings.LastInstalledVersion, out lastInstalled))
            {
                this.Version = lastInstalled;
            }

            RegisterForUpdates();
        }

        public string GetServerExe()
        {
            return Path.Combine(this.profileSnapshot.InstallDirectory, Config.Default.ServerBinaryRelativePath, Config.Default.ServerExe);
        }

        public async Task StartAsync()
        {
            if(!System.Environment.Is64BitOperatingSystem)
            {
                MessageBox.Show("ARK: Survival Evolved(tm) Server requires a 64-bit operating system to run.  Your operating system is 32-bit and therefore the Ark Server Manager cannot start the server.  You may still load and save profiles and settings files for use on other machines.", "64-bit OS Required", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            switch(this.Status)
            {
                case ServerStatus.Running:
                case ServerStatus.Initializing:
                case ServerStatus.Stopping:
                    Debug.WriteLine("Server {0} already running.", profileSnapshot.ProfileName);
                    return;
            }

            UnregisterForUpdates();
            this.Status = ServerStatus.Initializing;
            
            var serverExe = GetServerExe();
            var serverArgs = this.profileSnapshot.ServerArgs;

            if (Config.Default.ManageFirewallAutomatically)
            {
                var ports = new List<int>() { this.profileSnapshot.QueryPort, this.profileSnapshot.ServerConnectionPort };
                if(this.profileSnapshot.RCONEnabled)
                {
                    ports.Add(this.profileSnapshot.RCONPort);
                }

                if (!FirewallUtils.EnsurePortsOpen(serverExe, ports.ToArray(), "ARK Server: " + this.profileSnapshot.ServerName))
                {
                    var result = MessageBox.Show("Failed to automatically set firewall rules.  If you are running custom firewall software, you may need to set your firewall rules manually.  You may turn off automatic firewall management in Settings.\r\n\r\nWould you like to continue running the server anyway?", "Automatic Firewall Management Error", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                }
            }

            var startInfo = new ProcessStartInfo();
            Process process;
            try
            {
                process = Process.Start(serverExe, serverArgs);
                process.EnableRaisingEvents = true;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                throw new FileNotFoundException(String.Format("Unable to find {0} at {1}.  Server Install Directory: {2}", Config.Default.ServerExe, serverExe, this.profileSnapshot.InstallDirectory), serverExe, ex);
            }
            finally
            {
                RegisterForUpdates();
            }
            
            return;            
        }

        public async Task StopAsync()
        {
            switch(this.Status)
            {
                case ServerStatus.Running:
                case ServerStatus.Initializing:
                    try
                    {
                        UnregisterForUpdates();

                        var ts = new TaskCompletionSource<bool>();
                        EventHandler handler = (s, e) => ts.TrySetResult(true);
                        var process = this.serverProcess;
                        if (process != null)
                        {
                            try
                            {
                                this.Status = ServerStatus.Stopping;
                                this.Steam = SteamStatus.Unavailable;
                                process.Exited += handler;
                                process.CloseMainWindow();
                                await ts.Task;
                            }
                            finally
                            {
                                process.Exited -= handler;
                            }
                        }                    
                    }
                    catch(InvalidOperationException)
                    {                    
                    }
                    finally
                    {
                        this.Status = ServerStatus.Stopped;
                        this.Steam = SteamStatus.Unavailable;
                    }
                    break;
            }            
        }

        public async Task<bool> UpgradeAsync(CancellationToken cancellationToken, bool validate)
        {
            if (!System.Environment.Is64BitOperatingSystem)
            {
                var result = MessageBox.Show("ARK: Survival Evolved(tm) Server requires a 64-bit operating system to run.  Your operating system is 32-bit and therefore the Ark Server Manager will be unable to start the server, but you may still install it or load and save profiles and settings files for use on other machines.\r\n\r\nDo you wish to continue?", "64-bit OS Required", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    return false;
                }
            }

            string serverExe = GetServerExe();
            try
            {
                await StopAsync();
                this.Status = ServerStatus.Updating;

                // Run the SteamCMD to install the server
                var steamCmdPath = System.IO.Path.Combine(Config.Default.DataDir, Config.Default.SteamCmdDir, Config.Default.SteamCmdExe);
                Directory.CreateDirectory(this.profileSnapshot.InstallDirectory);
                var steamArgs = String.Format(Config.Default.SteamCmdInstallServerArgsFormat, this.profileSnapshot.InstallDirectory, validate ? "validate" : String.Empty);
                var process = Process.Start(steamCmdPath, steamArgs);
                process.EnableRaisingEvents = true;
                var ts = new TaskCompletionSource<bool>();
                using (var cancelRegistration = cancellationToken.Register(() => { try { process.CloseMainWindow(); } finally { ts.TrySetCanceled(); } }))
                {
                    process.Exited += (s, e) => ts.TrySetResult(process.ExitCode == 0);
                    process.ErrorDataReceived += (s, e) => ts.TrySetException(new Exception(e.Data));
                    var success = await ts.Task;
                    if(success && ServerManager.Instance.AvailableVersion != null)
                    {
                        this.Version = ServerManager.Instance.AvailableVersion;
                    }

                    return success;
                }
            }
            catch(TaskCanceledException)
            {
                return false;
            }
            finally
            {
                this.Status = ServerStatus.Stopped;
            }
        }
    
        public void Dispose()
        {
            this.updateRegistration.DisposeAsync().DoNotWait();
        }


        private void GetServerEndpoints(out IPEndPoint localServerQueryEndPoint, out IPEndPoint steamServerQueryEndPoint)
        {
            //
            // Get the local endpoint for querying the local network
            //

            IPAddress localServerIpAddress;
            if (!String.IsNullOrWhiteSpace(this.profileSnapshot.ServerIP) && IPAddress.TryParse(this.profileSnapshot.ServerIP, out localServerIpAddress))
            {
                // Use the explicit Server IP
                localServerQueryEndPoint = new IPEndPoint(localServerIpAddress, Convert.ToUInt16(this.profileSnapshot.QueryPort));
            }
            else
            {
                // No Server IP specified, use Loopback
                localServerQueryEndPoint = new IPEndPoint(IPAddress.Loopback, Convert.ToUInt16(this.profileSnapshot.QueryPort));
            }

            //
            // Get the public endpoint for querying Steam
            //
            steamServerQueryEndPoint = null;
            if (!String.IsNullOrWhiteSpace(Config.Default.MachinePublicIP))
            {
                IPAddress steamServerIpAddress;
                if (IPAddress.TryParse(Config.Default.MachinePublicIP, out steamServerIpAddress))
                {
                    // Use the Public IP explicitly specified
                    steamServerQueryEndPoint = new IPEndPoint(steamServerIpAddress, Convert.ToUInt16(this.profileSnapshot.QueryPort));
                }
                else
                {
                    // Resolve the IP from the DNS name provided
                    try
                    {
                        var addresses = Dns.GetHostAddresses(Config.Default.MachinePublicIP);
                        if (addresses.Length > 0)
                        {
                            steamServerQueryEndPoint = new IPEndPoint(addresses[0], Convert.ToUInt16(this.profileSnapshot.QueryPort));
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Failed to resolve DNS address {0}: {1}\r\n{2}", Config.Default.MachinePublicIP, ex.Message, ex.StackTrace);
                    }
                }
            }
        }

        private void ProcessSteamUpdate(IAsyncDisposable registration, ServerStatusWatcher.ServerStatusUpdate update)
        {
            if(!Object.ReferenceEquals(registration, this.updateRegistration))
            {
                return;
            }

            TaskUtils.RunOnUIThreadAsync(() =>
            {
                if (this.Status == ServerStatus.Running)
                {
                    switch (update.Status)
                    {
                        case ServerStatusWatcher.ServerStatus.Stopped:
                        case ServerStatusWatcher.ServerStatus.Initializing:
                        case ServerStatusWatcher.ServerStatus.NotInstalled:
                            if (this.Status == ServerStatus.Running)
                            {
                                this.Steam = SteamStatus.WaitingForPublication;
                            }
                            else
                            {
                                this.Steam = SteamStatus.Unavailable;
                            }
                            break;

                        case ServerStatusWatcher.ServerStatus.Running:
                            this.Steam = SteamStatus.Available;
                            break;
                    }
                }
                else
                {
                    this.Steam = SteamStatus.Unavailable;
                }
            }).DoNotWait();
        }

        private void ProcessLocalUpdate(IAsyncDisposable registration, ServerStatusWatcher.ServerStatusUpdate update)
        {
            if(!Object.ReferenceEquals(registration, this.updateRegistration))
            {
                return;
            }

            TaskUtils.RunOnUIThreadAsync(() =>
            {
                switch (update.Status)
                {
                    case ServerStatusWatcher.ServerStatus.NotInstalled:
                        this.Status = ServerStatus.Uninstalled;
                        this.Steam = SteamStatus.Unavailable;
                        break;

                    case ServerStatusWatcher.ServerStatus.Initializing:
                        this.Status = ServerStatus.Initializing;
                        this.Steam = SteamStatus.Unavailable;
                        break;

                    case ServerStatusWatcher.ServerStatus.Stopped:
                        this.Status = ServerStatus.Stopped;
                        this.Steam = SteamStatus.Unavailable;
                        break;

                    case ServerStatusWatcher.ServerStatus.Running:
                        this.Status = ServerStatus.Running;
                        break;
                }

                if (update.ServerInfo != null)
                {
                    var match = Regex.Match(update.ServerInfo.Name, @"\(v([0-9]+\.[0-9]*)\)");
                    if (match.Success && match.Groups.Count >= 2)
                    {
                        var serverVersion = match.Groups[1].Value;
                        Version temp;
                        if (!String.IsNullOrWhiteSpace(serverVersion) && Version.TryParse(serverVersion, out temp))
                        {
                            this.Version = temp;
                        }
                    }

                    this.Players = update.ServerInfo.Players;
                    this.MaxPlayers = update.ServerInfo.MaxPlayers;
                }

                this.serverProcess = update.Process;
            }).DoNotWait();
        }
    }
}
