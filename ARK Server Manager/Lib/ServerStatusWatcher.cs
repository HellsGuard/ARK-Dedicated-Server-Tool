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
        public ConcurrentDictionary<IPEndPoint, int> SteamWatches = new ConcurrentDictionary<IPEndPoint, int>();
        public ConcurrentDictionary<IPEndPoint, ServerInfo> LocalWatches = new ConcurrentDictionary<IPEndPoint, ServerInfo>();

        private Task steamWatchTask;
        private Task localWatchTask;
        
        private int watchGeneration = 0;

        public ServerStatusWatcher()
        {
            steamWatchTask = Task.Factory.StartNew(async () => await StartSteamDataStream());
            localWatchTask = Task.Factory.StartNew(async () => await StartLocalWatch());
        }

        public ServerInfo GetLastServerInfo(IPEndPoint server)
        {
            ServerInfo info = null;
            if(!LocalWatches.TryGetValue(server, out info))
            {
                LocalWatches.TryAdd(server, null);
            }

            return info;
        }

        public bool GetLastSteamVisible(IPEndPoint server)
        {
            int lastSeenGeneration;

            LocalWatches.TryAdd(server, null);

            if(SteamWatches.TryGetValue(server, out lastSeenGeneration))
            {
                if(lastSeenGeneration >= this.watchGeneration - 1)
                {
                    return true;
                }
            }
            
            return false;
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
                            var server = ServerQuery.GetServerInstance(EngineType.Source, new IPEndPoint(IPAddress.Loopback, (ushort)endPoint.Port));                            
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
        private async Task StartSteamDataStream()
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
                        app.Dispatcher.BeginInvoke(new Action(() => Debug.WriteLine("Starting Steam data stream...")));
                    }
            
                    this.watchGeneration++;
                    masterServer = MasterQuery.GetMasterServerInstance(EngineType.Source);

                    foreach(var localServer in this.LocalWatches.Keys.Where(p => !IPAddress.IsLoopback(p.Address)))
                    {
                        var finishedSteamProcessing = new TaskCompletionSource<bool>();
                        masterServer.GetAddresses(Region.Rest_of_the_world, endPoints =>
                            {
                                var currentApp = App.Current;
                                if (currentApp != null)
                                {
                                    currentApp.Dispatcher.BeginInvoke(new Action(() => Debug.WriteLine(String.Format("Received {0} entries", endPoints.Count))));
                                }
                                foreach (var endPoint in endPoints)
                                {
                                    if (endPoint.Address.Equals(masterServer.SeedEndpoint.Address))
                                    {
                                        finishedSteamProcessing.TrySetResult(true);
                                    }
                                    else if (LocalWatches.ContainsKey(endPoint))
                                    {
                                        SteamWatches[endPoint] = watchGeneration;
                                    }
                                }
                            }, new IpFilter() { IpAddr = localServer.Address.ToString() });

                        await finishedSteamProcessing.Task;
                    }
                }

                await Task.Delay(5000);
            }
        }
    }
}
