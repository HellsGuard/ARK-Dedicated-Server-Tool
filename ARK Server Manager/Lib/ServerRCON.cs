using ARK_Server_Manager.Lib.ViewModel.RCON;
using ArkData;
using NLog;
using System;
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

        private static readonly char[] lineSplitChars = new char[] { '\n' };
        private static readonly char[] argsSplitChars = new char[] { ' ' };
        private readonly ActionQueue commandProcessor = new ActionQueue(TaskScheduler.Default);
        private readonly ActionQueue outputProcessor = new ActionQueue(TaskScheduler.FromCurrentSynchronizationContext());
        private readonly List<CommandListener> commandListeners = new List<CommandListener>();
        private RCONParameters rconParams;
        private QueryMaster.Rcon console;
        private bool processingListplayers = false;
        private bool updatingPlayerDetails = false;
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
                if (processingListplayers)
                {
                    LogEvent(LogEventType.Event, "listplayers is already being processed.");
                    command.suppressOutput = true;
                    return;
                }
                processingListplayers = true;

                try
                {
                    var output = new List<string>();
                    //
                    // Update the visible player list
                    //
                    var newPlayerList = new List<PlayerInfo>();
                    foreach (var line in command.lines)
                    {
                        var elements = line.Split(',');
                        if (elements.Length == 2)
                        {
                            var newPlayer = new PlayerInfo(this.debugLogger)
                            {
                                SteamName = elements[0].Substring(elements[0].IndexOf('.') + 1).Trim(),
                                SteamId = Int64.Parse(elements[1]),
                                IsOnline = true
                            };

                            if (newPlayerList.FirstOrDefault(p => p.SteamId == newPlayer.SteamId) != null)
                            {
                                // We received a duplicate.  Ignore it.
                                continue;
                            }

                            newPlayerList.Add(newPlayer);

                            var existingPlayer = this.Players.FirstOrDefault(p => p.SteamId == newPlayer.SteamId);
                            bool playerJoined = existingPlayer == null || existingPlayer.IsOnline == false;

                            if (existingPlayer == null)
                            {
                                this.Players.Add(newPlayer);
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
                    }

                    var droppedPlayers = this.Players.Where(p => newPlayerList.FirstOrDefault(np => np.SteamId == p.SteamId) == null).ToArray();
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

                    this.Players.Sort(p => !p.IsOnline);
                    this.CountPlayers = this.Players.Count(p => p.IsOnline);

                    if (this.Players.Count == 0 || newPlayerList.Count > 0)
                    {
                        commandProcessor.PostAction(UpdatePlayerDetails);
                    }

                    this.CountInvalidPlayers = this.Players.Count(p => !p.IsValid);

                    command.suppressOutput = false;
                    command.lines = output;
                }
                finally
                {
                    processingListplayers = false;
                }
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

        private async Task UpdatePlayerDetails()
        {
            if (updatingPlayerDetails)
                return;
            updatingPlayerDetails = true;

            try
            {
                if (!String.IsNullOrEmpty(rconParams.InstallDirectory))
                {
                    var savedPath = ServerProfile.GetProfileSavePath(rconParams.InstallDirectory, rconParams.AltSaveDirectoryName, rconParams.PGM_Enabled, rconParams.PGM_Name);
                    ArkDataContainer dataContainer = null;

                    try
                    {
                        dataContainer = await ArkDataContainer.CreateAsync(savedPath);
                    }
                    catch (Exception ex)
                    {
                        errorLogger.Error($"{nameof(UpdatePlayerDetails)} - Error: CreateAsync. {ex.Message}\r\n{ex.StackTrace}");
                        return;
                    }

                    try
                    {
                        await dataContainer.LoadSteamAsync(SteamUtils.SteamWebApiKey);
                    }
                    catch (Exception ex)
                    {
                        errorLogger.Error($"{nameof(UpdatePlayerDetails)} - Error: LoadSteamAsync. {ex.Message}\r\n{ex.StackTrace}");
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
                                    debugLogger.Debug($"{nameof(UpdatePlayerDetails)} - Error: corrupted profile.\r\n{playerData.Filename}.");
                                }
                            }

                            if (player != null)
                                player.UpdateDataAsync(playerData, savedPath).DoNotWait();
                        }

                        this.Players = new SortableObservableCollection<PlayerInfo>(players);
                        OnPlayerCollectionUpdated();
                    }).DoNotWait();
                }
            }
            finally
            {
                updatingPlayerDetails = false;
            }
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
