using System.Net;
using System.Windows;

namespace ARK_Server_Manager.Lib
{
    public class RCONParameters
    {
        public string ProfileName { get; set; }

        public string RCONHost { get; set; }

        public int RCONPort { get; set; }

        public string AdminPassword { get; set; }

        public string InstallDirectory { get; set; }

        public string AltSaveDirectoryName { get; set; }

        public bool PGM_Enabled { get; set; }

        public string PGM_Name { get; set; }

        public Rect RCONWindowExtents { get; set; }

        public int MaxPlayers { get; set; }

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
