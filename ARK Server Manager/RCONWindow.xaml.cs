using ARK_Server_Manager.Lib;
using ARK_Server_Manager.Lib.ViewModel;
using ARK_Server_Manager.Lib.ViewModel.RCON;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace ARK_Server_Manager
{
    public enum PlayerSortType
    {
        Online = 0,
        Name = 1,
        Tribe = 2,
        LastUpdated = 3,
    }

    [Flags]
    public enum PlayerFilterType
    {
        None            = 0,
        Offline         = 0x1,
        Online          = 0x2,
        Banned          = 0x4,
        Whitelisted     = 0x8
    }

    public enum InputMode
    {
        Command,
        Global,
        Broadcast,
    }

    public enum InputWindowMode
    {
        None,
        ServerChatTo,
        RenamePlayer,
        RenameTribe,
    }

    public class ScrollToBottomAction : TriggerAction<RichTextBox>
    {
        protected override void Invoke(object parameter)
        {
            AssociatedObject.ScrollToEnd();
        }
    }

    public class RCONOutput_CommandTime : Run
    {
        public RCONOutput_CommandTime() : this(DateTime.Now) { }
        public RCONOutput_CommandTime(DateTime time) : base($"[{time.ToString("g")}] ") { }
    }

    public class RCONOutput_TimedCommand : Span
    {
        protected RCONOutput_TimedCommand() : base()
        {
            base.Inlines.Add(new RCONOutput_CommandTime());
        }

        public RCONOutput_TimedCommand(Inline output) : this()
        {            
            base.Inlines.Add(output);
        }

        public RCONOutput_TimedCommand(string output) : this(new Run(output)) { }

    }

    public class RCONOutput_Comment : Run
    {
        public RCONOutput_Comment(string value) : base(value) { }
    }

    public class RCONOutput_ChatSend : RCONOutput_TimedCommand
    {
        public RCONOutput_ChatSend(string target, string output) : base($"[{target}] {output}") { }
    }
    public class RCONOutput_Broadcast : RCONOutput_ChatSend
    {
        public RCONOutput_Broadcast(string output) : base("ALL", output) { }
    }

    public class RCONOutput_ConnectionChanged : RCONOutput_TimedCommand
    {
        public RCONOutput_ConnectionChanged(bool isConnected) : base(isConnected ? "Connection established." : "Connection lost.") { }
    }

    public class RCONOutput_Command : RCONOutput_TimedCommand
    {
        public RCONOutput_Command(string text) : base(text) { }
    };

    public class RCONOutput_NoResponse : RCONOutput_TimedCommand
    {
        public RCONOutput_NoResponse() : base("Command returned no data") { }
    };

    public class RCONOutput_CommandOutput : RCONOutput_TimedCommand
    {
        public RCONOutput_CommandOutput(string text) : base(text) { }
    };

    /// <summary>
    /// Interaction logic for RCON.xaml
    /// </summary>
    public partial class RCONWindow : Window
    {
        public bool ScrollOnNewInput
        {
            get { return (bool)GetValue(ScrollOnNewInputProperty); }
            set { SetValue(ScrollOnNewInputProperty, value); }
        }

        public static readonly DependencyProperty ScrollOnNewInputProperty = DependencyProperty.Register(nameof(ScrollOnNewInput), typeof(bool), typeof(RCONWindow), new PropertyMetadata(true));

        public ICollectionView  PlayersView
        {
            get { return (ICollectionView)GetValue(PlayersViewProperty); }
            set { SetValue(PlayersViewProperty, value); }
        }

        public static readonly DependencyProperty PlayersViewProperty = DependencyProperty.Register(nameof(PlayersView), typeof(ICollectionView), typeof(RCONWindow), new PropertyMetadata(null));


        public PlayerSortType PlayerSorting
        {
            get { return (PlayerSortType)GetValue(PlayerSortingProperty); }
            set { SetValue(PlayerSortingProperty, value); }
        }

        public static readonly DependencyProperty PlayerSortingProperty = DependencyProperty.Register(nameof(PlayerSorting), typeof(PlayerSortType), typeof(RCONWindow), new PropertyMetadata(PlayerSortType.Online));

        public PlayerFilterType PlayerFiltering
        {
            get { return (PlayerFilterType)GetValue(PlayerFilteringProperty); }
            set { SetValue(PlayerFilteringProperty, value); }
        }

        public static readonly DependencyProperty PlayerFilteringProperty = DependencyProperty.Register(nameof(PlayerFiltering), typeof(PlayerFilterType), typeof(RCONWindow), new PropertyMetadata(PlayerFilterType.Online | PlayerFilterType.Offline | PlayerFilterType.Banned | PlayerFilterType.Whitelisted));


        public RCONParameters RCONParameters
        {
            get { return (RCONParameters)GetValue(ServerProperty); }
            set { SetValue(ServerProperty, value); }
        }

        public static readonly DependencyProperty ServerProperty = DependencyProperty.Register(nameof(RCONParameters), typeof(RCONParameters), typeof(RCONWindow), new PropertyMetadata(null));

        public Config CurrentConfig
        {
            get { return (Config)GetValue(CurrentConfigProperty); }
            set { SetValue(CurrentConfigProperty, value); }
        }

        public static readonly DependencyProperty CurrentConfigProperty = DependencyProperty.Register(nameof(CurrentConfig), typeof(Config), typeof(RCONWindow), new PropertyMetadata(Config.Default));

        public ServerRCON ServerRCON
        {
            get { return (ServerRCON)GetValue(ServerRCONProperty); }
            set { SetValue(ServerRCONProperty, value); }
        }

        public static readonly DependencyProperty ServerRCONProperty = DependencyProperty.Register(nameof(ServerRCON), typeof(ServerRCON), typeof(RCONWindow), new PropertyMetadata(null));    

        public InputMode CurrentInputMode
        {
            get { return (InputMode)GetValue(CurrentInputModeProperty); }
            set { SetValue(CurrentInputModeProperty, value); }
        }

        public static readonly DependencyProperty CurrentInputModeProperty = DependencyProperty.Register(nameof(CurrentInputMode), typeof(InputMode), typeof(RCONWindow), new PropertyMetadata(InputMode.Command));

        public RCONWindow(RCONParameters parameters)
        {
            InitializeComponent();
            this.CurrentInputWindowMode = InputWindowMode.None;
            this.RCONParameters = parameters;
            this.PlayerFiltering = (PlayerFilterType)Config.Default.RCON_PlayerListFilter;
            this.PlayerSorting = (PlayerSortType)Config.Default.RCON_PlayerListSort;
            this.ServerRCON = new ServerRCON(parameters);
            this.ServerRCON.RegisterCommandListener(RenderRCONCommandOutput);            
            this.PlayersView = CollectionViewSource.GetDefaultView(this.ServerRCON.Players);
            this.PlayersView.Filter = p =>
            {
                var player = p as PlayerInfo;

                return (this.PlayerFiltering.HasFlag(PlayerFilterType.Online) && player.IsOnline) ||
                       (this.PlayerFiltering.HasFlag(PlayerFilterType.Offline) && !player.IsOnline) ||
                       (this.PlayerFiltering.HasFlag(PlayerFilterType.Banned) && player.IsBanned) ||
                       (this.PlayerFiltering.HasFlag(PlayerFilterType.Whitelisted) && player.IsWhitelisted);
            };

            var notifier = new PropertyChangeNotifier(this.ServerRCON, ServerRCON.StatusProperty, (s, a) =>
            {
                this.RenderConnectionStateChange(a);
            });
            this.DataContext = this;

            AddCommentsBlock(
                "Enter commands or chat into the box at the bottom.",
                "In Command mode, everything you enter will be a normal admin command",
                "In Broadcast mode, everything you enter will be a global broadcast",
                "You may always prefix a command with / to be treated as a command and not chat.",
                "Right click on players in the list to access player commands",
                "Type /help to get help");

            if (this.RCONParameters.RCONWindowExtents.Width > 50 && this.RCONParameters.RCONWindowExtents.Height > 50)
            {
                this.Left = this.RCONParameters.RCONWindowExtents.Left;
                this.Top = this.RCONParameters.RCONWindowExtents.Top;
                this.Width = this.RCONParameters.RCONWindowExtents.Width;
                this.Height = this.RCONParameters.RCONWindowExtents.Height;

                //
                // Fix issues where the console was saved while offscreen.
                if(this.Left == -32000)
                {
                    this.Left = 0;
                }

                if(this.Top == -32000)
                {
                    this.Top = 0;
                }
            }

            this.ConsoleInput.Focus();
        }

        private static Dictionary<Server, RCONWindow> RCONWindows = new Dictionary<Server, RCONWindow>();

        public static RCONWindow GetRCONForServer(Server server)
        {
            RCONWindow window;
            if(!RCONWindows.TryGetValue(server, out window) || !window.IsLoaded)
            {
                window = new RCONWindow(new RCONParameters()
                {
                    AdminPassword = server.Runtime.ProfileSnapshot.AdminPassword,
                    InstallDirectory = server.Runtime.ProfileSnapshot.InstallDirectory,
                    ProfileName = server.Profile.ProfileName,
                    RCONPort = server.Runtime.ProfileSnapshot.RCONPort,
                    ServerIP = server.Runtime.ProfileSnapshot.ServerIP,
                    RCONWindowExtents = server.Profile.RCONWindowExtents,
                    MaxPlayers = server.Runtime.MaxPlayers,
                    Server = server
                });
                RCONWindows[server] = window;
            }

            return window;
        }

        public static RCONWindow GetRCON(RCONParameters parameters)
        {
            return new RCONWindow(parameters);
        }

        public static void CloseAllWindows()
        {
            foreach(var window in RCONWindows.Values)
            {
                if(window.IsLoaded)
                {
                    window.Close();
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            this.ServerRCON.DisposeAsync().DoNotWait();
            base.OnClosing(e);
        }

        private InputWindowMode CurrentInputWindowMode
        {
            get;
            set;
        }

        public ICommand Button1Command
        {
            get
            {
                return new RelayCommand<object>(
                    execute: (_) =>
                    {
                        inputBox.Visibility = System.Windows.Visibility.Collapsed;
                        dockPanel.IsEnabled = true;

                        PlayerInfo player;
                        var inputText = inputTextBox.Text;

                        switch (this.CurrentInputWindowMode)
                        {
                            case InputWindowMode.ServerChatTo:
                                player = inputBox.Tag as PlayerInfo;
                                if (player != null)
                                    this.ServerRCON.IssueCommand($"ServerChatTo \"{player.SteamId}\" {inputText}");
                                break;

                            case InputWindowMode.RenamePlayer:
                                player = inputBox.Tag as PlayerInfo;
                                if (player != null)
                                    this.ServerRCON.IssueCommand($"RenamePlayer \"{player.ArkData.CharacterName}\" {inputText}");
                                break;

                            case InputWindowMode.RenameTribe:
                                player = inputBox.Tag as PlayerInfo;
                                if (player != null)
                                    this.ServerRCON.IssueCommand($"RenameTribe \"{player.TribeName}\" {inputText}");
                                break;

                            default:
                                break;
                        }

                        // Clear InputBox.
                        inputTextBox.Text = String.Empty;
                        this.CurrentInputWindowMode = InputWindowMode.None;
                    },
                    canExecute: (_) => true
                );
            }
        }

        public ICommand Button2Command
        {
            get
            {
                return new RelayCommand<object>(
                    execute: (_) =>
                    {
                        inputBox.Visibility = System.Windows.Visibility.Collapsed;
                        dockPanel.IsEnabled = true;

                        switch (this.CurrentInputWindowMode)
                        {
                            default:
                                break;
                        }

                        // Clear InputBox.
                        inputTextBox.Text = String.Empty;
                        this.CurrentInputWindowMode = InputWindowMode.None;
                    },
                    canExecute: (_) => true
                );
            }
        }

        public ICommand ClearLogsCommand
        {
            get
            {
                return new RelayCommand<object>(
                    execute: (_) =>
                    {
                        string logsDir = String.Empty;
                        try
                        {
                            logsDir = App.GetProfileLogDir(this.RCONParameters.ProfileName);
                            Directory.Delete(logsDir, true);
                        }
                        catch (Exception)
                        {
                            // Ignore any failures here, best effort only.
                        }

                        MessageBox.Show($"Logs in {logsDir} deleted.", "Logs deleted", MessageBoxButton.OK, MessageBoxImage.Information);
                    },
                    canExecute: (_) => this.RCONParameters.Server != null
                );
            }
        }

        public ICommand ViewLogsCommand
        {
            get
            {
                return new RelayCommand<object>(
                    execute: (_) =>
                    {
                        string logsDir = String.Empty;
                        try
                        {
                            logsDir = App.GetProfileLogDir(this.RCONParameters.ProfileName);
                            Process.Start(logsDir);
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show($"Unable to open the logs directory at {logsDir}.  Please make sure this directory exists and that you have permission to access it.\nException: {ex.Message}", "Can't open logs", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    },
                    canExecute: (_) => this.RCONParameters.Server != null
                );
            }
        }

        public ICommand SaveWorldCommand
        {
            get
            {
                return new RelayCommand<object>(
                    execute: (_) =>
                    {
                        var message = "A world save is about to be performed, you may experience some lag during this process. Please be patient.";
                        this.ServerRCON.IssueCommand($"broadcast {message}");

                        this.ServerRCON.IssueCommand("saveworld");
                    },
                    canExecute: (_) => true
                );
            }
        }

        public ICommand DestroyWildDinosCommand
        {
            get
            {
                return new RelayCommand<object>(
                    execute: (_) =>
                    {
                        var message = "About to destroy all wild creatures, you may experience some lag during this process. Please be patient.";
                        this.ServerRCON.IssueCommand($"broadcast {message}");

                        this.ServerRCON.IssueCommand("DestroyWildDinos");
                    },
                    canExecute: (_) => true
                );
            }
        }

        public ICommand SortPlayersCommand
        {
            get
            {
                return new RelayCommand<PlayerSortType>(
                    execute: (sort) => 
                    {
                        this.PlayersView.SortDescriptions.Clear();
                        Config.Default.RCON_PlayerListSort = (int)this.PlayerSorting;
                        switch(sort)
                        {
                            case PlayerSortType.Name:
                                this.PlayersView.ToggleSorting(nameof(PlayerInfo.SteamName));
                                break;

                            case PlayerSortType.Online:
                                this.PlayersView.ToggleSorting(nameof(PlayerInfo.IsOnline), ListSortDirection.Descending);
                                break;

                            case PlayerSortType.Tribe:
                                this.PlayersView.ToggleSorting(nameof(PlayerInfo.TribeName));
                                break;

                            case PlayerSortType.LastUpdated:
                                this.PlayersView.ToggleSorting(nameof(PlayerInfo.LastUpdated), ListSortDirection.Descending);
                                break;
                        }
                    },
                    canExecute: (sort) => true
                );
            }
        }       

        public ICommand FilterPlayersCommand
        {
            get
            {
                return new RelayCommand<PlayerFilterType>(
                    execute: (filter) => 
                    {
                        this.PlayerFiltering ^= filter;
                        Config.Default.RCON_PlayerListFilter = (int)this.PlayerFiltering;
                        this.PlayersView.Refresh();
                    },
                    canExecute: (filter) => true
                );
            }
        }

        public ICommand ChatPlayerCommand
        {
            get
            {
                return new RelayCommand<PlayerInfo>(
                    execute: (player) => 
                    {
                        dockPanel.IsEnabled = false;

                        CurrentInputWindowMode = InputWindowMode.ServerChatTo;
                        inputBox.Tag = player;
                        inputTitle.Text = $"Send a chat message to : {player.SteamName}";
                        inputTextBox.Text = string.Empty;
                        button1.Content = "Send";
                        button2.Content = "Cancel";
                        inputBox.Visibility = System.Windows.Visibility.Visible;
                    },
                    canExecute: (player) => true //player != null && player.IsOnline
                );
            }
        }

        public ICommand RenamePlayerCommand
        {
            get
            {
                return new RelayCommand<PlayerInfo>(
                    execute: (player) => 
                    {
                        dockPanel.IsEnabled = false;

                        CurrentInputWindowMode = InputWindowMode.RenamePlayer;
                        inputBox.Tag = player;
                        inputTitle.Text = $"Rename player : from {player.ArkData.CharacterName}";
                        inputTextBox.Text = string.Empty;
                        button1.Content = "Change";
                        button2.Content = "Cancel";
                        inputBox.Visibility = System.Windows.Visibility.Visible;
                    },
                    canExecute: (player) => player != null && player.IsOnline
                );
            }
        }

        public ICommand RenameTribeCommand
        {
            get
            {
                return new RelayCommand<PlayerInfo>(
                    execute: (player) => 
                    {
                        dockPanel.IsEnabled = false;

                        CurrentInputWindowMode = InputWindowMode.RenameTribe;
                        inputBox.Tag = player;
                        inputTitle.Text = $"Rename tribe : from {player.TribeName}";
                        inputTextBox.Text = string.Empty;
                        button1.Content = "Change";
                        button2.Content = "Cancel";
                        inputBox.Visibility = System.Windows.Visibility.Visible;
                    },
                    canExecute: (player) => player != null && player.IsOnline
                );
            }
        }

        public ICommand KillPlayerCommand
        {
            get
            {
                return new RelayCommand<PlayerInfo>(
                    execute: (player) => { this.ServerRCON.IssueCommand($"KillPlayer {player.ArkData.Id}"); },
                    canExecute: (player) => false //player != null && player.IsOnline
                );
            }
        }

        public ICommand KickPlayerCommand
        {
            get
            {
                return new RelayCommand<PlayerInfo>(
                    execute: (player) => { this.ServerRCON.IssueCommand($"KickPlayer {player.SteamId}"); },
                    canExecute: (player) => player != null && player.IsOnline
                    );
            }
        }

        public ICommand BanPlayerCommand
        {
            get
            {
                return new RelayCommand<PlayerInfo>(
                    execute: (player) => { var command = player.IsBanned ? "unbanplayer" : "banplayer" ;  this.ServerRCON.IssueCommand($"{command} {player.SteamId}"); },
                    canExecute: (player) => true
                    );
            }
        }

        public ICommand WhitelistPlayerCommand
        {
            get
            {
                return new RelayCommand<PlayerInfo>(
                    execute: (player) => { var command = player.IsWhitelisted ? "DisallowPlayerToJoinNoCheck" : "AllowPlayerToJoinNoCheck"; this.ServerRCON.IssueCommand($"{command} {player.SteamId}"); },
                    canExecute: (player) => true
                );
            }
        }

        public ICommand ViewPlayerSteamProfileCommand
        {
            get
            {
                return new RelayCommand<PlayerInfo>(
                    execute: (player) => { Process.Start(player.ArkData.ProfileUrl); },
                    canExecute: (player) => player != null && player.ArkData != null && !String.IsNullOrWhiteSpace(player.ArkData.ProfileUrl)
                );
            }
        }

        public ICommand ViewPlayerProfileCommand
        {
            get
            {
                return new RelayCommand<PlayerInfo>(
                    execute: (player) => {
                        var window = new PlayerProfile(player, this.RCONParameters.InstallDirectory);
                        window.Owner = this;
                        window.ShowDialog();
                    },
                    canExecute: (player) => player != null && player.ArkData != null
                    );
            }
        }

        public ICommand ViewPlayerTribeCommand
        {
            get
            {
                return new RelayCommand<PlayerInfo>(
                    execute: (player) => {
                        var window = new TribeProfile(player, this.ServerRCON.Players, this.RCONParameters.InstallDirectory);
                        window.Owner = this;
                        window.ShowDialog();
                    },
                    canExecute: (player) => player != null && player.ArkData != null && !String.IsNullOrWhiteSpace(player.TribeName)
                    );
            }
        }

        public ICommand CopySteamIDCommand
        {
            get
            {
                return new RelayCommand<PlayerInfo>(
                    execute: (player) => 
                    {
                        try
                        {
                            System.Windows.Clipboard.SetText(player.SteamId.ToString());
                            MessageBox.Show($"{player.SteamName}'s Steam ID copied to the clipboard", "Steam ID copied", MessageBoxButton.OK);
                        }
                        catch
                        {
                            MessageBox.Show("Clipboard could not be opened.  Another application may be using it.  Please try closing other applications and trying again.", "Copy to clipboard failed.", MessageBoxButton.OK);
                        }
                    },
                    canExecute: (player) => player != null
                    );

            }
        }

        public ICommand CopyPlayerIDCommand
        {
            get
            {
                return new RelayCommand<PlayerInfo>(
                    execute: (player) => 
                    {
                        if (player.ArkData != null)
                        {
                            try
                            {
                                System.Windows.Clipboard.SetText(player.ArkData.Id.ToString());
                                MessageBox.Show($"{player.SteamName}'s ingame UE4 ID copied to the clipboard", "Player ID copied", MessageBoxButton.OK);
                            }
                            catch
                            {
                                MessageBox.Show("Clipboard could not be opened.  Another application may be using it.  Please try closing other applications and trying again.", "Copy to clipboard failed.", MessageBoxButton.OK);
                            }
                        }
                    },
                    canExecute: (player) => player != null && player.ArkData != null
                    );

            }
        }

        private void RenderConnectionStateChange(DependencyPropertyChangedEventArgs a)
        {
            var oldStatus = (ServerRCON.ConsoleStatus)a.OldValue;
            var newStatus = (ServerRCON.ConsoleStatus)a.NewValue;
            if(oldStatus != newStatus)
            {
                Paragraph p = new Paragraph();
                if (newStatus == ServerRCON.ConsoleStatus.Connected)
                {
                    p.Inlines.Add(new RCONOutput_ConnectionChanged(true));
                }
                else
                {
                    p.Inlines.Add(new RCONOutput_ConnectionChanged(false));
                }

                AddBlockContent(p);
            }
        }

        private void RenderRCONCommandOutput(ServerRCON.ConsoleCommand command)
        {
            //
            // Format output
            //
            Paragraph p = new Paragraph();

            if (!command.suppressCommand)
            {
                foreach (var element in FormatCommandInput(command))
                {
                    p.Inlines.Add(element);
                }
            }

            if (!command.suppressOutput)
            {
                foreach (var element in FormatCommandOutput(command))
                {
                    p.Inlines.Add(element);
                }
            }

            if (!(command.suppressCommand && command.suppressOutput))
            {
                if (p.Inlines.Count > 0)
                {
                    AddBlockContent(p);
                }
            }
        }

        private void AddBlockContent(Block b)
        {
            ConsoleContent.Blocks.Add(b);            
        }

        private IEnumerable<Inline> FormatCommandInput(ServerRCON.ConsoleCommand command)
        {
            if (command.command.Equals("broadcast", StringComparison.OrdinalIgnoreCase))
            {
                yield return new RCONOutput_Broadcast(command.args);
            }
            else
            {
                yield return new RCONOutput_Command($"> {command.rawCommand}");
            }

            if(!command.suppressOutput && command.lines.Count() > 0)
            {
                yield return new LineBreak();
            }
        }

        private void AddCommentsBlock(params string[] lines)
        {
            var p = new Paragraph();
            bool firstLine = true;

            foreach (var output in lines)
            {
                var trimmed = output.TrimEnd();
                if (!firstLine)
                {
                    p.Inlines.Add(new LineBreak());                    
                }

                firstLine = false;

                p.Inlines.Add(new RCONOutput_Comment(output));
            }

            AddBlockContent(p);
        }

        private IEnumerable<Inline> FormatCommandOutput(ServerRCON.ConsoleCommand command)
        {
            bool firstLine = true;
            
            foreach (var output in command.lines)
            {
                var trimmed = output.TrimEnd();
                if(!firstLine)
                {
                    yield return new LineBreak();
                }
                firstLine = false;

                if (output == ServerRCON.NoResponseOutput)
                {
                    yield return new RCONOutput_NoResponse();
                }
                else
                {
                    yield return new RCONOutput_CommandOutput(trimmed);
                }
            }
        }

        private void ConsoleInput_KeyUp(object sender, KeyEventArgs e)
        {            
            if(e.Key == Key.Enter)
            {
                var textBox = (TextBox)sender;
                var effectiveMode = this.CurrentInputMode;
                var commandText = textBox.Text.Trim();
                if (commandText.StartsWith("/help"))
                {
                    AddCommentsBlock(
                        "Known commands:",
                        "   AllowPlayerToJoinNoCheck <steam id> - Adds the specified player to the server's whitelist.",
                        "   BanPlayer <steam id> - Add the specified player to the server's banned list. ",
                        "   Broadcast <message> - Broadcast a message to all players on the server. ",
                        "   DestroyAll <class name> - Destroys ALL creatures of the specified class.",
                        "   DestroyAllEnemies - Destroys all non-player creatures on the map, including tamed creatures. This does not prevent new ones from spawning as usual.",
                        "   DestroyStructures - Destroys all structures owned by all players on the map.",
                        "   DestroyWildDinos - Destroys all untamed creatures on the map. Useful for helping newly-released creatures to spawn.",
                        "   DisallowPlayerToJoinNoCheck <steam id> - Removes the specified player from the server's whitelist.",
                        "   DoExit - Shuts down the server as soon as possible.",
                        "   GetChat - Returns the latest chat buffer (the same amount that the clients see).",
                        "   GiveItemNumToPlayer <player id> <item id> <quantity> <quality> <blueprint> - Adds the specified item to the player's inventory (or its blueprint) in the specified quantity and with the specified quality.",
                        "   GiveExpToPlayer <player id> <how much> <from tribe share> <prevent sharing with tribe> - Gives the specified player the specified amount of experience points.",
                        "   KickPlayer <steam id> - Forcibly disconnect the specified player from the server.",
                        "   KillPlayer <player id> - Kills the specified player.",
                        "   ListPlayers - List all connected players and their Steam IDs.",
                        "   PlayersOnly - Stops all creature movement in the game world and halts crafting. Players can still move normally. Repeat the command to disable its effects.",
                        "   RenamePlayer \"<player>\" <new name> - Renames the player specified by their in-game string name.",
                        "   RenameTribe \"<tribe>\" <new name> - Renames the tribe specified by it's string name.",
                        "   SaveWorld - Forces the server to save the game world to disk in its current state.",
                        "   ServerChat <message> - Sends a chat message to all currently connected players.",
                        "   ServerChatTo \"<steam id>\" <message> - Sends a direct chat message to the player specified by their int64 encoded steam id.",
                        "   ServerChatToPlayer \"<player>\" <message> - Sends a direct chat message to the player specified by their in-game player name.",
                        "   SetMessageOfTheDay <message> - Sets the server's 'message of the day', displayed to players when they connect to it.",
                        "   SetTimeOfDay <hour>:<minute>[:<second>] - Sets the game world's time of day to the specified time.",
                        "   ShowMessageOfTheDay - Displays the message of the day.",
                        "   Slomo <factor> - Sets the game speed multiplier. Lower values slow time, change back to 1 to set back to normal.",
                        "   UnBanPlayer <steam id> - Remove the specified player from the server's banned list.",
                        "where:",
                        "   <player> specifies the character name of the player",
                        "   <steam id> is the long numerical id of the player",
                        "   <player id> specifies the ingame UE4 ID of the player"
                        );
                }
                else
                {
                    if (commandText.StartsWith("/"))
                    {
                        effectiveMode = InputMode.Command;
                        commandText = commandText.Substring(1);
                    }

                    switch (effectiveMode)
                    {
                        case InputMode.Broadcast:
                            this.ServerRCON.IssueCommand($"broadcast {commandText}");
                            break;

                        case InputMode.Global:
                            if (!String.IsNullOrWhiteSpace(Config.Default.RCON_AdminName))
                            {
                                this.ServerRCON.IssueCommand($"serverchat [{Config.Default.RCON_AdminName}] {commandText}");
                            }
                            else
                            {
                                this.ServerRCON.IssueCommand($"serverchat {commandText}");
                            }
                            break;

                        case InputMode.Command:
                            this.ServerRCON.IssueCommand(commandText);
                            break;

#if false
                    case InputMode.Chat:
                        this.ServerRCON.IssueCommand(textBox.Text);
                        break;
#endif
                    }
                }

                textBox.Text = String.Empty;
            }
        }

        private void RCON_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.WindowState != WindowState.Minimized)
            {
                Rect savedRect = this.RCONParameters.RCONWindowExtents;
                this.RCONParameters.RCONWindowExtents = new Rect(savedRect.Location, e.NewSize);
            }
        }

        private void RCON_LocationChanged(object sender, EventArgs e)
        {
            if (this.WindowState != WindowState.Minimized && this.Left != -32000 && this.Top != -32000)
            {
                Rect savedRect = this.RCONParameters.RCONWindowExtents;
                this.RCONParameters.RCONWindowExtents = new Rect(new Point(this.Left, this.Top), savedRect.Size);
                if (this.RCONParameters.Server != null)
                {
                    this.RCONParameters.Server.Profile.RCONWindowExtents = this.RCONParameters.RCONWindowExtents;
                }
            }
        }
    }
}
