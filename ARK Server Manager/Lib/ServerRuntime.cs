using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        #region Model Properties
        
        public ServerStatus Status = ServerStatus.Unknown;
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
            UpdateStatusAsync();
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

        public Version InstalledVersion
        {
            get { return Version; }
            private set { this.Version = value; OnPropertyChanged("Version"); }
        }

        public async Task UpdateStatusAsync()
        {
            if (!File.Exists(Path.Combine(this.Settings.InstallDirectory, Config.Default.ServerBinaryRelativePath, Config.Default.ServerExe)))
            {
                this.ExecutionStatus = ServerStatus.Uninstalled;
            }
            else            
            {
                this.ExecutionStatus = ServerStatus.Stopped;

                if(this.serverProcess == null)
                {
                    this.serverProcess = FindMatchingServerProcess();
                }

                if(this.serverProcess != null)
                {
                    this.ExecutionStatus = ServerStatus.Running;
                    // Determine whether we are initializing or accepting connections
                    // TODO: Implement
                }
            }

            await Task.FromResult<bool>(true);
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

        public async Task StartAsync()
        {
            if (this.IsRunning)
            {
                Debug.WriteLine("Server {0} already running.", Settings.ProfileName);
                return;
            }

            this.Settings.WriteINIFile();
            var serverExe = Path.Combine(this.Settings.InstallDirectory, Config.Default.ServerBinaryRelativePath, Config.Default.ServerExe);
            var serverArgs = this.Settings.GetServerArgs();
            var startInfo = new ProcessStartInfo();
            try
            {
                this.serverProcess = Process.Start(serverExe, serverArgs);
            }
            catch(System.ComponentModel.Win32Exception ex)
            {
                throw new FileNotFoundException(String.Format("Unable to find SteamCmd.exe at {0}.  Server Install Directory: {1}", serverExe, this.Settings.InstallDirectory), serverExe, ex);
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
                    }

                    this.ExecutionStatus = ServerStatus.Stopped;

                }
                catch(InvalidOperationException)
                {                    
                }                
            }            
        }

        public async Task UpgradeAsync(CancellationToken cancellationToken)
        {
            string serverExe = System.IO.Path.Combine(this.Settings.InstallDirectory, Config.Default.ServerBinaryRelativePath, Config.Default.ServerExe);

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
                    var steamArgs = String.Format(Config.Default.SteamCmdInstallServerArgsFormat, this.Settings.InstallDirectory);
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
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class ServerRuntimeViewModel : ViewModelBase
    {
        private ServerSettings serverSettings;
        private ServerRuntime model;

        public ServerRuntime Model
        {
            get { return this.model; }
        }

        public ServerRuntimeViewModel(ServerSettings serverSettings)
        {            
            this.serverSettings = serverSettings;  
            this.model = new ServerRuntime(serverSettings);
            this.model.PropertyChanged += (s, e) => base.OnPropertyChanged(e.PropertyName);
            this.model.UpdateStatusAsync();
        }

        public ServerRuntime.ServerStatus Status
        {
            get { return Get<ServerRuntime.ServerStatus>(model); }
        }

        public Version Version
        {
            get { return Get<Version>(model); }
        }
    }
}
