using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WPFSharp.Globalizer;

namespace ARK_Server_Manager.Lib
{
    public class ServerRuntime : DependencyObject, IDisposable
    {
        public event EventHandler StatusUpdate;

        private GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public struct RuntimeProfileSnapshot
        {
            public string ProfileName;            
            public string InstallDirectory;
            public string AltSaveDirectoryName;
            public string AdminPassword;
            public string ServerName;
            public string ServerArgs;
            public string ServerIP;
            public int ServerConnectionPort;
            public int QueryPort;
            public bool UseRawSockets;
            public bool RCONEnabled;
            public int RCONPort;
            public bool SotFServer;
            public string ServerMap;
            public string ServerMapModId;
            public string TotalConversionModId;
            public List<string> ServerModIds;
            public string LastInstalledVersion;
        };

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

        private List<PropertyChangeNotifier> profileNotifiers = new List<PropertyChangeNotifier>();
        private Process serverProcess;
        private IAsyncDisposable updateRegistration;

        public ServerRuntime()
        {
        }

        #region Properties

        public static readonly DependencyProperty SteamProperty = DependencyProperty.Register(nameof(Steam), typeof(SteamStatus), typeof(ServerRuntime), new PropertyMetadata(SteamStatus.Unknown));
        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(nameof(Status), typeof(ServerStatus), typeof(ServerRuntime), new PropertyMetadata(ServerStatus.Unknown));
        public static readonly DependencyProperty MaxPlayersProperty = DependencyProperty.Register(nameof(MaxPlayers), typeof(int), typeof(ServerRuntime), new PropertyMetadata(0));
        public static readonly DependencyProperty PlayersProperty = DependencyProperty.Register(nameof(Players), typeof(int), typeof(ServerRuntime), new PropertyMetadata(0));
        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register(nameof(Version), typeof(Version), typeof(ServerRuntime), new PropertyMetadata(new Version()));
        public static readonly DependencyProperty ProfileSnapshotProperty = DependencyProperty.Register(nameof(ProfileSnapshot), typeof(RuntimeProfileSnapshot), typeof(ServerRuntime), new PropertyMetadata(null));

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

        public RuntimeProfileSnapshot ProfileSnapshot
        {
            get { return (RuntimeProfileSnapshot)GetValue(ProfileSnapshotProperty); }
            set { SetValue(ProfileSnapshotProperty, value); }
        }

        #endregion

        public void Dispose()
        {
            this.updateRegistration.DisposeAsync().DoNotWait();
        }

        public Task AttachToProfile(ServerProfile profile)
        {
            AttachToProfileCore(profile);
            GetProfilePropertyChanges(profile);
            return TaskUtils.FinishedTask;
        }

        private void AttachToProfileCore(ServerProfile profile)
        {
            UnregisterForUpdates();

            this.ProfileSnapshot = new RuntimeProfileSnapshot
            {
                ProfileName = profile.ProfileName,
                InstallDirectory = profile.InstallDirectory,
                AltSaveDirectoryName = profile.AltSaveDirectoryName,
                AdminPassword = profile.AdminPassword,
                ServerName = profile.ServerName,
                ServerArgs = profile.GetServerArgs(),
                ServerIP = String.IsNullOrWhiteSpace(profile.ServerIP) ? IPAddress.Loopback.ToString() : profile.ServerIP,
                ServerConnectionPort = profile.ServerConnectionPort,
                QueryPort = profile.ServerPort,
                UseRawSockets = profile.UseRawSockets,
                RCONEnabled = profile.RCONEnabled,
                RCONPort = profile.RCONPort,
                SotFServer = profile.SOTF_Enabled,
                ServerMap = ModUtils.GetMapName(profile.ServerMap),
                ServerMapModId = ModUtils.GetMapModId(profile.ServerMap),
                TotalConversionModId = profile.TotalConversionModId ?? string.Empty,
                ServerModIds = ModUtils.GetModIdList(profile.ServerModIds),
                LastInstalledVersion =  string.IsNullOrWhiteSpace(profile.LastInstalledVersion) ? new Version(0, 0).ToString() : profile.LastInstalledVersion,
            };

            Version lastInstalled;
            if (Version.TryParse(profile.LastInstalledVersion, out lastInstalled))
            {
                this.Version = lastInstalled;
            }

            RegisterForUpdates();
        }

