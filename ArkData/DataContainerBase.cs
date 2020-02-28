﻿using SSQLib;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Runtime.Remoting;
using System;

/// <summary>
/// The container for the data.
/// </summary>
namespace ArkData
{
    public partial class DataContainer
    {
        /// <summary>
        /// A list of all players registered on the server.
        /// </summary>
        public List<PlayerData> Players { get; set; }
        /// <summary>
        /// A list of all tribes registered on the server.
        /// </summary>
        public List<TribeData> Tribes { get; set; }
        /// <summary>
        /// Indicates whether the steam user data has been loaded.
        /// </summary>
        private bool SteamLoaded { get; set; }

        /// <summary>
        /// Constructs the DataContainer.
        /// </summary>
        public DataContainer()
        {
            Players = new List<PlayerData>();
            Tribes = new List<TribeData>();
            SteamLoaded = false;
        }

        /// <summary>
        /// Links the online players, to the player profiles.
        /// </summary>
        /// <param name="ipString">The server ip address.</param>
        /// <param name="port">The Steam query port.</param>
        private void LinkOnlinePlayers(string ipString, int port)
        {
            try
            {
                var online = Enumerable.OfType<PlayerInfo>(new SSQL().Players(new IPEndPoint(IPAddress.Parse(ipString), port))).ToList();

                for (var i = 0; i < Players.Count; i++)
                {
                    var online_player = online.SingleOrDefault(p => p.Name == Players[i].SteamName);
                    if (online_player != null)
                        Players[i].Online = true;
                    else
                        Players[i].Online = false;
                }
            }
            catch (SSQLServerException)
            {
                throw new ServerException("The connection to the server failed. Please check the configured IP address and port.");
            }
        }

        /// <summary>
        /// Links the players to their tribes and the tribes to the players.
        /// </summary>
        private void LinkPlayerTribe()
        {
            for (var i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                player.OwnedTribes = Tribes.Where(t => t.OwnerId == player.Id).ToList();
                player.Tribe = Tribes.SingleOrDefault(t => t.Id == player.TribeId);
            }

            for (var i = 0; i < Tribes.Count; i++)
            {
                var tribe = Tribes[i];
                tribe.Owner = Players.SingleOrDefault(p => p.Id == tribe.OwnerId);
                tribe.Players = Players.Where(p => p.TribeId == tribe.Id).ToList();
            }
        }

        /// <summary>
        /// Deserializes JSON from Steam API and links Steam profile to player profile.
        /// </summary>
        /// <param name="jsonString">The JSON data string.</param>
        private void LinkSteamProfiles(string jsonString, DateTime lastSteamUpdateUtc)
        {
            var profiles = JsonConvert.DeserializeObject<Models.SteamResponse<Models.SteamProfile>>(jsonString).response.players;

            for (var i = 0; i < profiles.Count; i++)
            {
                var player = Players.Single(p => p.SteamId == profiles[i].steamid);
                player.SteamName = profiles[i].personaname;
                player.ProfileUrl = profiles[i].profileurl;
                player.AvatarUrl = profiles[i].avatar;
                player.LastSteamUpdateUtc = lastSteamUpdateUtc;
            }
        }

        /// <summary>
        /// Deserializes JSON from Steam API and links Steam ban data to player profile.
        /// </summary>
        /// <param name="jsonString">The JSON data string.</param>
        private void LinkSteamBans(string jsonString)
        {
            var bans = JsonConvert.DeserializeObject<Models.SteamPlayerResponse<Models.SteamBan>>(jsonString).players;
            for (var i = 0; i < bans.Count; i++)
            {
                var player = Players.Single(p => p.SteamId == bans[i].SteamId);
                player.CommunityBanned = bans[i].CommunityBanned;
                player.VACBanned = bans[i].VACBanned;
                player.NumberOfVACBans = bans[i].NumberOfVACBans;
                player.NumberOfGameBans = bans[i].NumberOfGameBans;
                player.DaysSinceLastBan = bans[i].DaysSinceLastBan;
            }
        }
    }
}
