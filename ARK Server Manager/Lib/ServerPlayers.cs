using ARK_Server_Manager.Lib.ViewModel.RCON;
using ArkData;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ARK_Server_Manager.Lib
{
    public class ServerPlayers : DependencyObject
    {
        private const int PLAYER_LIST_INTERVAL = 5000;
        private const int STEAM_UPDATE_INTERVAL = 60;

        private enum LogEventType
        {
            All,
            Event,
        }

        public event EventHandler PlayersCollectionUpdated;

        public static readonly DependencyProperty PlayersProperty = DependencyProperty.Register(nameof(Players), typeof(SortableObservableCollection<PlayerInfo>), typeof(ServerPlayers), new PropertyMetadata(null));
        public static readonly DependencyProperty CountPlayersProperty = DependencyProperty.Register(nameof(CountPlayers), typeof(int), typeof(ServerPlayers), new PropertyMetadata(0));
        public static readonly DependencyProperty CountInvalidPlayersProperty = DependencyProperty.Register(nameof(CountInvalidPlayers), typeof(int), typeof(ServerPlayers), new PropertyMetadata(0));

        private static readonly ConcurrentDictionary<string, bool> _locks = new ConcurrentDictionary<string, bool>();
        private PlayerListParameters _playerListParameters;

        private Logger _allLogger;
        private Logger _eventLogger;
        private Logger _debugLogger;
        private Logger _errorLogger;

        public ServerPlayers(PlayerListParameters parameters)
        {
            this.Players = new SortableObservableCollection<PlayerInfo>();

            _playerListParameters = parameters;

            _allLogger = App.GetProfileLogger(_playerListParameters.ProfileName, "PlayerList_All", LogLevel.Info, LogLevel.Info);
            _eventLogger = App.GetProfileLogger(_playerListParameters.ProfileName, "PlayerList_Event", LogLevel.Info, LogLevel.Info);
            _debugLogger = App.GetProfileLogger(_playerListParameters.ProfileName, "PlayerList_Debug", LogLevel.Trace, LogLevel.Debug);
            _errorLogger = App.GetProfileLogger(_playerListParameters.ProfileName, "PlayerList_Error", LogLevel.Error, LogLevel.Fatal);

            UpdatePlayerListAsync().DoNotWait();
        }

        public SortableObservableCollection<PlayerInfo> Players
        {
            get { return (SortableObservableCollection<PlayerInfo>)GetValue(PlayersProperty); }
            set { SetValue(PlayersProperty, value); }
        }

        public int CountPlayers
        {
            get { return (int)GetValue(CountPlayersProperty); }
            set { SetValue(CountPlayersProperty, value); }
        }

        public int CountInvalidPlayers
        {
            get { return (int)GetValue(CountInvalidPlayersProperty); }
            set { SetValue(CountInvalidPlayersProperty, value); }
        }

        private void LogEvent(LogEventType eventType, string message)
        {
            switch (eventType)
            {
                case LogEventType.All:
                    _allLogger.Info(message);
                    return;

                case LogEventType.Event:
                    _eventLogger.Info(message);
                    return;
            }
        }

        protected void OnPlayerCollectionUpdated()
        {
            PlayersCollectionUpdated?.Invoke(this, EventArgs.Empty);
        }

        private async Task UpdatePlayerListAsync()
        {
            lock (_locks)
            {
                if (_locks.TryGetValue($"{this.GetHashCode()}|PlayerList", out bool value) && value || !_locks.TryAdd($"{this.GetHashCode()}|PlayerList", true))
                {
                    Task.Delay(PLAYER_LIST_INTERVAL).ContinueWith(t => UpdatePlayerListAsync());
                    return;
                }
            }

            await UpdatePlayersAsync();
            await Task.Delay(PLAYER_LIST_INTERVAL).ContinueWith(t => UpdatePlayerListAsync());
        }

        private async Task UpdatePlayersAsync()
        {
            var players = new List<PlayerInfo>(this.Players);

            await UpdatePlayerDetailsAsync(players).ContinueWith(t =>
            {
                TaskUtils.RunOnUIThreadAsync(() => {
                    this.CountPlayers = this.Players.Count(p => p.IsOnline);
                    this.CountInvalidPlayers = this.Players.Count(p => !p.IsValid);
                }).Wait();

                _locks.TryRemove($"{this.GetHashCode()}|PlayerList", out bool value);
            });
        }

        private async Task UpdatePlayerDetailsAsync(List<PlayerInfo> players)
        {
            if (!string.IsNullOrWhiteSpace(_playerListParameters.InstallDirectory))
            {
                var savedPath = ServerProfile.GetProfileSavePath(_playerListParameters.InstallDirectory, _playerListParameters.AltSaveDirectoryName, _playerListParameters.PGM_Enabled, _playerListParameters.PGM_Name);
                DataContainer dataContainer = null;
                DateTime lastSteamUpdateUtc = DateTime.MinValue;

                try
                {
                    DataFileDetails.PlayerFileFolder = savedPath;
                    DataFileDetails.TribeFileFolder = savedPath;
                    dataContainer = await DataContainer.CreateAsync();
                }
                catch (Exception ex)
                {
                    _errorLogger.Error($"{nameof(UpdatePlayerDetailsAsync)} - Error: CreateAsync. {ex.Message}\r\n{ex.StackTrace}");
                    return;
                }

                await TaskUtils.RunOnUIThreadAsync(() => {
                    foreach (var playerData in dataContainer.Players)
                    {
                        playerData.LastSteamUpdateUtc = this.Players.FirstOrDefault(p => playerData.SteamId.Equals(p.PlayerData?.SteamId))?.PlayerData?.LastSteamUpdateUtc ?? DateTime.MinValue;
                    }
                });

                try
                {
                    lastSteamUpdateUtc = await dataContainer.LoadSteamAsync(SteamUtils.SteamWebApiKey, STEAM_UPDATE_INTERVAL);
                }
                catch (Exception ex)
                {
                    _errorLogger.Error($"{nameof(UpdatePlayerDetailsAsync)} - Error: LoadSteamAsync. {ex.Message}\r\n{ex.StackTrace}");
                    return;
                }

                await Task.Run(async () => {
                    foreach (var playerData in dataContainer.Players)
                    {
                        PlayerInfo player = null;

                        if (Int64.TryParse(playerData.SteamId, out long steamId))
                        {
                            player = players.FirstOrDefault(p => p.SteamId == steamId);
                            if (player == null)
                            {
                                player = new PlayerInfo(_debugLogger)
                                {
                                    SteamId = steamId,
                                    SteamName = playerData.SteamName
                                };
                                players.Add(player);
                            }
                            player.IsValid = true;
                        }
                        else
                        {
                            var filename = Path.GetFileNameWithoutExtension(playerData.Filename);
                            if (Int64.TryParse(filename, out steamId))
                            {
                                player = players.FirstOrDefault(p => p.SteamId == steamId);
                                if (player == null)
                                {
                                    player = new PlayerInfo(_debugLogger)
                                    {
                                        SteamId = steamId,
                                        SteamName = "< corrupted profile >"
                                    };
                                    players.Add(player);
                                }
                                player.IsValid = false;
                            }
                            else
                            {
                                _debugLogger.Debug($"{nameof(UpdatePlayerDetailsAsync)} - Error: corrupted profile.\r\n{playerData.Filename}.");
                            }
                        }

                        if (player != null)
                        {
                            player.UpdateData(playerData, playerData.LastSteamUpdateUtc.Equals(lastSteamUpdateUtc));

                            await TaskUtils.RunOnUIThreadAsync(() => {
                                player.IsAdmin = _playerListParameters?.Server?.Profile?.ServerFilesAdmins?.Any(u => u.SteamId.Equals(player.SteamId.ToString(), StringComparison.OrdinalIgnoreCase)) ?? false;
                                player.IsWhitelisted = _playerListParameters?.Server?.Profile?.ServerFilesWhitelisted?.Any(u => u.SteamId.Equals(player.SteamId.ToString(), StringComparison.OrdinalIgnoreCase)) ?? false;

                                player.UpdateAvatarImageAsync(savedPath).DoNotWait();
                            });
                        }
                    }

                    players.TrimExcess();
                });
            }

            await TaskUtils.RunOnUIThreadAsync(() =>
            {
                this.Players = new SortableObservableCollection<PlayerInfo>(players);
                OnPlayerCollectionUpdated();
            });
        }
    }
}