        private void GetProfilePropertyChanges(ServerProfile profile)
        {
            foreach(var notifier in profileNotifiers)
            {
                notifier.Dispose();
            }

            profileNotifiers.Clear();
            profileNotifiers.AddRange(PropertyChangeNotifier.GetNotifiers(
                profile,
                new[] { 
                    ServerProfile.InstallDirectoryProperty,
                    ServerProfile.ServerPortProperty,
                    ServerProfile.ServerConnectionPortProperty,
                    ServerProfile.ServerIPProperty
                },
                (s, p) =>
                {
                    if (Status == ServerStatus.Stopped || Status == ServerStatus.Uninstalled || Status == ServerStatus.Unknown) { AttachToProfileCore(profile); }
                }));

        }

        private void GetServerEndpoints(out IPEndPoint localServerQueryEndPoint, out IPEndPoint steamServerQueryEndPoint)
        {
            //
            // Get the local endpoint for querying the local network
            //

            IPAddress localServerIpAddress;
            if (!String.IsNullOrWhiteSpace(this.ProfileSnapshot.ServerIP) && IPAddress.TryParse(this.ProfileSnapshot.ServerIP, out localServerIpAddress))
            {
                // Use the explicit Server IP
                localServerQueryEndPoint = new IPEndPoint(localServerIpAddress, Convert.ToUInt16(this.ProfileSnapshot.QueryPort));
            }
            else
            {
                // No Server IP specified, use Loopback
                localServerQueryEndPoint = new IPEndPoint(IPAddress.Loopback, Convert.ToUInt16(this.ProfileSnapshot.QueryPort));
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
                    steamServerQueryEndPoint = new IPEndPoint(steamServerIpAddress, Convert.ToUInt16(this.ProfileSnapshot.QueryPort));
                }
                else
                {
                    // Resolve the IP from the DNS name provided
                    try
                    {
                        var addresses = Dns.GetHostAddresses(Config.Default.MachinePublicIP);
                        if (addresses.Length > 0)
                        {
                            steamServerQueryEndPoint = new IPEndPoint(addresses[0], Convert.ToUInt16(this.ProfileSnapshot.QueryPort));
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Failed to resolve DNS address {0}: {1}\r\n{2}", Config.Default.MachinePublicIP, ex.Message, ex.StackTrace);
                    }
                }
            }
        }

        public string GetServerExe()
        {
            return Path.Combine(this.ProfileSnapshot.InstallDirectory, Config.Default.ServerBinaryRelativePath, Config.Default.ServerExe);
        }

        private void ProcessStatusUpdate(IAsyncDisposable registration, ServerStatusWatcher.ServerStatusUpdate update)
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

                    case ServerStatusWatcher.ServerStatus.Unknown:
                        this.Status = ServerStatus.Unknown;
                        this.Steam = SteamStatus.Unknown;
                        break;

                    case ServerStatusWatcher.ServerStatus.Running:
                        this.Status = ServerStatus.Running;
                        this.Steam = SteamStatus.WaitingForPublication;
                        break;

                    case ServerStatusWatcher.ServerStatus.Published:
                        this.Status = ServerStatus.Running;
                        this.Steam = SteamStatus.Available;
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

                    // set the player count using the players list, as this should only contain the current valid players.
                    this.Players = update.Players.Count;
                    this.MaxPlayers = update.ServerInfo.MaxPlayers;
                }

                this.serverProcess = update.Process;

