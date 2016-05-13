using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ARK_Server_Manager.Lib
{
    public class RCONParameters
    {
        public string ProfileName { get; set; }

        public string ServerIP { get; set; }

        public int RCONPort { get; set; }

        public string AdminPassword { get; set; }

        public string InstallDirectory { get; set; }

        public Rect RCONWindowExtents { get; set; }

        public int MaxPlayers { get; set; }

        public Server Server { get; set; }
    }
}
