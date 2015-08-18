using QueryMaster;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ARK_Server_Manager.Lib
{
    using NLog;
    using StatusCallback = Action<IAsyncDisposable, ARK_Server_Manager.Lib.ServerStatusWatcher.ServerStatusUpdate>;

    public class ServerStatusWatcher
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private const int LocalStatusQueryDelay = 3500; // milliseconds

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
            /// The server process was found, but the server is not responding on its port
            /// </summary>
            Initializing,

            /// <summary>
            /// The server is responding on its port
            /// </summary>
            Running,
            
            /// <summary>
            /// The server appears on the Steam Master servers
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

            public async Task DisposeAsync()
            {
                await UnregisterAction();
            }
        }

        private readonly List<ServerStatusUpdateRegistration> serverRegistrations = new List<ServerStatusUpdateRegistration>();
        private readonly ActionBlock<Func<Task>> eventQueue;

        private ServerStatusWatcher()
        {
            eventQueue = new ActionBlock<Func<Task>>(async f => await f.Invoke(), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });
            eventQueue.Post(DoLocalUpdate);
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

        public IAsyncDisposable RegisterForUpdates(string installDirectory, IPEndPoint localEndpoint, IPEndPoint steamEndpoint, Action<IAsyncDisposable, ServerStatusUpdate> updateCallback)
        {
            var registration = new ServerStatusUpdateRegistration 
            { 
                InstallDirectory = installDirectory,
                LocalEndpoint = localEndpoint, 
                SteamEndpoint = steamEndpoint, 
                UpdateCallback = updateCallback
            };

            registration.UnregisterAction = async () => 
                {
                    var tcs = new TaskCompletionSource<bool>();
                    eventQueue.Post(() => 
                    {
                        if(serverRegistrations.Contains(registration))
                        {
                            logger.Debug("Removing registration for L:{0} S:{1}", registration.LocalEndpoint, registration.SteamEndpoint);
                            serverRegistrations.Remove(registration);
                        }
                        tcs.TrySetResult(true);
                        return Task.FromResult(true);
                    });

                    await tcs.Task;
                };

            eventQueue.Post(() =>
                {
                    if(!serverRegistrations.Contains(registration))
                    {
                        logger.Debug("Adding registration for L:{0} S:{1}", registration.LocalEndpoint, registration.SteamEndpoint);
                        serverRegistrations.Add(registration);
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
                    var commandLine = ServerUpdater.GetCommandLineForProcess(process.Id);

                    if (commandLine.Contains(updateContext.InstallDirectory) && commandLine.Contains(Config.Default.ServerExe))
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
                logger.Debug("Exception while checking process status: {0}\n{1}", ex.Message, ex.StackTrace);
            }

            return ServerProcessStatus.Stopped;
        }

        private async Task DoLocalUpdate()
        {
            try
            {
                foreach (var registration in this.serverRegistrations)
                {
                    ServerStatusUpdate statusUpdate = new ServerStatusUpdate();
                    try
                    {
                        logger.Debug("Start: {0}", registration.LocalEndpoint);
                        statusUpdate = await GenerateServerStatusUpdateAsync(registration);
                        
                        PostServerStatusUpdate(registration, registration.UpdateCallback, statusUpdate);
                    }
                    catch (Exception ex)
                    {
                        // We don't want to stop other registration queries or break the ActionBlock
                        logger.Debug("Exception in local update: {0} \n {1}", ex.Message, ex.StackTrace);
                        Debugger.Break();
                    }
                    finally
                    {
                        logger.Debug("End: {0}: {1}", registration.LocalEndpoint, statusUpdate.Status);
                    }
                }
            }
            finally
            {
                Task.Delay(LocalStatusQueryDelay).ContinueWith(_ => eventQueue.Post(DoLocalUpdate)).DoNotWait();
            }
            return;
        }

        private void PostServerStatusUpdate(ServerStatusUpdateRegistration registration, StatusCallback callback, ServerStatusUpdate statusUpdate)
        {
            eventQueue.Post(() =>
            {
                if (this.serverRegistrations.Contains(registration))
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

        private static async Task<ServerStatusUpdate> GenerateServerStatusUpdateAsync(ServerStatusUpdateRegistration registration)
        {
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

                case ServerProcessStatus.Running:
                    break;

                default:
                    Debugger.Break();
                    break;
            }

            ServerStatus currentStatus = ServerStatus.Initializing;
            //
            // If the process was running do we then perform network checks.
            //
            ServerInfo localInfo;
            ReadOnlyCollection<Player> players;
            localInfo = GetLocalNetworkStatus(registration.LocalEndpoint, out players);

            if(localInfo != null)
            {
                currentStatus = ServerStatus.Running;

                //
                // Now that it's running, we can check the publication status.
                //
                logger.Debug("Checking server public status at {0}", registration.SteamEndpoint);
                var serverInfo = await NetworkUtils.GetServerNetworkInfo(registration.SteamEndpoint);
                if (serverInfo != null)
                {                    
                    currentStatus = ServerStatus.Published;
                }
                else
                {
                    logger.Debug("No public status returned for {0}", registration.SteamEndpoint);
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
            var server = ServerQuery.GetServerInstance(EngineType.Source, specificEndpoint);
            ServerInfo serverInfo = null;
            players = null;
            try
            {
                serverInfo = server.GetInfo();
            }
            catch (SocketException ex)
            {
                logger.Debug("GetInfo failed: {0}: {1}", specificEndpoint, ex.Message);
                // Common when the server is unreachable.  Ignore it.
            }

            if (serverInfo != null)
            {
                try
                {
                    players = server.GetPlayers();
                }
                catch (SocketException)
                {
                    // Common when the server is unreachable.  Ignore it.
                }
            }

            return serverInfo;
        }
    }
}
