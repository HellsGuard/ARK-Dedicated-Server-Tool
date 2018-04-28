using ARK_Server_Manager.Lib.ViewModel.RCON;
using ArkData;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ARK_Server_Manager.Lib
{
    public class ServerPlayers : DependencyObject, IAsyncDisposable
    {
        private enum LogEventType
        {
            All,
            Event,
        }

        public event EventHandler PlayersCollectionUpdated;

        private const int PLAYER_LIST_INTERVAL = 5000;

        public static readonly DependencyProperty PlayersProperty = DependencyProperty.Register(nameof(Players), typeof(SortableObservableCollection<PlayerInfo>), typeof(ServerPlayers), new PropertyMetadata(null));
        public static readonly DependencyProperty CountPlayersProperty = DependencyProperty.Register(nameof(CountPlayers), typeof(int), typeof(ServerPlayers), new PropertyMetadata(0));
        public static readonly DependencyProperty CountInvalidPlayersProperty = DependencyProperty.Register(nameof(CountInvalidPlayers), typeof(int), typeof(ServerPlayers), new PropertyMetadata(0));

        private readonly ActionQueue _commandProcessor = new ActionQueue(TaskScheduler.Default);
        private readonly ActionQueue _outputProcessor = new ActionQueue(TaskScheduler.FromCurrentSynchronizationContext());
        private PlayerListParameters _playerListParameters;
        private bool _processingListplayers = false;
        private bool _updatingPlayerDetails = false;

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

            _commandProcessor.PostAction(UpdatePlayerList);
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

        public async Task DisposeAsync()
        {
            await _commandProcessor.DisposeAsync();
            await _outputProcessor.DisposeAsync();
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

        internal void OnPlayerCollectionUpdated()
        {
            PlayersCollectionUpdated?.Invoke(this, EventArgs.Empty);
        }

        private async Task UpdatePlayerDetails()
        {
            if (_updatingPlayerDetails)
                return;
            _updatingPlayerDetails = true;

            try
            {
                if (!string.IsNullOrWhiteSpace(_playerListParameters.InstallDirectory) && !string.IsNullOrWhiteSpace(_playerListParameters.ServerMap))
                {
                    var savedPath = ServerProfile.GetProfileSavePath(_playerListParameters.InstallDirectory, _playerListParameters.AltSaveDirectoryName, _playerListParameters.PGM_Enabled, _playerListParameters.PGM_Name);
                    DataContainer dataContainer = null;

                    try
                    {
                        DataFileDetails.PlayerFileFolder = savedPath;
                        DataFileDetails.TribeFileFolder = savedPath;
                        dataContainer = await DataContainer.CreateAsync();
                    }
                    catch (Exception ex)
                    {
                        _errorLogger.Error($"{nameof(UpdatePlayerDetails)} - Error: CreateAsync. {ex.Message}\r\n{ex.StackTrace}");
                        return;
                    }

                    try
                    {
                        await dataContainer.LoadSteamAsync(SteamUtils.SteamWebApiKey);
                    }
                    catch (Exception ex)
                    {
                        _errorLogger.Error($"{nameof(UpdatePlayerDetails)} - Error: LoadSteamAsync. {ex.Message}\r\n{ex.StackTrace}");
                        return;
                    }

                    TaskUtils.RunOnUIThreadAsync(() =>
                    {
                        // create a new temporary list
                        List<PlayerInfo> players = new List<PlayerInfo>(this.Players.Count + dataContainer.Players.Count);
                        players.AddRange(this.Players);

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
                                    _debugLogger.Debug($"{nameof(UpdatePlayerDetails)} - Error: corrupted profile.\r\n{playerData.Filename}.");
                                }
                            }

                            if (player != null)
                                player.UpdateDataAsync(_playerListParameters?.Server?.Profile, playerData, savedPath).DoNotWait();
                        }

                        this.Players = new SortableObservableCollection<PlayerInfo>(players);
                        OnPlayerCollectionUpdated();
                    }).DoNotWait();
                }
            }
            finally
            {
                _updatingPlayerDetails = false;
            }
        }

        private Task UpdatePlayerList()
        {
            return _commandProcessor.PostAction(() =>
            {
                _outputProcessor.PostAction(() => UpdatePlayers());
                Task.Delay(PLAYER_LIST_INTERVAL).ContinueWith(t => _commandProcessor.PostAction(UpdatePlayerList)).DoNotWait();
            });
        }

        //
        // This is bound to the UI thread
        //
        private void UpdatePlayers()
        {
            if (_processingListplayers)
            {
                var message = "Player list is already being processed.";
                LogEvent(LogEventType.Event, message);
                LogEvent(LogEventType.All, message);
                return;
            }

            _processingListplayers = true;

            try
            {
                _commandProcessor.PostAction(UpdatePlayerDetails);

                this.CountPlayers = this.Players.Count(p => p.IsOnline);
                this.CountInvalidPlayers = this.Players.Count(p => !p.IsValid);
            }
            finally
            {
                _processingListplayers = false;
            }
        }
    }
}
