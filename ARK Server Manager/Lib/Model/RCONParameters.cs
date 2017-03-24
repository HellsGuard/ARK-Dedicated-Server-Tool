using System.Net;
using System.Windows;

namespace ARK_Server_Manager.Lib
{
    public class RCONParameters : DependencyObject
    {
        public static readonly DependencyProperty ProfileNameProperty = DependencyProperty.Register(nameof(ProfileName), typeof(string), typeof(RCONParameters), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty MaxPlayersProperty = DependencyProperty.Register(nameof(MaxPlayers), typeof(int), typeof(RCONParameters), new PropertyMetadata(0));

        public string ProfileName
        {
            get { return (string)GetValue(ProfileNameProperty); }
            set { SetValue(ProfileNameProperty, value); }
        }

        public string RCONHost { get; set; }

        public int RCONPort { get; set; }

        public string AdminPassword { get; set; }

        public string InstallDirectory { get; set; }

        public string AltSaveDirectoryName { get; set; }

        public bool PGM_Enabled { get; set; }

        public string PGM_Name { get; set; }

        public Rect RCONWindowExtents { get; set; }

        public int MaxPlayers
        {
            get { return (int)GetValue(MaxPlayersProperty); }
            set { SetValue(MaxPlayersProperty, value); }
        }

        public Server Server { get; set; }

        public IPAddress RCONHostIP
        {
            get
            {
                try
                {
                    var ipAddresses = Dns.GetHostAddresses(RCONHost);
                    if (ipAddresses.Length > 0)
                        return ipAddresses[0].MapToIPv4();
                }
                catch {}

                return IPAddress.None;
            }
        }
    }
}
