using QueryMaster;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ARK_Server_Manager.Lib
{
    using NLog;
    using StatusCallback = Action<IAsyncDisposable, ServerStatusWatcher.ServerStatusUpdate>;

    public class ServerStatusWatcher
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private const int LOCAL_STATUS_QUERY_DELAY = 5000; // milliseconds
        private const int REMOTE_STATUS_QUERY_DELAY = 60000; // milliseconds
        private const int REMOTE_CALL_QUERY_DELAY = 3600000; // milliseconds

        private enum ServerProcessStatus
        {
            /// <summary>
            /// The server binary could not be found
            /// </summary>
            NotInstalled,

            /// <summary>
            /// The server binary was found, but the process was not.
            /// </summary>
            Stopped,

            /// <summary>
            /// The server binary was found, the process was found, but no permissions to access the process.
            /// </summary>
            Unknown,

            /// <summary>
            /// The server process was found
            /// </summary>
            Running,
        }

        public enum ServerStatus
        {
            /// <summary>
            /// The server binary couldnot be found.
            /// </summary>
            NotInstalled,

            /// <summary>
            /// The server binary was found, but the process was not
            /// </summary>
            Stopped,

            /// <summary>
            /// The server binary was found, the process was found, but no permissions to access the process.
            /// </summary>
            Unknown,

            /// <summary>
            /// The server process was found, but the server is not responding on its port
            /// </summary>
            Initializing,

            /// <summary>
            /// The server is responding locally on its port, a local check was made
            /// </summary>
            RunningLocalCheck,

            /// <summary>
            /// The server is responding locally on its port, a public check was made
            /// </summary>
            RunningExternalCheck,

            /// <summary>
            /// The server is responding externally on its port
            /// </summary>
            Published,
        }

        public struct ServerStatusUpdate
        {
            public Process Process;
            public ServerStatus Status;
            public ServerInfo ServerInfo;
            public ReadOnlyCollection<Player> Players;
        }

        private class ServerStatusUpdateRegistration  : IAsyncDisposable
        {
            public string InstallDirectory;
            public IPEndPoint LocalEndpoint;
            public IPEndPoint SteamEndpoint;
            public StatusCallback UpdateCallback;
            public Func<Task> UnregisterAction;

            public string AsmId;
            public string ProfileId;

            public async Task DisposeAsync()
            {
                await UnregisterAction();
            }
        }

        private readonly List<ServerStatusUpdateRegistration> _serverRegistrations = new List<ServerStatusUpdateRegistration>();
        private readonly ActionBlock<Func<Task>> _eventQueue;
        private readonly Dictionary<string, DateTime> _lastExternalCallQuery = new Dictionary<string, DateTime>();
        private readonly Dictionary<string, DateTime> _lastExternalStatusQuery = new Dictionary<string, DateTime>();

        private ServerStatusWatcher()
        {
            _eventQueue = new ActionBlock<Func<Task>>(async f => await f.Invoke(), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });
            _eventQueue.Post(DoLocalUpdate);
        }

        static ServerStatusWatcher()
        {
            ServerStatusWatcher.Instance = new ServerStatusWatcher();
        }

        public static ServerStatusWatcher Instance
        {
            get;
            private set;
        }

        public IAsyncDisposable RegisterForUpdates(string installDirectory, string profileId, IPEndPoint localEndpoint, IPEndPoint steamEndpoint, Action<IAsyncDisposable, ServerStatusUpdate> updateCallback)
        {
            var registration = new ServerStatusUpdateRegistration 
            { 
                AsmId = Config.Default.ASMUniqueKey,
                InstallDirectory = installDirectory,
                ProfileId = profileId,
                LocalEndpoint = localEndpoint, 
                SteamEndpoint = steamEndpoint, 
                UpdateCallback = updateCallback,
            };

            registration.UnregisterAction = async () => 
                {
                    var tcs = new TaskCompletionSource<bool>();
                    _eventQueue.Post(() => 
                    {
                        if(_serverRegistrations.Contains(registration))
                        {
                            Logger.Debug("Removing registration for L:{0} S:{1}", registration.LocalEndpoint, registration.SteamEndpoint);
                            _serverRegistrations.Remove(registration);
                        }
                        tcs.TrySetResult(true);
                        return Task.FromResult(true);
                    });

                    await tcs.Task;
                };

            _eventQueue.Post(() =>
                {
                    if (!_serverRegistrations.Contains(registration))
                    {
                        Logger.Debug("Adding registration for L:{0} S:{1}", registration.LocalEndpoint, registration.SteamEndpoint);
                        _serverRegistrations.Add(registration);

                        var registrationKey = registration.SteamEndpoint.ToString();
                        _lastExternalCallQuery[registrationKey] = DateTime.MinValue;
                        _lastExternalStatusQuery[registrationKey] = DateTime.MinValue;
                    }
                    return Task.FromResult(true);
                }
            );

            return registration;
        }

        private static ServerProcessStatus GetServerProcessStatus(ServerStatusUpdateRegistration updateContext, out Process serverProcess)
        {
            serverProcess = null;
            if (String.IsNullOrWhiteSpace(updateContext.InstallDirectory))
            {
                return ServerProcessStatus.NotInstalled;
            }

            var serverExePath = Path.Combine(updateContext.InstallDirectory, Config.Default.ServerBinaryRelativePath, Config.Default.ServerExe);
            if(!File.Exists(serverExePath))
            {
                return ServerProcessStatus.NotInstalled;
            }

            //
            // The server appears to be installed, now determine if it is running or stopped.
            //
            try
            {
                foreach (var process in Process.GetProcessesByName(Config.Default.ServerProcessName))
                {
                    var commandLine = ProcessUtils.GetCommandLineForProcess(process.Id);

                    if (commandLine != null && commandLine.Contains(updateContext.InstallDirectory) && commandLine.Contains(Config.Default.ServerExe))
                    {
                        // Does this match our server exe and port?
                        var serverArgMatch = String.Format(Config.Default.ServerCommandLineArgsMatchFormat, updateContext.LocalEndpoint.Port);
                        if (commandLine.Contains(serverArgMatch))
                        {
                            // Was an IP set on it?
                            var anyIpArgMatch = String.Format(Config.Default.ServerCommandLineArgsIPMatchFormat, String.Empty);
                            if (commandLine.Contains(anyIpArgMatch))
                            {
                                // If we have a specific IP, check for it.
                                var ipArgMatch = String.Format(Config.Default.ServerCommandLineArgsIPMatchFormat, updateContext.LocalEndpoint.Address.ToString());
                                if (!commandLine.Contains(ipArgMatch))
                                {
                                    // Specific IP set didn't match
                                    continue;
                                }

                                // Specific IP matched
                            }

                            // Either specific IP matched or no specific IP was set and we will claim this is ours.

                            process.EnableRaisingEvents = true;
                            if (process.HasExited)
                            {
                                return ServerProcessStatus.Stopped;
                            }

                            serverProcess = process;
                            return ServerProcessStatus.Running;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Debug("Exception while checking process status: {0}\n{1}", ex.Message, ex.StackTrace);
            }

            return ServerProcessStatus.Stopped;
        }

        private async Task DoLocalUpdate()
        {
            try
            {
                foreach (var registration in this._serverRegistrations)
                {
                    ServerStatusUpdate statusUpdate = new ServerStatusUpdate();
                    try
                    {
                        Logger.Debug("Start: {0}", registration.LocalEndpoint);
                        statusUpdate = await GenerateServerStatusUpdateAsync(registration);
                        
                        PostServerStatusUpdate(registration, registration.UpdateCallback, statusUpdate);
                    }
                    catch (Exception ex)
                    {
                        // We don't want to stop other registration queries or break the ActionBlock
                        Logger.Debug("Exception in local update: {0} \n {1}", ex.Message, ex.StackTrace);
                        Debugger.Break();
                    }
                    finally
                    {
                        Logger.Debug("End: {0}: {1}", registration.LocalEndpoint, statusUpdate.Status);
                    }
                }
            }
            finally
            {
                Task.Delay(LOCAL_STATUS_QUERY_DELAY).ContinueWith(_ => _eventQueue.Post(DoLocalUpdate)).DoNotWait();
            }
        }

        private void PostServerStatusUpdate(ServerStatusUpdateRegistration registration, StatusCallback callback, ServerStatusUpdate statusUpdate)
        {
            _eventQueue.Post(() =>
            {
                if (this._serverRegistrations.Contains(registration))
                {
                    try
                    {
                        callback(registration, statusUpdate);
                    }
                    catch (Exception ex)
                    {
                        DebugUtils.WriteFormatThreadSafeAsync("Exception during local status update callback: {0}\n{1}", ex.Message, ex.StackTrace).DoNotWait();
                    }
                }
                return TaskUtils.FinishedTask;
            });
        }

        private async Task<ServerStatusUpdate> GenerateServerStatusUpdateAsync(ServerStatusUpdateRegistration registration)
        {
            var registrationKey = registration.SteamEndpoint.ToString();

            //
            // First check the process status
            //
            Process process;
            var processStatus = GetServerProcessStatus(registration, out process);
            switch(processStatus)
            {
                case ServerProcessStatus.NotInstalled:
                    return new ServerStatusUpdate { Status = ServerStatus.NotInstalled };

                case ServerProcessStatus.Stopped:
                    return new ServerStatusUpdate { Status = ServerStatus.Stopped };

                case ServerProcessStatus.Unknown:
                    return new ServerStatusUpdate { Status = ServerStatus.Unknown };

                case ServerProcessStatus.Running:
                    break;

                default:
                    Debugger.Break();
                    break;
            }

            var currentStatus = ServerStatus.Initializing;

            //
            // If the process was running do we then perform network checks.
            //
            Logger.Debug("Checking server local status at {0}", registration.LocalEndpoint);

            // get the server information direct from the server using local connection.
            ReadOnlyCollection<Player> players;
            var localInfo = GetLocalNetworkStatus(registration.LocalEndpoint, out players);

            if (localInfo != null)
            {
                currentStatus = ServerStatus.RunningLocalCheck;

                //
                // Now that it's running, we can check the publication status.
                //
                Logger.Debug("Checking server public status at {0}", registration.SteamEndpoint);

                // get the server information direct from the server using public connection.
                var serverStatus = NetworkUtils.CheckServerStatusDirect(registration.SteamEndpoint);
                // check if the server returned the information.
                if (!serverStatus)
                {
                    // server did not return any information
                    var lastExternalStatusQuery = _lastExternalStatusQuery.ContainsKey(registrationKey) ? _lastExternalStatusQuery[registrationKey] : DateTime.MinValue;
                    if (DateTime.Now >= lastExternalStatusQuery.AddMilliseconds(REMOTE_STATUS_QUERY_DELAY))
                    {
                        currentStatus = ServerStatus.RunningExternalCheck;

                        // get the server information direct from the server using external connection.
                        serverStatus = await NetworkUtils.CheckServerStatusViaAPI(registration.SteamEndpoint);

                        _lastExternalStatusQuery[registrationKey] = DateTime.Now;
                    }
                }

                var lastExternalCallQuery = _lastExternalCallQuery.ContainsKey(registrationKey) ? _lastExternalCallQuery[registrationKey] : DateTime.MinValue;
                if (lastExternalCallQuery == DateTime.MinValue)
                {
                    // perform a server call to the web api.
                    await NetworkUtils.PerformServerCallToAPI(registration.SteamEndpoint, registration.AsmId, registration.ProfileId);

                    _lastExternalCallQuery[registrationKey] = DateTime.Now;
                }

                // check if the server returned the information.
                if (serverStatus)
                {                    
                    currentStatus = ServerStatus.Published;
                }
                else
                {
                    Logger.Debug("No public status returned for {0}", registration.SteamEndpoint);
                }
            }

            var statusUpdate = new ServerStatusUpdate
            {
                Process = process,
                Status = currentStatus,
                ServerInfo = localInfo,
                Players = players
            };

            return await Task.FromResult(statusUpdate);
        }

        private static ServerInfo GetLocalNetworkStatus(IPEndPoint specificEndpoint, out ReadOnlyCollection<Player> players)
        {
            players = null;

            ServerInfo serverInfo = null;
            try
            {
                using (var server = ServerQuery.GetServerInstance(EngineType.Source, specificEndpoint))
                {
                    serverInfo = server.GetInfo();
                    players = server.GetPlayers();
                }

                // return the list of valid players only.
                if (players != null)
                    players = new ReadOnlyCollection<Player>(players.Where(record => !string.IsNullOrWhiteSpace(record.Name)).ToList());
            }
            catch (SocketException ex)
            {
                Logger.Debug("GetInfo failed: {0}: {1}", specificEndpoint, ex.Message);
                // Common when the server is unreachable.  Ignore it.
            }

            return serverInfo;
        }
    }
}
