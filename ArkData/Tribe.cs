using System;
using System.Collections.Generic;

namespace ArkData
{
    public class Tribe
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime FileCreated { get; set; }
        public DateTime FileUpdated { get; set; }
        public int? OwnerId { get; set; }
        public virtual ICollection<Player> Players { get; set; }
        public virtual Player Owner { get; set; }

        public Tribe()
        {
            this.Players = (ICollection<Player>)new HashSet<Player>();
        }
    }
}
