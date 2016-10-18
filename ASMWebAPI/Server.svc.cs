using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using ASMWebAPI.Models;
using NLog;

namespace ASMWebAPI
{
    public class Server : IServer
    {
        public static Logger Logger = LogManager.GetCurrentClassLogger();

        public bool CheckServerStatusA(IPEndPoint endpoint)
        {
            try
            {
                using (var server = QueryMaster.ServerQuery.GetServerInstance(QueryMaster.EngineType.Source, endpoint))
                {
                    Logger.Info($"Check server status requested for: {endpoint.Address}:{endpoint.Port}");
                    var serverInfo = server.GetInfo();
                    return serverInfo != null;
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"Exception checking server status for: {endpoint.Address}:{endpoint.Port} {ex.Message}");
                return false;
            }
        }

        public bool CheckServerStatusB(string ipString, int port)
        {
            try
            {
                IPAddress ipAddress;
                if (!IPAddress.TryParse(ipString, out ipAddress))
                    return false;
                var endpoint = new IPEndPoint(ipAddress, port);

                return CheckServerStatusA(endpoint);
            }
            catch (Exception ex)
            {
                Logger.Debug($"Exception checking server status for: {ipString}:{port} {ex.Message}");
                return false;
            }
        }

        public ServerInfo GetServerInfoA(IPEndPoint endpoint)
        {
            try
            {
                using (var server = QueryMaster.ServerQuery.GetServerInstance(QueryMaster.EngineType.Source, endpoint))
                {
                    Logger.Info($"Get server info requested for: {endpoint.Address}:{endpoint.Port}");
                    var serverInfo = server.GetInfo();
                    if (serverInfo != null)
                    {
                        var result = new ServerInfo
                        {
                            Name = serverInfo.Name,
                            Map = serverInfo.Map,
                            PlayerCount = serverInfo.Players,
                            MaxPlayers = serverInfo.MaxPlayers
                        };

                        // get the name and version of the server using regular expression.
                        if (!string.IsNullOrWhiteSpace(result.Name))
                        {
                            var match = Regex.Match(result.Name, @" - \(v([0-9]+\.[0-9]*)\)");
                            if (match.Success && match.Groups.Count >= 2)
                            {
                                // remove the version number from the name
                                result.Name = result.Name.Replace(match.Groups[0].Value, "");

                                // get the version number
                                var serverVersion = match.Groups[1].Value;
                                Version version;
                                if (!string.IsNullOrWhiteSpace(serverVersion) && Version.TryParse(serverVersion, out version))
                                {
                                    result.Version = version;
                                }
                            }
                        }

                        var players = server.GetPlayers();
                        if (players != null)
                        {
                            // set the number of players based on the player list, excludes any players in the list without a valid name.
                            result.PlayerCount = players.Count(record => !string.IsNullOrWhiteSpace(record.Name));

                            result.Players = new List<PlayerInfo>(result.PlayerCount);
                            foreach (var player in players.Where(record => !string.IsNullOrWhiteSpace(record.Name)))
                            {
                                var playerInfo = new PlayerInfo
                                {
                                    Name = player.Name,
                                    Time = player.Time
                                };
                                result.Players.Add(playerInfo);
                            }
                        }

                        return result;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Debug($"Exception getting server info for: {endpoint.Address}:{endpoint.Port} {ex.Message}");
                return null;
            }
        }

        public ServerInfo GetServerInfoB(string ipString, int port)
        {
            try
            {
                IPAddress ipAddress;
                if (!IPAddress.TryParse(ipString, out ipAddress))
                    return null;
                var endpoint = new IPEndPoint(ipAddress, port);

                return GetServerInfoA(endpoint);
            }
            catch (Exception ex)
            {
                Logger.Debug($"Exception getting server info for: {ipString}:{port} {ex.Message}");
                return null;
            }
        }
    }
}
