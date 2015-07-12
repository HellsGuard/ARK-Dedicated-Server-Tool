using QueryMaster;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ARK_Server_Manager.Lib
{
    public class ServerStatusWatcher
    {
        private const int SteamStatusQueryDelay = 10000; // milliseconds
        private const int LocalStatusQueryDelay = 5000; // milliseconds

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
            Running
        }

        public struct ServerStatusUpdate
        {
            public Process Process;
            public ServerStatus Status;
            public ServerInfo ServerInfo;
            public ReadOnlyCollection<Player> Players;
        }

        private class ServerStatusUpdateRegistration : IDisposable
        {
            public string InstallDirectory;
            public IPEndPoint LocalEndpoint;
            public IPEndPoint SteamEndpoint;
            public Action<ServerStatusUpdate> LocalCallback;
            public Action<ServerStatusUpdate> SteamCallback;
            public Action DisposeAction;

            public void Dispose()
            {
                DisposeAction.Invoke();
            }
        }

        private readonly List<ServerStatusUpdateRegistration> serverRegistrations = new List<ServerStatusUpdateRegistration>();
        private readonly ActionBlock<Func<Task>> eventQueue;

        private ServerStatusWatcher()
        {
            eventQueue = new ActionBlock<Func<Task>>(async f => await f.Invoke(), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });
            eventQueue.Post(DoLocalUpdate);
            eventQueue.Post(DoSteamUpdate);
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

        public IDisposable RegisterForUpdates(string installDirectory, IPEndPoint localEndpoint, IPEndPoint steamEndpoint, Action<ServerStatusUpdate> localUpdateAction, Action<ServerStatusUpdate> steamUpdateAction)
        {
            var registration = new ServerStatusUpdateRegistration 
            { 
                InstallDirectory = installDirectory,
                LocalEndpoint = localEndpoint, 
                SteamEndpoint = steamEndpoint, 
                LocalCallback = localUpdateAction,
                SteamCallback = steamUpdateAction
            };

            registration.DisposeAction = () => 
                {
                    var tcs = new TaskCompletionSource<bool>();
                    eventQueue.Post(() => 
                    {
                        if(serverRegistrations.Contains(registration))
                        {
                            serverRegistrations.Remove(registration);
                        }
                        tcs.TrySetResult(true);
                        return Task.FromResult(true);
                    });

                    tcs.Task.Wait();
                };

            eventQueue.Post(() =>
                {
                    if(!serverRegistrations.Contains(registration))
                    {
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
                    var commandLineBuilder = new StringBuilder();

                    using (var searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
                    {
                        foreach (var @object in searcher.Get())
                        {
                            commandLineBuilder.Append(@object["CommandLine"] + " ");
                        }
                    }

                    var commandLine = commandLineBuilder.ToString();

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
                Debugger.Break();
            }

            return ServerProcessStatus.Stopped;
        }

        private async Task DoLocalUpdate()
        {
            Debug.WriteLine("Watching local servers.");
            try
            {
                foreach (var registration in this.serverRegistrations)
                {
                    try
                    {
                        var endpoint = registration.LocalEndpoint;
                        var callback = registration.LocalCallback;

                        await GenerateAndPostServerStatusUpdateAsync(registration, endpoint, callback);
                    }
                    catch (Exception ex)
                    {
                        // We don't want to stop other registration queries or break the ActionBlock
                        Debugger.Break();
                    }
                }
            }
            finally
            {
                Task.Delay(LocalStatusQueryDelay).ContinueWith(_ => eventQueue.Post(DoLocalUpdate)).DoNotWait();
            }
            return;
        }

        private async Task GenerateAndPostServerStatusUpdateAsync(ServerStatusUpdateRegistration registration, IPEndPoint specificEndpoint, Action<ServerStatusUpdate> callback)
        {
            var statusUpdate = await GenerateServerStatusUpdateAsync(registration, specificEndpoint);
            PostServerStatusUpdate(registration, callback, statusUpdate);
            return;
        }

        private void PostServerStatusUpdate(ServerStatusUpdateRegistration registration, Action<ServerStatusUpdate> callback, ServerStatusUpdate statusUpdate)
        {
            eventQueue.Post(() =>
            {
                if (this.serverRegistrations.Contains(registration))
                {
                    try
                    {
                        callback(statusUpdate);
                    }
                    catch (Exception ex)
                    {
                        DebugUtils.WriteFormatThreadSafeAsync("Exception during local status update callback: {0}\n{1}", ex.Message, ex.StackTrace).DoNotWait();
                    }
                }
                return TaskUtils.FinishedTask;
            });
        }

        private static async Task<ServerStatusUpdate> GenerateServerStatusUpdateAsync(ServerStatusUpdateRegistration registration, IPEndPoint specificEndpoint)
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

            //
            // Only if the process was running do we then perform network checks.
            //
            var server = ServerQuery.GetServerInstance(EngineType.Source, specificEndpoint);
            ServerInfo serverInfo = null;
            ReadOnlyCollection<Player> players = null;
            var serverStatus = ServerStatus.Initializing;
            try
            {
                serverInfo = server.GetInfo();
                serverStatus = ServerStatus.Running;
            }
            catch (SocketException)
            {
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

            var statusUpdate = new ServerStatusUpdate
            {
                Process = process,
                Status = serverStatus,
                ServerInfo = serverInfo,
                Players = players
            };

            return await Task.FromResult(statusUpdate);
        }

        private async Task DoSteamUpdate()
        {
            MasterServer masterServer = null;
            try
            { 
                await DebugUtils.WriteFormatThreadSafeAsync("Starting Steam data stream...");
            
                masterServer = MasterQuery.GetMasterServerInstance(EngineType.Source);

                foreach(var registration in this.serverRegistrations)
                {
                    var finishedSteamProcessing = new TaskCompletionSource<ServerStatusUpdate>();
                    ServerStatusUpdate statusUpdate = new ServerStatusUpdate { Status = ServerStatus.NotInstalled };
                        
                    //
                    // The code in here is called repeatedly by the QueryMaster code.
                    //
                    masterServer.GetAddresses(Region.Rest_of_the_world, async endPoints =>
                        {
                            DebugUtils.WriteFormatThreadSafeAsync(String.Format("Received {0} entries", endPoints.Count)).DoNotWait();

                            foreach (var endPoint in endPoints)
                            {
                                if (endPoint.Address.Equals(masterServer.SeedEndpoint.Address))
                                {                                    
                                    finishedSteamProcessing.TrySetResult(statusUpdate);
                                }
                                else if (registration.SteamEndpoint.Equals(endPoint))
                                {
                                    statusUpdate = await GenerateServerStatusUpdateAsync(registration, endPoint);                                                                     
                                    finishedSteamProcessing.TrySetResult(statusUpdate);
                                    break;
                                }
                            }
                        }, new IpFilter() { IpAddr = registration.SteamEndpoint.Address.ToString() });

                    statusUpdate = await finishedSteamProcessing.Task;
                    PostServerStatusUpdate(registration, registration.SteamCallback, statusUpdate);
                }                
            }
            finally
            {
                if(masterServer != null)
                {
                    masterServer.Dispose();
                }

                Task.Delay(SteamStatusQueryDelay).ContinueWith(_ => this.eventQueue.Post(DoSteamUpdate)).DoNotWait();
            }
        }
    }
}
