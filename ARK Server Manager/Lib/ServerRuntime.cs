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
    public class ServerRuntime : ISettingsBag, INotifyPropertyChanged
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

        public SteamStatus Steam = SteamStatus.Unknown;
        public ServerStatus Status = ServerStatus.Unknown;
        public int MaxPlayers = 0;
        public int Players = 0;
        public Version Version = new Version();
        
        #endregion

        private Process serverProcess;

        public ServerSettings Settings
        {
            get;
            private set;
        }

        public ServerRuntime(ServerSettings settings)
        {
            this.Settings = settings;
            Version lastInstalled;
            if (Version.TryParse(settings.LastInstalledVersion, out lastInstalled))
            {
                this.InstalledVersion = lastInstalled;
            }
        }

        public object this[string propertyName]
        {
            get { return this.GetType().GetField(propertyName).GetValue(this); }
            set { this.GetType().GetField(propertyName).SetValue(this, value); }
        }

        public bool IsRunning
        {
            get { return serverProcess != null && serverProcess.HasExited == false; }
        }

        public ServerStatus ExecutionStatus
        {
            get { return Status; }
            private set { this.Status = value; OnPropertyChanged("Status"); }
        }

        public SteamStatus SteamAvailability
        {
            get { return Steam; }
            private set { this.Steam = value; OnPropertyChanged("Steam"); }
        }

        public Version InstalledVersion
        {
            get { return Version; }
            private set { this.Version = value; OnPropertyChanged("Version"); }
        }

        public int RunningMaxPlayers
        {
            get { return this.MaxPlayers; }
            private set { this.MaxPlayers = value; OnPropertyChanged("MaxPlayers"); }
        }

        public int RunningPlayers
        {
            get { return this.Players; }
            private set { this.Players = value; OnPropertyChanged("Players"); }
        }

        public Task UpdateStatusAsync(CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(async () =>
                {
                    await PeriodicUpdateCheck(cancellationToken);
                });
        }

        private async Task PeriodicUpdateCheck(CancellationToken cancellationToken)
        {            
            while (!cancellationToken.IsCancellationRequested)
            {
                //
                // Check the status of the server locally and on Steam
                //
                if (!File.Exists(GetServerExe()))
                {
                    this.ExecutionStatus = ServerStatus.Uninstalled;
                    this.SteamAvailability = SteamStatus.Unavailable;
                }
                else
                {
                    if (this.serverProcess == null)
                    {
                        this.serverProcess = FindMatchingServerProcess();
                    }

                    if (this.serverProcess != null)
                    {
                        //
                        // Get the local endpoint for querying the local network
                        //
                        IPEndPoint localServerQueryEndPoint;
                        IPAddress localServerIpAddress;
                        if(!String.IsNullOrWhiteSpace(this.Settings.ServerIP) && IPAddress.TryParse(this.Settings.ServerIP, out localServerIpAddress))
                        { 
                            localServerQueryEndPoint= new IPEndPoint(localServerIpAddress, Convert.ToUInt16(this.Settings.ServerPort));
                        }
                        else
                        {
                            localServerQueryEndPoint = new IPEndPoint(IPAddress.Loopback, Convert.ToUInt16(this.Settings.ServerPort));
                        }

                        //
                        // Get the public endpoint for querying Steam
                        IPEndPoint steamServerQueryEndPoint = null;  
                        if (!String.IsNullOrWhiteSpace(Config.Default.MachinePublicIP))
                        {
                            IPAddress steamServerIpAddress;
                            if (IPAddress.TryParse(Config.Default.MachinePublicIP, out steamServerIpAddress))
                            {
                                steamServerQueryEndPoint = new IPEndPoint(steamServerIpAddress, Convert.ToUInt16(this.Settings.ServerPort));
                            }
                            else
                            {
                                var addresses = Dns.GetHostAddresses(Config.Default.MachinePublicIP);
                                if (addresses.Length > 0)
                                {
                                    steamServerQueryEndPoint = new IPEndPoint(addresses[0], Convert.ToUInt16(this.Settings.ServerPort));
                                }
                            }
                        }

                        var localServerInfo = App.ServerWatcher.GetLocalServerInfo(localServerQueryEndPoint);
                        ServerInfo steamServerInfo = null;
                        if (steamServerQueryEndPoint != null)
                        {
                            steamServerInfo = App.ServerWatcher.GetSteamServerInfo(steamServerQueryEndPoint);
                        }
                            
                        if(localServerInfo != null)
                        {
                            this.ExecutionStatus = ServerStatus.Running;
                            this.RunningMaxPlayers = localServerInfo.MaxPlayers;
                            this.RunningPlayers = localServerInfo.Players;

                            // Get the version
                            var match = Regex.Match(localServerInfo.Name, @"\(v([0-9]+\.[0-9]*)\)");
                            if (match.Success && match.Groups.Count >= 2)
                            {
                                var serverVersion = match.Groups[1].Value;
                                Version temp;
                                if (!String.IsNullOrWhiteSpace(serverVersion) && Version.TryParse(serverVersion, out temp))
                                {
                                    this.InstalledVersion = temp;
                                    this.Settings.LastInstalledVersion = serverVersion;
                                }
                            }

                            if (steamServerQueryEndPoint == null)
                            {
                                this.SteamAvailability = SteamStatus.NeedPublicIP;
                            }
                            else
                            {
                                if (steamServerInfo != null)
                                {
                                    this.SteamAvailability = SteamStatus.Available;
                                }
                                else
                                {
                                    this.SteamAvailability = SteamStatus.WaitingForPublication;
                                }
                            }
                        }
                        else
                        {
                            this.ExecutionStatus = ServerStatus.Initializing;
                            this.SteamAvailability = SteamStatus.Unavailable;
                        }
                    }
                    else
                    {
                        this.ExecutionStatus = ServerStatus.Stopped;
                        this.SteamAvailability = SteamStatus.Unavailable;
                    }
                }

                await Task.Delay(1000);
            }            
        }
        private Process FindMatchingServerProcess()
        {
            foreach(var process in Process.GetProcessesByName(Config.Default.ServerProcessName))
            {
                var commandLineBuilder = new StringBuilder();

                using (var searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
                {
                    foreach (var @object in searcher.Get())
                    {
                        commandLineBuilder.Append(@object["CommandLine"] + " ");
                    }
                }

                var commandLine = commandLineBuilder.ToString();

                if (commandLine.Contains(Config.Default.ServerExe))
                {
                    // Does this match our server?
                    var serverArgMatch = String.Format(Config.Default.ServerCommandLineArgsMatchFormat, this.Settings.ServerPort);
                    if (commandLine.Contains(serverArgMatch))
                    {
                        process.EnableRaisingEvents = true;
                        return process;
                    }
                }
            }

            return null;
        }

        public string GetServerExe()
        {
            return Path.Combine(this.Settings.InstallDirectory, Config.Default.ServerBinaryRelativePath, Config.Default.ServerExe);
        }

        public async Task StartAsync()
        {
            if(!System.Environment.Is64BitOperatingSystem)
            {
                MessageBox.Show("ARK: Survival Evolved(tm) Server requires a 64-bit operating system to run.  Your operating system is 32-bit and therefore the Ark Server Manager cannot start the server.  You may still load and save profiles and settings files for use on other machines.", "64-bit OS Required", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (this.IsRunning)
            {
                Debug.WriteLine("Server {0} already running.", Settings.ProfileName);
                return;
            }

            this.Settings.WriteINIFile();
            var serverExe = GetServerExe();
            var serverArgs = this.Settings.GetServerArgs();
            var startInfo = new ProcessStartInfo();
            try
            {
                this.serverProcess = Process.Start(serverExe, serverArgs);
            }
            catch(System.ComponentModel.Win32Exception ex)
            {
                throw new FileNotFoundException(String.Format("Unable to find {0} at {1}.  Server Install Directory: {2}", Config.Default.ServerExe, serverExe, this.Settings.InstallDirectory), serverExe, ex);
            }

            this.serverProcess.EnableRaisingEvents = true;
            this.ExecutionStatus = ServerStatus.Running;
            // TODO: Ensure the watchdog is running and start with Initializing instead of Running
            return;            
        }

        public async Task StopAsync()
        {
            if(this.IsRunning)
            {
                try
                {
                    var ts = new TaskCompletionSource<bool>();
                    EventHandler handler = (s, e) => ts.TrySetResult(true);
                    try
                    {
                        this.ExecutionStatus = ServerStatus.Stopping;
                        this.serverProcess.Exited += handler;
                        this.serverProcess.CloseMainWindow();
                        await ts.Task;
                    }
                    finally
                    {
                        this.serverProcess.Exited -= handler; 
                        this.serverProcess = null;
                    }

                    this.ExecutionStatus = ServerStatus.Stopped;

                }
                catch(InvalidOperationException)
                {                    
                }
            }            
        }

        public async Task UpgradeAsync(CancellationToken cancellationToken, bool validate)
        {
            if (!System.Environment.Is64BitOperatingSystem)
            {
                var result = MessageBox.Show("ARK: Survival Evolved(tm) Server requires a 64-bit operating system to run.  Your operating system is 32-bit and therefore the Ark Server Manager will be unable to start the server, but you may still install it or load and save profiles and settings files for use on other machines.\r\n\r\nDo you wish to continue?", "64-bit OS Required", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            string serverExe = GetServerExe();

            // TODO: Do a version check
            if (true)
            {
                try
                {
                    await StopAsync();

                    this.ExecutionStatus = ServerStatus.Updating;

                    // Run the SteamCMD to install the server
                    var steamCmdPath = System.IO.Path.Combine(Config.Default.DataDir, Config.Default.SteamCmdDir, Config.Default.SteamCmdExe);
                    Directory.CreateDirectory(this.Settings.InstallDirectory);
                    var steamArgs = String.Format(Config.Default.SteamCmdInstallServerArgsFormat, this.Settings.InstallDirectory, validate ? "validate" : String.Empty);
                    var process = Process.Start(steamCmdPath, steamArgs);
                    process.EnableRaisingEvents = true;
                    var ts = new TaskCompletionSource<bool>();
                    using (var cancelRegistration = cancellationToken.Register(() => { try { process.CloseMainWindow(); } finally { ts.TrySetCanceled(); } }))
                    {
                        process.Exited += (s, e) => ts.TrySetResult(process.ExitCode == 0);
                        process.ErrorDataReceived += (s, e) => ts.TrySetException(new Exception(e.Data));
                        await ts.Task;
                    }
                }
                catch(TaskCanceledException)
                {
                }
                finally
                {
                    this.ExecutionStatus = ServerStatus.Stopped;
                }
            }
        }
    
        protected void OnPropertyChanged(string propertyName)
        {
            var propChanged = this.PropertyChanged;
            if (propChanged != null)
            {
                // TODO: When the app shuts down, we need to kill outstanding update tasks.
                if (App.Current != null)
                {
                    App.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            propChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
                        }));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class ServerRuntimeViewModel : ViewModelBase
    {
        private ServerSettings serverSettings;
        private ServerRuntime model;
        CancellationTokenSource updateCancellation = new CancellationTokenSource();

        public ObservableCollection<IPEndPoint> MasterServers
        {
            get;
            set;
        }

        public ServerRuntime Model
        {
            get { return this.model; }
        }

        public ServerRuntimeViewModel(ServerSettings serverSettings)
        {            
            this.serverSettings = serverSettings;  
            this.model = new ServerRuntime(serverSettings);
            this.model.PropertyChanged += (s, e) => base.OnPropertyChanged(e.PropertyName);
            this.model.UpdateStatusAsync(updateCancellation.Token);            
        }

        public ServerRuntime.SteamStatus Steam
        {
            get { return Get<ServerRuntime.SteamStatus>(model); }
        }

        public ServerRuntime.ServerStatus Status
        {
            get { return Get<ServerRuntime.ServerStatus>(model); }
        }

        public Version Version
        {
            get { return Get<Version>(model); }
        }

        public int MaxPlayers
        {
            get { return Get<int>(model); }
        }

        public int Players
        {
            get { return Get<int>(model); }
        }

    }
}
