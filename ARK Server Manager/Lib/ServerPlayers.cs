using ARK_Server_Manager.Lib.ViewModel.RCON;
using ArkData;
using NLog;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
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

        private readonly ConcurrentDictionary<long, PlayerInfo> _players = new ConcurrentDictionary<long, PlayerInfo>();
        private readonly object _updatePlayerCollectionLock = new object();
        private CancellationTokenSource _cancellationTokenSource = null;
        private PlayerListParameters _playerListParameters;

        private Logger _allLogger;
        private Logger _eventLogger;
        private Logger _debugLogger;
        private Logger _errorLogger;
        private bool _disposed = false;

        public ServerPlayers(PlayerListParameters parameters)
        {
            this.Players = new SortableObservableCollection<PlayerInfo>();

            _playerListParameters = parameters;

            _allLogger = App.GetProfileLogger(_playerListParameters.ProfileName, "PlayerList_All", LogLevel.Info, LogLevel.Info);
            _eventLogger = App.GetProfileLogger(_playerListParameters.ProfileName, "PlayerList_Event", LogLevel.Info, LogLevel.Info);
            _debugLogger = App.GetProfileLogger(_playerListParameters.ProfileName, "PlayerList_Debug", LogLevel.Trace, LogLevel.Debug);
            _errorLogger = App.GetProfileLogger(_playerListParameters.ProfileName, "PlayerList_Error", LogLevel.Error, LogLevel.Fatal);

            UpdatePlayersAsync().DoNotWait();
        }

        public void Dispose()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }
            _disposed = true;
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

        private async Task UpdatePlayersAsync()
        {
            if (_disposed)
                return;

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            await UpdatePlayerDetailsAsync(_cancellationTokenSource.Token)
                .ContinueWith(async t1 =>
                {
                    await TaskUtils.RunOnUIThreadAsync(() =>
                    {
                        UpdatePlayerCollection();
                    });
                }, TaskContinuationOptions.NotOnCanceled)
                .ContinueWith(t2 =>
                {
                    var cancelled = _cancellationTokenSource.IsCancellationRequested;
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;

                    if (!cancelled)
                        Task.Delay(PLAYER_LIST_INTERVAL).ContinueWith(t3 => UpdatePlayersAsync());
                });
        }

        private async Task UpdatePlayerDetailsAsync(CancellationToken token)
        {
            if (!string.IsNullOrWhiteSpace(_playerListParameters.InstallDirectory))
            {
                var savedPath = ServerProfile.GetProfileSavePath(_playerListParameters.InstallDirectory, _playerListParameters.AltSaveDirectoryName, _playerListParameters.PGM_Enabled, _playerListParameters.PGM_Name);
                DataContainer dataContainer = null;
                DateTime lastSteamUpdateUtc = DateTime.MinValue;

                try
                {
                    // load the player data from the files.
                    dataContainer = await DataContainer.CreateAsync(savedPath, savedPath);
                }
                catch (Exception ex)
                {
                    _errorLogger.Error($"{nameof(UpdatePlayerDetailsAsync)} - Error: CreateAsync. {ex.Message}\r\n{ex.StackTrace}");
                    return;
                }

                token.ThrowIfCancellationRequested();
                await Task.Run(() =>
                {
                    // update the player data with the latest steam update value from the players collection
                    foreach (var playerData in dataContainer.Players)
                    {
                        if (!long.TryParse(playerData.SteamId, out long steamId))
                            continue;

                        _players.TryGetValue(steamId, out PlayerInfo player);
                        player?.UpdateSteamData(playerData);
                    }
                }, token);

                try
                {
                    // load the player data from steam
                    lastSteamUpdateUtc = await dataContainer.LoadSteamAsync(SteamUtils.SteamWebApiKey, STEAM_UPDATE_INTERVAL);
                }
                catch (Exception ex)
                {
                    _errorLogger.Error($"{nameof(UpdatePlayerDetailsAsync)} - Error: LoadSteamAsync. {ex.Message}\r\n{ex.StackTrace}");
                }

                token.ThrowIfCancellationRequested();

                var totalPlayers = dataContainer.Players.Count;
                foreach (var playerData in dataContainer.Players)
                {
                    token.ThrowIfCancellationRequested();
                    await Task.Run(async () =>
                    {
                        if (long.TryParse(playerData.SteamId, out long steamId))
                        {
                            var validPlayer = new PlayerInfo(_debugLogger)
                            {
                                SteamId = steamId,
                                SteamName = playerData.SteamName,
                                IsValid = true,
                            };

                            _players.AddOrUpdate(steamId, validPlayer, (k, v) => { v.SteamName = playerData.SteamName; v.IsValid = true; return v; });
                        }
                        else
                        {
                            var filename = Path.GetFileNameWithoutExtension(playerData.Filename);
                            if (long.TryParse(filename, out steamId))
                            {
                                var invalidPlayer = new PlayerInfo(_debugLogger)
                                {
                                    SteamId = steamId,
                                    SteamName = "< corrupted profile >",
                                    IsValid = false,
                                };

                                _players.AddOrUpdate(steamId, invalidPlayer, (k, v) => { v.SteamName = "< corrupted profile >"; v.IsValid = false; return v; });
                            }
                            else
                            {
                                _debugLogger.Debug($"{nameof(UpdatePlayerDetailsAsync)} - Error: corrupted profile.\r\n{playerData.Filename}.");
                            }
                        }

                        if (_players.TryGetValue(steamId, out PlayerInfo player) && player != null)
                        {
                            player.UpdateData(playerData, lastSteamUpdateUtc);

                            await TaskUtils.RunOnUIThreadAsync(() =>
                            {
                                player.IsAdmin = _playerListParameters?.Server?.Profile?.ServerFilesAdmins?.Any(u => u.SteamId.Equals(player.SteamId.ToString(), StringComparison.OrdinalIgnoreCase)) ?? false;
                                player.IsWhitelisted = _playerListParameters?.Server?.Profile?.ServerFilesWhitelisted?.Any(u => u.SteamId.Equals(player.SteamId.ToString(), StringComparison.OrdinalIgnoreCase)) ?? false;

                                if (totalPlayers <= Config.Default.RCON_MaximumPlayerAvatars && Config.Default.RCON_ShowPlayerAvatars)
                                    player.UpdateAvatarImageAsync(savedPath).DoNotWait();
                            });
                        }
                    }, token);
                }

                token.ThrowIfCancellationRequested();

                // remove any players that do not have a player file.
                var droppedPlayers = _players.Values.Where(p => dataContainer.Players.FirstOrDefault(pd => pd.SteamId == p.SteamId.ToString()) == null).ToArray();
                foreach (var droppedPlayer in droppedPlayers)
                {
                    _players.TryRemove(droppedPlayer.SteamId, out PlayerInfo player);
                }
            }
        }

        private void UpdatePlayerCollection()
        {
            lock (_updatePlayerCollectionLock)
            {
                this.Players = new SortableObservableCollection<PlayerInfo>(_players.Values);
                this.CountPlayers = this.Players.Count;
                this.CountInvalidPlayers = this.Players.Count(p => !p.IsValid);

                OnPlayerCollectionUpdated();
            }
        }
    }
}
