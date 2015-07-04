using QueryMaster;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib
{
    public class ServerStatusWatcher
    {
        public ConcurrentDictionary<IPEndPoint, ServerInfo> SteamWatches = new ConcurrentDictionary<IPEndPoint, ServerInfo>();
        public ConcurrentDictionary<IPEndPoint, ServerInfo> LocalWatches = new ConcurrentDictionary<IPEndPoint, ServerInfo>();

        private Task steamWatchTask;
        private Task localWatchTask;
        
        public ServerStatusWatcher()
        {
            steamWatchTask = Task.Factory.StartNew(async () => await StartSteamWatch());
            localWatchTask = Task.Factory.StartNew(async () => await StartLocalWatch());
        }

        /// <summary>
        /// Gets the status of a server from the local machine
        /// </summary>
        /// <param name="server">The server's private address</param>
        /// <returns>The server info, or null.</returns>
        public ServerInfo GetLocalServerInfo(IPEndPoint server)
        {
            ServerInfo info = LocalWatches.GetOrAdd(server, (ServerInfo)null);
            return info;
        }

        /// <summary>
        /// Gets the status of a server from the Steam master server
        /// </summary>
        /// <param name="server">The server's public address</param>
        /// <returns>The server info, or null.</returns>
        public ServerInfo GetSteamServerInfo(IPEndPoint server)
        {
            ServerInfo info = SteamWatches.GetOrAdd(server, (ServerInfo)null);
            return info;
        }

        private async Task StartLocalWatch()
        {
            while(true)
            {
                if(this.LocalWatches.Count > 0)
                {
                    Debug.WriteLine("Watching local servers.");
                    foreach(var endPoint in LocalWatches.Keys)
                    {
                        try
                        {
                            var server = ServerQuery.GetServerInstance(EngineType.Source, endPoint);                            
                            var serverInfo = server.GetInfo();
                            this.LocalWatches[endPoint] = serverInfo;
                        }
                        catch(SocketException)
                        {
                            this.LocalWatches[endPoint] = null;
                        }
                    }
                }

                await Task.Delay(5000);
            }
        }
        private async Task StartSteamWatch()
        {
            MasterServer masterServer = null; ;
            while(true)
            {
                if (masterServer != null)
                {
                    masterServer.Dispose();
                    masterServer = null;
                }

                if(this.LocalWatches.Values.Count(info => info != null) > 0)
                {
                    var app = App.Current;
                    if (app != null)
                    {
                        await app.Dispatcher.BeginInvoke(new Action(() => Debug.WriteLine("Starting Steam data stream...")));
                    }
            
                    masterServer = MasterQuery.GetMasterServerInstance(EngineType.Source);

                    foreach(var steamServer in this.SteamWatches.Keys.ToArray())
                    {
                        var finishedSteamProcessing = new TaskCompletionSource<bool>();
                        var gotServer = false;
                        
                        //
                        // The code in here is called repeatedly by the QueryMaster code.
                        //
                        masterServer.GetAddresses(Region.Rest_of_the_world, endPoints =>
                            {
                                var currentApp = App.Current;
                                if (currentApp != null)                                
                                {
                                    var dispatcher = currentApp.Dispatcher;
                                    if (dispatcher != null)
                                    {
                                        dispatcher.BeginInvoke(new Action(() => Debug.WriteLine(String.Format("Received {0} entries", endPoints.Count))));
                                    }
                                }

                                foreach (var endPoint in endPoints)
                                {
                                    if (endPoint.Address.Equals(masterServer.SeedEndpoint.Address))
                                    {
                                        finishedSteamProcessing.TrySetResult(true);
                                    }
                                    else if (SteamWatches.ContainsKey(endPoint))
                                    {
                                        gotServer = true;
                                        finishedSteamProcessing.TrySetResult(true);
                                    }
                                }
                            }, new IpFilter() { IpAddr = steamServer.Address.ToString() });

                        await finishedSteamProcessing.Task;

                        try
                        {
                            if (gotServer)
                            {
                                var server = ServerQuery.GetServerInstance(EngineType.Source, steamServer);
                                if (server != null)
                                {
                                    var serverInfo = server.GetInfo();
                                    if (serverInfo != null)
                                    {
                                        SteamWatches[steamServer] = serverInfo;
                                    }
                                    else
                                    {
                                        SteamWatches[steamServer] = null;
                                    }
                                }
                                else
                                {
                                    SteamWatches[steamServer] = null;
                                }
                            }
                            else
                            {
                                SteamWatches[steamServer] = null;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(String.Format("Unexpected exception getting server info: {0}\n{1}", ex.Message, ex.StackTrace));
                            SteamWatches[steamServer] = null;
                        }
                    }
                }

                await Task.Delay(10000);
            }
        }
    }
}
