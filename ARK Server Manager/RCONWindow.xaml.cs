using ARK_Server_Manager.Lib;
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
        public ServerRCON ServerRCON
        {
            get { return (ServerRCON)GetValue(ServerRCONProperty); }
            set { SetValue(ServerRCONProperty, value); }
        }

        public static readonly DependencyProperty ServerRCONProperty = DependencyProperty.Register(nameof(ServerRCON), typeof(ServerRCON), typeof(RCONWindow), new PropertyMetadata(null));    

        public RCONWindow(Server server)
        {
            InitializeComponent();
            this.ServerRCON = new ServerRCON(server);
            this.ServerRCON.RegisterCommandListener(RenderRCONCommandOutput);
            this.DataContext = this;
        }

        private void RenderRCONCommandOutput(ServerRCON.ConsoleCommand command)
        {
            //
            // Format output
            //
            Paragraph p = new Paragraph();
            foreach (var element in FormatCommandInput(command))
            {
                p.Inlines.Add(element);
            }
            p.Inlines.Add(new LineBreak());
            foreach (var element in FormatCommandOutput(command))
            {
                p.Inlines.Add(element);
            }

            ConsoleContent.Blocks.Add(p);
        }

        private IEnumerable<Inline> FormatCommandInput(ServerRCON.ConsoleCommand command)
        {
            yield return new Bold(new Run("> " + command.command));
        }

        private IEnumerable<Inline> FormatCommandOutput(ServerRCON.ConsoleCommand command)
        {
            foreach (var output in command.lines)
            {
                yield return new Run(output);
                yield return new LineBreak();
            }
        }

        private void ConsoleInput_KeyUp(object sender, KeyEventArgs e)
        {            
            if(e.Key == Key.Enter)
            {
                this.ServerRCON.IssueCommand(((TextBox)sender).Text);
            }
        }
    }
}
