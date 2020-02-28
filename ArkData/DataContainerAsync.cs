﻿using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ArkData
{
    /// <summary>
    /// The container for the data.
    /// </summary>
    public partial class DataContainer
    {
        /// <summary>
        /// Instantiates the DataContainer and parses all the user-data files
        /// </summary>
        /// <returns>The async task context containing the resulting container.</returns>
        public static async Task<DataContainer> CreateAsync(string playerFileFolder, string tribeFileFolder)
        {
            var playerFiles = new string[0];
            var tribeFiles = new string[0];

            if (Directory.Exists(playerFileFolder))
            {
                playerFiles = Directory.GetFiles(playerFileFolder).Where(f => Path.GetFileNameWithoutExtension(f).StartsWith(DataFileDetails.PlayerFilePrefix)
                    && Path.GetFileNameWithoutExtension(f).EndsWith(DataFileDetails.PlayerFileSuffix)
                    && Path.GetExtension(f).Equals(DataFileDetails.PlayerFileExtension)).ToArray();
            }
            if (Directory.Exists(tribeFileFolder))
            {
                tribeFiles = Directory.GetFiles(tribeFileFolder).Where(f => Path.GetFileNameWithoutExtension(f).StartsWith(DataFileDetails.TribeFilePrefix)
                    && Path.GetFileNameWithoutExtension(f).EndsWith(DataFileDetails.TribeFileSuffix)
                    && Path.GetExtension(f).Equals(DataFileDetails.TribeFileExtension)).ToArray();
            }

            var container = new DataContainer();

            foreach (var file in playerFiles)
                container.Players.Add(await Parser.ParsePlayerAsync(file));

            foreach (var file in tribeFiles)
                container.Tribes.Add(await Parser.ParseTribeAsync(file));

            container.LinkPlayerTribe();

            return container;
        }

        /// <summary>
        /// Loads the profile data for all users from the steam service
        /// </summary>
        /// <returns>The async task context.</returns>
        public async Task<DateTime> LoadSteamAsync(string apiKey, int steamUpdateInterval = 0)
        {
            const int MAX_STEAM_IDS = 100;

            // need to make multiple calls of 100 steam id's.
            var lastSteamUpdateUtc = DateTime.UtcNow;
            var startIndex = 0;
            var playerSteamIds = Players.Where(p => p.LastSteamUpdateUtc.AddMinutes(steamUpdateInterval) < DateTime.UtcNow).Select(p => p.SteamId).ToArray();

            while (true)
            {
                // check if the start index has exceeded the Players list count.
                if (startIndex >= playerSteamIds.Length) break;
                // get the number of steam ids to read.
                int steamIdsCount = System.Math.Min(MAX_STEAM_IDS, playerSteamIds.Length - startIndex);
                // get a comma delimited list of the steam ids to process
                var builder = string.Join(",", playerSteamIds, startIndex, steamIdsCount);

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new System.Uri("https://api.steampowered.com/");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var response = await client.GetAsync(string.Format("ISteamUser/GetPlayerSummaries/v0002/?key={0}&steamids={1}", apiKey, builder));
                    if (response.IsSuccessStatusCode)
                        using (var reader = new StreamReader(await response.Content.ReadAsStreamAsync()))
                        {
                            LinkSteamProfiles(await reader.ReadToEndAsync(), lastSteamUpdateUtc);
                        }
                    else
                        throw new System.Net.WebException("The Steam API request was unsuccessful. Are you using a valid key?");

                    response = await client.GetAsync(string.Format("ISteamUser/GetPlayerBans/v1/?key={0}&steamids={1}", apiKey, builder));
                    if (response.IsSuccessStatusCode)
                        using (var reader = new StreamReader(await response.Content.ReadAsStreamAsync()))
                        {
                            LinkSteamBans(await reader.ReadToEndAsync());
                        }
                    else
                        throw new System.Net.WebException("The Steam API request was unsuccessful. Are you using a valid key?");
                }

                startIndex += steamIdsCount;
            }

            SteamLoaded = true;
            return lastSteamUpdateUtc;
        }

        /// <summary>
        /// Fetches the player server status. Can only be done after fetching Steam player data.
        /// </summary>
        /// <param name="ipString">The IP of the server.</param>
        /// <param name="port">The port of the server.</param>
        /// <returns>The async task context.</returns>
        public Task LoadOnlinePlayersAsync(string ipString, int port)
        {
            if (SteamLoaded)
                return Task.Run(() =>
                {
                    LinkOnlinePlayers(ipString, port);
                });
            else
                throw new System.Exception("The Steam user data should be loaded before the server status can be checked.");
        }
    }
}
