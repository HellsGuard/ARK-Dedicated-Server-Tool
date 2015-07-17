using ARK_Server_Manager.Lib;
using QueryMaster;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SteamKit2;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for RCON.xaml
    /// </summary>
    public partial class RCONWindow : Window
    {
        public enum ConsoleStatus
        {
            Disconnected,
            Connected,
        };

        private struct ConsoleCommand
        {
            public ConsoleStatus status;
            public string command;
            public IEnumerable<string> lines;
        };

        private const int ConnectionRetryDelay = 2000;
        private readonly IPEndPoint endpoint;
        private readonly string password;

        ActionBlock<string> InputProcessor;
        ActionBlock<ConsoleCommand> OutputProcessor;

        private Rcon console;
        CancellationTokenSource terminateConsole = new CancellationTokenSource();



        public string ServerName
        {
            get { return (string)GetValue(ServerNameProperty); }
            set { SetValue(ServerNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ServerName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ServerNameProperty =
            DependencyProperty.Register("ServerName", typeof(string), typeof(RCONWindow), new PropertyMetadata("(Unknown)"));

        public string ConsoleInput
        {
            get { return (string)GetValue(ConsoleInputProperty); }
            set { SetValue(ConsoleInputProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ConsoleInput.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ConsoleInputProperty =
            DependencyProperty.Register("ConsoleInput", typeof(string), typeof(RCONWindow), new PropertyMetadata(String.Empty, OnInputChanged));

        public RCONWindow(string serverName, IPEndPoint endpoint, string authPassword)
        {
            InitializeComponent();
            this.endpoint = endpoint;
            this.password = authPassword;
            this.DataContext = this;
            this.ServerName = serverName;

            this.InputProcessor = new ActionBlock<string>(new Func<string, Task>(ProcessInput), 
                                                          new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1, CancellationToken = terminateConsole.Token });
            this.OutputProcessor = new ActionBlock<ConsoleCommand>(new Func<ConsoleCommand, Task>(ProcessOutput),
                                              new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1, 
                                                                                  CancellationToken = terminateConsole.Token, 
                                                                                  TaskScheduler = TaskScheduler.FromCurrentSynchronizationContext() });

            Task.Factory.StartNew(async () => await StartConnectionAsync());
        }

        private Task ProcessOutput(ConsoleCommand command)
        {
            //
            // Handle results
            HandleCommand(command);

            //
            // Format output
            //
            Paragraph p = new Paragraph();
            foreach(var element in FormatCommandInput(command))
            {
                p.Inlines.Add(element);
            }
            p.Inlines.Add(new LineBreak());
            foreach (var element in FormatCommandOutput(command))
            {
                p.Inlines.Add(element);
            }

            ConsoleContent.Blocks.Add(p);
            return Task.FromResult<bool>(true);
        }

        private void HandleCommand(ConsoleCommand command)
        {
#if false
            if(command.command.StartsWith("listplayers"))
            {
                foreach(var line in command.lines)
                {
                    var elements = line.Split(',');
                    if(elements.Length > 0)
                    {
                        long steamId;
                        if(Int64.TryParse(elements[elements.Length-1], out steamId))
                        {
                            using(dynamic steamUser = WebAPI.GetInterface("ISteamUser"))
                            {
                                steamUser.GetPlayerSummaries(steamids: steamId);
                            }   
                        }
                    }
                }
            }
#endif
        }

        private IEnumerable<Inline> FormatCommandInput(ConsoleCommand command)
        {
            yield return new Bold(new Run("> " + command.command));
        }

        private IEnumerable<Inline> FormatCommandOutput(ConsoleCommand command)
        {
            foreach(var output in command.lines)
            {
                yield return new Run(output);
                yield return new LineBreak();
            }
        }

        private Task ProcessInput(string arg)
        {
            char[] splitChars = new char[] { '\n' };

            var result = this.console.SendCommand(arg);
            var lines = result.Split(splitChars, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim());

            ConsoleCommand output = new ConsoleCommand
            {
                status = ConsoleStatus.Connected,
                command = arg,
                lines = lines
            };

            this.OutputProcessor.Post(output);
            return Task.FromResult<bool>(true);
        }

        private Task StartConnectionAsync()
        {
            var server = ServerQuery.GetServerInstance(EngineType.Source, this.endpoint);
            this.console = server.GetControl(this.password);
            return Task.FromResult(true);
        }


        private static void OnInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            string input = e.NewValue as string;
            if (String.IsNullOrEmpty(input))
            {
                return;
            }

            ((RCONWindow)d).InputProcessor.Post(input);
            ((RCONWindow)d).ConsoleInput = String.Empty;
        }

        private void ConsoleInput_KeyUp(object sender, KeyEventArgs e)
        {            
            if(e.Key == Key.Enter)
            {
                ((TextBox)sender).GetBindingExpression(TextBox.TextProperty).UpdateSource();
            }
        }
    }
}
