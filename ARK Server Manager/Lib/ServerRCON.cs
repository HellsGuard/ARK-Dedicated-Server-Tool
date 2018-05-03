using ARK_Server_Manager.Lib.ViewModel.RCON;
using ArkData;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace ARK_Server_Manager.Lib
{
    public class ServerRCON : DependencyObject, IAsyncDisposable
    {
        public event EventHandler PlayersCollectionUpdated;

        private const int STEAM_UPDATE_INTERVAL = 60;
        private const int LIST_PLAYERS_INTERVAL = 5000;
        private const int GET_CHAT_INTERVAL = 1000;
        private const string NoResponseMatch = "Server received, But no response!!";
        public const string NoResponseOutput = "NO_RESPONSE";

        public enum ConsoleStatus
        {
            Disconnected,
            Connected,
        };
        private enum LogEventType
        {
            All,
            Chat,
            Event
        }

        public class ConsoleCommand
        {
            public ConsoleStatus status;
            public string rawCommand;

            public string command;
            public string args;

            public bool suppressCommand;
            public bool suppressOutput;
            public IEnumerable<string> lines = new string[0];
        };

        private class CommandListener : IDisposable
        {
            public Action<ConsoleCommand> Callback { get; set; }
            public Action<CommandListener> DisposeAction { get; set; }

            public void Dispose()
            {
                DisposeAction(this);
            }
        }

        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(nameof(Status), typeof(ConsoleStatus), typeof(ServerRCON), new PropertyMetadata(ConsoleStatus.Disconnected));
        public static readonly DependencyProperty PlayersProperty = DependencyProperty.Register(nameof(Players), typeof(SortableObservableCollection<PlayerInfo>), typeof(ServerRCON), new PropertyMetadata(null));
        public static readonly DependencyProperty CountPlayersProperty = DependencyProperty.Register(nameof(CountPlayers), typeof(int), typeof(ServerRCON), new PropertyMetadata(0));
        public static readonly DependencyProperty CountInvalidPlayersProperty = DependencyProperty.Register(nameof(CountInvalidPlayers), typeof(int), typeof(ServerRCON), new PropertyMetadata(0));

        private static readonly ConcurrentDictionary<string, bool> locks = new ConcurrentDictionary<string, bool>();
        private static readonly char[] lineSplitChars = new char[] { '\n' };
        private static readonly char[] argsSplitChars = new char[] { ' ' };
        private readonly ActionQueue commandProcessor = new ActionQueue(TaskScheduler.Default);
        private readonly ActionQueue outputProcessor = new ActionQueue(TaskScheduler.FromCurrentSynchronizationContext());
        private readonly List<CommandListener> commandListeners = new List<CommandListener>();
        private RCONParameters rconParams;
        private QueryMaster.Rcon console;
        private int maxCommandRetries = 3;

        private Logger chatLogger;
        private Logger allLogger;
        private Logger eventLogger;
        private Logger debugLogger;
        private Logger errorLogger;

        public ServerRCON(RCONParameters parameters)
        {
            this.rconParams = parameters;
            this.Players = new SortableObservableCollection<PlayerInfo>();

            this.allLogger = App.GetProfileLogger(this.rconParams.ProfileName, "RCON_All", LogLevel.Info, LogLevel.Info);
            this.chatLogger = App.GetProfileLogger(this.rconParams.ProfileName, "RCON_Chat", LogLevel.Info, LogLevel.Info);
            this.eventLogger = App.GetProfileLogger(this.rconParams.ProfileName, "RCON_Event", LogLevel.Info, LogLevel.Info);
            this.debugLogger = App.GetProfileLogger(this.rconParams.ProfileName, "RCON_Debug", LogLevel.Trace, LogLevel.Debug);
            this.errorLogger = App.GetProfileLogger(this.rconParams.ProfileName, "RCON_Error", LogLevel.Error, LogLevel.Fatal);

            commandProcessor.PostAction(AutoPlayerList);
            commandProcessor.PostAction(AutoGetChat);
        }

        public ConsoleStatus Status
        {
            get { return (ConsoleStatus)GetValue(StatusProperty); }
            set { SetValue(StatusProperty, value); }
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
                    this.allLogger.Info(message);
                    return;

                case LogEventType.Chat:
                    this.chatLogger.Info(message);
                    return;

                case LogEventType.Event:
                    this.eventLogger.Info(message);
                    return;
            }
        }

        private Task AutoPlayerList()
        {
            lock (locks)
            {
                if (locks.TryGetValue($"{this.GetHashCode()}|PlayerList", out bool value) && value || !locks.TryAdd($"{this.GetHashCode()}|PlayerList", true))
                {
                    Task.Delay(LIST_PLAYERS_INTERVAL).ContinueWith(t => AutoPlayerList());
                    return TaskUtils.FinishedTask;
                }
            }

            return this.commandProcessor.PostAction(() =>
            {
                ProcessInput(new ConsoleCommand() { rawCommand = "listplayers", suppressCommand = true, suppressOutput = true });
                Task.Delay(LIST_PLAYERS_INTERVAL).ContinueWith(t => commandProcessor.PostAction(AutoPlayerList)).DoNotWait();
            });
        }

        private Task AutoGetChat()
        {
            return this.commandProcessor.PostAction(() =>
            {
                ProcessInput(new ConsoleCommand() { rawCommand = "getchat", suppressCommand = true, suppressOutput = true });
                Task.Delay(GET_CHAT_INTERVAL).ContinueWith(t => commandProcessor.PostAction(AutoGetChat)).DoNotWait();
            });
        }

        public async Task DisposeAsync()
        {
            await this.commandProcessor.DisposeAsync();
            await this.outputProcessor.DisposeAsync();

            foreach (var listener in this.commandListeners)
            {
                listener.Dispose();
            }
        }

        public IDisposable RegisterCommandListener(Action<ConsoleCommand> callback)
        {
            var listener = new CommandListener { Callback = callback, DisposeAction = UnregisterCommandListener };
            this.commandListeners.Add(listener);
            return listener;
        }

        private void UnregisterCommandListener(CommandListener listener)
        {
            this.commandListeners.Remove(listener);
        }

        private bool ProcessInput(ConsoleCommand command)
        {
            try
            {
                if (!command.suppressCommand)
                {
                    LogEvent(LogEventType.All, command.rawCommand);
                }

                var args = command.rawCommand.Split(argsSplitChars, 2);
                command.command = args[0];
                if (args.Length > 1)
                {
                    command.args = args[1];
                }

                string result = String.Empty;

                result = SendCommand(command.rawCommand);

                var lines = result.Split(lineSplitChars, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToArray();

                if (!command.suppressOutput)
                {
                    foreach (var line in lines)
                    {
                        LogEvent(LogEventType.All, line);
                    }
                }

                if (lines.Length == 1 && lines[0].StartsWith(NoResponseMatch))
                {
                    lines[0] = NoResponseOutput;
                }

                command.status = ConsoleStatus.Connected;
                command.lines = lines;

                this.outputProcessor.PostAction(() => ProcessOutput(command));
                return true;
            }
            catch (Exception ex)
            {
                errorLogger.Error($"Failed to send command '{command.rawCommand}'. {ex.Message}");
                command.status = ConsoleStatus.Disconnected;
                this.outputProcessor.PostAction(() => ProcessOutput(command));
                return false;
            }
        }

        //
        // This is bound to the UI thread
        //
        private void ProcessOutput(ConsoleCommand command)
        {
            //
            // Handle results
            //
            HandleCommand(command);
            NotifyCommand(command);
        }

        //
        // This is bound to the UI thread
        //
        private void HandleCommand(ConsoleCommand command)
        {
            //
            // Change the connection state as appropriate
            //
            this.Status = command.status;

            //
            // Perform per-command special processing to extract data
            //
            if (command.command.Equals("listplayers", StringComparison.OrdinalIgnoreCase))
            {
                var output = new List<string>();
                //
                // Update the visible player list
                //
                var players = new List<PlayerInfo>(this.Players);
                var newPlayers = new List<PlayerInfo>();
                foreach (var line in command.lines)
                {
                    var elements = line.Split(',');
                    if (elements.Length != 2)
                        // Invalid data. Ignore it.
                        continue;

                    var steamId = Int64.Parse(elements[1]);
                    if (newPlayers.FirstOrDefault(p => p.SteamId == steamId) != null)
                        // Duplicate data. Ignore it.
                        continue;

                    var newPlayer = new PlayerInfo(this.debugLogger)
                    {
                        SteamName = elements[0].Substring(elements[0].IndexOf('.') + 1).Trim(),
                        SteamId = steamId,
                        IsOnline = true,
                    };
                    newPlayers.Add(newPlayer);

                    var existingPlayer = players.FirstOrDefault(p => p.SteamId == newPlayer.SteamId);
                    bool playerJoined = existingPlayer == null || existingPlayer.IsOnline == false;

                    if (existingPlayer == null)
                    {
                        players.Add(newPlayer);
                    }
                    else
                    {
                        existingPlayer.IsOnline = true;
                    }

                    if (playerJoined)
                    {
                        var message = $"Player '{newPlayer.SteamName}' joined the game.";
                        output.Add(message);
                        LogEvent(LogEventType.Event, message);
                        LogEvent(LogEventType.All, message);
                    }
                }

                var droppedPlayers = players.Where(p => newPlayers.FirstOrDefault(np => np.SteamId == p.SteamId) == null).ToArray();
                foreach (var player in droppedPlayers)
                {
                    if (player.IsOnline)
                    {
                        var message = $"Player '{player.SteamName}' left the game.";
                        output.Add(message);
                        LogEvent(LogEventType.Event, message);
                        LogEvent(LogEventType.All, message);
                        player.IsOnline = false;
                    }
                }

                UpdatePlayerDetailsAsync(players).ContinueWith(t =>
                {
                    this.CountPlayers = this.Players.Count(p => p.IsOnline);
                    this.CountInvalidPlayers = this.Players.Count(p => !p.IsValid);

                    locks.TryRemove($"{this.GetHashCode()}|PlayerList", out bool value);
                }).DoNotWait();

                command.suppressOutput = false;
                command.lines = output;
            }
            else if (command.command.Equals("getchat", StringComparison.OrdinalIgnoreCase))
            {
                // TODO: Extract the player name from the chat
                var lines = command.lines.Where(l => !String.IsNullOrEmpty(l) && l != NoResponseOutput).ToArray();
                if (lines.Length == 0 && command.suppressCommand)
                {
                    command.suppressOutput = true;
                }
                else
                {
                    command.suppressOutput = false;
                    command.lines = lines;
                    foreach (var line in lines)
                    {
                        LogEvent(LogEventType.Chat, line);
                        LogEvent(LogEventType.All, line);
                    }
                }
            }
            else if (command.command.Equals("broadcast", StringComparison.OrdinalIgnoreCase))
            {
                LogEvent(LogEventType.Chat, command.rawCommand);
                command.suppressOutput = true;
            }
            else if (command.command.Equals("serverchat", StringComparison.OrdinalIgnoreCase))
            {
                LogEvent(LogEventType.Chat, command.rawCommand);
                command.suppressOutput = true;
            }
        }

        //
        // This is bound to the UI thread
        //
        private void NotifyCommand(ConsoleCommand command)
        {
            foreach (var listener in commandListeners)
            {
                try
                {
                    listener.Callback(command);
                }
                catch (Exception ex)
                {
                    errorLogger.Error("Exception in command listener: {0}\n{1}", ex.Message, ex.StackTrace);
                }
            }
        }

        public Task<bool> IssueCommand(string userCommand)
        {
            return this.commandProcessor.PostAction(() => ProcessInput(new ConsoleCommand() { rawCommand = userCommand }));
        }

        private string SendCommand(string command)
        {
            const int RETRY_DELAY = 100;

            Exception lastException = null;
            int retries = 0;

            while (retries < maxCommandRetries)
            {
                if (this.console != null)
                {
                    try
                    {
                        var result = this.console.SendCommand(command);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        // we will simply retry
                        lastException = ex;
                    }

                    Task.Delay(RETRY_DELAY).Wait();
                }

                try
                {
                    Reconnect();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                retries++;
            }

            this.maxCommandRetries = 10;
            errorLogger.Error($"Failed to connect to RCON at {this.rconParams.RCONHostIP}:{this.rconParams.RCONPort} with {this.rconParams.AdminPassword}. {lastException.Message}");
            throw new Exception($"Command failed to send after {maxCommandRetries} attempts.  Last exception: {lastException.Message}", lastException);
        }

        private async Task UpdatePlayerDetailsAsync(List<PlayerInfo> players)
        {
            if (!string.IsNullOrWhiteSpace(rconParams.InstallDirectory))
            {
                var savedPath = ServerProfile.GetProfileSavePath(rconParams.InstallDirectory, rconParams.AltSaveDirectoryName, rconParams.PGM_Enabled, rconParams.PGM_Name);
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
                    errorLogger.Error($"{nameof(UpdatePlayerDetailsAsync)} - Error: CreateAsync. {ex.Message}\r\n{ex.StackTrace}");
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
                    errorLogger.Error($"{nameof(UpdatePlayerDetailsAsync)} - Error: LoadSteamAsync. {ex.Message}\r\n{ex.StackTrace}");
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
                                player = new PlayerInfo(this.debugLogger)
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
                                    player = new PlayerInfo(this.debugLogger)
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
                                debugLogger.Debug($"{nameof(UpdatePlayerDetailsAsync)} - Error: corrupted profile.\r\n{playerData.Filename}.");
                            }
                        }

                        if (player != null)
                        {
                            player.UpdateData(playerData, playerData.LastSteamUpdateUtc.Equals(lastSteamUpdateUtc));

                            await TaskUtils.RunOnUIThreadAsync(() => {
                                player.IsAdmin = rconParams?.Server?.Profile?.ServerFilesAdmins?.Any(u => u.SteamId.Equals(player.SteamId.ToString(), StringComparison.OrdinalIgnoreCase)) ?? false;
                                player.IsWhitelisted = rconParams?.Server?.Profile?.ServerFilesWhitelisted?.Any(u => u.SteamId.Equals(player.SteamId.ToString(), StringComparison.OrdinalIgnoreCase)) ?? false;

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

        private bool Reconnect()
        {
            if (this.console != null)
            {
                this.console.Dispose();
                this.console = null;
            }

            var endpoint = new IPEndPoint(this.rconParams.RCONHostIP, this.rconParams.RCONPort);
            var server = QueryMaster.ServerQuery.GetServerInstance(QueryMaster.EngineType.Source, endpoint);
            this.console = server.GetControl(this.rconParams.AdminPassword);
            return true;
        }

        internal void OnPlayerCollectionUpdated()
        {
            PlayersCollectionUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}
