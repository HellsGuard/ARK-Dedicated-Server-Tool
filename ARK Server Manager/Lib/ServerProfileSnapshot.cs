using System;
using System.Collections.Generic;
using System.Net;

namespace ARK_Server_Manager.Lib
{
    public class ServerProfileSnapshot
    {
        public string ProfileId;
        public string ProfileName;
        public string InstallDirectory;
        public string AltSaveDirectoryName;
        public bool PGM_Enabled;
        public string PGM_Name;
        public string AdminPassword;
        public string ServerName;
        public string ServerArgs;
        public string ServerIP;
        public int ServerPort;
        public int QueryPort;
        public bool UseRawSockets;
        public bool RCONEnabled;
        public int RCONPort;
        public string ServerMap;
        public string ServerMapModId;
        public string TotalConversionModId;
        public List<string> ServerModIds;
        public string LastInstalledVersion;
        public int MotDDuration;
        public bool ForceRespawnDinos;

        public string SchedulerKey;
        public bool EnableAutoBackup;
        public bool EnableAutoUpdate;
        public bool EnableAutoShutdown1;
        public bool RestartAfterShutdown1;
        public bool UpdateAfterShutdown1;
        public bool EnableAutoShutdown2;
        public bool RestartAfterShutdown2;
        public bool UpdateAfterShutdown2;
        public bool AutoRestartIfShutdown;

        public bool SotFEnabled;

        public int MaxPlayerCount;

        public bool ServerUpdated;

        public static ServerProfileSnapshot Create(ServerProfile profile)
        {
            return new ServerProfileSnapshot
            {
                ProfileId = profile.ProfileID,
                ProfileName = profile.ProfileName,
                InstallDirectory = profile.InstallDirectory,
                AltSaveDirectoryName = profile.AltSaveDirectoryName,
                PGM_Enabled = profile.PGM_Enabled,
                PGM_Name = profile.PGM_Name,
                AdminPassword = profile.AdminPassword,
                ServerName = profile.ServerName,
                ServerArgs = profile.GetServerArgs(),
                ServerIP = string.IsNullOrWhiteSpace(profile.ServerIP) ? IPAddress.Loopback.ToString() : profile.ServerIP.Trim(),
                ServerPort = profile.ServerConnectionPort,
                QueryPort = profile.ServerPort,
                UseRawSockets = profile.UseRawSockets,
                RCONEnabled = profile.RCONEnabled,
                RCONPort = profile.RCONPort,
                ServerMap = ServerProfile.GetProfileMapName(profile),
                ServerMapModId = ServerProfile.GetProfileMapModId(profile),
                TotalConversionModId = profile.TotalConversionModId ?? string.Empty,
                ServerModIds = ModUtils.GetModIdList(profile.ServerModIds),
                MotDDuration = Math.Max(profile.MOTDDuration, 10),
                ForceRespawnDinos = profile.ForceRespawnDinos,

                SchedulerKey = profile.GetProfileKey(),
                EnableAutoBackup = profile.EnableAutoBackup,
                EnableAutoUpdate = profile.EnableAutoUpdate,
                EnableAutoShutdown1 = profile.EnableAutoShutdown1,
                RestartAfterShutdown1 = profile.RestartAfterShutdown1,
                UpdateAfterShutdown1 = profile.UpdateAfterShutdown1,
                EnableAutoShutdown2 = profile.EnableAutoShutdown2,
                RestartAfterShutdown2 = profile.RestartAfterShutdown2,
                UpdateAfterShutdown2 = profile.UpdateAfterShutdown2,
                AutoRestartIfShutdown = profile.AutoRestartIfShutdown,

                SotFEnabled = profile.SOTF_Enabled,

                LastInstalledVersion = profile.LastInstalledVersion ?? new Version(0, 0).ToString(),
                MaxPlayerCount = profile.MaxPlayers,

                ServerUpdated = false,
            };
        }

        public void Update(ServerProfile profile)
        {
            profile.LastInstalledVersion = LastInstalledVersion;
        }
    }
}
