using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib
{
    public class NetworkAdapterEntry
    {
        public NetworkAdapterEntry(IPAddress address, string description)
        {
            this.IPAddress = address.ToString();
            this.Description = description;
        }

        public NetworkAdapterEntry(string address, string description)
        {
            this.IPAddress = address;
            this.Description = description;
        }

        public string IPAddress
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }
    }

    public static class NetworkUtils
    {
        public static Logger Logger = LogManager.GetCurrentClassLogger();

        public static List<NetworkAdapterEntry> GetAvailableIPV4NetworkAdapters()
        {
            List<NetworkAdapterEntry> adapters = new List<NetworkAdapterEntry>();
            
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach(var ifc in interfaces)
            {
                var ipProperties = ifc.GetIPProperties();
                if(ipProperties != null)
                {
                    adapters.AddRange(ipProperties.UnicastAddresses.Select(a => a.Address)
                                                                   .Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(a))
                                                                   .Select(a => new NetworkAdapterEntry(a, ifc.Description)));
                }
            }

            return adapters;
        }

        public static async Task<Version> GetLatestASMVersion()
        {
            using (var webClient = new WebClient())
            {
                try
                {
                    string latestVersion = null;

                    if (App.Instance.BetaVersion)
                        latestVersion = await webClient.DownloadStringTaskAsync(Config.Default.LatestASMBetaVersionUrl);
                    else
                        latestVersion = await webClient.DownloadStringTaskAsync(Config.Default.LatestASMVersionUrl);

                    return Version.Parse(latestVersion);
                }
                catch (Exception ex)
                {
                    Logger.Debug(String.Format("Exception checking for ASM version: {0}\r\n{1}", ex.Message, ex.StackTrace));
                    return new Version();
                }
            }
        }

        public static NetworkAdapterEntry GetPreferredIP(IEnumerable<NetworkAdapterEntry> adapters)
        {
            //
            // Try for a 192.168. address first
            //
            var preferredIp = adapters.FirstOrDefault(a => a.IPAddress.StartsWith("192.168."));
            if (preferredIp == null)
            {
                //
                // Try a 10.0 address next
                //
                preferredIp = adapters.FirstOrDefault(a => a.IPAddress.StartsWith("10.0."));
                if (preferredIp == null)
                {
                    // 
                    // Sad.  Just take the first.
                    //
                    preferredIp = adapters.FirstOrDefault();
                }
            }

            return preferredIp;
        }

        public static async Task<string> DiscoverPublicIPAsync()
        {
            using (var webClient = new WebClient())
            {
                var publicIP = await webClient.DownloadStringTaskAsync(Config.Default.PublicIPCheckUrl);
                IPAddress address;
                if(IPAddress.TryParse(publicIP, out address))
                {
                    return publicIP;
                }

                return String.Empty;
            }
        }

        public static bool CheckServerStatusDirect(IPEndPoint endpoint)
        {
            try
            {
                QueryMaster.ServerInfo serverInfo;

                using (var server = QueryMaster.ServerQuery.GetServerInstance(QueryMaster.EngineType.Source, endpoint))
                {
                    serverInfo = server.GetInfo();
                }

                return serverInfo != null;
            }
            catch (Exception ex)
            {
                Logger.Debug($"Exception checking status direct for: {endpoint.Address}:{endpoint.Port} {ex.Message}\r\n{ex.StackTrace}");
                return false;
            }
        }

        public static async Task<bool> CheckServerStatusViaSteam(IPEndPoint endpoint)
        {
            try
            {
                var details = await SteamUtils.GetSteamServerDetails(endpoint);
                return (details?.servers?.Count > 0);
            }
            catch (Exception ex)
            {
                Logger.Debug($"Exception checking status via steam for: {endpoint.Address}:{endpoint.Port} {ex.Message}\r\n{ex.StackTrace}");
                return false;
            }
        }
    }
}
