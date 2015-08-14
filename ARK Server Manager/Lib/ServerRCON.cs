using ARK_Server_Manager.Lib.ViewModel.RCON;
using NLog;
using System;
using System.Collections.Generic;
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

        public struct ConsoleCommand
        {
            public ConsoleStatus status;
            public string command;
            public IEnumerable<string> lines;
        };

        private const int ConnectionRetryDelay = 2000;

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
        }

        public Task<bool> IssueCommand(string userCommand)
        {
            return this.commandProcessor.PostAction(() => ProcessInput(userCommand));
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
            if(command.command.StartsWith("listplayers"))
            {                               
                foreach(var line in command.lines)
                {                    
                    var elements = line.Split(',');
                    if(elements.Length == 2)
                    {
                        var newPlayer = new ViewModel.RCON.PlayerInfo()
                        {
                            SteamName = elements[0].Substring(elements[0].IndexOf('.')+1),
                            SteamId = Int64.Parse(elements[1])
                        };

                        var existingPlayer = this.Players.FirstOrDefault(p => p.SteamId == newPlayer.SteamId);
                        if(existingPlayer == null)
                        {
                            this.Players.Add(newPlayer);
                            newPlayer.GetSteamInfoAsync();
                        }
                    }
                }
            }

        }

        private static readonly char[] splitChars = new char[] { '\n' };
        private bool ProcessInput(string command)
        {
            try
            {
                string result = String.Empty;
                if (this.console == null)
                {
                    // Try connecting to the server
                    if (!Reconnect())
                    {
                        return false;
                    }
                }

                result = this.console.SendCommand(command);

                var lines = result.Split(splitChars, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim());

                ConsoleCommand output = new ConsoleCommand
                {
                    status = ConsoleStatus.Connected,
                    command = command,
                    lines = lines
                };

                this.outputProcessor.Post(output);
                return true;
            }
            catch(Exception ex)
            {
                _logger.Debug("Failed to send command '{0}'.  {1}\n{2}", command, ex.Message, ex.ToString());
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
