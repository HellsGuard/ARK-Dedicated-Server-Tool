using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ArkData
{
    /// <summary>
    /// The container for the ARK data.
    /// </summary>
    public partial class ArkDataContainer
    {
        /// <summary>
        /// Loads the profile data for all users from the steam service
        /// </summary>
        /// <param name="apiKey">The Steam API key</param>
        public void LoadSteam(string apiKey)
        {
            const int MAX_STEAM_IDS = 100;

            // need to make multiple calls of 100 steam id's.
            var startIndex = 0;
            var playerSteamIds = Players.Select(p => p.SteamId).ToArray();

            while (true)
            {
                // check if the start index has exceeded the Players list count.
                if (startIndex >= Players.Count) break;
                // get the number of steam ids to read.
                int steamIdsCount = System.Math.Min(MAX_STEAM_IDS, Players.Count - startIndex);
                // get a comma delimited list of the steam ids to process
                var builder = string.Join(",", playerSteamIds, startIndex, steamIdsCount);

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new System.Uri("https://api.steampowered.com/");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var response = client.GetAsync(string.Format("ISteamUser/GetPlayerSummaries/v0002/?key={0}&steamids={1}", apiKey, builder)).Result;
                    if (response.IsSuccessStatusCode)
                        using (var reader = new StreamReader(response.Content.ReadAsStreamAsync().Result))
                        {
                            LinkSteamProfiles(reader.ReadToEnd());
                        }
                    else
                        throw new System.Net.WebException("The Steam API request was unsuccessful. Are you using a valid key?");

                    response = client.GetAsync(string.Format("ISteamUser/GetPlayerBans/v1/?key={0}&steamids={1}", apiKey, builder)).Result;
                    if (response.IsSuccessStatusCode)
                        using (var reader = new StreamReader(response.Content.ReadAsStreamAsync().Result))
                        {
                            LinkSteamBans(reader.ReadToEnd());
                        }
                    else
                        throw new System.Net.WebException("The Steam API request was unsuccessful. Are you using a valid key?");
                }

                startIndex += steamIdsCount;
            }

            SteamLoaded = true;
        }

        /// <summary>
        /// Fetches the player server status. Can only be done after fetching Steam player data.
        /// </summary>
        /// <param name="ipString">The IP of the server.</param>
        /// <param name="port">The port of the server.</param>
        public void LoadOnlinePlayers(string ipString, int port)
        {
            if (SteamLoaded)
            {
                LinkOnlinePlayers(ipString, port);
            }
            else
                throw new System.Exception("The Steam user data should be loaded before the server status can be checked.");
        }

        /// <summary>
        /// Instantiates the ArkDataContainer and parses all the user data files
        /// </summary>
        /// <param name="directory">The directory containing the profile and tribe files.</param>
        public static ArkDataContainer Create(string directory)
        {
            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException("The ARK data directory couldn't be found.");

            var playerFiles = Directory.GetFiles(directory).Where(p => p.ToLower().Contains(".arkprofile")).ToArray();
            var tribeFiles = Directory.GetFiles(directory).Where(p => p.ToLower().Contains(".arktribe")).ToArray();

            if (playerFiles.Length == 0 && tribeFiles.Length == 0)
                throw new FileLoadException("The directory did not contain any of the parseable files.");

            var container = new ArkDataContainer();

            for (var i = 0; i < playerFiles.Length; i++)
                container.Players.Add(Parser.ParsePlayer(playerFiles[i]));

            for (var i = 0; i < tribeFiles.Length; i++)
                container.Tribes.Add(Parser.ParseTribe(tribeFiles[i]));

            container.LinkPlayerTribe();

            return container;
        }
    }
}
