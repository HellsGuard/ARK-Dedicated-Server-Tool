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
        public DinoSpawnList DinoSpawns = new DinoSpawnList();

        public bool EnableLevelProgressions = false;
        public LevelList PlayerLevels = new LevelList();
        public LevelList DinoLevels = new LevelList();

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

        public void ResetDinoSpawnsToDefault()
        {            
            this.DinoSpawns.Clear();
            this.DinoSpawns.InsertSpawns(GameData.DinoSpawns);
        }

        public enum LevelProgression
        {
            Player,
            Dino
        };

        public void ResetLevelProgressionToDefault(LevelProgression levelProgression)
        {
            LevelList list = GetLevelList(levelProgression);

            list.Clear();
            list.InsertLevels(GameData.LevelProgression);
        }

        public void ClearLevelProgression(LevelProgression levelProgression)
        {
            var list = GetLevelList(levelProgression);
            list.Clear();
            list.Add(new Level { LevelIndex = 0, XPRequired = 1, EngramPoints = 0 });
            list.UpdateTotals();
        }

        private LevelList GetLevelList(LevelProgression levelProgression)
        {
            LevelList list = null;
            switch (levelProgression)
            {
                case LevelProgression.Player:
                    list = this.PlayerLevels;
                    break;

                case LevelProgression.Dino:
                    list = this.DinoLevels;
                    break;

                default:
                    throw new ArgumentException("Invalid level progression type specified.");
            }
            return list;
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

                if (settings.DinoSpawns.Count == 0)
                {
                    settings.ResetDinoSpawnsToDefault();
                    settings.EnableDinoSpawns = false;
                }

                if (settings.PlayerLevels.Count == 0)
                {
                    settings.ResetLevelProgressionToDefault(LevelProgression.Player);
                    settings.ResetLevelProgressionToDefault(LevelProgression.Dino);
                    settings.EnableLevelProgressions = false;
                }

                //
                // Since these are not inserted the normal way, we force a recomputation here.
                //
                settings.PlayerLevels.UpdateTotals();
                settings.DinoLevels.UpdateTotals();
            }
            else
            {
                settings = LoadFromINIFiles(path);

                if (settings.DinoSpawns.Count == 0)
                {
                    settings.ResetDinoSpawnsToDefault();
                    settings.EnableDinoSpawns = false;
                }
                else
                {
                    settings.EnableDinoSpawns = true;
                }

                if (settings.PlayerLevels.Count == 0)
                {
                    settings.ResetLevelProgressionToDefault(LevelProgression.Player);
                    settings.ResetLevelProgressionToDefault(LevelProgression.Dino);
                    settings.EnableLevelProgressions = false;
                }
                else
                {
                    settings.EnableLevelProgressions = true;
                }
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
            settings.DinoSpawns = DinoSpawnList.FromINIValues(dinoSpawnWeightSources);
            
            // 
            // Levels
            //
            var levelRampOverrides = strings.Where(s => s.StartsWith("LevelExperienceRampOverrides=")).ToArray();
            var engramPointOverrides = strings.Where(s => s.StartsWith("OverridePlayerLevelEngramPoints="));
            if (levelRampOverrides.Length > 0)
            {
                settings.PlayerLevels = LevelList.FromINIValues(levelRampOverrides[0], engramPointOverrides);

                if(levelRampOverrides.Length > 1)
                {
                    settings.DinoLevels = LevelList.FromINIValues(levelRampOverrides[1], null);
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
                catch(IOException)
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
                values.AddRange(this.DinoSpawns.ToINIValues());
            }

            if(this.EnableLevelProgressions)            
            {
                //
                // These must be added in this order: Player, then Dinos, per the ARK INI file format.
                //
                values.Add(this.PlayerLevels.ToINIValueForXP());
                values.Add(this.DinoLevels.ToINIValueForXP());
                values.AddRange(this.PlayerLevels.ToINIValuesForEngramPoints());
            }

            if (this.EnableDinoSpawns || this.EnableLevelProgressions)
            {
                // WARNING: This will delete everything in this section before writing the new values.
                iniFile.IniWriteSection(IniFileSections.GameMode, values.ToArray(), IniFiles.Game);
            }            
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
            settings.ResetDinoSpawnsToDefault();
            settings.ResetLevelProgressionToDefault(LevelProgression.Player);
            settings.ResetLevelProgressionToDefault(LevelProgression.Dino);
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
        public DinoSpawnList DinoSpawns
        {
            get { return Get<DinoSpawnList > (model); }
            set { Set(model, value); }       
        }

        public bool EnableLevelProgressions
        {
            get { return Get<bool>(model); }
            set { Set(model, value); }       
        }

        public LevelList PlayerLevels
        {
            get { return Get<LevelList>(model); }
            set { Set(model, value); }       
        }
        public LevelList DinoLevels
        {
            get { return Get<LevelList>(model); }
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
