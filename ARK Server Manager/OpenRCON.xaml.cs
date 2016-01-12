using ARK_Server_Manager.Lib.ViewModel;
using System;
using System.Net;
using System.Windows;
using System.Windows.Input;

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

        public int RCONPort
        {
            get { return (int)GetValue(RCONPortProperty); }
            set { SetValue(RCONPortProperty, value); }
        }

        public static readonly DependencyProperty RCONPortProperty = DependencyProperty.Register(nameof(RCONPort), typeof(int), typeof(OpenRCON), new PropertyMetadata(32330));

        public string Password
        {
            get { return (string)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }

        public static readonly DependencyProperty PasswordProperty = DependencyProperty.Register(nameof(Password), typeof(string), typeof(OpenRCON), new PropertyMetadata(String.Empty));

        public OpenRCON()
        {
            InitializeComponent();
            LoadDefaults();
            this.DataContext = this;
        }

        public ICommand ConnectCommand => new RelayCommand<object>(
            execute: _ => {
                var window = RCONWindow.GetRCON(new Lib.RCONParameters()
                {
                    ProfileName = $"Remote: {ServerIP}:{RCONPort}",
                    ServerIP = ServerIP,
                    RCONPort = RCONPort,
                    AdminPassword = Password,
                    InstallDirectory = String.Empty,
                    RCONWindowExtents = Rect.Empty
                });
                window.Owner = this.Owner;
                window.Show();

                SaveDefaults();
                this.Close();
            },
            canExecute: _ => true
        );

        private void LoadDefaults()
        {
            Config.Default.Reload();
            if (!String.IsNullOrWhiteSpace(Config.Default.OpenRCON_ServerIP))
                ServerIP = Config.Default.OpenRCON_ServerIP;
            RCONPort = Config.Default.OpenRCON_RCONPort;
        }
        private void SaveDefaults()
        {
            Config.Default.OpenRCON_ServerIP = ServerIP;
            Config.Default.OpenRCON_RCONPort = RCONPort;
            Config.Default.Save();
        }
    }
}
