using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Xml.Serialization;

namespace ARK_Server_Manager.Lib
{
    public interface ISettingsBag
    {
        object this[string propertyName] { get; set; }
    }

    [XmlRoot("ArkServerProfile")]
    [Serializable()]
    public class ServerSettings : ISettingsBag
    {
        public const string ProfileNameProperty = "ProfileName";

        public string ProfileName = Config.Default.DefaultServerProfileName;

        #region Server properties
        
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "GlobalVoiceChat")]
        public bool EnableGlobalVoiceChat = true;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "ProximityVoiceChat")]
        public bool EnableProximityChat = true;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "NoTributeDownloads", InvertBoolean = true)]
        public bool EnableTributeDownloads = false;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "bAllowFlyerCarryPVE")]
        public bool EnableFlyerCarry = true;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "bDisableStructureDecayPVE", InvertBoolean = true)]
        public bool EnableStructureDecay = false;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "AlwaysNotifyPlayerLeft")]
        public bool EnablePlayerLeaveNotifications = true;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "DontAlwaysNotifyPlayerJoined", InvertBoolean = true)]
        public bool EnablePlayerJoinedNotifications = true;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "ServerHardcore")]        
        public bool EnableHardcore = false;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "ServerPVE", InvertBoolean = true)]        
        public bool EnablePVP = false;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "ServerCrosshair")]        
        public bool AllowCrosshair = false;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "ServerForceNoHud", InvertBoolean = true)]        
        public bool AllowHUD = true;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "AllowThirdPersonPlayer")]        
        public bool AllowThirdPersonView = false;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "ShowMapPlayerLocation")]        
        public bool AllowMapPlayerLocation = true;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "EnablePVPGamma")]        
        public bool AllowPVPGamma = false;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "ServerPassword")]        
        public string ServerPassword = "";

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "ServerAdminPassword")]        
        public string AdminPassword = "";

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.GameSession, "MaxPlayers")]        
        public int MaxPlayers = 5;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "DifficultyOffset")]        
        public float DifficultyOffset = 0.25f;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, "MaxStructuresInRange")]  
        public float MaxStructuresVisible = 1300;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.SessionSettings, "SessionName")]  
        public string ServerName = Config.Default.DefaultServerName;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.SessionSettings, "QueryPort")]  
        public int ServerPort = 27015;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.SessionSettings, "Port")]
        public int ServerConnectionPort = 7777;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.SessionSettings, "MultiHome")]
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.MultiHome, "MultiHome", WriteBoolValueIfNonEmpty = true)]
        public string ServerIP = String.Empty;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.MessageOfTheDay, "Message", ClearSection=true)]
        public string MOTD = String.Empty;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.MessageOfTheDay, "Duration")]
        public int MOTDDuration = 20;

        public bool EnableKickIdlePlayers = false;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings, ConditionedOn="EnableKickIdlePlayers")]
        public float KickIdlePlayersPeriod = 2400;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float AutoSavePeriodMinutes = 15;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float TamingSpeedMultiplier = 1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float HarvestAmountMultiplier=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float PlayerCharacterWaterDrainMultiplier=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float PlayerCharacterFoodDrainMultiplier=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float DinoCharacterFoodDrainMultiplier = 1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float PlayerCharacterStaminaDrainMultiplier = 1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float DinoCharacterStaminaDrainMultiplier = 1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float PlayerCharacterHealthRecoveryMultiplier = 1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float DinoCharacterHealthRecoveryMultiplier = 1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float DinoCountMultiplier=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float HarvestHealthMultiplier = 1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float PvEStructureDecayDestructionPeriod = 0;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float PvEStructureDecayPeriodMultiplier = 1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float ResourcesRespawnPeriodMultiplier = 1;

        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float DayCycleSpeedScale=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]
        public float NightTimeSpeedScale = 1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float DayTimeSpeedScale=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float DinoDamageMultiplier=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float TamedDinoDamageMultiplier=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float PlayerDamageMultiplier=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float StructureDamageMultiplier=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float PlayerResistanceMultiplier=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float DinoResistanceMultiplier=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float TamedDinoResistanceMultiplier=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float StructureResistanceMultiplier=1;
        [IniFileEntry(IniFiles.GameUserSettings, IniFileSections.ServerSettings)]        
        public float XPMultiplier = 1;

        public bool EnableDinoSpawns = false;
        public List<DinoSpawn> DinoSpawns = new List<DinoSpawn>();

        public bool EnableLevelProgressions = false;
        public List<Level> PlayerLevels = new List<Level>();
        public List<Level> DinoLevels = new List<Level>();

        public string SaveDirectory = String.Empty;
        public string InstallDirectory = String.Empty;

        public string LastInstalledVersion = String.Empty;
        public string AdditionalArgs = String.Empty;
        
        public string ServerMap = Config.Default.DefaultServerMap;

        [XmlIgnore()]
        public ObservableCollection<string> Whitelist = new ObservableCollection<string>();

        #endregion

        [XmlIgnore()]
        public bool IsDirty = true;

        [XmlIgnore()]
        private string LastSaveLocation = String.Empty;

        private ServerSettings()
        {
            ServerPassword = PasswordUtils.GeneratePassword(16);
            AdminPassword = PasswordUtils.GeneratePassword(16);
            GetDefaultDirectories();
        }

        private void GetDefaultDinoSpawns()
        {
            var spawns = new DinoSpawn[] 
            {
                new DinoSpawn { Name = "Anky",       SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F },
                new DinoSpawn { Name = "Argent",     SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F },
                new DinoSpawn { Name = "Bat",        SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F },
                new DinoSpawn { Name = "Bronto",     SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F },
                new DinoSpawn { Name = "Carno",      SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F },
                new DinoSpawn { Name = "Coel",       SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F },
                new DinoSpawn { Name = "Dilo",       SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F },
                new DinoSpawn { Name = "Dodo",       SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F },
                new DinoSpawn { Name = "Mammoth",    SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F },
                new DinoSpawn { Name = "Mega",       SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F },
                new DinoSpawn { Name = "Para",       SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F },
                new DinoSpawn { Name = "Phiomia",    SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F },
                new DinoSpawn { Name = "Piranha",    SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F },
                new DinoSpawn { Name = "Ptera",      SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F },
                new DinoSpawn { Name = "Raptor",     SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F },
                new DinoSpawn { Name = "Rex",        SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F },
                new DinoSpawn { Name = "Sabertooth", SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F },
                new DinoSpawn { Name = "Sarco",      SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F },
                new DinoSpawn { Name = "Scorpion",   SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F },
                new DinoSpawn { Name = "Stego",      SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F },
                new DinoSpawn { Name = "Spino",      SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F },
                new DinoSpawn { Name = "Spider",     SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F },
                new DinoSpawn { Name = "Titanboa",   SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F },
                new DinoSpawn { Name = "Trike",      SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F },
                new DinoSpawn { Name = "Turtle",     SpawnWeightMultiplier = 0.1F, OverrideSpawnLimitPercentage = true, SpawnLimitPercentage = 1.0F }
            };

            foreach(var spawn in spawns)
            {
                this.DinoSpawns.Add(spawn);
            }
        }

        private void GetDefaultLevels()
        {
            var playerLevels = new Level[]
            {             
                new Level { LevelIndex=0	, XPRequired=1	    , EngramPoints=10 },
                new Level { LevelIndex=1	, XPRequired=5	    , EngramPoints=10 },
                new Level { LevelIndex=2	, XPRequired=15	    , EngramPoints=10 },
                new Level { LevelIndex=3	, XPRequired=20	    , EngramPoints=10 },
                new Level { LevelIndex=4	, XPRequired=35	    , EngramPoints=10 },
                new Level { LevelIndex=5	, XPRequired=60	    , EngramPoints=10 },
                new Level { LevelIndex=6	, XPRequired=95	    , EngramPoints=10 },
                new Level { LevelIndex=7	, XPRequired=140	, EngramPoints=10 },
                new Level { LevelIndex=8	, XPRequired=195	, EngramPoints=10 },
                new Level { LevelIndex=9	, XPRequired=260	, EngramPoints=10 },
                new Level { LevelIndex=10	, XPRequired=335	, EngramPoints=15 },
                new Level { LevelIndex=11	, XPRequired=420	, EngramPoints=15 },
                new Level { LevelIndex=12	, XPRequired=515	, EngramPoints=15 },
                new Level { LevelIndex=13	, XPRequired=620	, EngramPoints=15 },
                new Level { LevelIndex=14	, XPRequired=735	, EngramPoints=15 },
                new Level { LevelIndex=15	, XPRequired=860	, EngramPoints=15 },
                new Level { LevelIndex=16	, XPRequired=995	, EngramPoints=15 },
                new Level { LevelIndex=17	, XPRequired=1140	, EngramPoints=15 },
                new Level { LevelIndex=18	, XPRequired=1295	, EngramPoints=15 },
                new Level { LevelIndex=19	, XPRequired=1460	, EngramPoints=15 },
                new Level { LevelIndex=20	, XPRequired=1635	, EngramPoints=20 },
                new Level { LevelIndex=21	, XPRequired=1820	, EngramPoints=20 },
                new Level { LevelIndex=22	, XPRequired=2015	, EngramPoints=20 },
                new Level { LevelIndex=23	, XPRequired=2220	, EngramPoints=20 },
                new Level { LevelIndex=24	, XPRequired=2435	, EngramPoints=20 },
                new Level { LevelIndex=25	, XPRequired=2660	, EngramPoints=20 },
                new Level { LevelIndex=26	, XPRequired=2895	, EngramPoints=20 },
                new Level { LevelIndex=27	, XPRequired=3140	, EngramPoints=20 },
                new Level { LevelIndex=28	, XPRequired=3395	, EngramPoints=20 },
                new Level { LevelIndex=29	, XPRequired=3660	, EngramPoints=20 },
                new Level { LevelIndex=30	, XPRequired=3935	, EngramPoints=25 },
                new Level { LevelIndex=31	, XPRequired=4220	, EngramPoints=25 },
                new Level { LevelIndex=32	, XPRequired=4515	, EngramPoints=25 },
                new Level { LevelIndex=33	, XPRequired=4820	, EngramPoints=25 },
                new Level { LevelIndex=34	, XPRequired=5135	, EngramPoints=25 },
                new Level { LevelIndex=35	, XPRequired=5460	, EngramPoints=25 },
                new Level { LevelIndex=36	, XPRequired=5795	, EngramPoints=25 },
                new Level { LevelIndex=37	, XPRequired=6140	, EngramPoints=25 },
                new Level { LevelIndex=38	, XPRequired=6495	, EngramPoints=25 },
                new Level { LevelIndex=39	, XPRequired=6860	, EngramPoints=25 },
                new Level { LevelIndex=40	, XPRequired=7235	, EngramPoints=30 },
                new Level { LevelIndex=41	, XPRequired=7620	, EngramPoints=30 },
                new Level { LevelIndex=42	, XPRequired=8015	, EngramPoints=30 },
                new Level { LevelIndex=43	, XPRequired=8420	, EngramPoints=30 },
                new Level { LevelIndex=44	, XPRequired=8835	, EngramPoints=30 },
                new Level { LevelIndex=45	, XPRequired=9260	, EngramPoints=30 },
                new Level { LevelIndex=46	, XPRequired=9695	, EngramPoints=30 },
                new Level { LevelIndex=47	, XPRequired=10140	, EngramPoints=30 },
                new Level { LevelIndex=48	, XPRequired=10595	, EngramPoints=30 },
                new Level { LevelIndex=49	, XPRequired=11060	, EngramPoints=30 },
                new Level { LevelIndex=50	, XPRequired=11535	, EngramPoints=35 },
                new Level { LevelIndex=51	, XPRequired=12020	, EngramPoints=35 },
                new Level { LevelIndex=52	, XPRequired=12515	, EngramPoints=35 },
                new Level { LevelIndex=53	, XPRequired=13020	, EngramPoints=35 },
                new Level { LevelIndex=54	, XPRequired=13535	, EngramPoints=35 },
                new Level { LevelIndex=55	, XPRequired=14060	, EngramPoints=35 },
                new Level { LevelIndex=56	, XPRequired=14595	, EngramPoints=35 },
                new Level { LevelIndex=57	, XPRequired=15140	, EngramPoints=35 },
                new Level { LevelIndex=58	, XPRequired=15695	, EngramPoints=35 },
                new Level { LevelIndex=59	, XPRequired=16285	, EngramPoints=35 },
                new Level { LevelIndex=60	, XPRequired=16910	, EngramPoints=50 },
                new Level { LevelIndex=61	, XPRequired=17570	, EngramPoints=50 },
                new Level { LevelIndex=62	, XPRequired=18265	, EngramPoints=50 },
                new Level { LevelIndex=63	, XPRequired=18995	, EngramPoints=50 },
                new Level { LevelIndex=64	, XPRequired=19760	, EngramPoints=50 },
                new Level { LevelIndex=65	, XPRequired=20560	, EngramPoints=50 },
                new Level { LevelIndex=66	, XPRequired=21395	, EngramPoints=50 },
                new Level { LevelIndex=67	, XPRequired=22265	, EngramPoints=50 },
                new Level { LevelIndex=68	, XPRequired=23170	, EngramPoints=72 },
                new Level { LevelIndex=69	, XPRequired=24110	, EngramPoints=73 },
                new Level { LevelIndex=70	, XPRequired=25085	, EngramPoints=74 },
                new Level { LevelIndex=71	, XPRequired=26095	, EngramPoints=75 },
                new Level { LevelIndex=72	, XPRequired=27140	, EngramPoints=76 },
                new Level { LevelIndex=73	, XPRequired=28220	, EngramPoints=77 },
                new Level { LevelIndex=74	, XPRequired=29335	, EngramPoints=78 },
                new Level { LevelIndex=75	, XPRequired=30485	, EngramPoints=79 },
                new Level { LevelIndex=76	, XPRequired=31670	, EngramPoints=80 },
                new Level { LevelIndex=77	, XPRequired=32890	, EngramPoints=81 },
                new Level { LevelIndex=78	, XPRequired=34145	, EngramPoints=82 },
                new Level { LevelIndex=79	, XPRequired=35435	, EngramPoints=83 },
                new Level { LevelIndex=80	, XPRequired=36760	, EngramPoints=84 },
                new Level { LevelIndex=81	, XPRequired=38120	, EngramPoints=85 },
                new Level { LevelIndex=82	, XPRequired=39515	, EngramPoints=86 },
                new Level { LevelIndex=83	, XPRequired=40945	, EngramPoints=87 },
                new Level { LevelIndex=84	, XPRequired=42410	, EngramPoints=88 },
                new Level { LevelIndex=85	, XPRequired=43910	, EngramPoints=89 },
                new Level { LevelIndex=86	, XPRequired=45445	, EngramPoints=90 },
                new Level { LevelIndex=87	, XPRequired=47015	, EngramPoints=91 },
                new Level { LevelIndex=88	, XPRequired=48620	, EngramPoints=92 },
                new Level { LevelIndex=89	, XPRequired=50260	, EngramPoints=93 },
                new Level { LevelIndex=90	, XPRequired=51935	, EngramPoints=94 },
                new Level { LevelIndex=91	, XPRequired=53645	, EngramPoints=95 },
                new Level { LevelIndex=92	, XPRequired=55390	, EngramPoints=96 },
                new Level { LevelIndex=93	, XPRequired=57170	, EngramPoints=97 },
                new Level { LevelIndex=94	, XPRequired=58985	, EngramPoints=98 },
                new Level { LevelIndex=95	, XPRequired=60835	, EngramPoints=99 },
                new Level { LevelIndex=96	, XPRequired=62720	, EngramPoints=100},
                new Level { LevelIndex=97	, XPRequired=64640	, EngramPoints=101},
                new Level { LevelIndex=98	, XPRequired=66595	, EngramPoints=102},
                new Level { LevelIndex=99	, XPRequired=68585	, EngramPoints=103}
            };

            foreach(var level in playerLevels)
            {
                this.PlayerLevels.Add(level);
                this.DinoLevels.Add(level);
            }
        }

        public static ServerSettings LoadFrom(string path)
        {
            ServerSettings settings;
            if (Path.GetExtension(path) == Config.Default.ProfileExtension)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ServerSettings));
                using (var reader = File.OpenRead(path))
                {
                    settings = (ServerSettings)serializer.Deserialize(reader);
                    settings.IsDirty = false;
                }
            }
            else
            {
                settings = LoadFromINIFiles(path);           
            }

            if(settings.DinoSpawns.Count == 0)
            {
                settings.GetDefaultDinoSpawns();
            }
            else
            {
                settings.EnableDinoSpawns = true;
            }

            if(settings.PlayerLevels.Count == 0)
            {
                settings.GetDefaultLevels();
            }
            else
            {
                settings.EnableLevelProgressions = true;
            }

            settings.LastSaveLocation = path;
            return settings;
        }

        private static ServerSettings LoadFromINIFiles(string path)
        {
            ServerSettings settings;
            IniFile iniFile = new IniFile(Path.GetDirectoryName(path));
            settings = new ServerSettings();
            iniFile.Deserialize(settings);

            var strings = iniFile.IniReadSection(IniFileSections.GameMode, IniFiles.Game);

            //
            // Dino spawn weights
            //
            var dinoSpawnWeightSources = strings.Where(s => s.StartsWith("DinoSpawnWeightMultipliers="));
            settings.DinoSpawns = DinoSpawn.FromINIValues(dinoSpawnWeightSources);
            
            // 
            // Levels
            //
            var levelRampOverrides = strings.Where(s => s.StartsWith("LevelExperienceRampOverrides=")).ToArray();
            var engramPointOverrides = strings.Where(s => s.StartsWith("OverridePlayerLevelEngramPoints="));
            if (levelRampOverrides.Length > 0)
            {
                settings.PlayerLevels = Level.FromINIValues(levelRampOverrides[0], engramPointOverrides);

                if(levelRampOverrides.Length > 1)
                {
                    settings.DinoLevels = Level.FromINIValues(levelRampOverrides[1], null);
                }
            }
          
            settings.InstallDirectory = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(path)))));
            return settings;
        }

        public void Save()
        {           
            //
            // Save the profile
            //
            XmlSerializer serializer = new XmlSerializer(this.GetType());
            using (var writer = new StreamWriter(GetProfilePath()))
            {
                serializer.Serialize(writer, this);
            }

            //
            // Write the INI files
            //
            SaveINIFiles();

            //
            // If this was a rename, remove the old profile after writing the new one.
            //
            if(!String.Equals(GetProfilePath(), this.LastSaveLocation))
            {
                try
                {
                    if (File.Exists(this.LastSaveLocation))
                    {
                        File.Delete(this.LastSaveLocation);
                    }
                }
                catch(IOException ex)
                {
                    // We tried...
                }

                this.LastSaveLocation = GetProfilePath();
            }
        }

        public string GetProfilePath()
        {
            return Path.Combine(Config.Default.ConfigDirectory, Path.ChangeExtension(this.ProfileName, Config.Default.ProfileExtension));
        }

        public void SaveINIFiles()
        {
            string configDir = Path.Combine(this.InstallDirectory, Config.Default.ServerConfigRelativePath);
            Directory.CreateDirectory(configDir);

            var iniFile = new IniFile(configDir);            
            iniFile.Serialize(this);

            //
            // Write the Game.ini, but only if the user enabled Dino Spawns.
            //
            var values = new List<string>();
            if (this.EnableDinoSpawns)
            {
                values.AddRange(DinoSpawn.ToINIValues(this.DinoSpawns));
            }

            if(this.EnableLevelProgressions)            
            {
                //
                // These must be added in this order: Player, then Dinos, per the ARK INI file format.
                //
                values.Add(Level.ToINIValueForXP(this.PlayerLevels));
                values.Add(Level.ToINIValueForXP(this.DinoLevels));
                values.AddRange(Level.ToINIValuesForEngramPoints(this.PlayerLevels));
            }

            // WARNING: This will delete everything in this section before writing the new values.
            iniFile.IniWriteSection(IniFileSections.GameMode, values.ToArray(), IniFiles.Game);
            
        }

        public string GetServerArgs()
        {
            var serverArgs = new StringBuilder();
            serverArgs.Append(this.ServerMap);

            // These are used to match the server to the profile.
            serverArgs.Append("?QueryPort=").Append(this.ServerPort);
            if (!String.IsNullOrEmpty(this.ServerIP))
            {
                serverArgs.Append("?MultiHome=").Append(this.ServerIP);
            }

#if false
            serverArgs.Append("?GlobalVoiceChat=").Append(this.EnableGlobalVoiceChat);
            serverArgs.Append("?ProximityChat=").Append(this.EnableProximityChat);
            serverArgs.Append("?NoTributeDownloads=").Append(!this.EnableTributeDownloads);
            serverArgs.Append("?bAllowFlyerCarryPVE=").Append(this.EnableFlyerCarry);
            serverArgs.Append("?bDisableStructureDecayPVE=").Append(!this.EnableStructureDecay);
            serverArgs.Append("?AlwaysNotifyPlayerLeft=").Append(this.EnablePlayerLeaveNotifications);
            serverArgs.Append("?DontAlwaysNotifyPlayerJoined=").Append(!this.EnablePlayerJoinedNotifications);
            serverArgs.Append("?ServerHardcore=").Append(this.EnableHardcore);           
            serverArgs.Append("?ServerPVE=").Append(!this.EnablePVP);

            serverArgs.Append("?ServerCrosshair=").Append(this.AllowCrosshair);
            serverArgs.Append("?ServerForceNoHud=").Append(!this.AllowHUD);
            serverArgs.Append("?AllowThirdPersonPlayer=").Append(this.AllowThirdPersonView);
            serverArgs.Append("?ShowMapPlayerLocation=").Append(this.AllowMapPlayerLocation);
            serverArgs.Append("?EnablePVPGamma=").Append(this.AllowPVPGamma);


            serverArgs.Append("?ServerPassword=").Append(this.ServerPassword);
            serverArgs.Append("?ServerAdminPassword=").Append(this.AdminPassword);
            serverArgs.Append("?MaxPlayers=").Append(this.MaxPlayers);
            serverArgs.Append("?DifficultyOffset=").Append(this.DifficultyOffset);
            serverArgs.Append("?MaxStructuresInRange=").Append(this.MaxStructuresVisible);
            
            serverArgs.Append("?SessionName=").Append('"').Append(this.ServerName).Append('"');
            serverArgs.Append("?QueryPort=").Append(this.ServerPort);
            if(!String.IsNullOrWhiteSpace(this.ServerIP))
            {
                serverArgs.Append("?MultiHome=").Append(this.ServerIP);
            }
            
            if(!String.IsNullOrWhiteSpace(this.SaveDirectory))
            {
                // TODO: This doesn't appear to work 
                serverArgs.Append("?\"AltSaveDirectoryName=").Append(this.SaveDirectory).Append('"');
            }

            if(!String.IsNullOrWhiteSpace(this.MOTD))
            {
                // TODO: This needs to go into the MessageOfTheDay INI file section
                serverArgs.Append("?MOTD=").Append('"').Append(this.MOTD).Append('"');
            }
#endif

            // Currently this setting does not seem to get picked up from the INI file.
            serverArgs.Append("?MaxPlayers=").Append(this.MaxPlayers);
            serverArgs.Append("?Port=").Append(this.ServerConnectionPort);

            serverArgs.Append("?listen");

            serverArgs.Append(this.AdditionalArgs);

            serverArgs.Append(' ');
            serverArgs.Append(Config.Default.ServerCommandLineStandardArgs);

            return serverArgs.ToString();
        }

        public object this[string propertyName]
        {
            get { return this.GetType().GetField(propertyName).GetValue(this); }
            set { this.GetType().GetField(propertyName).SetValue(this, value); this.IsDirty = true; }
        }

        private void GetDefaultDirectories()
        {
            if (String.IsNullOrWhiteSpace(SaveDirectory))
            {
                SaveDirectory = Path.IsPathRooted(Config.Default.ServersInstallDir) ? Path.Combine(Config.Default.ServersInstallDir, Config.Default.ServerConfigRelativePath)
                                                                                    : Path.Combine(Config.Default.DataDir, Config.Default.ServersInstallDir, Config.Default.ServerConfigRelativePath);
            }

            if (String.IsNullOrWhiteSpace(InstallDirectory))
            {
                InstallDirectory = Path.IsPathRooted(Config.Default.ServersInstallDir) ? Path.Combine(Config.Default.ServersInstallDir)
                                                                                       : Path.Combine(Config.Default.DataDir, Config.Default.ServersInstallDir);
            }
        }

        internal static ServerSettings GetDefault()
        {
            var settings = new ServerSettings();
            settings.GetDefaultDinoSpawns();
            settings.GetDefaultLevels();
            return settings;
        }
    }

    public class ServerSettingsViewModel : ViewModelBase
    {
        ServerSettings model;

        public ServerSettings Model
        {
            get { return model; }
        }

        public ServerSettingsViewModel(ServerSettings settings)
        {
            this.model = settings;
        }

        public bool IsDirty
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }
        public string ProfileName {
            get { return Get<string>(model); }
            set { Set(model, value); }
        }

        public string ServerMap
        {
            get { return Get<string>(model); }
            set { Set(model, value); }
        }

        public string ServerPassword
        {
            get { return Get<string>(model); }
            set { Set(model, value); }
        }

        public string AdminPassword
        {
            get { return Get<string>(model); }
            set { Set(model, value); }
        }

        public string ServerName
        {
            get { return Get<string>(model); }
            set { Set(model, value); }
        }

        public int ServerPort
        {
            get { return Get<int>(model); }
            set { Set(model, value); }
        }

        public int ServerConnectionPort
        {
            get { return Get<int>(model); }
            set { Set(model, value); }
        }

        public string ServerIP
        {
            get { return Get<string>(model); }
            set { Set(model, value); }
        }
        public string SaveDirectory
        {
            get { return Get<string>(model); }
            set { Set(model, value); }
        }
        public string InstallDirectory
        {
            get { return Get<string>(model); }
            set { Set(model, value); }
        }

        public string AdditionalArgs
        {
            get { return Get<string>(model); }
            set { Set(model, value); }
        }

        public string MOTD
        {
            get { return Get<string>(model); }
            set { Set(model, value); }
        }

        public int MOTDDuration
        {
            get { return Get<int>(model); }
            set { Set(model, value); }
        }

        public bool EnableKickIdlePlayers
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }

        }
        public float KickIdlePlayersPeriod
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }

        public float AutoSavePeriodMinutes
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }

        public int MaxPlayers
        {
            get { return Get<int>(model); }
            set { Set(model, value); }
        }

        public float DifficultyOffset
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }

        public float MaxStructuresVisible
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }

        public bool EnableGlobalVoiceChat
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }

        public bool EnableProximityChat
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }

        public bool EnableTributeDownloads
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }

        public bool EnableFlyerCarry
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }

        public bool EnableStructureDecay
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }

        public bool EnablePlayerLeaveNotifications
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }
        public bool EnablePlayerJoinedNotifications
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }
        public bool EnableHardcore
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }
        public bool EnablePVP
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }
        public bool AllowCrosshair
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }
        public bool AllowHUD
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }
        public bool AllowThirdPersonView
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }
        public bool AllowMapPlayerLocation
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }

        public bool AllowPVPGamma
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }
        }

        public float TamingSpeedMultiplier
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }

        public float HarvestAmountMultiplier
        {
            get { return Get<float>(model); }
            set { Set(model, value); }            
        }
        public float PlayerCharacterWaterDrainMultiplier
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }
        public float PlayerCharacterFoodDrainMultiplier
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }
        public float DinoCharacterFoodDrainMultiplier
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }
        public float PlayerCharacterStaminaDrainMultiplier
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }
        public float DinoCharacterStaminaDrainMultiplier
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }
        public float PlayerCharacterHealthRecoveryMultiplier
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }
        public float DinoCharacterHealthRecoveryMultiplier
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }
        public float DinoCountMultiplier
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }
        public float HarvestHealthMultiplier
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }
        public float PvEStructureDecayDestructionPeriod
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }

        public float PvEStructureDecayPeriodMultiplier
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }

        public float ResourcesRespawnPeriodMultiplier
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }

        public float DayCycleSpeedScale
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }
        public float NightTimeSpeedScale
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }
        public float DayTimeSpeedScale
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }
        public float DinoDamageMultiplier
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }
        public float TamedDinoDamageMultiplier
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }
        public float PlayerDamageMultiplier
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }
        public float StructureDamageMultiplier
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }
        public float PlayerResistanceMultiplier
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }
        public float DinoResistanceMultiplier
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }
        public float TamedDinoResistanceMultiplier
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }
        public float StructureResistanceMultiplier
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }
        public float XPMultiplier
        {
            get { return Get<float>(model); }
            set { Set(model, value); }
        }

        public bool EnableDinoSpawns
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }       
        }
        public List<DinoSpawn> DinoSpawns
        {
            get { return Get<List<DinoSpawn>>(model); }
            set { Set(model, value); }       
        }

        public bool EnableLevelProgressions
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }       
        }

        public List<Level> PlayerLevels
        {
            get { return Get<List<Level>>(model); }
            set { Set(model, value); }       
        }
        public List<Level> DinoLevels
        {
            get { return Get<List<Level>>(model); }
            set { Set(model, value); }
        }

        public ObservableCollection<string> Whitelist
        {
            get { return Get<ObservableCollection<string>>(model); }
            set { Set(model, value); }
        }
    }

    public class DifficultyOffsetValueConverter : IValueConverter
    {
        public const double MinValue = 50;
        public const double MaxValue = 300;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double scaledValue = System.Convert.ToDouble(value); ;
            var sliderValue = MinValue + (scaledValue * (MaxValue - MinValue));
            sliderValue = Math.Max(MinValue, Math.Min(MaxValue, sliderValue));
            return sliderValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var sliderValue = System.Convert.ToDouble(value);
            sliderValue = (double)sliderValue - (double)MinValue;
            var scaledValue = sliderValue / (double)(MaxValue - MinValue);
            scaledValue = Math.Max(0, Math.Min(1.0f, scaledValue));
            return scaledValue;
        }
    }
}
