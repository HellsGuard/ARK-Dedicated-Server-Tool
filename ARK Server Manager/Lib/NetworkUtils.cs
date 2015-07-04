using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
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
                                                                   .Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && 
                                                                               !IPAddress.IsLoopback(a))
                                                                   .Select(a => new NetworkAdapterEntry(a, ifc.Description)));
                }
            }

            return adapters;
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

        public class AvailableVersion
        {
            public bool IsValid
            {
                get;
                set;
            }

            public Version Current
            {
                get;
                set;
            }

            public Version Upcoming
            {
                get;
                set;
            }

            public string UpcomingETA
            {
                get;
                set;
            }
        }

        public static async Task<AvailableVersion> CheckForUpdatesAsync()
        {
            AvailableVersion result = new AvailableVersion();

            try
            {
                string jsonString;                
                using (var client = new WebClient())
                {
                    jsonString = await client.DownloadStringTaskAsync(Config.Default.AvailableVersionUrl);
                }
                JObject query = JObject.Parse(jsonString);

                var availableVersion = query.SelectToken("version.current");
                var upcomingVersion = query.SelectToken("version.upcoming.version");
                var upcomingETA = query.SelectToken("version.upcoming.version.eta");

                if (availableVersion != null)
                {
                    var versionMatch = new Regex(@"[^\d]*(?<version>\d*(\.\d*)?)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture).Match((string)availableVersion);
                    if (versionMatch.Success)
                    {
                        Version ver;
                        if (Version.TryParse(versionMatch.Groups["version"].Value, out ver))
                        {
                            result.IsValid = true;
                            result.Current = ver;
                        }
                    }
                }

                if (upcomingVersion != null)
                {
                    var versionMatch = new Regex(@"[^\d]*(?<version>\d*(\.\d*)?)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture).Match((string)upcomingVersion);
                    if (versionMatch.Success)
                    {
                        Version ver;
                        if (Version.TryParse(versionMatch.Value, out ver))
                        {
                            result.Upcoming = ver;
                        }
                    }
                }

                if (upcomingETA != null)
                {
                    result.UpcomingETA = (string)upcomingETA;
                }                
            }
            catch (Exception ex)
            {
                Debug.WriteLine(String.Format("Exception checking for version: {0}\r\n{1}", ex.Message, ex.StackTrace));
            }

            return result;
        }
    }
}