                StatusUpdate?.Invoke(this, EventArgs.Empty);
            }).DoNotWait();
        }

        private void RegisterForUpdates()
        {
            if (this.updateRegistration == null)
            {
                IPEndPoint localServerQueryEndPoint;
                IPEndPoint steamServerQueryEndPoint;
                GetServerEndpoints(out localServerQueryEndPoint, out steamServerQueryEndPoint);
                this.updateRegistration = ServerStatusWatcher.Instance.RegisterForUpdates(this.ProfileSnapshot.InstallDirectory, localServerQueryEndPoint, steamServerQueryEndPoint, ProcessStatusUpdate);
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
        

        public Task StartAsync()
        {
            if(!System.Environment.Is64BitOperatingSystem)
            {
                MessageBox.Show("ARK: Survival Evolved(tm) Server requires a 64-bit operating system to run.  Your operating system is 32-bit and therefore the Ark Server Manager cannot start the server.  You may still load and save profiles and settings files for use on other machines.", "64-bit OS Required", MessageBoxButton.OK, MessageBoxImage.Error);
                return TaskUtils.FinishedTask;
            }

            switch(this.Status)
            {
                case ServerStatus.Running:
                case ServerStatus.Initializing:
                case ServerStatus.Stopping:
                    Debug.WriteLine("Server {0} already running.", this.ProfileSnapshot.ProfileName);
                    return TaskUtils.FinishedTask;
            }

            UnregisterForUpdates();
            this.Status = ServerStatus.Initializing;
            
            var serverExe = GetServerExe();
            var serverArgs = this.ProfileSnapshot.ServerArgs;

            if (Config.Default.ManageFirewallAutomatically)
            {
                var ports = new List<int>() { this.ProfileSnapshot.QueryPort, this.ProfileSnapshot.ServerConnectionPort };
                if (this.ProfileSnapshot.RCONEnabled)
                {
                    ports.Add(this.ProfileSnapshot.RCONPort);
                }

                if(this.ProfileSnapshot.UseRawSockets)
                {
                    ports.Add(this.ProfileSnapshot.ServerConnectionPort + 1);
                }

                if (!FirewallUtils.EnsurePortsOpen(serverExe, ports.ToArray(), "ARK Server: " + this.ProfileSnapshot.ServerName))
                {
                    var result = MessageBox.Show("Failed to automatically set firewall rules.  If you are running custom firewall software, you may need to set your firewall rules manually.  You may turn off automatic firewall management in Settings.\r\n\r\nWould you like to continue running the server anyway?", "Automatic Firewall Management Error", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.No)
                    {
                        return TaskUtils.FinishedTask;
                    }
                }
            }

            try
            {
                var startInfo = new ProcessStartInfo()
                {
                    FileName = serverExe,
                    Arguments = serverArgs,
                };

                var process = Process.Start(startInfo);
                process.EnableRaisingEvents = true;
            }
            catch (Win32Exception ex)
            {
                throw new FileNotFoundException(String.Format("Unable to find {0} at {1}.  Server Install Directory: {2}", Config.Default.ServerExe, serverExe, this.ProfileSnapshot.InstallDirectory), serverExe, ex);
            }
            finally
            {
                RegisterForUpdates();
            }
            
            return TaskUtils.FinishedTask;            
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

                        if (this.serverProcess != null)
                        {
                            this.Status = ServerStatus.Stopping;
                            this.Steam = SteamStatus.Unavailable;

                            await ProcessUtils.SendStop(this.serverProcess);
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

        public async Task<bool> UpgradeAsync(CancellationToken cancellationToken, bool updateServer, bool validate, bool updateMods, ProgressDelegate progressCallback)
        {
            if (updateServer && !Environment.Is64BitOperatingSystem)
            {
                var result = MessageBox.Show("The ARK server requires a 64-bit operating system to run. Your operating system is 32-bit and therefore the Ark Server Manager will be unable to start the server, but you may still install it or load and save profiles and settings files for use on other machines.\r\n\r\nDo you wish to continue?", "64-bit OS Required", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    return false;
                }
            }

            try
            {
                await StopAsync();

                this.Status = ServerStatus.Updating;

                // Run the SteamCMD to install the server
                var steamCmdFile = Updater.GetSteamCmdFile();
                if (string.IsNullOrWhiteSpace(steamCmdFile) || !File.Exists(steamCmdFile))
                {
                    progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} ***********************************");
                    progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} ERROR: SteamCMD could not be found. Expected location is {steamCmdFile}");
                    progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} ***********************************");
                    return false;
                }

                // record the start time of the process, this is used to determine if any files changed in the download process.
                var startTime = DateTime.Now;

                var gotNewVersion = false;
                var downloadSuccessful = false;
                var success = false;

                if (updateServer)
                {
                    // *********************
                    // Server Update Section
                    // *********************

                    downloadSuccessful = !Config.Default.SteamCmdRedirectOutput;
                    DataReceivedEventHandler serverOutputHandler = (s, e) =>
                    {
                        var dataValue = e.Data ?? string.Empty;
                        progressCallback?.Invoke(0, dataValue);
                        if (!gotNewVersion && dataValue.Contains("downloading,"))
                        {
                            gotNewVersion = true;
                        }
                        if (dataValue.StartsWith("Success!"))
                        {
                            downloadSuccessful = true;
                        }
                    };

                    progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} Started server update.\r\n");

                    var steamCmdInstallServerArgsFormat = this.ProfileSnapshot.SotFServer ? Config.Default.SteamCmdInstallServerArgsFormat_SotF : Config.Default.SteamCmdInstallServerArgsFormat;
                    var steamCmdArgs = String.Format(steamCmdInstallServerArgsFormat, this.ProfileSnapshot.InstallDirectory, validate ? "validate" : string.Empty);

                    success = await ServerUpdater.UpgradeServerAsync(steamCmdFile, steamCmdArgs, this.ProfileSnapshot.InstallDirectory, Config.Default.SteamCmdRedirectOutput ? serverOutputHandler : null, cancellationToken);
                    if (success && downloadSuccessful)
                    {
                        progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} Finished server update.");

                        if (Directory.Exists(this.ProfileSnapshot.InstallDirectory))
                        {
                            if (!Config.Default.SteamCmdRedirectOutput)
                                // check if any of the server files have changed.
                                gotNewVersion = ServerApp.HasNewServerVersion(this.ProfileSnapshot.InstallDirectory, startTime);

                            progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} New server version - {gotNewVersion.ToString().ToUpperInvariant()}.");

                            //// update the version number of the server.
                            //var versionFile = Path.Combine(this.ProfileSnapshot.InstallDirectory, Config.Default.VersionFile);
                            //this.Version = Updater.GetServerVersion(versionFile);

                            //progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} Server version: {this.Version}\r\n");
                        }
                    }
                    else
                    {
                        success = false;
                        progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} ****************************");
                        progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} ERROR: Failed server update.");
                        progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} ****************************\r\n");

                        if (Config.Default.SteamCmdRedirectOutput)
                            progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} If the server update keeps failing try disabling the '{_globalizer.GetResourceString("GlobalSettings_SteamCmdRedirectOutputLabel")}' option in the settings window.\r\n");
                    }
                }
                else
                    success = true;

                if (success)
                {
                    if (updateMods)
                    {
                        // ******************
                        // Mod Update Section
                        // ******************

                        // build a list of mods to be processed
                        var modIdList = new List<string>();
                        if (!string.IsNullOrWhiteSpace(this.ProfileSnapshot.ServerMapModId))
                            modIdList.Add(this.ProfileSnapshot.ServerMapModId);
                        if (!string.IsNullOrWhiteSpace(this.ProfileSnapshot.TotalConversionModId))
                            modIdList.Add(this.ProfileSnapshot.TotalConversionModId);
                        modIdList.AddRange(this.ProfileSnapshot.ServerModIds);

                        modIdList = ModUtils.ValidateModList(modIdList);

                        // get the details of the mods to be processed.
                        var modDetails = SteamUtils.GetSteamModDetails(modIdList);
                        if (modDetails != null)
                        {
                            for (var index = 0; index < modIdList.Count; index++)
                            {
                                var modId = modIdList[index];
                                var modSuccess = false;
                                gotNewVersion = false;
                                downloadSuccessful = false;

                                progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} Started processing mod {index + 1} of {modIdList.Count}.");
                                progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} Mod {modId}.");

                                // check if the steam information was downloaded
                                var modDetail = modDetails.publishedfiledetails?.FirstOrDefault(m => m.publishedfileid.Equals(modId, StringComparison.OrdinalIgnoreCase));
                                if (modDetail != null)
                                {
                                    progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} {modDetail.title ?? string.Empty}.\r\n");

                                    var modCachePath = ModUtils.GetModCachePath(modId, this.ProfileSnapshot.SotFServer);
                                    var cacheTimeFile = ModUtils.GetLatestModCacheTimeFile(modId, this.ProfileSnapshot.SotFServer);
                                    var modPath = ModUtils.GetModPath(this.ProfileSnapshot.InstallDirectory, modId);
                                    var modTimeFile = ModUtils.GetLatestModTimeFile(this.ProfileSnapshot.InstallDirectory, modId);

                                    var modCacheLastUpdated = 0;
                                    var downloadMod = true;
                                    var copyMod = true;

                                    if (downloadMod)
                                    {
                                        // check if the mod needs to be downloaded, or force the download.
                                        if (Config.Default.ServerUpdate_ForceUpdateMods)
                                        {
                                            progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} Forcing mod download - ASM setting is TRUE.");
                                        }
                                        else
                                        {
                                            // check if the mod detail record is valid (private mod).
                                            if (modDetail.time_updated <= 0)
                                            {
                                                progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} Forcing mod download - mod is private.");
                                            }
                                            else
                                            {
                                                modCacheLastUpdated = ModUtils.GetModLatestTime(cacheTimeFile);
                                                if (modCacheLastUpdated <= 0)
                                                {
                                                    progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} Forcing mod download - mod cache is not versioned.");
                                                }
                                                else
                                                {
                                                    var steamLastUpdated = modDetail.time_updated;
                                                    if (steamLastUpdated <= modCacheLastUpdated)
                                                    {
                                                        progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} Skipping mod download - mod cache has the latest version.");
                                                        downloadMod = false;
                                                    }
                                                }
                                            }
                                        }

                                        if (downloadMod)
                                        {
                                            // mod will be downloaded
                                            downloadSuccessful = !Config.Default.SteamCmdRedirectOutput;
                                            DataReceivedEventHandler modOutputHandler = (s, e) =>
                                            {
                                                var dataValue = e.Data ?? string.Empty;
                                                progressCallback?.Invoke(0, dataValue);
                                                if (dataValue.StartsWith("Success."))
                                                {
                                                    downloadSuccessful = true;
                                                }
                                            };

                                            progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} Started mod download.\r\n");

                                            var steamCmdArgs = string.Empty;
                                            if (this.ProfileSnapshot.SotFServer)
                                            {
                                                if (Config.Default.SteamCmd_UseAnonymousCredentials)
                                                    steamCmdArgs = string.Format(Config.Default.SteamCmdInstallModArgsFormat_SotF, Config.Default.SteamCmd_AnonymousUsername, modId);
                                                else
                                                    steamCmdArgs = string.Format(Config.Default.SteamCmdInstallModArgsFormat_SotF, Config.Default.SteamCmd_Username, modId);
                                            }
                                            else
                                            {
                                                if (Config.Default.SteamCmd_UseAnonymousCredentials)
                                                    steamCmdArgs = string.Format(Config.Default.SteamCmdInstallModArgsFormat, Config.Default.SteamCmd_AnonymousUsername, modId);
                                                else
                                                    steamCmdArgs = string.Format(Config.Default.SteamCmdInstallModArgsFormat, Config.Default.SteamCmd_Username, modId);
                                            }

                                            modSuccess = await ServerUpdater.UpgradeModsAsync(steamCmdFile, steamCmdArgs, Config.Default.SteamCmdRedirectOutput ? modOutputHandler : null, cancellationToken);
                                            if (modSuccess && downloadSuccessful)
                                            {
                                                progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} Finished mod download.");
                                                copyMod = true;

                                                if (Directory.Exists(modCachePath))
                                                {
                                                    // check if any of the mod files have changed.
                                                    gotNewVersion = new DirectoryInfo(modCachePath).GetFiles("*.*", SearchOption.AllDirectories).Any(file => file.LastWriteTime >= startTime);

                                                    progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} New mod version - {gotNewVersion.ToString().ToUpperInvariant()}.");

                                                    var steamLastUpdated = modDetail.time_updated.ToString();
                                                    if (modDetail.time_updated <= 0)
                                                    {
                                                        // get the version number from the steamcmd workshop file.
                                                        steamLastUpdated = ModUtils.GetSteamWorkshopLatestTime(ModUtils.GetSteamWorkshopFile(this.ProfileSnapshot.SotFServer), modId).ToString();
                                                    }

                                                    // update the last updated file with the steam updated time.
                                                    File.WriteAllText(cacheTimeFile, steamLastUpdated);

                                                    progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} Mod Cache version: {steamLastUpdated}\r\n");
                                                }
                                            }
                                            else
                                            {
                                                modSuccess = false;
                                                progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} ***************************");
                                                progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} ERROR: Mod download failed.");
                                                progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} ***************************\r\n");

                                                if (Config.Default.SteamCmdRedirectOutput)
                                                    progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} If the mod update keeps failing try disabling the '{_globalizer.GetResourceString("GlobalSettings_SteamCmdRedirectOutputLabel")}' option in the settings window.\r\n");
                                                copyMod = false;
                                            }
                                        }
                                        else
                                            modSuccess = true;
                                    }
                                    else
                                        modSuccess = true;

                                    if (copyMod)
                                    {
                                        // check if the mod needs to be copied, or force the copy.
                                        if (Config.Default.ServerUpdate_ForceCopyMods)
                                        {
                                            progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} Forcing mod copy - ASM setting is TRUE.");
                                        }
                                        else
                                        {
                                            // check the mod version against the cache version.
                                            var modLastUpdated = ModUtils.GetModLatestTime(modTimeFile);
                                            if (modLastUpdated <= 0)
                                            {
                                                progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} Forcing mod copy - mod is not versioned.");
                                            }
                                            else
                                            {
                                                modCacheLastUpdated = ModUtils.GetModLatestTime(cacheTimeFile);
                                                if (modCacheLastUpdated <= modLastUpdated)
                                                {
                                                    progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} Skipping mod copy - mod has the latest version.");
                                                    progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} Mod version: {modLastUpdated}");
                                                    copyMod = false;
                                                }
                                            }
                                        }

                                        if (copyMod)
                                        {
                                            try
                                            {
                                                if (Directory.Exists(modCachePath))
                                                {
                                                    progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} Started mod copy.");
                                                    ModUtils.CopyMod(modCachePath, modPath, modId, null);
                                                    progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} Finished mod copy.");

                                                    var modLastUpdated = ModUtils.GetModLatestTime(modTimeFile);
                                                    progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} Mod version: {modLastUpdated}");
                                                }
                                                else
                                                {
                                                    modSuccess = false;
                                                    progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} ****************************************************");
                                                    progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} ERROR: Mod cache was not found, mod was not updated.");
                                                    progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} ****************************************************");
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                modSuccess = false;
                                                progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} ***********************");
                                                progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} ERROR: Failed mod copy.\r\n{ex.Message}");
                                                progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} ***********************");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // no steam information downloaded, display an error, mod might no longer be available
                                    progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} *******************************************************************");
                                    progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} ERROR: Mod cannot be updated, unable to download steam information.");
                                    progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} *******************************************************************");
                                }

                                if (!modSuccess)
                                    success = false;
                                progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} Finished processing mod {modId}.\r\n");
                            }
                        }
                        else
                        {
                            success = false;
                            // no steam information downloaded, display an error
                            progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} ********************************************************************");
                            progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} ERROR: Mods cannot be updated, unable to download steam information.");
                            progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} ********************************************************************\r\n");
                        }
                    }
                }
                else
                {
                    if (updateServer && updateMods)
                    {
                        progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} ***********************************************************");
                        progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} ERROR: Mods were not processed as server update had errors.");
                        progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} ***********************************************************\r\n");
                    }
                }

                progressCallback?.Invoke(0, $"{Updater.OUTPUT_PREFIX} Finished upgrade process.");
                return success;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
            finally
            {
                this.Status = ServerStatus.Stopped;
            }
        }
    }
}
