using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace ArkServerManager.Plugin.Discord
{
    internal static class NetworkUtils
    {
        public static async Task<IPAddress> DiscoverPublicIPAsync()
        {
            using (var webClient = new WebClient())
            {
                var publicIP = await webClient.DownloadStringTaskAsync(Config.Default.PublicIPCheckUrl);

                if (IPAddress.TryParse(publicIP, out IPAddress address))
                    return address;

                return IPAddress.None;
            }
        }

        public static async Task PerformCallToAPIAsync(IPAddress ipAddress, string pluginId)
        {
            try
            {
                using (var client = new WebClient())
                {
                    var url = string.Format(Config.Default.PluginsCallUrlFormat, ipAddress, pluginId);
                    await client.DownloadStringTaskAsync(url);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed calling API for: {ipAddress} {ex.Message}");
            }
        }
    }
}
