using ARK_Server_Manager.Lib.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for OpenRCON.xaml
    /// </summary>
    public partial class OpenRCON : Window
    {


        public string ServerIP
        {
            get { return (string)GetValue(ServerIPProperty); }
            set { SetValue(ServerIPProperty, value); }
        }

        public static readonly DependencyProperty ServerIPProperty = DependencyProperty.Register(nameof(ServerIP), typeof(string), typeof(OpenRCON), new PropertyMetadata(IPAddress.Loopback.ToString()));

        public int ServerPort
        {
            get { return (int)GetValue(ServerPortProperty); }
            set { SetValue(ServerPortProperty, value); }
        }

        public static readonly DependencyProperty ServerPortProperty = DependencyProperty.Register(nameof(ServerPort), typeof(int), typeof(OpenRCON), new PropertyMetadata(32330));

        public string Password
        {
            get { return (string)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }

        public static readonly DependencyProperty PasswordProperty = DependencyProperty.Register(nameof(Password), typeof(string), typeof(OpenRCON), new PropertyMetadata(String.Empty));

        public OpenRCON()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public ICommand ConnectCommand => new RelayCommand<object>(
            execute: _ =>
            {
                var window = RCONWindow.GetRCON(new Lib.RCONParameters()
                {
                    ProfileName = $"Remote: {ServerIP}:{ServerPort}",
                    ServerIP = ServerIP,
                    RCONPort = ServerPort,
                    AdminPassword = Password,
                    InstallDirectory = String.Empty,
                    RCONWindowExtents = Rect.Empty
                });
                window.Owner = this.Owner;
                window.Show();
                this.Close();
            },
            canExecute: _ => true);
    }
}
