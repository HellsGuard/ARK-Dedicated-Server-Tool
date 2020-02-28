﻿using System;
using System.Collections.Generic;

namespace ArkData
{
    public class PlayerData
    {
        public long Id { get; set; }
        public string SteamId { get; set; }
        public string SteamName { get; set; }
        public string AvatarUrl { get; set; }
        public string CharacterName { get; set; }
        public bool Online { get; set; }
        public string File { get; set; }
        public string Filename { get; set; }
        public DateTime FileCreated { get; set; }
        public DateTime FileUpdated { get; set; }
        public int? TribeId { get; set; }
        public short Level { get; set; }
        public string ProfileUrl { get; set; }
        public bool CommunityBanned { get; set; }
        public bool VACBanned { get; set; }
        public int NumberOfVACBans { get; set; }
        public int DaysSinceLastBan { get; set; }
        public int NumberOfGameBans { get; set; }
        public virtual TribeData Tribe { get; set; }
        public virtual List<TribeData> OwnedTribes { get; set; }

        public DateTime LastSteamUpdateUtc { get; set; }
    }
}
