using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ASMWebAPI.Models
{
    [DataContract]
    public class ServerInfo
    {
        public ServerInfo()
        {
            Name = "unknown";
            Version = new Version(0, 0);
            Map = "unknown";
            PlayerCount = 0;
            MaxPlayers = 0;

            Players = null;
        }

        [DataMember]
        public string Name
        {
            get;
            set;
        }

        [DataMember]
        public Version Version
        {
            get;
            set;
        }

        [DataMember]
        public string Map
        {
            get;
            set;
        }

        [DataMember]
        public int PlayerCount
        {
            get;
            set;
        }

        [DataMember]
        public int MaxPlayers
        {
            get;
            set;
        }

        [DataMember]
        public IList<PlayerInfo> Players
        {
            get;
            set;
        }
    }
}