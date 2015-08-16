using ARK_Server_Manager.Lib.ViewModel.RCON;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ARK_Server_Manager.Lib
{
    public class ServerRCON : DependencyObject, IAsyncDisposable
    {
        public static Logger _logger = LogManager.GetCurrentClassLogger();

        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(nameof(Status), typeof(ConsoleStatus), typeof(ServerRCON), new PropertyMetadata(ConsoleStatus.Disconnected));
        public static readonly DependencyProperty PlayersProperty =
            DependencyProperty.Register(nameof(Players), typeof(SortableObservableCollection<PlayerInfo>), typeof(ServerRCON), new PropertyMetadata(null));

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
                
        public enum ConsoleStatus
        {
            Disconnected,
            Connected,
        };

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

        private const int ListPlayersPeriod = 5000;
        private const int GetChatPeriod = 1000;
        private readonly ActionQueue commandProcessor;
        private readonly ActionBlock<ConsoleCommand> outputProcessor;
        private ServerRuntime.RuntimeProfileSnapshot snapshot;
        private readonly PropertyChangeNotifier runtimeChangedNotifier;
        private QueryMaster.Rcon console;

        public ServerRCON(Server server)
        {
            this.runtimeChangedNotifier = new PropertyChangeNotifier(server.Runtime, ServerRuntime.ProfileSnapshotProperty, (s, d) =>
            {
                this.snapshot = (ServerRuntime.RuntimeProfileSnapshot)d.NewValue;
                commandProcessor.PostAction(() => Reconnect());
            });

            this.commandProcessor = new ActionQueue();

            // This is on the UI thread so we can do things like update dependency properties and whatnot.
            this.outputProcessor = new ActionBlock<ConsoleCommand>(new Func<ConsoleCommand, Task>(ProcessOutput),
                                              new ExecutionDataflowBlockOptions
                                              {
                                                  MaxDegreeOfParallelism = 1,
                                                  TaskScheduler = TaskScheduler.FromCurrentSynchronizationContext()
                                              });

            this.Players = new SortableObservableCollection<PlayerInfo>();

            this.snapshot = server.Runtime.ProfileSnapshot;
            commandProcessor.PostAction(() => Reconnect());
            commandProcessor.PostAction(AutoPlayerList);
            commandProcessor.PostAction(AutoGetChat);
        }

        private Task AutoPlayerList()
        {
            return this.commandProcessor.PostAction(() =>
            {
                ProcessInput(new ConsoleCommand() { rawCommand = "listplayers", suppressCommand = true, suppressOutput = true });
                Task.Delay(ListPlayersPeriod).ContinueWith(t => commandProcessor.PostAction(AutoPlayerList)).DoNotWait();
            });
        }

        private Task AutoGetChat()
        {
            return this.commandProcessor.PostAction(() =>
            {
                ProcessInput(new ConsoleCommand() { rawCommand = "getchat", suppressCommand = true, suppressOutput = false });
                Task.Delay(GetChatPeriod).ContinueWith(t => commandProcessor.PostAction(AutoGetChat)).DoNotWait();
            });
        }

        public Task<bool> IssueCommand(string userCommand)
        {
            return this.commandProcessor.PostAction(() => ProcessInput(new ConsoleCommand() { rawCommand = userCommand }));
        }

        public async Task DisposeAsync()
        {
            await this.commandProcessor.DisposeAsync();
            this.outputProcessor.Complete();
            this.runtimeChangedNotifier.Dispose();
        }

        private class CommandListener : IDisposable
        {
            public Action<ConsoleCommand> Callback { get; set; }
            public Action<CommandListener> DisposeAction { get; set; }

            public void Dispose()
            {
                DisposeAction(this);
            }
        }

        List<CommandListener> commandListeners = new List<CommandListener>();

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

        //
        // This is bound to the UI thread
        //
        private Task ProcessOutput(ConsoleCommand command)
        {
            //
            // Handle results
            //
            HandleCommand(command);
            NotifyCommand(command);
            return TaskUtils.FinishedTask;
        }

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
                    _logger.Error("Exception in command listener: {0}\n{1}", ex.Message, ex.StackTrace);
                }
            }
        }

        private void HandleCommand(ConsoleCommand command)
        {
            //
            // Change the connection state as appropriate
            //
            TaskUtils.RunOnUIThreadAsync(() => { this.Status = command.status; }).DoNotWait();

            //
            // Perform per-command special processing to extract data
            //
            if(command.command.Equals("listplayers", StringComparison.OrdinalIgnoreCase))
            {
                //
                // Update the visible player list
                //
                TaskUtils.RunOnUIThreadAsync(() =>
                {
                    var newPlayerList = new List<PlayerInfo>();                
                    foreach(var line in command.lines)
                    {                    
                        var elements = line.Split(',');
                        if(elements.Length == 2)
                        {
                            var newPlayer = new ViewModel.RCON.PlayerInfo()
                            {
                                SteamName = elements[0].Substring(elements[0].IndexOf('.') + 1).Trim(),
                                SteamId = Int64.Parse(elements[1]),
                                IsOnline = true
                            };

                            newPlayerList.Add(newPlayer);

                            var existingPlayer = this.Players.FirstOrDefault(p => p.SteamId == newPlayer.SteamId);
                            if (existingPlayer == null)
                            {
                                this.Players.Add(newPlayer);
                            }
                            else
                            {
                                existingPlayer.IsOnline = true;
                            }
                        }
                    }

               
                    var droppedPlayers = this.Players.Where(p => newPlayerList.FirstOrDefault(np => np.SteamId == p.SteamId) == null).ToArray();
                    foreach (var player in droppedPlayers)
                    {
                        player.IsOnline = false;
                    }

                    this.Players.Sort(p => !p.IsOnline);

                    if (this.Players.Count == 0 || newPlayerList.Count > 0)
                    {
                        commandProcessor.PostAction(UpdatePlayerDetails);
                    }
                }).DoNotWait();
            }
            else if(command.command.Equals("getchat", StringComparison.OrdinalIgnoreCase))
            {
                // TODO: Extract the player name from the chat
                var lines = command.lines.Where(l => !String.IsNullOrEmpty(l) && l != NoResponseOutput).ToArray();
                if(lines.Length == 0 && command.suppressCommand)
                {
                    command.suppressOutput = true;
                }
                else
                {
                    command.lines = lines;
                }
            }
            else if (command.command.Equals("broadcast", StringComparison.OrdinalIgnoreCase))
            {
                command.suppressOutput = true;
            }
            else if (command.command.Equals("serverchat", StringComparison.OrdinalIgnoreCase))
            {
                command.suppressOutput = true;
            }
        }

        private async Task UpdatePlayerDetails()
        {
            var savedArksPath = Path.Combine(snapshot.InstallDirectory, Config.Default.SavedArksRelativePath);
            var arkData = await ArkData.ArkDataContainer.CreateAsync(savedArksPath);
            await arkData.LoadSteamAsync(Config.Default.SteamAPIKey);
            TaskUtils.RunOnUIThreadAsync(() =>
            {
                foreach (var playerData in arkData.Players)
                {
                    var playerToUpdate = this.Players.FirstOrDefault(p => p.SteamId == Int64.Parse(playerData.SteamId));
                    if (playerToUpdate != null)
                    {
                        playerToUpdate.UpdateArkData(playerData).DoNotWait();
                    }
                    else
                    {
                        var newPlayer = new PlayerInfo() { SteamId = Int64.Parse(playerData.SteamId), SteamName = playerData.SteamName };
                        newPlayer.UpdateArkData(playerData).DoNotWait();
                        this.Players.Add(newPlayer);
                    }
                }
            }).DoNotWait();           
        }

        private static readonly char[] lineSplitChars = new char[] { '\n' };
        private static readonly char[] argsSplitChars = new char[] { ' ' };
        private const string NoResponseMatch = "Server received, But no response!!";
        public const string NoResponseOutput = "NO_RESPONSE";
        
        private bool ProcessInput(ConsoleCommand command)
        {
            try
            {
                var args = command.rawCommand.Split(argsSplitChars, 2);
                command.command = args[0];
                if(args.Length > 1)
                {
                    command.args = args[1];
                }

                string result = String.Empty;
                if (this.console == null)
                {
                    // Try connecting to the server
                    if (!Reconnect())
                    {
                        command.status = ConsoleStatus.Disconnected;
                        this.outputProcessor.Post(command);
                        return false;
                    }
                }

                result = this.console.SendCommand(command.rawCommand);

                var lines = result.Split(lineSplitChars, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToArray();

                if(lines.Length == 1 && lines[0].StartsWith(NoResponseMatch))
                {
                    lines[0] = NoResponseOutput;
                }

                command.status = ConsoleStatus.Connected;
                command.lines = lines;

                this.outputProcessor.Post(command);
                return true;
            }
            catch(Exception ex)
            {
                _logger.Debug("Failed to send command '{0}'.  {1}\n{2}", command, ex.Message, ex.ToString());
                command.status = ConsoleStatus.Disconnected;
                this.outputProcessor.Post(command);
                return false;
            }            
        }

        private bool Reconnect()
        {
            if(this.console != null)
            {
                this.console.Dispose();
                this.console = null;
            }
          
            try
            {
                var endpoint = new IPEndPoint(IPAddress.Parse(this.snapshot.ServerIP), this.snapshot.RCONPort);    
                var server = QueryMaster.ServerQuery.GetServerInstance(QueryMaster.EngineType.Source, endpoint);
                this.console = server.GetControl(this.snapshot.AdminPassword);
                return true;
            }
            catch(Exception ex)
            {
                _logger.Debug("Failed to connect to RCON at {0}:{1} with {2}: {3}\n{4}", 
                    this.snapshot.ServerIP, 
                    this.snapshot.RCONPort, 
                    this.snapshot.AdminPassword, 
                    ex.Message, 
                    ex.StackTrace);
                return false;
            }
        }
    }
}
