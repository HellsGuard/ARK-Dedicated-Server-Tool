using System;
using System.Runtime.Serialization;

namespace ASMWebAPI.Models
{
    [DataContract]
    public class PlayerInfo
    {
        public PlayerInfo()
        {
            Name = "unknown";
            Time = new TimeSpan();
        }

        [DataMember]
        public string Name
        {
            get;
            set;
        }

        [DataMember]
        public TimeSpan Time
        {
            get;
            set;
        }
    }
}